using ProjetoVN.Core.Messaging;

namespace ProjetoVN.Inventory.Messages
{
    public readonly struct ItemCollectedMessage : IMessage
    {
        public ItemDataSO CollectedItem { get; }

        public ItemCollectedMessage(ItemDataSO collectedItem)
        {
            CollectedItem = collectedItem;
        }
    }
}
