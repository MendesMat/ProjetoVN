using System.Collections.Generic;

namespace ProjetoVN.Inventory
{
    public sealed class InventoryModel
    {
        private readonly List<ItemDataSO> _items = new();

        public IReadOnlyList<ItemDataSO> Items => _items;

        public bool Add(ItemDataSO item)
        {
            if (item == null || Contains(item)) return false;

            _items.Add(item);
            return true;
        }

        public bool Remove(ItemDataSO item) => item != null && _items.Remove(item);

        public bool Contains(ItemDataSO item) => item != null && _items.Contains(item);
    }
}
