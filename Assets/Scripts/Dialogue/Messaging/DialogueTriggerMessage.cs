using ProjetoVN.Core.Messaging;

namespace ProjetoVN.Dialogue.Messaging
{
    public readonly struct DialogueTriggerMessage : IMessage
    {
        public readonly string TriggerType;
        public readonly string Parameter;

        public DialogueTriggerMessage(string triggerType, string parameter)
        {
            TriggerType = triggerType;
            Parameter = parameter;
        }
    }
}
