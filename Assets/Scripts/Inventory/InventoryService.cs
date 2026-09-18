using System.Collections.Generic;
using ProjetoVN.Core.Messaging;
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

        public IReadOnlyList<ItemDataSO> Items => _model.Items;

        public bool HasItem(ItemDataSO item) => _model.Contains(item);

        public bool CollectItem(ItemDataSO item)
        {
            if (!_model.Add(item)) return false;

            MessageBroker.Publish(new ItemCollectedMessage(item));
            return true;
        }

        public bool TryUseItem(ItemDataSO item)
        {
            if (!_model.Remove(item)) return false;

            MessageBroker.Publish(new ItemUsedMessage(item));
            return true;
        }

        public void ReplaceAll(IEnumerable<ItemDataSO> items)
        {
            _model.ReplaceAll(items);
            MessageBroker.Publish(new InventoryReplacedMessage());
        }
    }
}
