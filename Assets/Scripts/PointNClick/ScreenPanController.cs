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

        private void Awake()
        {
            if (environmentContainer == null)
                Debug.LogError("[ScreenPanController] 'environmentContainer' não foi atribuído. O cenário não vai se mover.", this);
        }

        private void Update()
        {
            if (environmentContainer == null) return;
            if (!PlayerInputGate.IsEnabled) return;

            PanDirection direction = DetectPanDirection();
            if (direction == PanDirection.None) return;

            ApplyPan(direction);
        }

        private PanDirection DetectPanDirection()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return PanDirection.None;

            float mouseX = mouse.position.ReadValue().x;
            if (!IsMouseInsideScreen(mouseX)) return PanDirection.None;

            if (mouseX < edgeThreshold) return PanDirection.Right;
            if (mouseX > Screen.width - edgeThreshold) return PanDirection.Left;

            return PanDirection.None;
        }

        public void CenterOn(float worldX)
        {
            if (environmentContainer == null) return;

            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("[ScreenPanController] Não há câmera principal (tag MainCamera). O cenário não foi centralizado.", this);
                return;
            }

            MoveContainerToX(environmentContainer.position.x + camera.transform.position.x - worldX);
        }

        private void ApplyPan(PanDirection direction)
        {
            MoveContainerToX(environmentContainer.position.x + CalculateDisplacement(direction));
        }

        private void MoveContainerToX(float x)
        {
            Vector3 position = environmentContainer.position;
            environmentContainer.position = new Vector3(Mathf.Clamp(x, minX, maxX), position.y, position.z);
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
