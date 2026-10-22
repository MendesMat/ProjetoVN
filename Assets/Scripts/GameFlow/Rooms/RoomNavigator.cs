using System;
using System.Linq;
using ProjetoVN.Dialogue.Logic;
using ProjetoVN.GameFlow.Persistence;
using ProjetoVN.PointNClick;
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

        public string CurrentEntryPointId { get; private set; }

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

        public bool ChangeRoom(string roomName) => ChangeRoom(roomName, null);

        public bool ChangeRoom(string roomName, string entryPointId)
        {
            if (!CanChangeRoom(roomName)) return false;

            _isChanging = true;
            _stateController.EnterRoomTransition();
            _ = ChangeRoomAsync(roomName, entryPointId);
            return true;
        }

        private bool CanChangeRoom(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                Debug.LogWarning("[RoomNavigator] ChangeRoom recebeu um nome de sala vazio. Troca ignorada.", this);
                return false;
            }

            if (_stateController == null)
            {
                Debug.LogError($"[RoomNavigator] Troca para '{roomName}' ignorada: falta o GameStateController (no mesmo objeto).", this);
                return false;
            }

            if (screenFader == null)
            {
                Debug.LogError($"[RoomNavigator] Troca para '{roomName}' ignorada: falta o ScreenFader (campo Screen Fader).", this);
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

        private async Awaitable ChangeRoomAsync(string roomName, string entryPointId)
        {
            try
            {
                await screenFader.FadeOutAsync(fadeSeconds, destroyCancellationToken);
                await SceneManager.LoadSceneAsync(roomName);
                destroyCancellationToken.ThrowIfCancellationRequested();
                ArriveAt(roomName, entryPointId);
                CurrentEntryPointId = entryPointId;
                SaveOnArrival();
                await screenFader.FadeInAsync(fadeSeconds, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                RecoverFromFailure(exception);
            }
            finally
            {
                _isChanging = false;
            }

            _stateController.LeaveRoomTransition();
        }

        // Um Awaitable descartado engole a exceção sem log, e o jogo ficaria preso em RoomTransitionState com a tela escura.
        private void RecoverFromFailure(Exception exception)
        {
            Debug.LogException(exception, this);

            if (screenFader != null) screenFader.HideImmediately();
        }

        private void SaveOnArrival()
        {
            if (GameSaveManager.Instance == null)
            {
                Debug.LogError("[RoomNavigator] Não há GameSaveManager: a chegada na sala não foi salva.", this);
                return;
            }

            GameSaveManager.Instance.Save();
        }

        private void ArriveAt(string roomName, string entryPointId)
        {
            if (string.IsNullOrWhiteSpace(entryPointId)) return;

            RoomEntryPoint entryPoint = FindEntryPoint(entryPointId);
            if (entryPoint == null)
            {
                Debug.LogError($"[RoomNavigator] A sala '{roomName}' não tem o ponto de entrada '{entryPointId}'. " +
                               "O jogador chega na posição em que a cena foi salva.", this);
                return;
            }

            CenterPanOn(entryPoint);
        }

        private static RoomEntryPoint FindEntryPoint(string entryPointId)
        {
            return FindObjectsByType<RoomEntryPoint>(FindObjectsSortMode.None)
                .FirstOrDefault(entryPoint => entryPoint.Id == entryPointId);
        }

        private static void CenterPanOn(RoomEntryPoint entryPoint)
        {
            ScreenPanController pan = FindFirstObjectByType<ScreenPanController>();
            if (pan == null) return;

            pan.CenterOn(entryPoint.transform.position.x);
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
