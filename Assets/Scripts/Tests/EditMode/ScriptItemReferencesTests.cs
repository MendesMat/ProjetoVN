using NUnit.Framework;
using ProjetoVN.Core.State;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ScriptItemReferencesTests
    {
        private const string Node = "no_teste";

        [SetUp]
        public void SetUp() => StoryState.ClearAll();

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        private static ScriptItemReferences ReferencesIn(string source)
        {
            ScriptRun run = ScriptRun.FromText(source);
            Assert.IsNotNull(run.Program, "o roteiro do teste precisa compilar");
            return ScriptItemReferences.In(run.Program);
        }

        private static ScriptItemReferences ReferencesInNodeBody(string body) =>
            ReferencesIn($"title: {Node}\n---\n{body}\n===\n");

        private static ScriptItemReference Reference(string source, string argument) => new(Node, source, argument);

        [TestCase("dar_item")]
        [TestCase("remover_item")]
        public void ItemCommand_IsALiteralReference_WithItsNodeAndId(string command)
        {
            ScriptItemReferences references = ReferencesInNodeBody($"<<{command} chave>>\nfim");

            CollectionAssert.AreEqual(new[] { Reference(command, "chave") }, references.Literal);
            Assert.IsEmpty(references.WithoutLiteralId);
        }

        [Test]
        public void QuotedIdInACommand_IsReadWithoutTheQuotes()
        {
            ScriptItemReferences references = ReferencesInNodeBody("<<dar_item \"chave\">>\nfim");

            CollectionAssert.AreEqual(new[] { Reference("dar_item", "chave") }, references.Literal);
        }

        [Test]
        public void HasItemFunction_IsALiteralReference()
        {
            ScriptItemReferences references = ReferencesInNodeBody("<<if tem_item(\"chave\")>>\ntem\n<<endif>>");

            CollectionAssert.AreEqual(new[] { Reference("tem_item", "chave") }, references.Literal);
            Assert.IsEmpty(references.WithoutLiteralId);
        }

        [Test]
        public void TwoFunctionCallsInOneExpression_AreTwoReferences()
        {
            ScriptItemReferences references = ReferencesInNodeBody(
                "<<if not tem_item(\"a\") and tem_item(\"b\")>>\ntem\n<<endif>>");

            CollectionAssert.AreEqual(
                new[] { Reference("tem_item", "a"), Reference("tem_item", "b") }, references.Literal);
        }

        [Test]
        public void OtherCommandsAndFunctions_AreNotReferences()
        {
            ScriptItemReferences references = ReferencesInNodeBody(
                "<<outro_comando chave>>\n<<dar_itens chave>>\n<<if visited(\"no_teste\")>>\nvoltou\n<<endif>>");

            Assert.IsEmpty(references.Literal);
            Assert.IsEmpty(references.WithoutLiteralId);
        }

        [Test]
        public void CommandInsideAComment_IsNotAReference()
        {
            ScriptItemReferences references = ReferencesInNodeBody("// <<dar_item chave>>\nfim");

            Assert.IsEmpty(references.Literal);
            Assert.IsEmpty(references.WithoutLiteralId);
        }

        [Test]
        public void ReferencesInTwoNodes_EachCarryTheirOwnNodeName()
        {
            ScriptItemReferences references = ReferencesIn(
                "title: primeiro\n---\n<<dar_item chave>>\n===\n" +
                "title: segundo\n---\n<<remover_item carta>>\n===\n");

            CollectionAssert.AreEquivalent(new[]
            {
                new ScriptItemReference("primeiro", "dar_item", "chave"),
                new ScriptItemReference("segundo", "remover_item", "carta"),
            }, references.Literal);
        }

        [Test]
        public void IdFromAVariable_InACommand_IsNotLiteral()
        {
            ScriptItemReferences references = ReferencesInNodeBody(
                "<<declare $qual = \"chave\">>\n<<dar_item {$qual}>>\nfim");

            Assert.IsEmpty(references.Literal);
            CollectionAssert.AreEqual(new[] { Reference("dar_item", "{0}") }, references.WithoutLiteralId);
        }

        [Test]
        public void IdFromAVariable_InTheFunction_IsNotLiteral()
        {
            ScriptItemReferences references = ReferencesInNodeBody(
                "<<declare $qual = \"chave\">>\n<<if tem_item($qual)>>\ntem\n<<endif>>");

            Assert.IsEmpty(references.Literal);
            Assert.AreEqual(1, references.WithoutLiteralId.Count);
            Assert.AreEqual(Node, references.WithoutLiteralId[0].NodeName);
            Assert.AreEqual("tem_item", references.WithoutLiteralId[0].Source);
        }

        [Test]
        public void IdFromAnExpression_InTheFunction_IsNotLiteral()
        {
            ScriptItemReferences references = ReferencesInNodeBody(
                "<<if tem_item(\"cha\" + \"ve\")>>\ntem\n<<endif>>");

            Assert.IsEmpty(references.Literal);
            Assert.AreEqual(1, references.WithoutLiteralId.Count);
        }

        [TestCase("<<dar_item>>", "")]
        [TestCase("<<dar_item a b>>", "a b")]
        public void CommandWithoutExactlyOneId_IsNotLiteral(string command, string argument)
        {
            ScriptItemReferences references = ReferencesInNodeBody(command + "\nfim");

            Assert.IsEmpty(references.Literal);
            CollectionAssert.AreEqual(new[] { Reference("dar_item", argument) }, references.WithoutLiteralId);
        }
    }
}
