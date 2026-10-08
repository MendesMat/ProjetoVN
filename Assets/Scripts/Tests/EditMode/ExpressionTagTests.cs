using System;
using NUnit.Framework;
using ProjetoVN.Dialogue.Logic;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ExpressionTagTests
    {
        [Test]
        public void FirstIn_NoTags_ReturnsNull()
        {
            Assert.IsNull(ExpressionTag.FirstIn(Array.Empty<string>()));
        }

        [Test]
        public void FirstIn_OneTag_ReturnsIt()
        {
            Assert.AreEqual("raiva", ExpressionTag.FirstIn(new[] { "raiva" }));
        }

        [Test]
        public void FirstIn_IgnoresLastLine()
        {
            Assert.AreEqual("raiva", ExpressionTag.FirstIn(new[] { "lastline", "raiva" }));
            Assert.IsNull(ExpressionTag.FirstIn(new[] { "lastline" }));
        }

        [Test]
        public void FirstIn_IgnoresLineIds()
        {
            Assert.IsNull(ExpressionTag.FirstIn(new[] { "line:abc123" }));
        }

        [Test]
        public void AllIn_TwoTags_ReturnsBothInOrder()
        {
            CollectionAssert.AreEqual(new[] { "raiva", "triste" }, ExpressionTag.AllIn(new[] { "raiva", "triste" }));
        }

        [Test]
        public void AllIn_NullMetadata_ReturnsEmpty()
        {
            Assert.IsEmpty(ExpressionTag.AllIn(null));
        }
    }
}
