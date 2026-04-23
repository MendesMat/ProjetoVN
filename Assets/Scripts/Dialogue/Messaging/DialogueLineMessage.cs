using Assets.Scripts.Core.Messaging;

namespace Assets.Scripts.Dialogue.Messaging
{
    public readonly struct DialogueLineMessage : IMessage
    {
        public readonly string SpeakerName;
        public readonly string Text;

        public DialogueLineMessage(string speakerName, string text)
        {
            SpeakerName = speakerName;
            Text = text;
        }
    }
}
