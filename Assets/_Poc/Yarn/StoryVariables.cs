using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Yarn;

namespace ProjetoVN.PocYarn
{
    public sealed class StoryVariables : IVariableStorage
    {
        private const string NamePrefix = "$";

        private readonly Dictionary<string, object> _values = new();

        public Program Program { get; set; }

        public ISmartVariableEvaluator SmartVariableEvaluator { get; set; }

        public bool TryGetValue<T>(string variableName, [NotNullWhen(true)] out T result)
        {
            result = default;
            if (!_values.TryGetValue(variableName, out object stored)) return false;
            if (stored is not T typed) return false;

            result = typed;
            return true;
        }

        public void SetValue(string variableName, string stringValue) => Store(variableName, stringValue);

        public void SetValue(string variableName, float floatValue) => Store(variableName, floatValue);

        public void SetValue(string variableName, bool boolValue) => Store(variableName, boolValue);

        public void Clear() => _values.Clear();

        public bool Contains(string variableName) => variableName != null && _values.ContainsKey(variableName);

        public (Dictionary<string, float> Floats, Dictionary<string, string> Strings, Dictionary<string, bool> Bools) GetAllVariables()
        {
            var floats = new Dictionary<string, float>();
            var strings = new Dictionary<string, string>();
            var bools = new Dictionary<string, bool>();

            foreach (KeyValuePair<string, object> entry in _values)
            {
                switch (entry.Value)
                {
                    case float number: floats[entry.Key] = number; break;
                    case string text: strings[entry.Key] = text; break;
                    case bool flag: bools[entry.Key] = flag; break;
                }
            }

            return (floats, strings, bools);
        }

        public void SetAllVariables(Dictionary<string, float> floats, Dictionary<string, string> strings, Dictionary<string, bool> bools, bool clear = true)
        {
            if (clear) Clear();

            foreach (KeyValuePair<string, float> entry in floats) SetValue(entry.Key, entry.Value);
            foreach (KeyValuePair<string, string> entry in strings) SetValue(entry.Key, entry.Value);
            foreach (KeyValuePair<string, bool> entry in bools) SetValue(entry.Key, entry.Value);
        }

        public VariableKind GetVariableKind(string name)
        {
            if (Contains(name)) return VariableKind.Stored;
            if (Program == null) return VariableKind.Unknown;

            return Program.GetVariableKind(name);
        }

        private void Store(string variableName, object value)
        {
            if (!IsValidName(variableName))
            {
                Debug.LogWarning($"[StoryVariables] Nome de variável inválido: '{variableName}'. O nome precisa começar com '{NamePrefix}'. Valor ignorado.");
                return;
            }

            _values[variableName] = value;
        }

        private static bool IsValidName(string variableName)
            => !string.IsNullOrWhiteSpace(variableName) && variableName.StartsWith(NamePrefix);
    }
}
