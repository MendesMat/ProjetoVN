using Assets.Scripts.Core.Messaging;
using ProjetoVN.Inventory.Messages;
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
                Debug.LogError($"[LockedActionBehaviour] 'requiredItem' não foi atribuído em '{gameObject.name}'.");
                return;
            }

            MessageBroker.Publish(new CheckItemRequestMessage(requiredItem.Id, OnCheckResult));
        }

        private void OnCheckResult(bool hasRequiredItem)
        {
            if (!hasRequiredItem)
            {
                OnLocked?.Invoke();
                return;
            }

            Unlock();
        }

        private void Unlock()
        {
            MessageBroker.Publish(new UseItemCommandMessage(requiredItem.Id));
            OnUnlocked?.Invoke();
        }
    }
}
