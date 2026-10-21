using System;
using NUnit.Framework;
using ProjetoVN.GameFlow.FrameRate;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class UnfocusedFrameRateTests
    {
        private static readonly FrameRateSettings BuildSettings = new FrameRateSettings(1, -1);
        private static readonly FrameRateSettings EditorSettings = new FrameRateSettings(0, 60);

        private UnfocusedFrameRate _limit;

        [SetUp]
        public void SetUp() => _limit = new UnfocusedFrameRate();

        [Test]
        public void Limit_TurnsVSyncOffAndCapsAtTheGivenFrameRate()
        {
            var limited = _limit.Limit(BuildSettings, 30);

            Assert.That(limited.VSyncCount, Is.EqualTo(0));
            Assert.That(limited.TargetFrameRate, Is.EqualTo(30));
        }

        [Test]
        public void TryRestore_AfterLimit_GivesBackTheSettingsFromBeforeTheLimit()
        {
            _limit.Limit(BuildSettings, 30);

            var restored = _limit.TryRestore(out var original);

            Assert.That(restored, Is.True);
            Assert.That(original.VSyncCount, Is.EqualTo(BuildSettings.VSyncCount));
            Assert.That(original.TargetFrameRate, Is.EqualTo(BuildSettings.TargetFrameRate));
        }

        [Test]
        public void TryRestore_WithoutLimit_ReturnsFalse()
        {
            Assert.That(_limit.TryRestore(out _), Is.False);
        }

        [Test]
        public void Limit_Twice_KeepsTheFirstOriginal()
        {
            var firstLimit = _limit.Limit(BuildSettings, 30);
            _limit.Limit(firstLimit, 30);

            _limit.TryRestore(out var original);

            Assert.That(original.VSyncCount, Is.EqualTo(BuildSettings.VSyncCount));
            Assert.That(original.TargetFrameRate, Is.EqualTo(BuildSettings.TargetFrameRate));
        }

        [Test]
        public void TryRestore_Twice_RestoresOnlyOnce()
        {
            _limit.Limit(BuildSettings, 30);
            _limit.TryRestore(out _);

            Assert.That(_limit.TryRestore(out _), Is.False);
        }

        [Test]
        public void TryRestore_WhenNotLimiting_GivesDefaultSettings()
        {
            _limit.Limit(BuildSettings, 30);
            _limit.TryRestore(out _);

            var restored = _limit.TryRestore(out var original);

            Assert.That(restored, Is.False);
            Assert.That(original.VSyncCount, Is.EqualTo(default(FrameRateSettings).VSyncCount));
            Assert.That(original.TargetFrameRate, Is.EqualTo(default(FrameRateSettings).TargetFrameRate));
        }

        [Test]
        public void Limit_AfterARestore_SavesTheNewCurrentSettings()
        {
            _limit.Limit(BuildSettings, 30);
            _limit.TryRestore(out _);
            _limit.Limit(EditorSettings, 30);

            _limit.TryRestore(out var original);

            Assert.That(original.VSyncCount, Is.EqualTo(EditorSettings.VSyncCount));
            Assert.That(original.TargetFrameRate, Is.EqualTo(EditorSettings.TargetFrameRate));
        }

        [Test]
        public void IsLimiting_IsTrueOnlyBetweenLimitAndRestore()
        {
            Assert.That(_limit.IsLimiting, Is.False);

            _limit.Limit(BuildSettings, 30);
            Assert.That(_limit.IsLimiting, Is.True);

            _limit.TryRestore(out _);
            Assert.That(_limit.IsLimiting, Is.False);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Limit_WithFrameRateBelowOne_Throws(int maxFrameRate)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _limit.Limit(BuildSettings, maxFrameRate));
        }
    }
}
