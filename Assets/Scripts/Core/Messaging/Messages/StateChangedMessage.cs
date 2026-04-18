using Assets.Scripts.Core.StateMachine;

namespace Assets.Scripts.Core.Messaging.Messages
{
    public readonly struct StateChangedMessage : IMessage
    {
        public BaseState Previous { get; }
        public BaseState Next { get; }

        internal StateChangedMessage(BaseState previous, BaseState next)
        {
            Previous = previous;
            Next = next;
        }
    }
}
