using Assets.Scripts.Core.Messaging;
using ProjetoVN.Inventory;

namespace ProjetoVN.Inventory.Messages
{
    public readonly struct CollectItemCommandMessage : IMessage
    {
        public Item ItemToCollect { get; }

        public CollectItemCommandMessage(Item itemToCollect)
        {
            ItemToCollect = itemToCollect;
        }
    }
}
