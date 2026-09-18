using ProjetoVN.Core.StateMachine;
using ProjetoVN.PointNClick;

namespace ProjetoVN.GameFlow.States
{
    public sealed class GameplayState : BaseState
    {
        public GameplayState(StateMachine stateMachine) : base(stateMachine) { }

        public override void Enter() => PlayerInputGate.SetEnabled(true);
    }
}
