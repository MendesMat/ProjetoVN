using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Inventory
{
    public sealed class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        private InventoryService _service;
        private readonly HashSet<string> _consumedWorldObjectIds = new();

        private void Awake()
        {
            Instance = this;
            _service = new InventoryService(new InventoryModel());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public IReadOnlyList<ItemDataSO> Items => _service.Items;

        public bool HasItem(ItemDataSO item) => _service.HasItem(item);

        public bool Collect(ItemDataSO item)
        {
            if (item == null)
            {
                Debug.LogError("[InventoryManager] Collect recebeu um ItemDataSO nulo.", this);
                return false;
            }

            return _service.CollectItem(item);
        }

        public bool TryUse(ItemDataSO item)
        {
            if (item == null)
            {
                Debug.LogError("[InventoryManager] TryUse recebeu um ItemDataSO nulo.", this);
                return false;
            }

            return _service.TryUseItem(item);
        }

        public void ReplaceAll(IEnumerable<ItemDataSO> items) => _service.ReplaceAll(items);

        public bool IsWorldObjectConsumed(string persistentId) => _consumedWorldObjectIds.Contains(persistentId);

        public void MarkWorldObjectConsumed(string persistentId) => _consumedWorldObjectIds.Add(persistentId);

        public IReadOnlyCollection<string> ConsumedWorldObjectIds => _consumedWorldObjectIds;

        public void ReplaceConsumedWorldObjectIds(IEnumerable<string> ids)
        {
            _consumedWorldObjectIds.Clear();
            foreach (string id in ids)
                _consumedWorldObjectIds.Add(id);
        }
    }
}
