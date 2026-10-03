using System.Diagnostics.CodeAnalysis;
using ProjetoVN.Core.State;
using Yarn;

namespace ProjetoVN.Dialogue.Logic
{
    /// <summary>
    /// O armazenamento de variáveis do Yarn Spinner sobre o <see cref="StoryState"/> (D-18). Não guarda
    /// valor nenhum: cada leitura e gravação vai ao <see cref="StoryState"/>, então o roteiro, os portões
    /// e o save enxergam sempre a mesma coisa. O nome vai com o <c>$</c>, como o Yarn o entrega.
    /// </summary>
    public sealed class StoryStateVariables : IVariableStorage
    {
        public Program Program { get; set; }

        public ISmartVariableEvaluator SmartVariableEvaluator { get; set; }

        public bool TryGetValue<T>(string variableName, [NotNullWhen(true)] out T result)
        {
            if (TryGetStored(variableName, out object stored))
            {
                if (stored is T typed)
                {
                    result = typed;
                    return true;
                }

                result = default;
                return false;
            }

            if (IsSmartVariable(variableName)) return SmartVariableEvaluator.TryGetSmartVariable(variableName, out result);

            if (Program != null) return Program.TryGetInitialValue(variableName, out result);

            result = default;
            return false;
        }

        public void SetValue(string variableName, string stringValue) => StoryState.SetText(variableName, stringValue);

        public void SetValue(string variableName, float floatValue) => StoryState.SetNumber(variableName, floatValue);

        public void SetValue(string variableName, bool boolValue) => StoryState.SetBool(variableName, boolValue);

        public void Clear() => StoryState.ClearAll();

        public bool Contains(string variableName) => TryGetStored(variableName, out _);

        public VariableKind GetVariableKind(string name)
        {
            if (Contains(name)) return VariableKind.Stored;
            if (Program == null) return VariableKind.Unknown;

            return Program.GetVariableKind(name);
        }

        private bool IsSmartVariable(string variableName) =>
            Program != null && SmartVariableEvaluator != null && Program.GetVariableKind(variableName) == VariableKind.Smart;

        private static bool TryGetStored(string variableName, out object value)
        {
            if (StoryState.TryGetBool(variableName, out bool flag))
            {
                value = flag;
                return true;
            }

            if (StoryState.TryGetNumber(variableName, out float number))
            {
                value = number;
                return true;
            }

            if (StoryState.TryGetText(variableName, out string text))
            {
                value = text;
                return true;
            }

            value = null;
            return false;
        }
    }
}
