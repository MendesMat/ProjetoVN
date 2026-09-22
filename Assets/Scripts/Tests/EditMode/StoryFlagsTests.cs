using System.Collections.Generic;
using NUnit.Framework;
using ProjetoVN.Core.State;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class StoryFlagsTests
    {
        #region Setup
        // StoryFlags é estático e compartilhado entre testes, igual ao MessageBroker.
        [SetUp]
        public void SetUp() => StoryFlags.ClearAll();

        [TearDown]
        public void TearDown() => StoryFlags.ClearAll();
        #endregion

        #region Leitura e escrita
        [Test]
        public void Set_ThenIsSet_ReturnsTrue()
        {
            Assert.IsTrue(StoryFlags.Set("falou-com-gotica"), "a primeira vez que uma flag é ligada conta como mudança");
            Assert.IsTrue(StoryFlags.IsSet("falou-com-gotica"));
        }

        [Test]
        public void Set_Twice_ReportsChangeOnlyOnce()
        {
            StoryFlags.Set("porta-destrancada");

            Assert.IsFalse(StoryFlags.Set("porta-destrancada"),
                "repetir um efeito num diálogo rejogado não pode parecer uma mudança nova");
            Assert.IsTrue(StoryFlags.IsSet("porta-destrancada"));
        }

        [Test]
        public void Clear_AfterSet_RemovesTheFlag()
        {
            StoryFlags.Set("porta-destrancada");

            Assert.IsTrue(StoryFlags.Clear("porta-destrancada"));
            Assert.IsFalse(StoryFlags.IsSet("porta-destrancada"));
        }

        [Test]
        public void Clear_WhenNotSet_ReportsNoChange()
        {
            Assert.IsFalse(StoryFlags.Clear("nunca-foi-ligada"));
        }

        [Test]
        public void IsSet_ForAnUnknownFlag_ReturnsFalse()
        {
            StoryFlags.Set("uma-flag");

            Assert.IsFalse(StoryFlags.IsSet("outra-flag"),
                "uma flag que nunca foi ligada não pode destravar conteúdo por engano");
        }
        #endregion

        #region Entradas inválidas
        [Test]
        public void IsSet_WithNullOrWhitespace_ReturnsFalse()
        {
            Assert.IsFalse(StoryFlags.IsSet(null));
            Assert.IsFalse(StoryFlags.IsSet(""));
            Assert.IsFalse(StoryFlags.IsSet("   "));
        }

        [Test]
        public void Set_WithNullOrWhitespace_StoresNothing()
        {
            Assert.IsFalse(StoryFlags.Set(null));
            Assert.IsFalse(StoryFlags.Set(""));
            Assert.IsFalse(StoryFlags.Set("   "));

            Assert.IsEmpty(StoryFlags.All, "um campo vazio no Inspector não pode virar uma flag fantasma");
        }
        #endregion

        #region Restauração
        [Test]
        public void ReplaceAll_DiscardsPreviousFlags()
        {
            StoryFlags.Set("da-sessao-antiga");

            StoryFlags.ReplaceAll(new List<string> { "do-save" });

            Assert.IsFalse(StoryFlags.IsSet("da-sessao-antiga"),
                "carregar um save não pode manter flags da sessão anterior");
            Assert.IsTrue(StoryFlags.IsSet("do-save"));
        }

        [Test]
        public void ReplaceAll_WithNull_EmptiesTheSet_WithoutThrowing()
        {
            StoryFlags.Set("da-sessao-antiga");

            Assert.DoesNotThrow(() => StoryFlags.ReplaceAll(null),
                "um save gravado antes do campo storyFlagIds existir precisa carregar limpo");
            Assert.IsEmpty(StoryFlags.All);
        }

        [Test]
        public void ReplaceAll_SkipsInvalidEntries()
        {
            StoryFlags.ReplaceAll(new List<string> { "valida", null, "", "   " });

            Assert.AreEqual(1, StoryFlags.All.Count);
            Assert.IsTrue(StoryFlags.IsSet("valida"));
        }

        [Test]
        public void ClearAll_EmptiesEverything()
        {
            StoryFlags.Set("uma");
            StoryFlags.Set("outra");

            StoryFlags.ClearAll();

            Assert.IsEmpty(StoryFlags.All, "Reset Session precisa apagar as flags, senão elas sobrevivem escondidas");
        }
        #endregion
    }
}
