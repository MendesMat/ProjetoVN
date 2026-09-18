using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Inventory
{
    [CreateAssetMenu(fileName = "ItemRegistry", menuName = "Items/Item Registry")]
    public sealed class ItemRegistry : ScriptableObject
    {
        [SerializeField] private List<ItemDataSO> items = new();

        public bool TryGetById(string id, out ItemDataSO item)
        {
            item = items.Find(i => i != null && i.Id == id);
            return item != null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<string>();
            foreach (ItemDataSO item in items)
            {
                if (item == null) continue;

                if (!seen.Add(item.Id))
                    Debug.LogError($"[ItemRegistry] Id duplicado: '{item.Id}' em {item.name}.", item);
            }
        }
#endif
    }
}
