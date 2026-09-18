using ProjetoVN.Core.StateMachine;
using ProjetoVN.PointNClick;

namespace ProjetoVN.GameFlow.States
{
    public sealed class DialogueState : BaseState
    {
        public DialogueState(StateMachine stateMachine) : base(stateMachine) { }

        public override void Enter() => PlayerInputGate.SetEnabled(false);
    }
}
