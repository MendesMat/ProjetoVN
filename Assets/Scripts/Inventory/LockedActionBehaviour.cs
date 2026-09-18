using UnityEngine;
using UnityEngine.Events;

namespace ProjetoVN.Inventory
{
    public sealed class LockedActionBehaviour : MonoBehaviour
    {
        [SerializeField] private ItemDataSO requiredItem;

        [Header("Events")]
        public UnityEvent OnUnlocked;
        public UnityEvent OnLocked;

        public void Interact()
        {
            if (requiredItem == null)
            {
                Debug.LogError("[LockedActionBehaviour] 'requiredItem' não foi atribuído.", this);
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[LockedActionBehaviour] Não há InventoryManager na cena. Interação ignorada.", this);
                return;
            }

            if (InventoryManager.Instance.TryUse(requiredItem))
                OnUnlocked?.Invoke();
            else
                OnLocked?.Invoke();
        }
    }
}
