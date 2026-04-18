using System;

namespace Assets.Scripts.Core.StateMachine
{
    public sealed class StateFactory<T> : IStateFactory where T : BaseState
    {
        private readonly Func<IStateMachine, T> factory;

        public StateFactory()
        {
            factory = sm => (T)Activator.CreateInstance(typeof(T), sm);
        }

        public StateFactory(Func<IStateMachine, T> customFactory)
        {
            factory = customFactory;
        }

        public BaseState Create(IStateMachine stateMachine) => factory(stateMachine);
    }
}
