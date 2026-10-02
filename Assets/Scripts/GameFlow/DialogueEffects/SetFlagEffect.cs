using ProjetoVN.Core.State;
using ProjetoVN.Dialogue.Data;
using UnityEngine;

namespace ProjetoVN.GameFlow.DialogueEffects
{
    /// <summary>Liga uma variável booleana da história. Ex.: "$falou_com_gotica", lida depois por uma escolha condicional.</summary>
    [CreateAssetMenu(fileName = "SetFlagEffect", menuName = "Dialogue/Effects/Set Flag")]
    public sealed class SetFlagEffect : DialogueEffectSO
    {
        [Tooltip("Nome da variável: '$' seguido de minúsculas sem acento, dígitos e '_'. Vazio = o efeito não faz nada e avisa.")]
        [SerializeField] private string flagId;

        public override void Execute()
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                Debug.LogError("[SetFlagEffect] O campo 'flagId' está vazio. Efeito ignorado.", this);
                return;
            }

            StoryState.SetBool(flagId, true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(flagId))
                Debug.LogWarning("[SetFlagEffect] O campo 'flagId' está vazio. Preencha um nome único (ex.: \"$falou_com_gotica\").", this);

            StoryVariableNameCheck.WarnIfOffConvention(nameof(SetFlagEffect), nameof(flagId), flagId, this);
        }
#endif
    }
}
