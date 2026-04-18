using System;

namespace Assets.Scripts.Core.StateMachine
{
    public interface IStateMachine
    {
        BaseState CurrentState { get; }
        event Action<BaseState, BaseState> OnStateChanged;

        void ChangeState(BaseState newState, bool saveToHistory = false);
        void PopState();
        void RegisterState<T>(IStateFactory factory) where T : BaseState;
        T GetOrCreateState<T>() where T : BaseState;
    }
}
