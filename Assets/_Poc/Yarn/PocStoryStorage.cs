using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Yarn.Unity;

namespace ProjetoVN.PocYarn
{
    public sealed class PocStoryStorage : VariableStorageBehaviour
    {
        private readonly StoryVariables _variables = new();

        public StoryVariables Variables => _variables;

        public override bool TryGetValue<T>(string variableName, [NotNullWhen(true)] out T result)
            => _variables.TryGetValue(variableName, out result);

        public override void SetValue(string variableName, string stringValue) => _variables.SetValue(variableName, stringValue);

        public override void SetValue(string variableName, float floatValue) => _variables.SetValue(variableName, floatValue);

        public override void SetValue(string variableName, bool boolValue) => _variables.SetValue(variableName, boolValue);

        public override void Clear() => _variables.Clear();

        public override bool Contains(string variableName) => _variables.Contains(variableName);

        public override void SetAllVariables(
            Dictionary<string, float> floats, Dictionary<string, string> strings, Dictionary<string, bool> bools, bool clear = true)
            => _variables.SetAllVariables(floats, strings, bools, clear);

        public override (Dictionary<string, float> FloatVariables, Dictionary<string, string> StringVariables, Dictionary<string, bool> BoolVariables) GetAllVariables()
        {
            var (floats, strings, bools) = _variables.GetAllVariables();
            return (floats, strings, bools);
        }
    }
}
