using Assets.Scripts.Core.Messaging;


namespace ProjetoVN.Inventory.Messages
{
    public readonly struct ItemUsedMessage : IMessage
    {
        public Item UsedItem { get; }

        public ItemUsedMessage(Item usedItem)
        {
            UsedItem = usedItem;
        }
    }
}
