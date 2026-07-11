using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Core.Messaging;
using ProjetoVN.Inventory;
using ProjetoVN.Inventory.Messages;
using UnityEngine;

namespace ProjetoVN.UI.Inventory
{
    public sealed class InventoryPresenter : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private GameObject itemSlotGameObject;
        [SerializeField] private List<ItemDataSO> itemDatabase;

        private readonly Dictionary<string, GameObject> _activeSlots = new();
        private readonly Queue<GameObject> _slotPool = new();

        private void OnEnable()
        {
            MessageBroker.Subscribe<ItemCollectedMessage>(OnItemCollected);
            MessageBroker.Subscribe<ItemUsedMessage>(OnItemUsed);
        }

        private void OnDisable()
        {
            MessageBroker.Unsubscribe<ItemCollectedMessage>(OnItemCollected);
            MessageBroker.Unsubscribe<ItemUsedMessage>(OnItemUsed);
        }

        private void OnItemCollected(ItemCollectedMessage message) =>
            AddSlot(message.CollectedItem);

        private void OnItemUsed(ItemUsedMessage message) =>
            RemoveSlot(message.UsedItem);

        private void AddSlot(Item item)
        {
            if (_activeSlots.ContainsKey(item.Id)) return;

            var slot = GetSlotFromPool();
            slot.name = $"Slot_{item.Id}";

            var slotUI = slot.GetComponent<ItemSlotUI>();
            if (slotUI != null)
            {
                ItemDataSO data = FindItemData(item.Id);
                slotUI.Setup(item.Name, data != null ? data.Icon : null);
            }

            _activeSlots[item.Id] = slot;
        }

        private void RemoveSlot(Item item)
        {
            if (!_activeSlots.TryGetValue(item.Id, out GameObject slot)) return;

            ReturnSlotToPool(slot);
            _activeSlots.Remove(item.Id);
        }

        private GameObject GetSlotFromPool()
        {
            if (_slotPool.Count > 0)
            {
                GameObject slot = _slotPool.Dequeue();
                slot.SetActive(true);
                slot.transform.SetAsLastSibling();
                return slot;
            }

            return Instantiate(itemSlotGameObject, slotsContainer);
        }

        private void ReturnSlotToPool(GameObject slot)
        {
            slot.SetActive(false);
            _slotPool.Enqueue(slot);
        }

        private ItemDataSO FindItemData(string itemId) =>
            itemDatabase.FirstOrDefault(data => data.Id == itemId);
    }
}
