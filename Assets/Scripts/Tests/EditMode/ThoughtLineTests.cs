using NUnit.Framework;
using ProjetoVN.Dialogue.Logic;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ThoughtLineTests
    {
        [Test]
        public void ReservedName_IsPensamento()
        {
            Assert.AreEqual("Pensamento", ThoughtLine.ReservedName);
        }

        [Test]
        public void Is_TrueForTheReservedName()
        {
            Assert.IsTrue(ThoughtLine.Is("Pensamento"));
        }

        [Test]
        public void Is_FalseInLowercase()
        {
            Assert.IsFalse(ThoughtLine.Is("pensamento"));
        }

        [Test]
        public void Is_FalseWithATrailingSpace()
        {
            Assert.IsFalse(ThoughtLine.Is("Pensamento "));
        }

        [Test]
        public void Is_FalseForNarrationAndForACharacter()
        {
            Assert.IsFalse(ThoughtLine.Is(null));
            Assert.IsFalse(ThoughtLine.Is(""));
            Assert.IsFalse(ThoughtLine.Is("Protagonista"));
        }
    }
}
