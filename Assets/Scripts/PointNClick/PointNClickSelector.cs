using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjetoVN.PointNClick
{
    public class PointNClickSelector : MonoBehaviour
    {
        private Camera _mainCamera;
        private InteractableItem _hoveredItem;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void OnDisable()
        {
            ClearHoverState();
        }

        private void Update()
        {
            if (!PlayerInputGate.IsEnabled)
            {
                ClearHoverState();
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            InteractableItem itemUnderMouse = FindItemUnderMouse(mouse);
            UpdateHoverState(itemUnderMouse);

            if (_hoveredItem == null) return;
            if (!mouse.leftButton.wasPressedThisFrame) return;
            if (!PlayerInputGate.CanClickThisFrame) return;

            _hoveredItem.OnClick();
        }

        private InteractableItem FindItemUnderMouse(Mouse mouse)
        {
            Vector2 screenPosition = mouse.position.ReadValue();
            Vector2 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);

            Collider2D hit = Physics2D.OverlapPoint(worldPosition);
            if (hit == null) return null;

            return hit.GetComponent<InteractableItem>();
        }

        private void UpdateHoverState(InteractableItem itemUnderMouse)
        {
            if (itemUnderMouse == _hoveredItem) return;

            if (_hoveredItem != null) _hoveredItem.OnHoverExit();
            _hoveredItem = itemUnderMouse;

            if (_hoveredItem != null) _hoveredItem.OnHoverEnter();
        }

        private void ClearHoverState()
        {
            if (_hoveredItem == null) return;

            _hoveredItem.OnHoverExit();
            _hoveredItem = null;
        }
    }
}
