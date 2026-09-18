using ProjetoVN.Dialogue.Data;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;

namespace ProjetoVN.GameFlow
{
    public sealed class InteractableDialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueData dialogueData;

        public void TriggerDialogue()
        {
            if (dialogueData == null)
            {
                Debug.LogWarning("[InteractableDialogueTrigger] Nenhum DialogueData atribuído.", this);
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[InteractableDialogueTrigger] Não há DialogueManager na cena. Clique ignorado.", this);
                return;
            }

            DialogueManager.Instance.StartDialogue(dialogueData);
        }
    }
}
