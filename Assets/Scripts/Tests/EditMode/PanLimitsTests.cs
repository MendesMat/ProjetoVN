using System;
using NUnit.Framework;
using ProjetoVN.PointNClick;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class PanLimitsTests
    {
        private const float Tolerance = 0.0001f;
        private const float HalfViewAt16By9 = 8.8889f;
        private const float HalfViewAt21By9 = 11.8519f;

        [Test]
        public void For_BackgroundWiderThanView_LimitsAtImageEdges()
        {
            PanRange range = PanLimits.For(-31.35f, 31.35f, 0f, HalfViewAt16By9);

            Assert.That(range.Min, Is.EqualTo(-22.4611f).Within(Tolerance));
            Assert.That(range.Max, Is.EqualTo(22.4611f).Within(Tolerance));
        }

        [Test]
        public void For_WiderScreen_NarrowsTheRange()
        {
            PanRange range = PanLimits.For(-31.35f, 31.35f, 0f, HalfViewAt21By9);

            Assert.That(range.Min, Is.EqualTo(-19.4981f).Within(Tolerance));
            Assert.That(range.Max, Is.EqualTo(19.4981f).Within(Tolerance));
        }

        [Test]
        public void For_BackgroundNarrowerThanView_CentersTheImage()
        {
            PanRange range = PanLimits.For(-10.45f, 10.45f, 0f, HalfViewAt21By9);

            Assert.That(range.Min, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(range.Max, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void For_OffCenterNarrowBackground_CentersOnTheImage()
        {
            PanRange range = PanLimits.For(2f, 10f, 0f, HalfViewAt16By9);

            Assert.That(range.Min, Is.EqualTo(-6f).Within(Tolerance));
            Assert.That(range.Max, Is.EqualTo(-6f).Within(Tolerance));
        }

        [Test]
        public void For_CameraNotAtOrigin_ShiftsTheRange()
        {
            const float cameraX = 3f;
            PanRange atOrigin = PanLimits.For(-31.35f, 31.35f, 0f, HalfViewAt16By9);

            PanRange shifted = PanLimits.For(-31.35f, 31.35f, cameraX, HalfViewAt16By9);

            Assert.That(shifted.Min, Is.EqualTo(atOrigin.Min + cameraX).Within(Tolerance));
            Assert.That(shifted.Max, Is.EqualTo(atOrigin.Max + cameraX).Within(Tolerance));
        }

        [Test]
        public void For_BackgroundExactlyAsWideAsView_HasNoMovement()
        {
            PanRange range = PanLimits.For(-HalfViewAt16By9, HalfViewAt16By9, 0f, HalfViewAt16By9);

            Assert.That(range.Min, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(range.Max, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Clamp_KeepsValuesInsideTheRange()
        {
            PanRange range = PanLimits.For(-31.35f, 31.35f, 0f, HalfViewAt16By9);

            Assert.That(range.Clamp(-100f), Is.EqualTo(range.Min));
            Assert.That(range.Clamp(5f), Is.EqualTo(5f));
            Assert.That(range.Clamp(100f), Is.EqualTo(range.Max));
        }

        [Test]
        public void For_InvertedEdges_Throws()
        {
            Assert.Throws<ArgumentException>(() => PanLimits.For(5f, -5f, 0f, HalfViewAt16By9));
        }

        [Test]
        public void For_NegativeHalfWidth_Throws()
        {
            Assert.Throws<ArgumentException>(() => PanLimits.For(-5f, 5f, 0f, -1f));
        }
    }
}
