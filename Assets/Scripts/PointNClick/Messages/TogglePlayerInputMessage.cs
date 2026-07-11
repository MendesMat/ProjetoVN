using Assets.Scripts.Core.Messaging;

namespace ProjetoVN.PointNClick.Messages
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
