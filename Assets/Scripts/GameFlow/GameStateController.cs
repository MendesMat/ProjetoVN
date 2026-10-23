using ProjetoVN.Core.Messaging;
using ProjetoVN.Core.StateMachine;
using ProjetoVN.Dialogue.Messaging;
using ProjetoVN.GameFlow.States;
using UnityEngine;

namespace ProjetoVN.GameFlow
{
    public sealed class GameStateController : MonoBehaviour
    {
        public static GameStateController Instance { get; private set; }

        private readonly StateMachine _stateMachine = new();

        private GameplayState _gameplayState;
        private DialogueState _dialogueState;
        private RoomTransitionState _roomTransitionState;
        private PauseState _pauseState;

        internal bool IsExploring => _stateMachine.CurrentState == _gameplayState;

        internal bool IsPaused => _stateMachine.CurrentState == _pauseState;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => Instance = null;

        private void Awake()
        {
            Instance = this;
            _gameplayState = new GameplayState(_stateMachine);
            _dialogueState = new DialogueState(_stateMachine);
            _roomTransitionState = new RoomTransitionState(_stateMachine);
            _pauseState = new PauseState(_stateMachine);
        }

        private void Start()
        {
            SubscribeToMessages();
            _stateMachine.ChangeState(_gameplayState);
        }

        private void Update()
        {
            _stateMachine.Tick();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnsubscribeFromMessages();
        }

        internal void EnterRoomTransition() => _stateMachine.ChangeState(_roomTransitionState);

        internal void LeaveRoomTransition() => _stateMachine.ChangeState(_gameplayState);

        /// <summary>A pausa só abre sobre a exploração ou sobre uma conversa: não sobre a troca de sala nem sobre outra pausa.</summary>
        internal bool TryEnterPause()
        {
            bool canPause = _stateMachine.CurrentState == _gameplayState || _stateMachine.CurrentState == _dialogueState;
            if (!canPause) return false;

            _stateMachine.Push(_pauseState);
            return true;
        }

        internal void LeavePause()
        {
            if (IsPaused) _stateMachine.Pop();
        }

        private void SubscribeToMessages()
        {
            MessageBroker.Subscribe<DialogueStartedMessage>(OnDialogueStarted);
            MessageBroker.Subscribe<DialogueEndedMessage>(OnDialogueEnded);
        }

        private void UnsubscribeFromMessages()
        {
            MessageBroker.Unsubscribe<DialogueStartedMessage>(OnDialogueStarted);
            MessageBroker.Unsubscribe<DialogueEndedMessage>(OnDialogueEnded);
        }

        private void OnDialogueStarted(DialogueStartedMessage _) => _stateMachine.ChangeState(_dialogueState);

        private void OnDialogueEnded(DialogueEndedMessage _) => _stateMachine.ChangeState(_gameplayState);
    }
}
