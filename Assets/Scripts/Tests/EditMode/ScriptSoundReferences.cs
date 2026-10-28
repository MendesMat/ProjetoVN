using System;
using System.Collections.Generic;
using Yarn;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>Um ponto do roteiro que pede um som: <c>tocar_musica</c>, <c>tocar_efeito</c>, <c>tocar_ambiente</c> ou <c>parar_ambiente</c>.</summary>
    public readonly struct ScriptSoundReference
    {
        public string NodeName { get; }
        public string Command { get; }

        /// <summary>O id do som; em uma referência sem id literal, o texto que o roteiro traz no lugar.</summary>
        public string Argument { get; }

        public ScriptSoundReference(string nodeName, string command, string argument)
        {
            NodeName = nodeName;
            Command = command;
            Argument = argument;
        }

        public override string ToString() => $"{NodeName}: {Command} '{Argument}'";
    }

    /// <summary>
    /// As referências a som de um roteiro compilado. Lê o programa, e não o texto do <c>.yarn</c>, porque o
    /// programa já descarta os comentários e diz em que nó cada instrução está.
    /// </summary>
    public sealed class ScriptSoundReferences
    {
        private const string StopAmbienceCommand = "parar_ambiente";
        private static readonly string[] CommandsWithAnId = { "tocar_musica", "tocar_efeito", "tocar_ambiente" };
        private static readonly char[] Whitespace = { ' ', '\t' };

        private readonly List<ScriptSoundReference> _literal = new();
        private readonly List<ScriptSoundReference> _withoutLiteralId = new();

        public IReadOnlyList<ScriptSoundReference> Literal => _literal;

        /// <summary>Id vindo de variável, ausente ou seguido de mais um parâmetro; e <c>parar_ambiente</c> com parâmetro.</summary>
        public IReadOnlyList<ScriptSoundReference> WithoutLiteralId => _withoutLiteralId;

        public static ScriptSoundReferences In(Program program)
        {
            var references = new ScriptSoundReferences();
            foreach (Node node in program.Nodes.Values)
            {
                foreach (Instruction instruction in node.Instructions)
                {
                    if (instruction.InstructionTypeCase != Instruction.InstructionTypeOneofCase.RunCommand) continue;

                    references.ReadCommand(node.Name, instruction.RunCommand.CommandText, instruction.RunCommand.SubstitutionCount);
                }
            }

            return references;
        }

        private void ReadCommand(string nodeName, string commandText, int substitutionCount)
        {
            string[] words = commandText.Split(Whitespace, 2, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return;

            string command = words[0];
            string argument = words.Length > 1 ? words[1].Trim() : string.Empty;
            var reference = new ScriptSoundReference(nodeName, command, argument.Trim('"'));

            if (command == StopAmbienceCommand)
            {
                if (argument.Length > 0) _withoutLiteralId.Add(reference);
                return;
            }

            if (Array.IndexOf(CommandsWithAnId, command) < 0) return;

            bool isOneLiteralId = substitutionCount == 0
                                  && argument.Length > 0
                                  && argument.IndexOfAny(Whitespace) < 0;
            (isOneLiteralId ? _literal : _withoutLiteralId).Add(reference);
        }
    }
}
