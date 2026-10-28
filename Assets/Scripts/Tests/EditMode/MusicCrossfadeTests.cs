using System;
using NUnit.Framework;
using ProjetoVN.Audio;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class MusicCrossfadeTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void ZeroSeconds_IsDoneAtOnce()
        {
            var crossfade = new MusicCrossfade(0f, 0f, 1f);

            Assert.That(crossfade.IsDone, Is.True);
            Assert.That(crossfade.IncomingVolume, Is.EqualTo(1f));
            Assert.That(crossfade.OutgoingVolume, Is.EqualTo(0f));
        }

        [Test]
        public void BeforeAdvancing_VolumesAreTheStartingOnes()
        {
            var crossfade = new MusicCrossfade(1f, 0f, 1f);

            Assert.That(crossfade.IsDone, Is.False);
            Assert.That(crossfade.IncomingVolume, Is.EqualTo(0f));
            Assert.That(crossfade.OutgoingVolume, Is.EqualTo(1f));
        }

        [Test]
        public void Halfway_BothVolumesAreEqualPower()
        {
            var crossfade = new MusicCrossfade(2f, 0f, 1f);

            crossfade.Advance(1f);

            float equalPower = (float)System.Math.Sqrt(0.5);
            Assert.That(crossfade.IncomingVolume, Is.EqualTo(equalPower).Within(Tolerance));
            Assert.That(crossfade.OutgoingVolume, Is.EqualTo(equalPower).Within(Tolerance));
            Assert.That(crossfade.IsDone, Is.False);
        }

        [Test]
        public void AfterTheDuration_IncomingIsFullAndOutgoingIsSilent()
        {
            var crossfade = new MusicCrossfade(2f, 0f, 1f);

            crossfade.Advance(2f);

            Assert.That(crossfade.IsDone, Is.True);
            Assert.That(crossfade.IncomingVolume, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(crossfade.OutgoingVolume, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void AdvancingPastTheEnd_ClampsTheVolumes()
        {
            var crossfade = new MusicCrossfade(2f, 0f, 1f);

            crossfade.Advance(2f * 2f);

            Assert.That(crossfade.IncomingVolume, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(crossfade.OutgoingVolume, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void ManySmallSteps_EqualOneBigStep()
        {
            var oneStep = new MusicCrossfade(1f, 0f, 1f);
            var manySteps = new MusicCrossfade(1f, 0f, 1f);

            oneStep.Advance(0.5f);
            for (int step = 0; step < 5; step++) manySteps.Advance(0.5f / 5);

            Assert.That(manySteps.IncomingVolume, Is.EqualTo(oneStep.IncomingVolume).Within(Tolerance));
            Assert.That(manySteps.OutgoingVolume, Is.EqualTo(oneStep.OutgoingVolume).Within(Tolerance));
        }

        [Test]
        public void InterruptedChange_StartsFromTheCurrentVolumes()
        {
            var crossfade = new MusicCrossfade(1f, 0.4f, 0.6f);

            Assert.That(crossfade.IncomingVolume, Is.EqualTo(0.4f).Within(Tolerance));
            Assert.That(crossfade.OutgoingVolume, Is.EqualTo(0.6f).Within(Tolerance));

            crossfade.Advance(1f);

            Assert.That(crossfade.IncomingVolume, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(crossfade.OutgoingVolume, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void NegativeSeconds_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MusicCrossfade(-1f, 0f, 1f));
        }
    }
}
