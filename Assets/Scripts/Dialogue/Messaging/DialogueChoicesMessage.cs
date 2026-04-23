using System.Collections.Generic;
using Assets.Scripts.Core.Messaging;
using Assets.Scripts.Dialogue.Data;

namespace Assets.Scripts.Dialogue.Messaging
{
    public readonly struct DialogueChoicesMessage : IMessage
    {
        public readonly IReadOnlyList<DialogueChoice> Choices;

        public DialogueChoicesMessage(IReadOnlyList<DialogueChoice> choices)
        {
            Choices = choices;
        }
    }
}
