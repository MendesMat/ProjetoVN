using ProjetoVN.Dialogue.Data;
using ProjetoVN.Inventory;
using UnityEngine;

namespace ProjetoVN.GameFlow.DialogueEffects
{
    /// <summary>Coloca um item no inventário do jogador. Ex.: a gótica entrega a chave numa escolha.</summary>
    [CreateAssetMenu(fileName = "GiveItemEffect", menuName = "Dialogue/Effects/Give Item")]
    public sealed class GiveItemEffect : DialogueEffectSO
    {
        [SerializeField] private ItemDataSO item;

        public override void Execute()
        {
            if (item == null)
            {
                Debug.LogError("[GiveItemEffect] O campo 'item' não foi atribuído. Efeito ignorado.", this);
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[GiveItemEffect] Não há InventoryManager. Efeito ignorado.", this);
                return;
            }

            // O retorno é ignorado de propósito: false só significa que o jogador já tem o item,
            // o que é normal ao rejogar um diálogo.
            InventoryManager.Instance.Collect(item);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (item == null)
                Debug.LogWarning("[GiveItemEffect] O campo 'item' está vazio.", this);
        }
#endif
    }
}
