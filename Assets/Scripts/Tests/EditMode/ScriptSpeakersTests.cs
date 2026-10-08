using System.Linq;
using NUnit.Framework;
using ProjetoVN.Core.State;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ScriptSpeakersTests
    {
        private const string Node = "no_teste";
        private const int FirstBodyLine = 3;
        private const string TwoOptionsThatStop = "-> Sim\n    <<stop>>\n-> Não\n    <<stop>>";

        [SetUp]
        public void SetUp() => StoryState.ClearAll();

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        private static ScriptSpeakers SpeakersInNodeBody(string body)
        {
            ScriptRun run = ScriptRun.FromText($"title: {Node}\n---\n{body}\n===\n");
            Assert.IsNotNull(run.Program, "o roteiro do teste precisa compilar");
            return ScriptSpeakers.In(run.StringTable.Values);
        }

        [Test]
        public void SpokenLine_CarriesTheCharacter_TheNodeAndTheLineNumber()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("Gótica: Oi.");

            ScriptSpeakerLine line = speakers.Spoken.Single();
            Assert.AreEqual("Gótica", line.Character);
            Assert.AreEqual(Node, line.NodeName);
            Assert.AreEqual(FirstBodyLine, line.LineNumber);
        }

        [Test]
        public void Narration_HasNoCharacter()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("Uma sala vazia.");

            Assert.AreEqual(1, speakers.Lines.Count);
            Assert.IsEmpty(speakers.Spoken);
        }

        [Test]
        public void NarrationWithAnEscapedColon_HasNoCharacter()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("Eram 10\\:30 da manhã.");

            Assert.AreEqual(1, speakers.Lines.Count);
            Assert.IsEmpty(speakers.Spoken);
        }

        [Test]
        public void SpokenLineWithATag_CarriesTheExpression()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("Gótica: Oi. #raiva");

            CollectionAssert.AreEqual(new[] { "raiva" }, speakers.Spoken.Single().Expressions);
        }

        [Test]
        public void Option_HasNoCharacter()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody(TwoOptionsThatStop);

            Assert.AreEqual(2, speakers.Lines.Count);
            Assert.IsEmpty(speakers.Spoken);
        }

        [Test]
        public void TagsOnAnOptionAndOnNarration_AreListedWithoutASpeaker()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("Narração. #raiva\n-> Sim #feliz\n    <<stop>>\n-> Não\n    <<stop>>");

            CollectionAssert.AreEquivalent(new[] { "raiva", "feliz" },
                speakers.TaggedWithoutSpeaker.SelectMany(line => line.Expressions));
        }

        [Test]
        public void SpeakerFromAnExpression_IsThePlaceholder_ThatNoRegistryHas()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("<<declare $quem = \"Gótica\">>\n{$quem}: oi");

            Assert.AreEqual("{0}", speakers.Spoken.Single().Character);
        }

        [Test]
        public void LineBeforeOptions_DoesNotCarryTheLastLineTagAsAnExpression()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("Gótica: E aí?\n" + TwoOptionsThatStop);

            Assert.IsEmpty(speakers.Spoken.Single().Expressions);
        }
    }
}
