using NUnit.Framework;
using ProjetoVN.Dialogue.Logic;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ScriptNodeNameTests
    {
        [TestCase("porta_trancada")]
        [TestCase("cap1_dd1")]
        [TestCase("_rascunho")]
        public void FollowsConvention_LowercaseDigitsAndUnderscore_ReturnsTrue(string name)
        {
            Assert.IsTrue(ScriptNodeName.FollowsConvention(name));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Porta")]
        [TestCase("porta trancada")]
        [TestCase("porta-trancada")]
        [TestCase("ação")]
        [TestCase("1porta")]
        public void FollowsConvention_OffConvention_ReturnsFalse(string name)
        {
            Assert.IsFalse(ScriptNodeName.FollowsConvention(name));
        }
    }
}
