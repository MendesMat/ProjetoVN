using ProjetoVN.Dialogue.Data;
using ProjetoVN.Inventory;
using UnityEngine;

namespace ProjetoVN.GameFlow.DialogueEffects
{
    /// <summary>
    /// Tira um item do inventário por decisão da história (ex.: entregar a chave a um NPC).
    /// Reaproveita TryUse, então publica ItemUsedMessage igual a um uso no mundo — hoje isso não
    /// faz diferença para ninguém, já que só o InventoryPresenter escuta e ele apenas remove o slot.
    /// </summary>
    [CreateAssetMenu(fileName = "RemoveItemEffect", menuName = "Dialogue/Effects/Remove Item")]
    public sealed class RemoveItemEffect : DialogueEffectSO
    {
        [SerializeField] private ItemDataSO item;

        public override void Execute()
        {
            if (item == null)
            {
                Debug.LogError("[RemoveItemEffect] O campo 'item' não foi atribuído. Efeito ignorado.", this);
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[RemoveItemEffect] Não há InventoryManager. Efeito ignorado.", this);
                return;
            }

            // false só significa que o jogador não tinha o item — um diálogo que não deveria
            // ter sido alcançado, ou já rejogado. Não é erro.
            InventoryManager.Instance.TryUse(item);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (item == null)
                Debug.LogWarning("[RemoveItemEffect] O campo 'item' está vazio.", this);
        }
#endif
    }
}
