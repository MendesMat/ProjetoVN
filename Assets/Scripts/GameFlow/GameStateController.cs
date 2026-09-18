using ProjetoVN.Core.Messaging;
using ProjetoVN.Core.StateMachine;
using ProjetoVN.Dialogue.Messaging;
using ProjetoVN.GameFlow.States;
using UnityEngine;

namespace ProjetoVN.GameFlow
{
    public sealed class GameStateController : MonoBehaviour
    {
        private readonly StateMachine _stateMachine = new();

        private GameplayState _gameplayState;
        private DialogueState _dialogueState;

        private void Awake()
        {
            _gameplayState = new GameplayState(_stateMachine);
            _dialogueState = new DialogueState(_stateMachine);
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
            UnsubscribeFromMessages();
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
