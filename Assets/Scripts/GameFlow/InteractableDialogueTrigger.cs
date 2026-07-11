using Assets.Scripts.Core.Messaging;
using Assets.Scripts.Dialogue.Data;
using Assets.Scripts.Dialogue.Messaging;
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
                Debug.LogWarning($"[InteractableDialogueTrigger] Nenhum DialogueData atribuído em '{gameObject.name}'.");
                return;
            }

            Debug.Log($"[InteractableDialogueTrigger] Clique em '{gameObject.name}' → DialogueRequestMessage: '{dialogueData.name}'");
            MessageBroker.Publish(new DialogueRequestMessage(dialogueData));
        }
    }
}
