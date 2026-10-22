using ProjetoVN.GameFlow.Persistence;
using UnityEngine;
using UnityEngine.Events;

namespace ProjetoVN.GameFlow.Menu
{
    /// <summary>
    /// O menu principal, na cena <c>Menu.unity</c>. Os botões o chamam por <c>UnityEvent</c> do Inspector (D-07); ele esconde
    /// a interface de jogo enquanto está aberto e entrega Novo Jogo e Continuar ao <see cref="GameSaveManager"/>.
    /// Só lê um manager a partir do <c>Start</c>: na primeira cena os managers ainda não existem no <c>Awake</c>.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [Tooltip("A sala onde o Novo Jogo começa: o nome da cena, exatamente como o arquivo (ex.: [Teste] Mecanicas). " +
                 "Precisa estar na lista de cenas do build. É o único lugar onde a sala inicial é configurada. " +
                 "Vazio = o Novo Jogo avisa no console e nada acontece.")]
        [SerializeField] private string startRoom = "[Teste] Mecanicas";

        [Tooltip("O id de um Room Entry Point da sala inicial: onde o jogador chega. " +
                 "Vazio = chega na posição em que a cena foi salva.")]
        [SerializeField] private string startEntryPointId;

        [Tooltip("Chamado quando o menu abre, com 'true' se existe um save que o Continuar consegue ler. " +
                 "Ligue no Set Interactable do botão Continuar. Sem ligação = o Continuar fica sempre como está.")]
        [SerializeField] private UnityEvent<bool> onSaveChecked;

        [Tooltip("Chamado pelo Novo Jogo quando já existe um save, no lugar de começar. " +
                 "Ligue na janela de confirmação. Sem ligação = o Novo Jogo com save não faz nada.")]
        [SerializeField] private UnityEvent onNewGameNeedsConfirmation;

        private void Start()
        {
            HideGameUI();
            NotifyWhetherThereIsASave();
        }

        private void OnDestroy()
        {
            if (GameUIVisibility.Instance != null)
                GameUIVisibility.Instance.Show();
        }

        public void RequestNewGame()
        {
            if (!TryGetSaveManager(out GameSaveManager saveManager)) return;

            if (saveManager.HasSave)
                onNewGameNeedsConfirmation.Invoke();
            else
                StartNewGame();
        }

        public void StartNewGame()
        {
            if (!TryGetSaveManager(out GameSaveManager saveManager)) return;

            saveManager.StartNewGame(startRoom, startEntryPointId);
        }

        public void ContinueGame()
        {
            if (!TryGetSaveManager(out GameSaveManager saveManager)) return;

            saveManager.Load();
        }

        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
        }

        private void HideGameUI()
        {
            if (GameUIVisibility.Instance == null)
            {
                Debug.LogError("[MainMenu] Não há GameUIVisibility (o GameUI.prefab não foi criado): " +
                               "a interface de jogo aparece por cima do menu.", this);
                return;
            }

            GameUIVisibility.Instance.Hide();
        }

        private void NotifyWhetherThereIsASave()
        {
            if (!TryGetSaveManager(out GameSaveManager saveManager)) return;

            onSaveChecked.Invoke(saveManager.HasSave);
        }

        private bool TryGetSaveManager(out GameSaveManager saveManager)
        {
            saveManager = GameSaveManager.Instance;
            if (saveManager != null) return true;

            Debug.LogError("[MainMenu] Não há GameSaveManager (o prefab Managers não foi criado). Ação ignorada.", this);
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(startRoom))
                Debug.LogWarning("[MainMenu] O campo Start Room está vazio: o Novo Jogo não leva a lugar nenhum.", this);

            WarnIfNotWired(onSaveChecked, "On Save Checked", "o Continuar não é desabilitado sem save");
            WarnIfNotWired(onNewGameNeedsConfirmation, "On New Game Needs Confirmation",
                "o Novo Jogo com save não faz nada");
        }

        private void WarnIfNotWired(UnityEventBase unityEvent, string fieldLabel, string consequence)
        {
            if (unityEvent.GetPersistentEventCount() > 0) return;

            Debug.LogWarning($"[MainMenu] O evento {fieldLabel} não tem ligação: {consequence}.", this);
        }
#endif
    }
}
