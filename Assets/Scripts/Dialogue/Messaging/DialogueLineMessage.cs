using Assets.Scripts.Core.Messaging;

namespace Assets.Scripts.Dialogue.Messaging
{
    public readonly struct DialogueLineMessage : IMessage
    {
        public readonly string CharacterId;
        public readonly string Text;
        public readonly int LineIndex;

        public DialogueLineMessage(string characterId, string text, int lineIndex)
        {
            CharacterId = characterId;
            Text = text;
            LineIndex = lineIndex;
        }
    }
}
