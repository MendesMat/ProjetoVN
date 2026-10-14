using NUnit.Framework;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class BubbleTextColorTests
    {
        private static readonly Color LunaBubble = new Color32(0xD9, 0xA6, 0xFF, 0xFF);
        private static readonly Color ProtagonistBubble = new Color32(0xFF, 0xF3, 0xFB, 0xFF);
        private static readonly Color NeutralBubble = new Color32(0x24, 0x11, 0x1F, 0xFF);
        private static readonly Color MidGrayBubble = new Color32(0x77, 0x77, 0x77, 0xFF);

        [Test]
        public void ContrastRatio_OfBlackAndWhite_Is21InBothDirections()
        {
            Assert.AreEqual(21f, BubbleTextColor.ContrastRatio(Color.black, Color.white), 0.01f);
            Assert.AreEqual(21f, BubbleTextColor.ContrastRatio(Color.white, Color.black), 0.01f);
        }

        [Test]
        public void ContrastRatio_OfAColorWithItself_Is1()
        {
            Assert.AreEqual(1f, BubbleTextColor.ContrastRatio(LunaBubble, LunaBubble), 0.001f);
        }

        [Test]
        public void LightBubble_GetsDarkText()
        {
            Assert.AreEqual(BubbleTextColor.Dark, BubbleTextColor.For(ProtagonistBubble));
        }

        [Test]
        public void PastelBubble_GetsDarkTextWithEnoughContrast()
        {
            Color text = BubbleTextColor.For(LunaBubble);

            Assert.AreEqual(BubbleTextColor.Dark, text);
            Assert.GreaterOrEqual(BubbleTextColor.ContrastRatio(text, LunaBubble), BubbleTextColor.MinimumContrast);
        }

        [Test]
        public void DarkBubble_GetsLightText()
        {
            Assert.AreEqual(BubbleTextColor.Light, BubbleTextColor.For(NeutralBubble));
        }

        [Test]
        public void MidGrayBubble_IsNotReadable()
        {
            Assert.IsFalse(BubbleTextColor.IsReadable(MidGrayBubble));
        }

        [Test]
        public void PastelAndDarkBubbles_AreReadable()
        {
            Assert.IsTrue(BubbleTextColor.IsReadable(LunaBubble));
            Assert.IsTrue(BubbleTextColor.IsReadable(NeutralBubble));
        }

        [Test]
        public void For_IgnoresTheAlphaOfTheBubble()
        {
            Color translucentLuna = new Color(LunaBubble.r, LunaBubble.g, LunaBubble.b, 0.2f);

            Assert.AreEqual(BubbleTextColor.For(LunaBubble), BubbleTextColor.For(translucentLuna));
        }
    }
}
