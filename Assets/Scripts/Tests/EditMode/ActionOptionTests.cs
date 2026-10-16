using System;
using NUnit.Framework;
using ProjetoVN.Dialogue.Logic;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ActionOptionTests
    {
        [Test]
        public void Tag_IsAcao()
        {
            Assert.AreEqual("acao", ActionOption.Tag);
        }

        [Test]
        public void IsMarkedIn_TrueWithTheTag()
        {
            Assert.IsTrue(ActionOption.IsMarkedIn(new[] { "acao" }));
        }

        [Test]
        public void IsMarkedIn_TrueAmongOtherTags()
        {
            Assert.IsTrue(ActionOption.IsMarkedIn(new[] { "raiva", "acao" }));
        }

        [Test]
        public void IsMarkedIn_FalseWithoutTags()
        {
            Assert.IsFalse(ActionOption.IsMarkedIn(Array.Empty<string>()));
            Assert.IsFalse(ActionOption.IsMarkedIn(null));
        }

        [Test]
        public void IsMarkedIn_FalseForAnExpression()
        {
            Assert.IsFalse(ActionOption.IsMarkedIn(new[] { "raiva" }));
            Assert.IsFalse(ActionOption.IsMarkedIn(new[] { "lastline" }));
        }

        [Test]
        public void IsMarkedIn_FalseForOtherSpellings()
        {
            Assert.IsFalse(ActionOption.IsMarkedIn(new[] { "Acao" }));
            Assert.IsFalse(ActionOption.IsMarkedIn(new[] { "ação" }));
            Assert.IsFalse(ActionOption.IsMarkedIn(new[] { "acoes" }));
        }
    }
}
