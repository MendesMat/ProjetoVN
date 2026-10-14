using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Dialogue.Logic
{
    public sealed class ConversationLog
    {
        private readonly List<ConversationEntry> _entries = new();

        public IReadOnlyList<ConversationEntry> Entries => _entries;

        public void AddSpokenLine(string speakerName, Color speakerColor, string text) => AddPlaceholder();

        public void AddProtagonistLine(string speakerName, Color speakerColor, string text) => AddPlaceholder();

        public void AddNarration(string text) => AddPlaceholder();

        public void AddItemReceived(string itemName) => AddPlaceholder();

        public void AddItemHandedOver(string itemName) => AddPlaceholder();

        public void Clear() => _entries.Clear();

        private void AddPlaceholder() =>
            _entries.Add(new ConversationEntry(ConversationEntryKind.Narration, null, Color.clear, string.Empty, false, false));
    }
}
