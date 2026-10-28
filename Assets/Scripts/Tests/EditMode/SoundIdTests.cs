using NUnit.Framework;
using ProjetoVN.Audio;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class SoundIdTests
    {
        [TestCase("freada")]
        [TestCase("onibus_estrada")]
        [TestCase("som_2")]
        [TestCase("_rascunho")]
        public void FollowsConvention_LowercaseDigitsAndUnderscore_ReturnsTrue(string id)
        {
            Assert.IsTrue(SoundId.FollowsConvention(id));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("Freada")]
        [TestCase("ônibus")]
        [TestCase("som-2")]
        [TestCase("$freada")]
        [TestCase(" freada")]
        [TestCase("freada ")]
        [TestCase("2freada")]
        public void FollowsConvention_OffConvention_ReturnsFalse(string id)
        {
            Assert.IsFalse(SoundId.FollowsConvention(id));
        }
    }
}
