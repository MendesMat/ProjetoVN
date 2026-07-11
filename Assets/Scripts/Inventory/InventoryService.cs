using Assets.Scripts.Core.Messaging;
using ProjetoVN.Inventory.Messages;

namespace ProjetoVN.Inventory
{
    public sealed class InventoryService
    {
        private readonly InventoryModel _model;

        public InventoryService(InventoryModel model)
        {
            _model = model;
        }

        public bool HasItem(string itemId) => _model.HasItem(itemId);

        public void CollectItem(Item item)
        {
            _model.AddItem(item);
            MessageBroker.Publish(new ItemCollectedMessage(item));
        }

        public bool TryUseItem(string itemId)
        {
            if (!_model.HasItem(itemId)) return false;

            Item usedItem = _model.FindById(itemId);
            _model.RemoveItem(itemId);
            MessageBroker.Publish(new ItemUsedMessage(usedItem));
            return true;
        }
    }
}
