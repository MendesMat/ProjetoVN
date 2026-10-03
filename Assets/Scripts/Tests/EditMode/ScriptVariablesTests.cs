using System.Linq;
using NUnit.Framework;
using ProjetoVN.Core.State;
using UnityEngine.TestTools;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ScriptVariablesTests
    {
        [SetUp]
        public void SetUp() => StoryState.ClearAll();

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        private const string FreeAndConditionalOptions =
            "title: no\n---\n<<declare $afinidade = 0>>\nfala antes\n" +
            "-> Livre\n    livre\n-> Segredo <<if $afinidade >= 1>>\n    segredo\n===\n";

        private const string OnlyConditionalOptions =
            "title: no\n---\n<<declare $afinidade = 0>>\nfala antes\n" +
            "-> Segredo <<if $afinidade >= 1>>\n    segredo\ndepois\n===\n";

        private static ScriptRun Compile(string source)
        {
            ScriptRun run = ScriptRun.FromText(source);
            Assert.IsEmpty(run.ErrorsAndWarnings, "o roteiro do teste precisa compilar sem erro nem aviso");
            return run;
        }

        [Test]
        public void Set_WritesTheVariableToStoryState()
        {
            ScriptRun run = Compile("title: no\n---\n<<declare $x = false>>\n<<set $x to true>>\nfim\n===\n");

            run.Start("no");

            Assert.IsTrue(StoryState.IsTrue("$x"));
        }

        [Test]
        public void If_ChoosesTheBranchByAValueWrittenOutsideTheScript()
        {
            ScriptRun run = Compile(
                "title: no\n---\n<<declare $falou_com_gotica = false>>\n" +
                "<<if $falou_com_gotica>>\nsim\n<<else>>\nnao\n<<endif>>\n===\n");
            StoryState.SetBool("$falou_com_gotica", true);

            run.Start("no");

            CollectionAssert.AreEqual(new[] { "sim" }, run.Lines);
        }

        [Test]
        public void DeclaredButNeverWrittenVariable_ReadsItsInitialValue_AndIsNotStored()
        {
            ScriptRun run = Compile(
                "title: no\n---\n<<declare $esta_aberta = true>>\n" +
                "<<if $esta_aberta>>\naberta\n<<else>>\nfechada\n<<endif>>\n===\n");

            run.Start("no");

            CollectionAssert.AreEqual(new[] { "aberta" }, run.Lines);
            Assert.AreEqual(0, StoryState.Bools.Count, "o valor inicial vem da declaração, não do StoryState");
        }

        [Test]
        public void IncrementingTwice_ReadsTheStoredValue_NotTheInitialOne()
        {
            ScriptRun run = Compile(
                "title: no\n---\n<<declare $afinidade = 0>>\n" +
                "<<set $afinidade to $afinidade + 1>>\n<<set $afinidade to $afinidade + 1>>\nfim\n===\n");

            run.Start("no");

            Assert.IsTrue(StoryState.TryGetNumber("$afinidade", out float value));
            Assert.AreEqual(2f, value);
        }

        [Test]
        public void Visited_BecomesTrueAfterPassingThroughTheNode_AndTheCounterLivesInStoryState()
        {
            ScriptRun run = Compile(
                "title: inicio\n---\n<<jump ponto>>\n===\n" +
                "title: ponto\n---\n<<if visited(\"ponto\")>>\nja_passou\n<<else>>\nprimeira_vez\n<<endif>>\n===\n" +
                "title: volta\n---\n<<if visited(\"ponto\")>>\nja_passou\n<<else>>\nprimeira_vez\n<<endif>>\n===\n");

            run.Start("inicio");
            run.Lines.Clear();
            run.Start("volta");

            CollectionAssert.AreEqual(new[] { "ja_passou" }, run.Lines);
            Assert.IsTrue(StoryState.TryGetNumber("$Yarn.Internal.Visiting.ponto", out float visits));
            Assert.AreEqual(1f, visits);
        }

        [Test]
        public void TextVariable_IsInterpolatedFromTheStoryState()
        {
            ScriptRun run = Compile("title: no\n---\n<<declare $nome_jogador = \"\">>\nOi, {$nome_jogador}!\n===\n");
            StoryState.SetText("$nome_jogador", "Ana");

            run.Start("no");

            CollectionAssert.AreEqual(new[] { "Oi, Ana!" }, run.Lines);
        }

        [Test]
        public void TheLineRightBeforeOptions_CarriesTheLastLineTag_AnOrdinaryLineDoesNot()
        {
            ScriptRun run = Compile(
                "title: no\n---\nfala comum\nfala antes das opcoes\n-> Sim\n    certo\n-> Nao\n    errado\n===\n");

            Assert.Contains("lastline", run.TagsOfLine("fala antes das opcoes"));
            Assert.IsFalse(run.TagsOfLine("fala comum").Contains("lastline"));
        }

        [Test]
        public void ConditionalOption_IsUnavailable_WhileTheConditionIsFalse()
        {
            ScriptRun run = Compile(FreeAndConditionalOptions);

            run.Start("no");

            CollectionAssert.AreEqual(new[] { "Livre", "Segredo" }, run.OptionTexts);
            CollectionAssert.AreEqual(new[] { "Segredo" }, run.UnavailableOptionTexts);
        }

        [Test]
        public void ConditionalOption_BecomesAvailable_WhenTheStoryStateSatisfiesTheCondition()
        {
            ScriptRun run = Compile(FreeAndConditionalOptions);
            StoryState.SetNumber("$afinidade", 1);

            run.Start("no");

            CollectionAssert.AreEqual(new[] { "Livre", "Segredo" }, run.OptionTexts);
            Assert.IsEmpty(run.UnavailableOptionTexts);
        }

        [Test]
        public void AllOptionsUnavailable_TheLineBeforeThemStillCarriesLastLine()
        {
            ScriptRun run = Compile(OnlyConditionalOptions);

            run.Start("no");

            CollectionAssert.AreEqual(new[] { "Segredo" }, run.UnavailableOptionTexts);
            Assert.Contains("lastline", run.TagsOfLine("fala antes"));
        }

        [Test]
        public void Declaration_CarriesTheTripleSlashCommentAsItsDescription()
        {
            ScriptRun run = Compile(
                "title: no\n---\n/// Afinidade com a Gótica.\n<<declare $afinidade = 0>>\n" +
                "<<set $afinidade to $afinidade + 1>>\nfim\n===\n");

            string[] descriptions = run.Declarations.Where(d => d.Name == "$afinidade").Select(d => d.Description).ToArray();

            CollectionAssert.AreEqual(new[] { "Afinidade com a Gótica." }, descriptions);
        }
    }
}
