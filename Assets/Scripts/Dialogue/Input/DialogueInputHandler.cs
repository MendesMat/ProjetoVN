using System.Collections.Generic;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjetoVN.Dialogue.Input
{
    public class DialogueInputHandler : MonoBehaviour
    {
        [SerializeField] private InputActionReference advanceDialogueAction;

        private readonly List<RaycastResult> _pointerHits = new();

        #region Unity Lifecycle
        private void OnEnable()
        {
            if (advanceDialogueAction == null)
            {
                Debug.LogError("[DialogueInputHandler] 'advanceDialogueAction' não foi atribuído. O diálogo não poderá avançar.", this);
                return;
            }

            advanceDialogueAction.action.Enable();
            advanceDialogueAction.action.performed += OnAdvanceDialogue;
        }

        private void OnDisable()
        {
            if (advanceDialogueAction == null) return;

            advanceDialogueAction.action.performed -= OnAdvanceDialogue;
            advanceDialogueAction.action.Disable();
        }
        #endregion

        #region Input Callbacks
        private void OnAdvanceDialogue(InputAction.CallbackContext context)
        {
            if (DialogueManager.Instance == null) return;
            if (!DialogueManager.Instance.IsDialogueActive()) return;
            if (IsClickOnUiButton(context)) return;

            DialogueManager.Instance.AdvanceDialogue();
        }

        // O clique de um botão da interface (como o do histórico) não avança a fala. A ação dispara ao apertar, antes
        // de o Button agir ao soltar, então só o raycast da interface sabe de quem é o clique. IsPointerOverGameObject
        // não serve: dentro de um callback de InputAction ele lê o estado do frame anterior.
        private bool IsClickOnUiButton(InputAction.CallbackContext context)
        {
            if (context.control.device is not Pointer pointer) return false;
            if (EventSystem.current == null) return false;

            var pointerData = new PointerEventData(EventSystem.current) { position = pointer.position.ReadValue() };
            _pointerHits.Clear();
            EventSystem.current.RaycastAll(pointerData, _pointerHits);

            return _pointerHits.Count > 0 && _pointerHits[0].gameObject.GetComponentInParent<Selectable>() != null;
        }
        #endregion
    }
}
