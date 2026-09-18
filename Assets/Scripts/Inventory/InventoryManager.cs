using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Inventory
{
    public sealed class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        private InventoryService _service;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[InventoryManager] Duplicata encontrada. Destruindo objeto.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _service = new InventoryService(new InventoryModel());
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
    }
}
