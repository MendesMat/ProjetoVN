using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Dialogue.Logic
{
    /// <summary>
    /// O histórico da conversa em curso (D-30): cada entrada já vem formatada em texto rico do TMP, na ordem em que
    /// aconteceu. C# puro, só leitura para quem exibe; quem o possui o zera a cada conversa.
    /// </summary>
    public sealed class ConversationLog
    {
        private const string ItemReceivedPrefix = "Você recebeu: ";
        private const string ItemHandedOverPrefix = "Você entregou: ";

        private readonly List<string> _entries = new();

        public IReadOnlyList<string> Entries => _entries;

        public void AddSpokenLine(string speakerName, Color speakerColor, string text) =>
            _entries.Add($"<color=#{ColorUtility.ToHtmlStringRGB(speakerColor)}>{speakerName}</color>: {text}");

        public void AddNarration(string text) => _entries.Add(text);

        public void AddItemReceived(string itemName) => _entries.Add(ItemReceivedPrefix + itemName);

        public void AddItemHandedOver(string itemName) => _entries.Add(ItemHandedOverPrefix + itemName);

        public void Clear() => _entries.Clear();
    }
}
