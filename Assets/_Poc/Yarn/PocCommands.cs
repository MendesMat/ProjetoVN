using ProjetoVN.Inventory;
using UnityEngine;
using Yarn.Unity;

namespace ProjetoVN.PocYarn
{
    public static class PocCommands
    {
        public static ItemRegistry Registry { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => Registry = null;

        [YarnCommand("dar_item")]
        public static void GiveItem(string itemId)
        {
            if (!TryFindItem(itemId, out ItemDataSO item)) return;

            // O retorno é ignorado de propósito: false só significa que o jogador já tem o item.
            InventoryManager.Instance.Collect(item);
        }

        [YarnFunction("tem_item")]
        public static bool HasItem(string itemId)
        {
            if (!TryFindItem(itemId, out ItemDataSO item)) return false;

            return InventoryManager.Instance.HasItem(item);
        }

        private static bool TryFindItem(string itemId, out ItemDataSO item)
        {
            item = null;

            if (Registry == null)
            {
                Debug.LogError("[PocCommands] Nenhum ItemRegistry foi registrado. Comando ignorado.");
                return false;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[PocCommands] Não há InventoryManager. Comando ignorado.");
                return false;
            }

            if (Registry.TryGetById(itemId, out item)) return true;

            Debug.LogError($"[PocCommands] O item '{itemId}' não existe no ItemRegistry. Comando ignorado.");
            return false;
        }
    }
}
