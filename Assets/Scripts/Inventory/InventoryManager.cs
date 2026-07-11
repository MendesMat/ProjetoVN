using Assets.Scripts.Core.Messaging;
using ProjetoVN.Inventory.Messages;
using UnityEngine;

namespace ProjetoVN.Inventory
{
    public sealed class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        public InventoryService Service { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Service = new InventoryService(new InventoryModel());
        }

        private void OnEnable()
        {
            MessageBroker.Subscribe<CollectItemCommandMessage>(OnCollectItem);
            MessageBroker.Subscribe<CheckItemRequestMessage>(OnCheckItem);
            MessageBroker.Subscribe<UseItemCommandMessage>(OnUseItem);
        }

        private void OnDisable()
        {
            MessageBroker.Unsubscribe<CollectItemCommandMessage>(OnCollectItem);
            MessageBroker.Unsubscribe<CheckItemRequestMessage>(OnCheckItem);
            MessageBroker.Unsubscribe<UseItemCommandMessage>(OnUseItem);
        }

        private void OnCollectItem(CollectItemCommandMessage msg) => Service.CollectItem(msg.ItemToCollect);
        private void OnCheckItem(CheckItemRequestMessage msg) => msg.Callback?.Invoke(Service.HasItem(msg.ItemId));
        private void OnUseItem(UseItemCommandMessage msg) => Service.TryUseItem(msg.ItemId);
    }
}
