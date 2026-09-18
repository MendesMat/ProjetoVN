using ProjetoVN.Core.Messaging;

namespace ProjetoVN.Dialogue.Messaging
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
