using NUnit.Framework;
using ProjetoVN.Core.State;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>O que o roteiro faz com os comandos de tela, visto pelo <see cref="ScriptRun"/>: o comando chega ao jogo e a conversa segue.</summary>
    public sealed class ScriptScreenCommandsTests
    {
        [SetUp]
        public void SetUp() => StoryState.ClearAll();

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        [TestCase("escurecer")]
        [TestCase("escurecer 2.5")]
        [TestCase("clarear")]
        [TestCase("clarear 0")]
        [TestCase("mostrar_imagem lembranca_pais")]
        [TestCase("mostrar_imagem lembranca_pais 1.5")]
        [TestCase("esconder_imagem")]
        [TestCase("esconder_imagem 2")]
        public void ScreenCommand_ReachesTheGameWithItsText_AndTheConversationGoesOn(string command)
        {
            ScriptRun run = ScriptRun.FromText($"title: no\n---\n<<{command}>>\nfim\n===\n");
            Assert.IsEmpty(run.ErrorsAndWarnings, "o roteiro do teste precisa compilar sem erro nem aviso");

            run.Start("no");

            CollectionAssert.AreEqual(new[] { command }, run.Commands);
            CollectionAssert.AreEqual(new[] { "fim" }, run.Lines);
            Assert.IsTrue(run.Completed);
        }
    }
}
