using ProjetoVN.Core.State;
using ProjetoVN.Dialogue.Data;
using UnityEngine;

namespace ProjetoVN.GameFlow.DialogueEffects
{
    /// <summary>Liga uma flag de história. Ex.: "falou-com-gotica", lido depois por uma escolha condicional.</summary>
    [CreateAssetMenu(fileName = "SetFlagEffect", menuName = "Dialogue/Effects/Set Flag")]
    public sealed class SetFlagEffect : DialogueEffectSO
    {
        [SerializeField] private string flagId;

        public override void Execute()
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                Debug.LogError("[SetFlagEffect] O campo 'flagId' está vazio. Efeito ignorado.", this);
                return;
            }

            StoryFlags.Set(flagId);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(flagId))
                Debug.LogWarning("[SetFlagEffect] O campo 'flagId' está vazio. Preencha um id único.", this);
        }
#endif
    }
}
