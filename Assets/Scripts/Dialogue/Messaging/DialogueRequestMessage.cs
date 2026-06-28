using Assets.Scripts.Core.Messaging;
using Assets.Scripts.Dialogue.Data;

namespace Assets.Scripts.Dialogue.Messaging
{
    public readonly struct DialogueRequestMessage : IMessage
    {
        public DialogueData Data { get; }

        public DialogueRequestMessage(DialogueData data)
        {
            Data = data;
        }
    }
}
