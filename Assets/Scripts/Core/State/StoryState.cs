using System.Collections.Generic;

namespace ProjetoVN.Core.State
{
    public static class StoryState
    {
        public static IReadOnlyDictionary<string, bool> Bools => new Dictionary<string, bool>();
        public static IReadOnlyDictionary<string, float> Numbers => new Dictionary<string, float>();
        public static IReadOnlyDictionary<string, string> Texts => new Dictionary<string, string>();

        public static bool IsTrue(string name) => false;
        public static bool TryGetBool(string name, out bool value) { value = false; return false; }
        public static bool TryGetNumber(string name, out float value) { value = 0f; return false; }
        public static bool TryGetText(string name, out string value) { value = null; return false; }

        public static bool SetBool(string name, bool value) => false;
        public static bool SetNumber(string name, float value) => false;
        public static bool SetText(string name, string value) => false;

        public static void ReplaceAll(
            IEnumerable<KeyValuePair<string, bool>> bools,
            IEnumerable<KeyValuePair<string, float>> numbers,
            IEnumerable<KeyValuePair<string, string>> texts)
        {
        }

        public static void ClearAll()
        {
        }
    }
}
