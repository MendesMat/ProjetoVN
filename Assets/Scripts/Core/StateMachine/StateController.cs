using UnityEngine;
using Assets.Scripts.Core.StateMachine.States;
using Assets.Scripts.Core.Messaging;
using Assets.Scripts.Core.Messaging.Messages;

namespace Assets.Scripts.Core.StateMachine
{
    public class StateController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _stateMachineRef;
        private IStateMachine stateMachine => _stateMachineRef as IStateMachine;

        private void Awake()
        {
            if (stateMachine == null)
            {
                Debug.LogError(
                    $"[StateController] O campo '_stateMachineRef' precisa referenciar um componente que implemente IStateMachine. " +
                    $"Atribua-o no Inspector do GameObject '{gameObject.name}'.");
                return;
            }

            RegisterFactories();
        }

        private void Start()
        {
            stateMachine.OnStateChanged += OnStateChangedHandler;
            MessageBroker.Subscribe<StateChangedMessage>(OnStateChangedMessageHandler);
            OnOpenMenu();
        }

        private void OnDestroy()
        {
            stateMachine.OnStateChanged -= OnStateChangedHandler;
            MessageBroker.Unsubscribe<StateChangedMessage>(OnStateChangedMessageHandler);
        }

        private void RegisterFactories()
        {
            stateMachine.RegisterState<MenuState>(new StateFactory<MenuState>());
            stateMachine.RegisterState<InventoryState>(new StateFactory<InventoryState>());
            stateMachine.RegisterState<DialogueState>(new StateFactory<DialogueState>());
        }

        public void OnOpenMenu()
        {
            stateMachine.ChangeState(stateMachine.GetOrCreateState<MenuState>());
        }

        public void OnOpenInventory()
        {
            stateMachine.ChangeState(stateMachine.GetOrCreateState<InventoryState>());
        }

        public void OnStartDialogue()
        {
            stateMachine.ChangeState(stateMachine.GetOrCreateState<DialogueState>());
        }

        private void OnStateChangedHandler(BaseState previous, BaseState next)
        {
            string from = previous != null ? previous.GetType().Name : "Nenhum";
            UnityEngine.Debug.Log($"[StateMachine] {from} → {next.GetType().Name}");
        }

        private void OnStateChangedMessageHandler(StateChangedMessage message)
        {
            string from = message.Previous != null ? message.Previous.GetType().Name : "Nenhum";
            UnityEngine.Debug.Log($"[MessageBroker] StateChanged: {from} → {message.Next.GetType().Name}");
        }
    }
}
