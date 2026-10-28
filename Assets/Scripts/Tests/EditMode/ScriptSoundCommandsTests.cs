using NUnit.Framework;
using ProjetoVN.Core.State;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>O que o roteiro faz com os comandos de som, visto pelo <see cref="ScriptRun"/>: o comando chega ao jogo e a conversa segue.</summary>
    public sealed class ScriptSoundCommandsTests
    {
        [SetUp]
        public void SetUp() => StoryState.ClearAll();

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        [TestCase("tocar_musica teste_musica_b")]
        [TestCase("tocar_efeito freada_batida")]
        [TestCase("tocar_ambiente onibus_estrada")]
        [TestCase("parar_ambiente")]
        public void SoundCommand_ReachesTheGameWithItsText_AndTheConversationGoesOn(string command)
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
