using UnityEngine;

namespace ProjetoVN.GameFlow.Menu
{
    /// <summary>
    /// Mostra e esconde a interface de jogo (<c>Canvas_Game</c>, no <c>GameUI.prefab</c>) ligando e desligando o
    /// <c>Canvas</c>, nunca o objeto: com o objeto ativo, o apresentador do diálogo e o inventário seguem registrados.
    /// Quem esconde a interface é o menu principal, que é uma cena por baixo dela (D-19).
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class GameUIVisibility : MonoBehaviour
    {
        public static GameUIVisibility Instance { get; private set; }

        private Canvas _canvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => Instance = null;

        private void Awake()
        {
            Instance = this;
            _canvas = GetComponent<Canvas>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show() => _canvas.enabled = true;

        public void Hide() => _canvas.enabled = false;
    }
}
