using System.Collections.Generic;
using System.Linq;

namespace ProjetoVN.Inventory
{
    public sealed class InventoryModel
    {
        private readonly List<Item> _items = new();
        public IReadOnlyList<Item> Items => _items.AsReadOnly();


        public void AddItem(Item item) => _items.Add(item);

        public bool RemoveItem(string itemId)
        {
            Item itemToRemove = FindById(itemId);
            if (itemToRemove == null) return false;

            _items.Remove(itemToRemove);
            return true;
        }

        public bool HasItem(string itemId) => FindById(itemId) != null;

        public Item FindById(string itemId) => _items.FirstOrDefault(i => i.Id == itemId);
    }
}
