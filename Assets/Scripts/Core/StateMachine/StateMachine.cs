using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Core.StateMachine
{
    public sealed class StateMachine
    {
        private readonly Stack<BaseState> _overlayStack = new();

        public BaseState CurrentState { get; private set; }

        public void ChangeState(BaseState next)
        {
            if (next == null)
            {
                Debug.LogError("[StateMachine] ChangeState recebeu null. Troca ignorada.");
                return;
            }

            if (next == CurrentState) return;

            Transition(next);
        }

        public void Push(BaseState overlay)
        {
            if (overlay == null)
            {
                Debug.LogError("[StateMachine] Push recebeu null. Operação ignorada.");
                return;
            }

            if (CurrentState != null) _overlayStack.Push(CurrentState);

            Transition(overlay);
        }

        public void Pop()
        {
            if (_overlayStack.Count == 0)
            {
                Debug.LogWarning("[StateMachine] Pop sem nenhum estado empilhado. Operação ignorada.");
                return;
            }

            Transition(_overlayStack.Pop());
        }

        public void Tick() => CurrentState?.Update();

        private void Transition(BaseState next)
        {
            BaseState previous = CurrentState;
            previous?.Exit();

            CurrentState = next;
            CurrentState.Enter();

            TraceTransition(previous, next);
        }

        [System.Diagnostics.Conditional("VN_TRACE_MESSAGES")]
        private static void TraceTransition(BaseState previous, BaseState next)
        {
            Debug.Log($"[StateMachine] {(previous != null ? previous.GetType().Name : "Nenhum")} → {next.GetType().Name}");
        }
    }
}
