using Assets.Scripts.Core.Messaging;
using Assets.Scripts.Core.Messaging.Messages;
using Assets.Scripts.Core.StateMachine;
using Assets.Scripts.Dialogue.Logic;
using Assets.Scripts.Dialogue.Messaging;
using UnityEngine;

namespace Assets.Scripts.GameStates
{
    public sealed class GameStateController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour stateMachineRef;

        private IStateMachine _stateMachine => stateMachineRef as IStateMachine;

        private void Awake()
        {
            if (_stateMachine == null)
            {
                Debug.LogError(
                    $"[GameStateController] O campo 'stateMachineRef' em '{gameObject.name}' " +
                    $"precisa referenciar um componente que implemente IStateMachine.");
                return;
            }

            RegisterStates();
        }

        private void Start()
        {
            SubscribeToMessages();
            EnterGameplay();
        }

        private void OnDestroy()
        {
            UnsubscribeFromMessages();
        }

        private void RegisterStates()
        {
            _stateMachine.RegisterState<GameplayState>(new StateFactory<GameplayState>());
            _stateMachine.RegisterState<DialogueState>(new StateFactory<DialogueState>());
        }

        private void SubscribeToMessages()
        {
            MessageBroker.Subscribe<DialogueRequestMessage>(OnDialogueRequested);
            MessageBroker.Subscribe<DialogueEndedMessage>(OnDialogueEnded);
        }

        private void UnsubscribeFromMessages()
        {
            MessageBroker.Unsubscribe<DialogueRequestMessage>(OnDialogueRequested);
            MessageBroker.Unsubscribe<DialogueEndedMessage>(OnDialogueEnded);
        }

        private void OnDialogueRequested(DialogueRequestMessage message)
        {
            Debug.Log($"[GameStateController] ← DialogueRequestMessage: '{message.Data.name}'");
            DialogueManager.Instance.StartDialogue(message.Data);
            EnterDialogue();
        }

        private void OnDialogueEnded(DialogueEndedMessage _)
        {
            Debug.Log("[GameStateController] ← DialogueEndedMessage: retornando ao Gameplay.");
            EnterGameplay();
        }

        private void EnterGameplay()
        {
            _stateMachine.ChangeState(_stateMachine.GetOrCreateState<GameplayState>());
            Debug.Log("[GameStateController] → Estado: GameplayState | → TogglePlayerInputMessage(true)");
            MessageBroker.Publish(new TogglePlayerInputMessage(true));
        }

        private void EnterDialogue()
        {
            _stateMachine.ChangeState(_stateMachine.GetOrCreateState<DialogueState>());
            Debug.Log("[GameStateController] → Estado: DialogueState | → TogglePlayerInputMessage(false)");
            MessageBroker.Publish(new TogglePlayerInputMessage(false));
        }
    }
}
