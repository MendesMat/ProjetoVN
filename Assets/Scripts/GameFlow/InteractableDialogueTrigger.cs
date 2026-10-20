using ProjetoVN.Core;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;

namespace ProjetoVN.GameFlow
{
    public sealed class InteractableDialogueTrigger : MonoBehaviour
    {
        [Tooltip("Nó do roteiro (.yarn) que este objeto inicia, escolhido na lista dos nós de Assets/Roteiro/. " +
                 "(nenhum) = o clique não inicia diálogo e avisa no console. " +
                 "Um nó que não existe mais continua no campo, com aviso.")]
        [ScriptNode]
        [SerializeField] private string nodeName;

        public void TriggerDialogue()
        {
            if (string.IsNullOrWhiteSpace(nodeName))
            {
                Debug.LogWarning("[InteractableDialogueTrigger] Nenhum nó de roteiro atribuído (campo Node Name).", this);
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[InteractableDialogueTrigger] Não há DialogueManager (o prefab Managers não foi criado). Clique ignorado.", this);
                return;
            }

            DialogueManager.Instance.StartDialogue(nodeName);
        }

#if UNITY_EDITOR
        // Vazio não avisa: o prefab Interactable traz este gatilho de fábrica e avisar encheria o console.
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(nodeName) || ScriptNodeName.FollowsConvention(nodeName)) return;

            Debug.LogWarning($"[InteractableDialogueTrigger] O nó '{nodeName}' foge do formato dos nomes de nó: minúsculas sem acento, dígitos e '_' " +
                             "(ex.: porta_trancada).", this);
        }
#endif
    }
}
