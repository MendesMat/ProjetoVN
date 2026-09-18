using UnityEngine;

namespace ProjetoVN.Inventory
{
    public sealed class CollectableItemBehaviour : MonoBehaviour
    {
        [SerializeField] private ItemDataSO itemData;

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

            InventoryManager.Instance.Collect(itemData);
            gameObject.SetActive(false);
        }
    }
}
