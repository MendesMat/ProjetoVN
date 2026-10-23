using ProjetoVN.Dialogue.Logic;
using ProjetoVN.GameFlow.Persistence;
using ProjetoVN.GameFlow.Rooms;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace ProjetoVN.GameFlow.Menu
{
    /// <summary>
    /// A pausa, no <c>Canvas_Pause</c> do <c>GameUI.prefab</c>. Lê a ação Pause (Esc), pede ao <see cref="GameStateController"/>
    /// o modo sobreposto e entrega salvar e voltar ao menu aos donos. Os botões o chamam por <c>UnityEvent</c> (D-07); a
    /// janela abre e fecha pelos eventos <c>On Paused</c> e <c>On Resumed</c>.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [Tooltip("A ação Pause do mapa UI (Esc e Start do controle). Vazio = a pausa não abre pelo teclado nem pelo controle.")]
        [SerializeField] private InputActionReference pauseAction;

        [Tooltip("A cena do menu principal, exatamente como o arquivo (ex.: Menu). Precisa estar na lista de cenas do build. " +
                 "Vazio = o Voltar ao menu avisa no console e nada acontece.")]
        [SerializeField] private string mainMenuScene = "Menu";

        [Tooltip("Chamado quando a pausa abre. Ligue na abertura da Pause Window (UIWindowManager.OpenWindow) e no " +
                 "SetActive(false) da mensagem Jogo salvo. Sem ligação = a pausa abre sem janela.")]
        [SerializeField] private UnityEvent onPaused;

        [Tooltip("Chamado quando a pausa fecha, por Continuar ou por Esc. Ligue no UIWindowManager.CloseAllWindows. " +
                 "Sem ligação = a janela fica na tela depois de retomar.")]
        [SerializeField] private UnityEvent onResumed;

        [Tooltip("Chamado ao abrir a pausa, com 'true' se salvar é possível (não há conversa aberta). " +
                 "Ligue no Set Interactable do botão Salvar, como ligação dinâmica. Sem ligação = o Salvar fica sempre como está.")]
        [SerializeField] private UnityEvent<bool> onSaveAvailabilityChecked;

        [Tooltip("Chamado depois de um salvar que deu certo. Ligue no SetActive(true) da mensagem Jogo salvo. " +
                 "Sem ligação = o jogador não vê retorno do salvar.")]
        [SerializeField] private UnityEvent onSaved;

        private bool IsPaused => GameStateController.Instance != null && GameStateController.Instance.IsPaused;

        private void OnEnable()
        {
            if (pauseAction == null)
            {
                Debug.LogError("[PauseMenu] 'pauseAction' não foi atribuído. A pausa não abre pelo teclado nem pelo controle.", this);
                return;
            }

            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePerformed;
        }

        private void OnDisable()
        {
            if (pauseAction == null) return;

            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }

        public void TogglePause()
        {
            if (IsPaused)
            {
                Resume();
                return;
            }

            TryPause();
        }

        public void Resume()
        {
            if (!HasGameStateController()) return;
            if (!IsPaused) return;

            onResumed.Invoke();
            GameStateController.Instance.LeavePause();
        }

        public void SaveGame()
        {
            if (GameSaveManager.Instance == null)
            {
                Debug.LogError("[PauseMenu] Não há GameSaveManager (o prefab Managers não foi criado). Salvar ignorado.", this);
                return;
            }

            if (GameSaveManager.Instance.Save()) onSaved.Invoke();
        }

        public void ReturnToMainMenu()
        {
            if (RoomNavigator.Instance == null)
            {
                Debug.LogError("[PauseMenu] Não há RoomNavigator (o prefab Managers não foi criado). Voltar ao menu ignorado.", this);
                return;
            }

            Resume();
            StopTheConversation();
            RoomNavigator.Instance.GoToMainMenu(mainMenuScene);
        }

        private void OnPausePerformed(InputAction.CallbackContext _) => TogglePause();

        // Esc no menu principal e durante a troca de sala é normal: a recusa é silenciosa para não encher o console.
        private void TryPause()
        {
            if (GameUIVisibility.Instance == null)
            {
                Debug.LogError("[PauseMenu] Não há GameUIVisibility (o GameUI.prefab não foi criado). Pausa ignorada.", this);
                return;
            }

            if (!GameUIVisibility.Instance.IsShown) return;
            if (!HasGameStateController()) return;
            if (!GameStateController.Instance.TryEnterPause()) return;

            onSaveAvailabilityChecked.Invoke(!IsInConversation());
            onPaused.Invoke();
        }

        private bool HasGameStateController()
        {
            if (GameStateController.Instance != null) return true;

            Debug.LogError("[PauseMenu] Não há GameStateController (o prefab Managers não foi criado). Pausa ignorada.", this);
            return false;
        }

        private void StopTheConversation()
        {
            if (IsInConversation()) DialogueManager.Instance.StopDialogue();
        }

        private static bool IsInConversation() => DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (pauseAction == null)
                Debug.LogWarning("[PauseMenu] O campo Pause Action está vazio: a pausa não abre pelo teclado nem pelo controle.", this);

            if (string.IsNullOrWhiteSpace(mainMenuScene))
                Debug.LogWarning("[PauseMenu] O campo Main Menu Scene está vazio: o Voltar ao menu não leva a lugar nenhum.", this);

            WarnIfNotWired(onPaused, "On Paused", "a pausa abre sem janela");
            WarnIfNotWired(onResumed, "On Resumed", "a janela fica na tela depois de retomar");
            WarnIfNotWired(onSaveAvailabilityChecked, "On Save Availability Checked", "o Salvar fica sempre como está");
            WarnIfNotWired(onSaved, "On Saved", "o jogador não vê retorno do salvar");
        }

        private void WarnIfNotWired(UnityEventBase unityEvent, string fieldLabel, string consequence)
        {
            if (unityEvent != null && unityEvent.GetPersistentEventCount() > 0) return;

            Debug.LogWarning($"[PauseMenu] O evento {fieldLabel} não tem ligação: {consequence}.", this);
        }
#endif
    }
}
