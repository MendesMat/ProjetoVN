using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjetoVN.PointNClick
{
    public class ScreenPanController : MonoBehaviour
    {
        [Header("Pan Settings")]
        [SerializeField] private float panSpeed = 300f;
        [SerializeField] private float edgeThreshold = 80f;

        [Header("Boundaries")]
        [SerializeField] private float minX = -5f;
        [SerializeField] private float maxX = 5f;

        [Header("References")]
        [SerializeField] private Transform environmentContainer;

        private enum PanDirection { None, Left, Right }

        private void Update()
        {
            PanDirection direction = DetectPanDirection();

            if (direction == PanDirection.None)
                return;

            ApplyPan(direction);
        }

        private PanDirection DetectPanDirection()
        {
            Mouse mouse = Mouse.current;

            if (mouse == null)
                return PanDirection.None;

            float mouseX = mouse.position.ReadValue().x;

            if (!IsMouseInsideScreen(mouseX))
                return PanDirection.None;

            if (mouseX < edgeThreshold)
                return PanDirection.Right;

            if (mouseX > Screen.width - edgeThreshold)
                return PanDirection.Left;

            return PanDirection.None;
        }

        private void ApplyPan(PanDirection direction)
        {
            float displacement = CalculateDisplacement(direction);
            Vector3 position = environmentContainer.position;
            float clampedX = Mathf.Clamp(position.x + displacement, minX, maxX);

            environmentContainer.position = new Vector3(
                clampedX,
                position.y,
                position.z
            );
        }

        private float CalculateDisplacement(PanDirection direction)
        {
            float sign = direction == PanDirection.Right ? 1f : -1f;
            return sign * panSpeed * Time.deltaTime;
        }

        private bool IsMouseInsideScreen(float mouseX)
        {
            return mouseX >= 0f && mouseX <= Screen.width;
        }
    }
}
