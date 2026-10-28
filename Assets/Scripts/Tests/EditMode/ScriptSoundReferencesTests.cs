using NUnit.Framework;
using ProjetoVN.Core.State;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ScriptSoundReferencesTests
    {
        private const string Node = "no_teste";

        [SetUp]
        public void SetUp() => StoryState.ClearAll();

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        private static ScriptSoundReferences ReferencesIn(string source)
        {
            ScriptRun run = ScriptRun.FromText(source);
            Assert.IsNotNull(run.Program, "o roteiro do teste precisa compilar");
            return ScriptSoundReferences.In(run.Program);
        }

        private static ScriptSoundReferences ReferencesInNodeBody(string body) =>
            ReferencesIn($"title: {Node}\n---\n{body}\n===\n");

        private static ScriptSoundReference Reference(string command, string argument) => new(Node, command, argument);

        [TestCase("tocar_musica")]
        [TestCase("tocar_efeito")]
        [TestCase("tocar_ambiente")]
        public void SoundCommand_IsALiteralReference_WithItsNodeCommandAndId(string command)
        {
            ScriptSoundReferences references = ReferencesInNodeBody($"<<{command} freada>>\nfim");

            CollectionAssert.AreEqual(new[] { Reference(command, "freada") }, references.Literal);
            Assert.IsEmpty(references.WithoutLiteralId);
        }

        [Test]
        public void QuotedId_IsReadWithoutTheQuotes()
        {
            ScriptSoundReferences references = ReferencesInNodeBody("<<tocar_efeito \"freada\">>\nfim");

            CollectionAssert.AreEqual(new[] { Reference("tocar_efeito", "freada") }, references.Literal);
        }

        [Test]
        public void StopAmbience_WithoutParameter_IsNotAReference()
        {
            ScriptSoundReferences references = ReferencesInNodeBody("<<parar_ambiente>>\nfim");

            Assert.IsEmpty(references.Literal);
            Assert.IsEmpty(references.WithoutLiteralId);
        }

        [Test]
        public void StopAmbience_WithAParameter_IsReproved()
        {
            ScriptSoundReferences references = ReferencesInNodeBody("<<parar_ambiente x>>\nfim");

            Assert.IsEmpty(references.Literal);
            CollectionAssert.AreEqual(new[] { Reference("parar_ambiente", "x") }, references.WithoutLiteralId);
        }

        [Test]
        public void IdFromAVariable_IsNotLiteral()
        {
            ScriptSoundReferences references = ReferencesInNodeBody(
                "<<declare $qual = \"freada\">>\n<<tocar_efeito {$qual}>>\nfim");

            Assert.IsEmpty(references.Literal);
            CollectionAssert.AreEqual(new[] { Reference("tocar_efeito", "{0}") }, references.WithoutLiteralId);
        }

        [TestCase("<<tocar_efeito>>", "")]
        [TestCase("<<tocar_efeito a b>>", "a b")]
        public void CommandWithoutExactlyOneId_IsNotLiteral(string command, string argument)
        {
            ScriptSoundReferences references = ReferencesInNodeBody(command + "\nfim");

            Assert.IsEmpty(references.Literal);
            CollectionAssert.AreEqual(new[] { Reference("tocar_efeito", argument) }, references.WithoutLiteralId);
        }

        [Test]
        public void CommandInsideAComment_IsNotAReference()
        {
            ScriptSoundReferences references = ReferencesInNodeBody("// <<tocar_efeito freada>>\nfim");

            Assert.IsEmpty(references.Literal);
            Assert.IsEmpty(references.WithoutLiteralId);
        }

        [Test]
        public void OtherCommands_AreNotReferences()
        {
            ScriptSoundReferences references = ReferencesInNodeBody(
                "<<tocar_musicas freada>>\n<<dar_item chave>>\n<<parar_ambientes>>\nfim");

            Assert.IsEmpty(references.Literal);
            Assert.IsEmpty(references.WithoutLiteralId);
        }

        [Test]
        public void ReferencesInTwoNodes_EachCarryTheirOwnNodeName()
        {
            ScriptSoundReferences references = ReferencesIn(
                "title: primeiro\n---\n<<tocar_musica tema>>\n===\n" +
                "title: segundo\n---\n<<tocar_ambiente chuva>>\n===\n");

            CollectionAssert.AreEquivalent(new[]
            {
                new ScriptSoundReference("primeiro", "tocar_musica", "tema"),
                new ScriptSoundReference("segundo", "tocar_ambiente", "chuva"),
            }, references.Literal);
        }
    }
}
