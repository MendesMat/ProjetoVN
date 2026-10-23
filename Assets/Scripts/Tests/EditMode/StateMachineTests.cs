using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjetoVN.Core.StateMachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class StateMachineTests
    {
        private StateMachine _machine;
        private List<string> _calls;
        private RecordingState _gameplay;
        private RecordingState _overlay;

        [SetUp]
        public void SetUp()
        {
            _machine = new StateMachine();
            _calls = new List<string>();
            _gameplay = new RecordingState(_machine, "gameplay", _calls);
            _overlay = new RecordingState(_machine, "overlay", _calls);
        }

        [Test]
        public void Push_ExitsTheCoveredStateAndEntersTheOverlay()
        {
            _machine.ChangeState(_gameplay);
            _calls.Clear();

            _machine.Push(_overlay);

            CollectionAssert.AreEqual(new[] { "gameplay.Exit", "overlay.Enter" }, _calls);
            Assert.AreSame(_overlay, _machine.CurrentState);
        }

        [Test]
        public void Pop_ExitsTheOverlayAndReentersTheCoveredState()
        {
            _machine.ChangeState(_gameplay);
            _machine.Push(_overlay);
            _calls.Clear();

            _machine.Pop();

            CollectionAssert.AreEqual(new[] { "overlay.Exit", "gameplay.Enter" }, _calls);
            Assert.AreSame(_gameplay, _machine.CurrentState);
        }

        [Test]
        public void Pop_WithoutOverlay_WarnsAndKeepsTheCurrentState()
        {
            _machine.ChangeState(_gameplay);
            _calls.Clear();

            LogAssert.Expect(LogType.Warning, new Regex("Pop sem nenhum estado empilhado"));
            _machine.Pop();

            CollectionAssert.IsEmpty(_calls);
            Assert.AreSame(_gameplay, _machine.CurrentState);
        }

        [Test]
        public void ChangeState_ToTheCurrentState_DoesNotReenterIt()
        {
            _machine.ChangeState(_gameplay);
            _calls.Clear();

            _machine.ChangeState(_gameplay);

            CollectionAssert.IsEmpty(_calls);
        }

        private sealed class RecordingState : BaseState
        {
            private readonly string _name;
            private readonly List<string> _calls;

            public RecordingState(StateMachine stateMachine, string name, List<string> calls) : base(stateMachine)
            {
                _name = name;
                _calls = calls;
            }

            public override void Enter() => _calls.Add($"{_name}.Enter");

            public override void Exit() => _calls.Add($"{_name}.Exit");
        }
    }
}
