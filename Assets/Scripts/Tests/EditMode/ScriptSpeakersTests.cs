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
            return ScriptSpeakers.In(run.StringTable, run.Program);
        }

        [Test]
        public void SpokenLine_CarriesTheCharacter_TheNodeAndTheLineNumber()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("Luna: Oi.");

            ScriptSpeakerLine line = speakers.Spoken.Single();
            Assert.AreEqual("Luna", line.Character);
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
            ScriptSpeakers speakers = SpeakersInNodeBody("Luna: Oi. #raiva");

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
        public void Thought_IsNotSpoken_AndIsMarkedAsThought()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("Pensamento: Sinto o peso da chave.");

            Assert.IsTrue(speakers.Lines.Single().IsThought);
            Assert.IsEmpty(speakers.Spoken);
        }

        [Test]
        public void ThoughtWithATag_IsListedWithoutASpeaker()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("Pensamento: Que dia. #raiva");

            ScriptSpeakerLine line = speakers.TaggedWithoutSpeaker.Single();
            Assert.AreEqual(Node, line.NodeName);
            Assert.AreEqual(FirstBodyLine, line.LineNumber);
        }

        [Test]
        public void LowercaseThought_IsSpoken()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("pensamento: oi");

            Assert.AreEqual("pensamento", speakers.Spoken.Single().Character);
        }

        [Test]
        public void SpeakerFromAnExpression_IsThePlaceholder_ThatNoRegistryHas()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("<<declare $quem = \"Luna\">>\n{$quem}: oi");

            Assert.AreEqual("{0}", speakers.Spoken.Single().Character);
        }

        [Test]
        public void LineBeforeOptions_DoesNotCarryTheLastLineTagAsAnExpression()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("Luna: E aí?\n" + TwoOptionsThatStop);

            Assert.IsEmpty(speakers.Spoken.Single().Expressions);
        }

        [Test]
        public void Option_IsMarkedAsOption_AndTheLineBeforeIsNot()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("Luna: E aí?\n" + TwoOptionsThatStop);

            Assert.IsFalse(speakers.Spoken.Single().IsOption);
            Assert.AreEqual(2, speakers.Lines.Count(line => line.IsOption));
        }

        [Test]
        public void OptionWithTheActionTag_IsAnAction_WithoutExpression()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("-> Assinar e sair #acao\n    <<stop>>\n-> Perguntar\n    <<stop>>");

            ScriptSpeakerLine action = speakers.Lines.Single(line => line.IsAction);
            Assert.IsTrue(action.IsOption);
            Assert.IsEmpty(action.Expressions);
            Assert.IsEmpty(speakers.TaggedWithoutSpeaker);
            Assert.IsEmpty(speakers.ActionOutsideOptions);
        }

        [Test]
        public void ConditionalOptionWithTheActionTag_IsAnAction()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody(
                "<<declare $porta_aberta_teste = false>>\n-> Abrir a porta <<if $porta_aberta_teste>> #acao\n    <<stop>>\n-> Ficar\n    <<stop>>");

            Assert.AreEqual(1, speakers.Lines.Count(line => line.IsAction && line.IsOption));
        }

        [Test]
        public void ActionTagOnNarrationASpokenLineAndAThought_IsListedOutsideOptions()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("Abro a porta. #acao\nLuna: Oi. #acao\nPensamento: Vou. #acao");

            CollectionAssert.AreEqual(new[] { FirstBodyLine, FirstBodyLine + 1, FirstBodyLine + 2 },
                speakers.ActionOutsideOptions.Select(line => line.LineNumber).OrderBy(number => number));
            Assert.IsEmpty(speakers.Lines.SelectMany(line => line.Expressions));
        }

        [Test]
        public void OptionWithTheActionTagAndAnExpression_KeepsTheExpressionWithoutASpeaker()
        {
            ScriptSpeakers speakers = SpeakersInNodeBody("-> Assinar #acao #raiva\n    <<stop>>\n-> Ficar\n    <<stop>>");

            ScriptSpeakerLine line = speakers.TaggedWithoutSpeaker.Single();
            CollectionAssert.AreEqual(new[] { "raiva" }, line.Expressions);
        }
    }
}
