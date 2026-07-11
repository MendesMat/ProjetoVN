using Assets.Scripts.Core.Messaging;
using ProjetoVN.Inventory.Messages;
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
                Debug.LogError($"[CollectableItemBehaviour] 'itemData' não foi atribuído em '{gameObject.name}'.");
                return;
            }

            MessageBroker.Publish(new CollectItemCommandMessage(itemData.ToDomainItem()));
            gameObject.SetActive(false);
        }
    }
}
