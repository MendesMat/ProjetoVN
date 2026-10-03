using ProjetoVN.Core.Messaging;
using ProjetoVN.Dialogue.Messaging;

namespace ProjetoVN.Dialogue.Logic
{
    /// <summary>
    /// Garante um <see cref="DialogueStartedMessage"/> e um <see cref="DialogueEndedMessage"/> por conversa,
    /// e nunca um Ended sem Started: o <c>GameFlow</c> troca de modo por essas duas mensagens.
    /// </summary>
    public sealed class ConversationNotifier
    {
        public bool IsOpen { get; private set; }

        public void Begin()
        {
            if (IsOpen) return;

            IsOpen = true;
            MessageBroker.Publish(new DialogueStartedMessage());
        }

        public void End()
        {
            if (!IsOpen) return;

            IsOpen = false;
            MessageBroker.Publish(new DialogueEndedMessage());
        }
    }
}
