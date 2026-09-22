using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Core.State
{
    /// <summary>
    /// Conjunto global de flags de história ("falou com a gótica", "porta destrancada").
    /// Estático como <see cref="Messaging.MessageBroker"/> e PlayerInputGate: o dono é o jogo inteiro,
    /// e quem lê (Dialogue, GameFlow, PointNClick, UI) não precisa de referência serializada.
    /// Persistido por GameSaveManager através de GameState.storyFlagIds.
    /// </summary>
    public static class StoryFlags
    {
        private static readonly HashSet<string> _flags = new();

        public static IReadOnlyCollection<string> All => _flags;

        public static bool IsSet(string flag) => IsValid(flag) && _flags.Contains(flag);

        /// <returns><c>true</c> se a flag mudou de estado; <c>false</c> se já estava ligada ou é inválida.</returns>
        public static bool Set(string flag) => IsValid(flag) && _flags.Add(flag);

        /// <returns><c>true</c> se a flag mudou de estado; <c>false</c> se já estava desligada ou é inválida.</returns>
        public static bool Clear(string flag) => IsValid(flag) && _flags.Remove(flag);

        /// <summary>Substitui todas as flags de uma vez. Usado ao carregar um save.</summary>
        public static void ReplaceAll(IEnumerable<string> flags)
        {
            _flags.Clear();

            if (flags == null) return;

            foreach (string flag in flags)
            {
                if (IsValid(flag)) _flags.Add(flag);
            }
        }

        public static void ClearAll() => _flags.Clear();

        private static bool IsValid(string flag) => !string.IsNullOrWhiteSpace(flag);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart() => ClearAll();
    }
}
