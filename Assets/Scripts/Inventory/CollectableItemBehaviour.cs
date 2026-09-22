using UnityEngine;

namespace ProjetoVN.Inventory
{
    public sealed class CollectableItemBehaviour : MonoBehaviour
    {
        [SerializeField] private ItemDataSO itemData;
        [SerializeField] private string persistentId;

        private void Reset()
        {
            persistentId = System.Guid.NewGuid().ToString();
        }

        [ContextMenu("Regenerate Persistent Id")]
        private void RegeneratePersistentId()
        {
            persistentId = System.Guid.NewGuid().ToString();
        }

        private void Start()
        {
            if (InventoryManager.Instance != null && InventoryManager.Instance.IsWorldObjectConsumed(persistentId))
                gameObject.SetActive(false);
        }

        public void Collect()
        {
            if (itemData == null)
            {
                Debug.LogError("[CollectableItemBehaviour] 'itemData' não foi atribuído.", this);
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[CollectableItemBehaviour] Não há InventoryManager na cena. Coleta ignorada.", this);
                return;
            }

            if (!InventoryManager.Instance.Collect(itemData))
            {
                Debug.LogWarning("[CollectableItemBehaviour] O item já estava no inventário. O objeto continua no mundo.", this);
                return;
            }

            InventoryManager.Instance.MarkWorldObjectConsumed(persistentId);
            gameObject.SetActive(false);
        }
    }
}
