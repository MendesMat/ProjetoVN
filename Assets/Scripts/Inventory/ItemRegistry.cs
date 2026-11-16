using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Inventory
{
    [CreateAssetMenu(fileName = "ItemRegistry", menuName = "Items/Item Registry")]
    public sealed class ItemRegistry : ScriptableObject
    {
        [Tooltip("Todos os itens do jogo. O roteiro e o save acham o item pelo Id só se ele estiver aqui: um item fora da lista " +
                 "não pode ser dado pelo roteiro nem volta ao carregar o save. Item novo entra aqui. " +
                 "Id repetido = erro no console; posição vazia = aviso.")]
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
            for (int index = 0; index < items.Count; index++)
            {
                ItemDataSO item = items[index];
                if (item == null)
                {
                    Debug.LogWarning($"[ItemRegistry] A posição {index} da lista 'Items' está vazia. Atribua um item ou remova a posição.", this);
                    continue;
                }

                if (!seen.Add(item.Id))
                    Debug.LogError($"[ItemRegistry] Id duplicado: '{item.Id}' em {item.name}.", item);
            }
        }
#endif
    }
}
