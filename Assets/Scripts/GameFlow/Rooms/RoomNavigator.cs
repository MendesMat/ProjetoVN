using System;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjetoVN.GameFlow.Rooms
{
    /// <summary>
    /// O dono da troca de sala. <see cref="ChangeRoom"/> confere o pedido, fecha o input pelo estado de transição,
    /// escurece a tela, carrega a cena e clareia. Vive no <c>Managers.prefab</c>; o nome da sala é o nome da cena,
    /// que precisa estar na lista de cenas do build.
    /// </summary>
    public sealed class RoomNavigator : MonoBehaviour
    {
        public static RoomNavigator Instance { get; private set; }

        [Tooltip("O fade de tela (Canvas_Fade, dentro do GameUI.prefab). " +
                 "Vazio = nenhuma troca de sala é aceita: ChangeRoom loga um erro e devolve false.")]
        [SerializeField] private ScreenFader screenFader;

        [Tooltip("Duração de cada metade da troca de sala: escurecer e clarear. 0 = troca sem fade.")]
        [Min(0f)]
        [SerializeField] private float fadeSeconds = 0.4f;

        private GameStateController _stateController;
        private bool _isChanging;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => Instance = null;

        private void Awake()
        {
            Instance = this;
            _stateController = GetComponent<GameStateController>();

            if (_stateController == null)
                Debug.LogError("[RoomNavigator] Não há GameStateController no mesmo objeto. Nenhuma troca de sala será aceita.", this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool ChangeRoom(string roomName)
        {
            if (!CanChangeRoom(roomName)) return false;

            _isChanging = true;
            _stateController.EnterRoomTransition();
            _ = ChangeRoomAsync(roomName);
            return true;
        }

        private bool CanChangeRoom(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                Debug.LogWarning("[RoomNavigator] ChangeRoom recebeu um nome de sala vazio. Troca ignorada.", this);
                return false;
            }

            if (_stateController == null || screenFader == null)
            {
                Debug.LogError($"[RoomNavigator] Troca para '{roomName}' ignorada: falta o GameStateController ou o ScreenFader.", this);
                return false;
            }

            if (!_stateController.IsExploring)
            {
                Debug.LogWarning($"[RoomNavigator] Troca para '{roomName}' ignorada: {DescribeWhyNotExploring()}.", this);
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(roomName))
            {
                Debug.LogError($"[RoomNavigator] A sala '{roomName}' não existe. A cena existe e está na lista de cenas do build?", this);
                return false;
            }

            return true;
        }

        private string DescribeWhyNotExploring()
        {
            if (_isChanging) return "já há uma troca de sala em curso";

            DialogueManager dialogue = DialogueManager.Instance;
            if (dialogue != null && dialogue.IsDialogueActive()) return "há uma conversa aberta";

            return "o jogo não está em exploração";
        }

        private async Awaitable ChangeRoomAsync(string roomName)
        {
            try
            {
                await screenFader.FadeOutAsync(fadeSeconds, destroyCancellationToken);
                await SceneManager.LoadSceneAsync(roomName);
                destroyCancellationToken.ThrowIfCancellationRequested();
                await screenFader.FadeInAsync(fadeSeconds, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                _isChanging = false;
            }

            _stateController.LeaveRoomTransition();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (screenFader == null)
                Debug.LogWarning("[RoomNavigator] O campo Screen Fader está vazio: nenhuma troca de sala será aceita.", this);
        }
#endif
    }
}
