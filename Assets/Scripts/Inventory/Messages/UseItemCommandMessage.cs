using Assets.Scripts.Core.Messaging;

namespace ProjetoVN.Inventory.Messages
{
    public readonly struct UseItemCommandMessage : IMessage
    {
        public string ItemId { get; }

        public UseItemCommandMessage(string itemId)
        {
            ItemId = itemId;
        }
    }
}
