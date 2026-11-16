using NUnit.Framework;
using ProjetoVN.GameFlow.ScreenEffects;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class AlphaTransitionTests
    {
        private const float Delta = 0.0001f;

        [Test]
        public void ZeroSeconds_ReachesTheTargetOnTheFirstAdvance_AndIsDone()
        {
            var transition = new AlphaTransition(0f, 1f, 0f);

            float alpha = transition.Advance(0.016f);

            Assert.AreEqual(1f, alpha);
            Assert.IsTrue(transition.IsDone);
        }

        [Test]
        public void ZeroSeconds_IsAlreadyDoneAtTheTarget_BeforeAnyAdvance()
        {
            var transition = new AlphaTransition(0f, 1f, 0f);

            Assert.IsTrue(transition.IsDone);
            Assert.AreEqual(1f, transition.Alpha);
        }

        [Test]
        public void NegativeSeconds_BehaveLikeZero()
        {
            var transition = new AlphaTransition(1f, 0f, -1f);

            float alpha = transition.Advance(0.016f);

            Assert.AreEqual(0f, alpha);
            Assert.IsTrue(transition.IsDone);
        }

        [Test]
        public void HalfTheTime_GivesHalfTheWay()
        {
            var transition = new AlphaTransition(0f, 1f, 2f);

            float alpha = transition.Advance(1f);

            Assert.AreEqual(0.5f, alpha, Delta);
            Assert.IsFalse(transition.IsDone);
        }

        [Test]
        public void StartsFromTheCurrentAlpha()
        {
            var transition = new AlphaTransition(0.4f, 1f, 2f);

            float alpha = transition.Advance(1f);

            Assert.AreEqual(0.7f, alpha, Delta);
        }

        [Test]
        public void ALongFrame_EndsExactlyAtTheTarget()
        {
            var transition = new AlphaTransition(1f, 0f, 2f);

            float alpha = transition.Advance(5f);

            Assert.AreEqual(0f, alpha);
            Assert.IsTrue(transition.IsDone);
        }

        [Test]
        public void GoingDown_WorksLikeGoingUp()
        {
            var transition = new AlphaTransition(1f, 0f, 2f);

            float alpha = transition.Advance(0.5f);

            Assert.AreEqual(0.75f, alpha, Delta);
        }

        [Test]
        public void SeveralAdvances_AddUpTheirTime()
        {
            var transition = new AlphaTransition(0f, 1f, 2f);

            transition.Advance(0.5f);
            float alpha = transition.Advance(0.5f);

            Assert.AreEqual(0.5f, alpha, Delta);
        }
    }
}
