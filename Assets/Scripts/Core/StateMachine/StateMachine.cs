using System;
using System.Collections.Generic;
using Assets.Scripts.Core.Messaging;
using Assets.Scripts.Core.Messaging.Messages;
using UnityEngine;

namespace Assets.Scripts.Core.StateMachine
{
    internal class StateMachine : MonoBehaviour, IStateMachine
    {
        public BaseState CurrentState { get; private set; }
        public event Action<BaseState, BaseState> OnStateChanged;

        private readonly Stack<BaseState> stateHistory = new();
        private readonly Dictionary<Type, BaseState> stateCache = new();
        private readonly Dictionary<Type, IStateFactory> stateFactories = new();

        private void Update()
        {
            CurrentState?.Update();
        }

        private void FixedUpdate()
        {
            CurrentState?.FixedUpdate();
        }

        public void RegisterState<T>(IStateFactory factory) where T : BaseState
        {
            stateFactories[typeof(T)] = factory;
        }

        public T GetOrCreateState<T>() where T : BaseState
        {
            Type stateType = typeof(T);

            if (stateCache.ContainsKey(stateType))
                return (T)stateCache[stateType];

            if (!stateFactories.TryGetValue(stateType, out IStateFactory factory))
                throw new InvalidOperationException(
                    $"[StateMachine] Nenhuma fábrica registrada para '{stateType.Name}'. " +
                    $"Chame RegisterState<{stateType.Name}>() antes de usar GetOrCreateState.");

            stateCache[stateType] = factory.Create(this);
            return (T)stateCache[stateType];
        }

        public void ChangeState(BaseState newState, bool saveToHistory = false)
        {
            BaseState previousState = CurrentState;

            if (previousState != null)
            {
                if (saveToHistory)
                    stateHistory.Push(previousState);

                previousState.Exit();
            }

            CurrentState = newState;
            CurrentState.Enter();

            OnStateChanged?.Invoke(previousState, CurrentState);
            MessageBroker.Publish(new StateChangedMessage(previousState, CurrentState));
        }

        public void PopState()
        {
            if (stateHistory.Count > 0)
                ChangeState(stateHistory.Pop());
        }
    }
}
