using Assets.Scripts.Core.Messaging;

namespace Assets.Scripts.Core.Messaging.Messages
{
    public readonly struct TogglePlayerInputMessage : IMessage
    {
        public bool IsEnabled { get; }

        public TogglePlayerInputMessage(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }
}
