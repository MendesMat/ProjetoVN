using System.Collections.Generic;
using System.Linq;
using ProjetoVN.Core.State;

namespace ProjetoVN.GameFlow.Persistence
{
    /// <summary>
    /// Copia o <see cref="StoryState"/> de e para o <see cref="GameState"/>. Mora separada do
    /// <c>GameSaveManager</c> porque ele é um MonoBehaviour que lê arquivo, e a cópia é C# puro testável.
    /// </summary>
    public static class StoryStatePersistence
    {
        public static void Capture(GameState target)
        {
            target.storyBools = StoryState.Bools
                .Select(entry => new StoryBoolEntry { name = entry.Key, value = entry.Value }).ToList();
            target.storyNumbers = StoryState.Numbers
                .Select(entry => new StoryNumberEntry { name = entry.Key, value = entry.Value }).ToList();
            target.storyTexts = StoryState.Texts
                .Select(entry => new StoryTextEntry { name = entry.Key, value = entry.Value }).ToList();
        }

        public static void Restore(GameState source)
        {
            StoryState.ReplaceAll(
                source.storyBools?.Select(entry => new KeyValuePair<string, bool>(entry.name, entry.value)),
                source.storyNumbers?.Select(entry => new KeyValuePair<string, float>(entry.name, entry.value)),
                source.storyTexts?.Select(entry => new KeyValuePair<string, string>(entry.name, entry.value)));
        }
    }
}
