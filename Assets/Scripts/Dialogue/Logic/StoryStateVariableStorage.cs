using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ProjetoVN.Core.State;
using Yarn.Unity;

namespace ProjetoVN.Dialogue.Logic
{
    /// <summary>
    /// O armazenamento de variáveis que o <c>DialogueRunner</c> do <c>Managers.prefab</c> usa. Só delega ao
    /// <see cref="StoryStateVariables"/>, que lê e grava no <see cref="StoryState"/>. Sem esta ligação, o runner
    /// criaria em silêncio um armazenamento próprio, e o roteiro passaria a ter um estado paralelo (D-18).
    /// </summary>
    public sealed class StoryStateVariableStorage : VariableStorageBehaviour
    {
        private readonly StoryStateVariables _variables = new();

        public override bool TryGetValue<T>(string variableName, [NotNullWhen(true)] out T result)
        {
            // O runner e a VM atribuem Program e SmartVariableEvaluator a este componente, não ao adaptador.
            _variables.Program = Program;
            _variables.SmartVariableEvaluator = SmartVariableEvaluator;
            return _variables.TryGetValue(variableName, out result);
        }

        public override void SetValue(string variableName, string stringValue) => _variables.SetValue(variableName, stringValue);

        public override void SetValue(string variableName, float floatValue) => _variables.SetValue(variableName, floatValue);

        public override void SetValue(string variableName, bool boolValue) => _variables.SetValue(variableName, boolValue);

        public override void Clear() => _variables.Clear();

        public override bool Contains(string variableName) => _variables.Contains(variableName);

        public override void SetAllVariables(
            Dictionary<string, float> floats, Dictionary<string, string> strings, Dictionary<string, bool> bools, bool clear = true)
        {
            if (clear) StoryState.ClearAll();

            foreach (KeyValuePair<string, float> entry in floats) StoryState.SetNumber(entry.Key, entry.Value);
            foreach (KeyValuePair<string, string> entry in strings) StoryState.SetText(entry.Key, entry.Value);
            foreach (KeyValuePair<string, bool> entry in bools) StoryState.SetBool(entry.Key, entry.Value);
        }

        public override (Dictionary<string, float> FloatVariables, Dictionary<string, string> StringVariables, Dictionary<string, bool> BoolVariables) GetAllVariables()
        {
            return (new Dictionary<string, float>(StoryState.Numbers),
                    new Dictionary<string, string>(StoryState.Texts),
                    new Dictionary<string, bool>(StoryState.Bools));
        }
    }
}
