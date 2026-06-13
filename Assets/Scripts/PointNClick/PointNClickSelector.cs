using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjetoVN.PointNClick
{
    public class PointNClickSelector : MonoBehaviour
    {
        private Camera mainCamera;
        private InteractableItem hoveredItem;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            InteractableItem itemUnderMouse = FindItemUnderMouse(mouse);

            UpdateHoverState(itemUnderMouse);

            if (hoveredItem != null && mouse.leftButton.wasPressedThisFrame) hoveredItem.OnClick();
        }

        private InteractableItem FindItemUnderMouse(Mouse mouse)
        {
            Vector2 screenPosition = mouse.position.ReadValue();
            Vector2 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);

            Collider2D hit = Physics2D.OverlapPoint(worldPosition);
            if (hit == null) return null;

            return hit.GetComponent<InteractableItem>();
        }

        private void UpdateHoverState(InteractableItem itemUnderMouse)
        {
            if (itemUnderMouse == hoveredItem) return;

            if (hoveredItem != null) hoveredItem.OnHoverExit();
            hoveredItem = itemUnderMouse;

            if (hoveredItem != null) hoveredItem.OnHoverEnter();
        }
    }
}
