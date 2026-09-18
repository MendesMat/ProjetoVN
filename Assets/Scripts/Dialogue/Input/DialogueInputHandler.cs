using ProjetoVN.Dialogue.Logic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjetoVN.Dialogue.Input
{
    public class DialogueInputHandler : MonoBehaviour
    {
        [SerializeField] private InputActionReference advanceDialogueAction;

        #region Unity Lifecycle
        private void OnEnable()
        {
            advanceDialogueAction.action.Enable();
            advanceDialogueAction.action.performed += OnAdvanceDialogue;
        }

        private void OnDisable()
        {
            advanceDialogueAction.action.performed -= OnAdvanceDialogue;
            advanceDialogueAction.action.Disable();
        }
        #endregion

        #region Input Callbacks
        private void OnAdvanceDialogue(InputAction.CallbackContext context)
        {
            if (DialogueManager.Instance == null) return;
            if (!DialogueManager.Instance.IsDialogueActive()) return;

            DialogueManager.Instance.AdvanceDialogue();
        }
        #endregion
    }
}
