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

        [Header("References")]
        [Tooltip("O objeto que o pan desloca: pai do fundo, dos objetos interativos e dos pontos de entrada (ex.: Environment/ScrollingBackground). " +
                 "Vazio = o cenário não anda, com aviso no Inspector e erro ao entrar na sala.")]
        [SerializeField] private Transform environmentContainer;

        [Tooltip("O objeto com as imagens de fundo da sala (ex.: Environment/ScrollingBackground/Backgrounds), dentro do Environment Container. " +
                 "O pan para quando a borda da imagem encosta na borda da tela, em qualquer formato de tela; todas as imagens filhas contam como uma só " +
                 "(uma panorâmica ou painéis lado a lado). Imagem mais estreita que a tela fica centralizada. " +
                 "Vazio, ou sem nenhum SpriteRenderer = o cenário não anda, com aviso no Inspector e erro ao entrar na sala.")]
        [SerializeField] private Transform background;

        private enum PanDirection { None, Left, Right }

        private bool _hasBackgroundEdges;
        private float _backgroundLeft;
        private float _backgroundRight;

        private void Awake()
        {
            if (environmentContainer == null)
            {
                Debug.LogError("[ScreenPanController] 'environmentContainer' não foi atribuído. O cenário não vai se mover.", this);
                return;
            }

            MeasureBackground();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (environmentContainer == null)
                Debug.LogWarning("[ScreenPanController] O campo 'Environment Container' está vazio: o cenário não anda. Atribua o objeto pai do fundo e dos interativos.", this);

            if (background == null)
            {
                Debug.LogWarning("[ScreenPanController] O campo 'Background' está vazio: o cenário não anda. Atribua o objeto com as imagens de fundo da sala.", this);
                return;
            }

            if (background.GetComponentInChildren<SpriteRenderer>() == null)
                Debug.LogWarning("[ScreenPanController] O 'Background' não tem nenhum SpriteRenderer nele nem nos filhos: o cenário não anda.", this);

            if (environmentContainer != null && !background.IsChildOf(environmentContainer))
                Debug.LogWarning("[ScreenPanController] O 'Background' precisa estar dentro do 'Environment Container': senão o fundo não anda com o pan e o limite sai errado.", this);
        }
#endif

        private void Update()
        {
            if (environmentContainer == null || !_hasBackgroundEdges) return;

            KeepContainerInsideLimits();

            if (!PlayerInputGate.IsEnabled) return;

            PanDirection direction = DetectPanDirection();
            if (direction == PanDirection.None) return;

            ApplyPan(direction);
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

        private void MeasureBackground()
        {
            if (background == null)
            {
                Debug.LogError("[ScreenPanController] 'background' não foi atribuído. O cenário não vai se mover.", this);
                return;
            }

            SpriteRenderer[] renderers = background.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length == 0)
            {
                Debug.LogError("[ScreenPanController] 'background' não tem nenhum SpriteRenderer. O cenário não vai se mover.", this);
                return;
            }

            Bounds union = renderers[0].bounds;
            foreach (SpriteRenderer renderer in renderers)
                union.Encapsulate(renderer.bounds);

            float containerX = environmentContainer.position.x;
            _backgroundLeft = union.min.x - containerX;
            _backgroundRight = union.max.x - containerX;
            _hasBackgroundEdges = true;
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

        private void ApplyPan(PanDirection direction)
        {
            MoveContainerToX(environmentContainer.position.x + CalculateDisplacement(direction));
        }

        private void KeepContainerInsideLimits()
        {
            MoveContainerToX(environmentContainer.position.x);
        }

        private void MoveContainerToX(float x)
        {
            if (!_hasBackgroundEdges) return;
            if (!TryCalculateRange(out PanRange range)) return;

            Vector3 position = environmentContainer.position;
            float clampedX = range.Clamp(x);
            if (Mathf.Approximately(clampedX, position.x)) return;

            environmentContainer.position = new Vector3(clampedX, position.y, position.z);
        }

        private bool TryCalculateRange(out PanRange range)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                range = default;
                return false;
            }

            float halfViewWidth = camera.orthographicSize * camera.aspect;
            range = PanLimits.For(_backgroundLeft, _backgroundRight, camera.transform.position.x, halfViewWidth);
            return true;
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
