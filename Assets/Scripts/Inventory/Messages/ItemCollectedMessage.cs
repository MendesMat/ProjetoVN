using Assets.Scripts.Core.Messaging;

namespace ProjetoVN.Inventory.Messages
{
    public readonly struct ItemCollectedMessage : IMessage
    {
        public Item CollectedItem { get; }

        public ItemCollectedMessage(Item collectedItem)
        {
            CollectedItem = collectedItem;
        }
    }
}
