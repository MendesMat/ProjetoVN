using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Core.State
{
    /// <summary>
    /// Estado global da história: booleanos, números e textos por nome ("falou com a Luna",
    /// "afinidade com a Luna", "nome do protagonista"). Estático como <see cref="Messaging.MessageBroker"/>
    /// e PlayerInputGate: o dono é o jogo inteiro, e quem lê (roteiro, portões, save) não precisa de
    /// referência serializada. Os nomes seguem <see cref="StoryVariableName"/>. Um nome mora em um só tipo.
    /// </summary>
    public static class StoryState
    {
        private static readonly Dictionary<string, bool> _bools = new();
        private static readonly Dictionary<string, float> _numbers = new();
        private static readonly Dictionary<string, string> _texts = new();

        public static IReadOnlyDictionary<string, bool> Bools => _bools;
        public static IReadOnlyDictionary<string, float> Numbers => _numbers;
        public static IReadOnlyDictionary<string, string> Texts => _texts;

        public static bool IsTrue(string name) => TryGetBool(name, out bool value) && value;

        public static bool TryGetBool(string name, out bool value) => _bools.TryGetValue(name ?? "", out value);

        public static bool TryGetNumber(string name, out float value) => _numbers.TryGetValue(name ?? "", out value);

        public static bool TryGetText(string name, out string value) => _texts.TryGetValue(name ?? "", out value);

        /// <returns><c>true</c> se gravou; <c>false</c> se o nome é inválido.</returns>
        public static bool SetBool(string name, bool value) => Write(name, _bools, value);

        /// <returns><c>true</c> se gravou; <c>false</c> se o nome é inválido.</returns>
        public static bool SetNumber(string name, float value) => Write(name, _numbers, value);

        /// <returns><c>true</c> se gravou; <c>false</c> se o nome é inválido. Texto <c>null</c> vira <c>""</c>.</returns>
        public static bool SetText(string name, string value) => Write(name, _texts, value ?? "");

        /// <summary>Substitui tudo de uma vez. Usado ao carregar um save. Cada argumento aceita <c>null</c>.</summary>
        public static void ReplaceAll(
            IEnumerable<KeyValuePair<string, bool>> bools,
            IEnumerable<KeyValuePair<string, float>> numbers,
            IEnumerable<KeyValuePair<string, string>> texts)
        {
            ClearAll();

            WriteEach(bools, _bools);
            WriteEach(numbers, _numbers);
            WriteEach(texts, _texts);
        }

        public static void ClearAll()
        {
            _bools.Clear();
            _numbers.Clear();
            _texts.Clear();
        }

        private static void WriteEach<T>(IEnumerable<KeyValuePair<string, T>> entries, Dictionary<string, T> target)
        {
            if (entries == null) return;

            foreach (KeyValuePair<string, T> entry in entries)
                Write(entry.Key, target, entry.Value);
        }

        private static bool Write<T>(string name, Dictionary<string, T> target, T value)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (!StoryVariableName.IsValid(name))
            {
                Debug.LogWarning($"[StoryState] Nome de variável inválido: '{name}'. O nome precisa começar com '$'. Valor ignorado.");
                return false;
            }

            RemoveFromAll(name);
            target[name] = value;
            return true;
        }

        private static void RemoveFromAll(string name)
        {
            _bools.Remove(name);
            _numbers.Remove(name);
            _texts.Remove(name);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart() => ClearAll();
    }
}
