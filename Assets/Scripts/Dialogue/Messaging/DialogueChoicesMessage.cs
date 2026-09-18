using System.Collections.Generic;
using ProjetoVN.Core.Messaging;
using ProjetoVN.Dialogue.Data;

namespace ProjetoVN.Dialogue.Messaging
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
