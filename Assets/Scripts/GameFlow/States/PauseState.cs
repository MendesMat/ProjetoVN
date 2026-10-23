using ProjetoVN.Core.StateMachine;
using ProjetoVN.Dialogue.Logic;
using ProjetoVN.PointNClick;
using UnityEngine;

namespace ProjetoVN.GameFlow.States
{
    /// <summary>
    /// Modo sobreposto da pausa (<c>Push</c>/<c>Pop</c>): fecha o input do mundo, bloqueia a entrada do diálogo e
    /// congela o tempo do jogo. Ao sair devolve o tempo e o diálogo; o input é do modo que volta a ficar por cima.
    /// </summary>
    public sealed class PauseState : BaseState
    {
        private float _timeScaleBeforePause = 1f;

        public PauseState(StateMachine stateMachine) : base(stateMachine) { }

        public override void Enter()
        {
            PlayerInputGate.SetEnabled(false);
            SetDialoguePaused(true);

            _timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
        }

        public override void Exit()
        {
            Time.timeScale = _timeScaleBeforePause;
            SetDialoguePaused(false);
        }

        private static void SetDialoguePaused(bool isPaused)
        {
            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[PauseState] Não há DialogueManager: a fala pode avançar com o jogo pausado.");
                return;
            }

            DialogueManager.Instance.SetPaused(isPaused);
        }
    }
}
