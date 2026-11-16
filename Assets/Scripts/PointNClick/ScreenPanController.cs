using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjetoVN.PointNClick
{
    public class ScreenPanController : MonoBehaviour
    {
        [Header("Pan Settings")]
        [Tooltip("Velocidade do cenário com o mouse na borda, em unidades do mundo por segundo (as salas usam 10). " +
                 "0 = o cenário não anda. Não aceita negativo.")]
        [Min(0f)]
        [SerializeField] private float panSpeed = 10f;

        [Tooltip("Largura, em pixels da tela, da faixa junto às bordas esquerda e direita onde o mouse faz o cenário andar (as salas usam 80). " +
                 "0 = o pan nunca dispara. Não aceita negativo.")]
        [Min(0f)]
        [SerializeField] private float edgeThreshold = 80f;

        [Header("Boundaries")]
        [Tooltip("Menor posição X que o Environment Container alcança: o limite do pan de um lado. A conta está em docs/autoria/salas.md, " +
                 "seção \"Pan do cenário\" (a [Teste] Mecanicas usa -22.4). Maior que Max X = aviso no console, e o pan pula entre os dois limites.")]
        [SerializeField] private float minX = -5f;

        [Tooltip("Maior posição X que o Environment Container alcança: o limite do pan do outro lado. A conta está em docs/autoria/salas.md, " +
                 "seção \"Pan do cenário\" (a [Teste] Mecanicas usa 22.4). Menor que Min X = aviso no console, e o pan pula entre os dois limites.")]
        [SerializeField] private float maxX = 5f;

        [Header("References")]
        [Tooltip("O objeto que o pan desloca: pai do fundo, dos objetos interativos e dos pontos de entrada (ex.: Environment/ScrollingBackground). " +
                 "Vazio = o cenário não anda, com aviso no Inspector e erro ao entrar na sala.")]
        [SerializeField] private Transform environmentContainer;

        private enum PanDirection { None, Left, Right }

        private void Awake()
        {
            if (environmentContainer == null)
                Debug.LogError("[ScreenPanController] 'environmentContainer' não foi atribuído. O cenário não vai se mover.", this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (environmentContainer == null)
                Debug.LogWarning("[ScreenPanController] O campo 'Environment Container' está vazio: o cenário não anda. Atribua o objeto pai do fundo e dos interativos.", this);

            if (minX > maxX)
                Debug.LogWarning($"[ScreenPanController] 'Min X' ({minX}) é maior que 'Max X' ({maxX}): o pan pula entre os dois limites. Veja a conta em docs/autoria/salas.md, seção \"Pan do cenário\".", this);
        }
#endif

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
