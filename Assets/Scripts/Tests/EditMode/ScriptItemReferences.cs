using System;
using System.Collections.Generic;
using Yarn;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>Um ponto do roteiro que cita um item: <c>dar_item</c>, <c>remover_item</c> ou <c>tem_item</c>.</summary>
    public readonly struct ScriptItemReference
    {
        public string NodeName { get; }
        public string Source { get; }

        /// <summary>O id do item; em uma referência sem id literal, o texto que o roteiro traz no lugar.</summary>
        public string Argument { get; }

        public ScriptItemReference(string nodeName, string source, string argument)
        {
            NodeName = nodeName;
            Source = source;
            Argument = argument;
        }

        public override string ToString() => $"{NodeName}: {Source} '{Argument}'";
    }

    /// <summary>
    /// As referências a item de um roteiro compilado. Lê o programa, e não o texto do <c>.yarn</c>, porque o
    /// programa já descarta os comentários e diz em que nó cada instrução está.
    /// </summary>
    public sealed class ScriptItemReferences
    {
        private const string HasItemFunction = "tem_item";
        private static readonly string[] ItemCommands = { "dar_item", "remover_item" };
        private static readonly char[] Whitespace = { ' ', '\t' };

        private readonly List<ScriptItemReference> _literal = new();
        private readonly List<ScriptItemReference> _withoutLiteralId = new();

        public IReadOnlyList<ScriptItemReference> Literal => _literal;

        /// <summary>Id vindo de variável ou expressão, ausente, ou seguido de mais um parâmetro.</summary>
        public IReadOnlyList<ScriptItemReference> WithoutLiteralId => _withoutLiteralId;

        public static ScriptItemReferences In(Program program)
        {
            var references = new ScriptItemReferences();
            foreach (Node node in program.Nodes.Values)
            {
                for (int index = 0; index < node.Instructions.Count; index++)
                    references.ReadInstruction(node, index);
            }

            return references;
        }

        private void ReadInstruction(Node node, int index)
        {
            Instruction instruction = node.Instructions[index];
            if (instruction.InstructionTypeCase == Instruction.InstructionTypeOneofCase.RunCommand)
                ReadCommand(node.Name, instruction.RunCommand.CommandText, instruction.RunCommand.SubstitutionCount);

            if (instruction.InstructionTypeCase == Instruction.InstructionTypeOneofCase.CallFunc
                && instruction.CallFunc.FunctionName == HasItemFunction)
                ReadHasItemCall(node, index);
        }

        private void ReadCommand(string nodeName, string commandText, int substitutionCount)
        {
            string[] words = commandText.Split(Whitespace, 2, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0 || Array.IndexOf(ItemCommands, words[0]) < 0) return;

            string argument = words.Length > 1 ? words[1].Trim() : string.Empty;
            bool isOneLiteralId = substitutionCount == 0
                                  && argument.Length > 0
                                  && argument.IndexOfAny(Whitespace) < 0;
            List<ScriptItemReference> target = isOneLiteralId ? _literal : _withoutLiteralId;
            target.Add(new ScriptItemReference(nodeName, words[0], argument.Trim('"')));
        }

        // O compilador empilha o argumento, depois a quantidade de argumentos, e só então chama a função.
        // Um id literal é um pushString duas instruções antes; qualquer outra coisa ali é variável ou expressão.
        private void ReadHasItemCall(Node node, int callIndex)
        {
            Instruction argument = callIndex >= 2 ? node.Instructions[callIndex - 2] : null;
            if (argument != null && argument.InstructionTypeCase == Instruction.InstructionTypeOneofCase.PushString)
            {
                _literal.Add(new ScriptItemReference(node.Name, HasItemFunction, argument.PushString.Value));
                return;
            }

            _withoutLiteralId.Add(new ScriptItemReference(node.Name, HasItemFunction, "(expressão)"));
        }
    }
}
