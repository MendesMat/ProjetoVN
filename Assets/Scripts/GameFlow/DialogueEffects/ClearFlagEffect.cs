using ProjetoVN.Core.State;
using ProjetoVN.Dialogue.Data;
using UnityEngine;

namespace ProjetoVN.GameFlow.DialogueEffects
{
    /// <summary>Desliga uma variável booleana da história (grava falso), para estados que podem voltar atrás.</summary>
    [CreateAssetMenu(fileName = "ClearFlagEffect", menuName = "Dialogue/Effects/Clear Flag")]
    public sealed class ClearFlagEffect : DialogueEffectSO
    {
        [Tooltip("Nome da variável: '$' seguido de minúsculas sem acento, dígitos e '_'. Vazio = o efeito não faz nada e avisa.")]
        [SerializeField] private string flagId;

        public override void Execute()
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                Debug.LogError("[ClearFlagEffect] O campo 'flagId' está vazio. Efeito ignorado.", this);
                return;
            }

            StoryState.SetBool(flagId, false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(flagId))
                Debug.LogWarning("[ClearFlagEffect] O campo 'flagId' está vazio. Preencha um nome único (ex.: \"$porta_destrancada\").", this);

            StoryVariableNameCheck.WarnIfOffConvention(nameof(ClearFlagEffect), nameof(flagId), flagId, this);
        }
#endif
    }
}
