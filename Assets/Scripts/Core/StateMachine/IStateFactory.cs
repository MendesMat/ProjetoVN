using System;

namespace Assets.Scripts.Core.StateMachine
{
    public interface IStateFactory
    {
        BaseState Create(IStateMachine stateMachine);
    }
}
