namespace ProjetoVN.Core.StateMachine
{
    public abstract class BaseState
    {
        protected StateMachine StateMachine { get; }

        protected BaseState(StateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void Exit() { }
    }
}
