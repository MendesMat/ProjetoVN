using ProjetoVN.Core.State;
using ProjetoVN.Dialogue.Data;
using UnityEngine;

namespace ProjetoVN.GameFlow.DialogueEffects
{
    /// <summary>Desliga uma flag de história, para estados que podem voltar atrás.</summary>
    [CreateAssetMenu(fileName = "ClearFlagEffect", menuName = "Dialogue/Effects/Clear Flag")]
    public sealed class ClearFlagEffect : DialogueEffectSO
    {
        [SerializeField] private string flagId;

        public override void Execute()
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                Debug.LogError("[ClearFlagEffect] O campo 'flagId' está vazio. Efeito ignorado.", this);
                return;
            }

            StoryFlags.Clear(flagId);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(flagId))
                Debug.LogWarning("[ClearFlagEffect] O campo 'flagId' está vazio. Preencha um id único.", this);
        }
#endif
    }
}
