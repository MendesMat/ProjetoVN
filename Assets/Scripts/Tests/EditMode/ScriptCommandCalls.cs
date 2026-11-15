using System.Collections.Generic;
using System.Linq;
using Yarn;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>Uma instrução <c>&lt;&lt;comando ...&gt;&gt;</c> de um nó do roteiro.</summary>
    public readonly struct ScriptCommandCall
    {
        public string NodeName { get; }
        public string Name { get; }

        /// <summary>As palavras depois do nome do comando. Uma expressão <c>{$x}</c> é uma, guardada como <c>{0}</c>.</summary>
        public IReadOnlyList<string> Words { get; }

        public int WordCount => Words.Count;

        public string Text { get; }

        public ScriptCommandCall(string nodeName, string name, IReadOnlyList<string> words, string text)
        {
            NodeName = nodeName;
            Name = name;
            Words = words;
            Text = text;
        }

        public override string ToString() => $"nó '{NodeName}': <<{Text}>>";
    }

    /// <summary>
    /// Os comandos de um roteiro compilado. Lê o programa, e não o texto do <c>.yarn</c>, porque o programa já
    /// descarta os comentários e transforma <c>&lt;&lt;jump&gt;&gt;</c>, <c>&lt;&lt;set&gt;&gt;</c> e afins em outras
    /// instruções, de modo que sobram só os comandos que o jogo executa.
    /// </summary>
    public sealed class ScriptCommandCalls
    {
        private readonly List<ScriptCommandCall> _calls = new();

        public IReadOnlyList<ScriptCommandCall> All => _calls;

        public static ScriptCommandCalls In(Program program)
        {
            var calls = new ScriptCommandCalls();
            foreach (Node node in program.Nodes.Values)
            {
                foreach (Instruction instruction in node.Instructions)
                {
                    if (instruction.InstructionTypeCase != Instruction.InstructionTypeOneofCase.RunCommand) continue;

                    calls.Read(node.Name, instruction.RunCommand.CommandText);
                }
            }

            return calls;
        }

        public IReadOnlyList<string> Problems(IEnumerable<ScriptCommandSignature> signatures)
        {
            Dictionary<string, ScriptCommandSignature> byName = signatures.ToDictionary(signature => signature.Name);
            return _calls.Select(call => ProblemOf(call, byName)).Where(problem => problem != null).ToList();
        }

        private void Read(string nodeName, string commandText)
        {
            string[] words = Yarn.Unity.DialogueRunner.SplitCommandText(commandText).ToArray();
            if (words.Length == 0) return;

            _calls.Add(new ScriptCommandCall(nodeName, words[0], words.Skip(1).ToArray(), commandText));
        }

        private static string ProblemOf(ScriptCommandCall call, IReadOnlyDictionary<string, ScriptCommandSignature> byName)
        {
            if (!byName.TryGetValue(call.Name, out ScriptCommandSignature signature))
                return $"{call}: o comando '{call.Name}' não existe";

            if (!signature.Accepts(call.WordCount))
                return $"{call}: {call.Name} pede {signature.Describe()}, recebeu {call.WordCount}";

            string typeProblem = signature.TypeProblemOf(call.Words);
            return typeProblem == null ? null : $"{call}: {call.Name} {typeProblem}";
        }
    }
}
