using UnityEngine;

namespace ProjetoVN.Dialogue.Logic
{
    public enum ConversationEntryKind
    {
        Speech,
        Narration,
        Thought,
        Item,
    }

    /// <summary>Uma linha do histórico da conversa (D-30), já com o que o balão precisa para ser desenhado.</summary>
    public readonly struct ConversationEntry
    {
        public ConversationEntry(
            ConversationEntryKind kind, string speakerName, Color speakerColor, string text, bool isProtagonist, bool showsName)
        {
            Kind = kind;
            SpeakerName = speakerName;
            SpeakerColor = speakerColor;
            Text = text;
            IsProtagonist = isProtagonist;
            ShowsName = showsName;
        }

        public ConversationEntryKind Kind { get; }
        public string SpeakerName { get; }
        public Color SpeakerColor { get; }
        public string Text { get; }
        public bool IsProtagonist { get; }
        public bool ShowsName { get; }
    }
}
