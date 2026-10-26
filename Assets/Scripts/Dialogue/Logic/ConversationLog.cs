using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Dialogue.Logic
{
    /// <summary>
    /// O histórico da conversa em curso (D-30): entradas estruturadas, na ordem em que aconteceram. Decide quando o
    /// nome aparece: só na primeira fala de uma sequência do mesmo personagem. C# puro, só leitura para quem exibe;
    /// quem o possui o zera a cada conversa.
    /// </summary>
    public sealed class ConversationLog
    {
        private const string ItemReceivedPrefix = "Você recebeu: ";
        private const string ItemHandedOverPrefix = "Você entregou: ";

        private readonly List<ConversationEntry> _entries = new();

        public IReadOnlyList<ConversationEntry> Entries => _entries;

        public void AddSpokenLine(string speakerName, Color speakerColor, string text) =>
            AddSpeech(speakerName, speakerColor, text, isProtagonist: false);

        public void AddProtagonistLine(string speakerName, Color speakerColor, string text) =>
            AddSpeech(speakerName, speakerColor, text, isProtagonist: true);

        public void AddNarration(string text) => AddWithoutSpeaker(ConversationEntryKind.Narration, text);

        public void AddThought(string text, Color thoughtColor) =>
            _entries.Add(new ConversationEntry(ConversationEntryKind.Thought, null, Opaque(thoughtColor), text, isProtagonist: false, showsName: false));

        public void AddItemReceived(string itemName) => AddWithoutSpeaker(ConversationEntryKind.Item, ItemReceivedPrefix + itemName);

        public void AddItemHandedOver(string itemName) => AddWithoutSpeaker(ConversationEntryKind.Item, ItemHandedOverPrefix + itemName);

        public void Clear() => _entries.Clear();

        private void AddSpeech(string speakerName, Color speakerColor, string text, bool isProtagonist)
        {
            bool showsName = !ContinuesSequenceOf(speakerName, isProtagonist);

            _entries.Add(new ConversationEntry(ConversationEntryKind.Speech, speakerName, Opaque(speakerColor), text, isProtagonist, showsName));
        }

        private static Color Opaque(Color color) => new(color.r, color.g, color.b, 1f);

        private void AddWithoutSpeaker(ConversationEntryKind kind, string text) =>
            _entries.Add(new ConversationEntry(kind, null, Color.clear, text, isProtagonist: false, showsName: false));

        // O lado também conta: o jogador pode ter escolhido para o protagonista o nome de outro personagem (#26).
        private bool ContinuesSequenceOf(string speakerName, bool isProtagonist)
        {
            if (_entries.Count == 0) return false;

            ConversationEntry previous = _entries[^1];
            return previous.Kind == ConversationEntryKind.Speech
                   && previous.IsProtagonist == isProtagonist
                   && string.Equals(previous.SpeakerName, speakerName, StringComparison.Ordinal);
        }
    }
}
