using NUnit.Framework;
using ProjetoVN.Core.State;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>O que o roteiro faz com os comandos e a função de item, visto pelo <see cref="ScriptRun"/>.</summary>
    public sealed class ScriptItemCommandsTests
    {
        private const string ConditionalOnItem =
            "title: no\n---\n<<if tem_item(\"chave\")>>\ncom a chave\n<<endif>>\nfim\n===\n";

        [SetUp]
        public void SetUp() => StoryState.ClearAll();

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        private static ScriptRun Compile(string source)
        {
            ScriptRun run = ScriptRun.FromText(source);
            Assert.IsEmpty(run.ErrorsAndWarnings, "o roteiro do teste precisa compilar sem erro nem aviso");
            return run;
        }

        [Test]
        public void HasItem_IsFalse_WhenThePlayerDoesNotOwnTheItem()
        {
            ScriptRun run = Compile(ConditionalOnItem);

            run.Start("no");

            CollectionAssert.AreEqual(new[] { "fim" }, run.Lines);
        }

        [Test]
        public void HasItem_IsTrue_WhenThePlayerOwnsTheItem()
        {
            ScriptRun run = Compile(ConditionalOnItem);
            run.OwnedItems.Add("chave");

            run.Start("no");

            CollectionAssert.AreEqual(new[] { "com a chave", "fim" }, run.Lines);
        }

        [TestCase("dar_item")]
        [TestCase("remover_item")]
        public void ItemCommand_ReachesTheGameWithItsId_AndTheConversationGoesOn(string command)
        {
            ScriptRun run = Compile($"title: no\n---\n<<{command} chave_teste>>\nfim\n===\n");

            run.Start("no");

            CollectionAssert.AreEqual(new[] { $"{command} chave_teste" }, run.Commands);
            CollectionAssert.AreEqual(new[] { "fim" }, run.Lines);
            Assert.IsTrue(run.Completed);
        }
    }
}
