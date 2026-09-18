using ProjetoVN.Core.Messaging;

namespace ProjetoVN.Inventory.Messages
{
    public readonly struct ItemUsedMessage : IMessage
    {
        public ItemDataSO UsedItem { get; }

        public ItemUsedMessage(ItemDataSO usedItem)
        {
            UsedItem = usedItem;
        }
    }
}
