using System.Collections.Generic;
using ProjetoVN.Core.Messaging;
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

        private readonly Dictionary<ItemDataSO, GameObject> _activeSlots = new();
        private readonly Queue<GameObject> _slotPool = new();

        private void OnEnable()
        {
            MessageBroker.Subscribe<ItemCollectedMessage>(OnItemCollected);
            MessageBroker.Subscribe<ItemUsedMessage>(OnItemUsed);

            Rebuild();
        }

        private void OnDisable()
        {
            MessageBroker.Unsubscribe<ItemCollectedMessage>(OnItemCollected);
            MessageBroker.Unsubscribe<ItemUsedMessage>(OnItemUsed);
        }

        private void OnItemCollected(ItemCollectedMessage message) => AddSlot(message.CollectedItem);

        private void OnItemUsed(ItemUsedMessage message) => RemoveSlot(message.UsedItem);

        private void Rebuild()
        {
            ClearAllSlots();

            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("[InventoryPresenter] Não há InventoryManager. O painel abrirá vazio.", this);
                return;
            }

            foreach (ItemDataSO item in InventoryManager.Instance.Items)
                AddSlot(item);
        }

        private void AddSlot(ItemDataSO item)
        {
            if (item == null || _activeSlots.ContainsKey(item)) return;

            GameObject slot = GetSlotFromPool();
            slot.name = $"Slot_{item.Id}";

            var slotUI = slot.GetComponent<ItemSlotUI>();
            if (slotUI != null) slotUI.Setup(item.ItemName, item.Icon);

            _activeSlots[item] = slot;
        }

        private void RemoveSlot(ItemDataSO item)
        {
            if (item == null) return;
            if (!_activeSlots.TryGetValue(item, out GameObject slot)) return;

            ReturnSlotToPool(slot);
            _activeSlots.Remove(item);
        }

        private void ClearAllSlots()
        {
            foreach (GameObject slot in _activeSlots.Values)
                ReturnSlotToPool(slot);

            _activeSlots.Clear();
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
    }
}
