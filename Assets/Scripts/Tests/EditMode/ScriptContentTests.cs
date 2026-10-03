using System.Linq;
using NUnit.Framework;
using ProjetoVN.Core.State;
using ProjetoVN.Dialogue.Logic;
using ProjetoVN.Inventory;
using UnityEditor;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>Os roteiros de verdade, em <c>Assets/Roteiro/</c>. Pega erro de roteirista antes do Play.</summary>
    public sealed class ScriptContentTests
    {
        private static readonly string[] ExpectedNodes =
        {
            "gotica_cheguei_cedo", "gotica_resposta_sim", "gotica_resposta_nao", "gotica_resposta_talvez",
            "gotica_resposta_sentar", "gotica_cadeira_no_fundo", "peguei_chave", "porta_trancada", "porta_destrancada",
            "variaveis",
        };

        private const string CentralVariablesFile = "variaveis.yarn";
        private const string GoticaAffinity = "$afinidade_gotica";
        private const string ConditionalOption = "Posso sentar perto de você?";
        private const int ConditionalOptionIndex = 3;
        private const string ConditionalLine = "Gótica: Gostei de você.";

        private const string KeyItemId = "chave_teste";
        private const string GoticaGivesTheKeyLine = "Gótica: Toma, achei esta chave no corredor.";

        [SetUp]
        public void SetUp() => StoryState.ClearAll();

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        [Test]
        public void ProjectScripts_CompileWithoutErrorsOrWarnings()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            Assert.IsEmpty(run.ErrorsAndWarnings.Select(d => d.ToString()),
                "aviso conta: é o que pega <<jump>> para nó inexistente, variável não declarada e código inalcançável");
        }

        [Test]
        public void ProjectScripts_HaveExactlyTheTestNodes_AllFollowingTheNamingConvention()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            CollectionAssert.AreEquivalent(ExpectedNodes, run.NodeNames.ToArray());
            foreach (string node in run.NodeNames) Assert.IsTrue(ScriptNodeName.FollowsConvention(node), node);
        }

        [Test]
        public void ProjectScripts_CiteOnlyItemIdsThatExistInTheItemRegistry()
        {
            ItemRegistry registry = LoadTheOnlyItemRegistry();
            ScriptItemReferences references = ScriptItemReferences.In(ScriptRun.FromProjectFiles().Program);

            Assert.IsEmpty(references.WithoutLiteralId,
                "o id de um item é sempre literal: nada de variável, expressão, id ausente ou parâmetro a mais");
            Assert.IsEmpty(references.Literal.Where(reference => !registry.TryGetById(reference.Argument, out _)),
                "todo id citado no roteiro precisa existir no ItemRegistry");
        }

        [Test]
        public void ProjectScripts_DeclareEveryVariableInTheCentralFile()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            Assert.IsEmpty(
                run.Declarations.Where(d => !d.SourceFileName.EndsWith(CentralVariablesFile)).Select(d => d.Name),
                "toda variável é declarada em Assets/Roteiro/" + CentralVariablesFile);
        }

        [Test]
        public void ProjectScripts_DescribeEveryDeclaredVariable()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            Assert.IsEmpty(
                run.Declarations.Where(d => string.IsNullOrWhiteSpace(d.Description)).Select(d => d.Name),
                "todo <<declare>> tem uma linha '/// descrição' logo acima");
        }

        [Test]
        public void ProjectScripts_DeclaredVariablesFollowTheNamingConvention()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            Assert.IsEmpty(run.Declarations.Select(d => d.Name).Where(name => !StoryVariableName.FollowsConvention(name)));
        }

        [TestCase("$falou_com_gotica", "Bool")]
        [TestCase("$porta_mecanicas_destrancada", "Bool")]
        [TestCase(GoticaAffinity, "Number")]
        public void ProjectScripts_DeclareTheVariablesTheTestSceneUses(string variable, string typeName)
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            string[] declaredTypes = run.Declarations.Where(d => d.Name == variable).Select(d => d.Type.Name).ToArray();

            CollectionAssert.AreEqual(new[] { typeName }, declaredTypes);
        }

        private static ItemRegistry LoadTheOnlyItemRegistry()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemRegistry");
            Assert.AreEqual(1, guids.Length, "o projeto tem um único ItemRegistry");

            return AssetDatabase.LoadAssetAtPath<ItemRegistry>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        [Test]
        public void GoticaChegueiCedo_AsksFourOptions()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            run.Start("gotica_cheguei_cedo");

            CollectionAssert.AreEqual(new[] { "Gótica: Acho que cheguei muito cedo..." }, run.Lines);
            CollectionAssert.AreEqual(new[] { "Sim", "Não", "Talvez", ConditionalOption }, run.OptionTexts);
        }

        [Test]
        public void GoticaChegueiCedo_FirstTalk_LocksTheConditionalOption()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            run.Start("gotica_cheguei_cedo");

            CollectionAssert.AreEqual(new[] { ConditionalOption }, run.UnavailableOptionTexts);
        }

        [Test]
        public void GoticaChegueiCedo_ChoosingSim_RaisesTheAffinity_AndShowsTheConditionalLine()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            run.Start("gotica_cheguei_cedo");

            run.Choose(0);

            Assert.IsTrue(StoryState.TryGetNumber(GoticaAffinity, out float affinity));
            Assert.AreEqual(1f, affinity);
            CollectionAssert.Contains(run.Lines, ConditionalLine);
        }

        [Test]
        public void GoticaChegueiCedo_ChoosingNao_KeepsTheAffinity_AndSkipsTheConditionalLine()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            run.Start("gotica_cheguei_cedo");

            run.Choose(1);

            Assert.IsFalse(StoryState.TryGetNumber(GoticaAffinity, out _));
            CollectionAssert.DoesNotContain(run.Lines, ConditionalLine);
        }

        [Test]
        public void GoticaChegueiCedo_SecondTalkAfterSim_OffersTheConditionalOption()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            run.Start("gotica_cheguei_cedo");
            run.Choose(0);
            run.Lines.Clear();

            run.Start("gotica_cheguei_cedo");
            Assert.IsEmpty(run.UnavailableOptionTexts);
            run.Choose(ConditionalOptionIndex);

            Assert.AreEqual("Gótica: Pode. Só não puxa assunto.", run.Lines[1]);
            Assert.IsTrue(run.Completed);
        }

        [Test]
        public void GoticaChegueiCedo_ChoosingSim_ShowsAllTheLines_Ends_AndMarksTheTalk()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            run.Start("gotica_cheguei_cedo");

            run.Choose(0);

            CollectionAssert.AreEqual(new[]
            {
                "Gótica: Acho que cheguei muito cedo...",
                "Gótica: Ele falou sim.",
                ConditionalLine,
                "Gótica: A escola está mais quieta do que eu imaginava.",
                GoticaGivesTheKeyLine,
                "Gótica: Já vou aproveitar pra pegar uma cadeira no fundo.",
            }, run.Lines);
            Assert.IsTrue(run.Completed);
            Assert.IsTrue(StoryState.IsTrue("$falou_com_gotica"));
        }

        [Test]
        public void GoticaChegueiCedo_ChoosingNao_DoesNotMarkTheTalk()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            run.Start("gotica_cheguei_cedo");

            run.Choose(1);

            Assert.AreEqual("Gótica: Ele falou não.", run.Lines[1]);
            Assert.IsFalse(StoryState.TryGetBool("$falou_com_gotica", out _));
        }

        [Test]
        public void GoticaChegueiCedo_GivesTheKey_WhenThePlayerDoesNotHaveIt()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            run.Start("gotica_cheguei_cedo");

            run.Choose(1);

            CollectionAssert.Contains(run.Lines, GoticaGivesTheKeyLine);
            CollectionAssert.AreEqual(new[] { "dar_item " + KeyItemId }, run.Commands);
        }

        [Test]
        public void GoticaChegueiCedo_DoesNotGiveTheKeyAgain_WhenThePlayerAlreadyHasIt()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            run.OwnedItems.Add(KeyItemId);
            run.Start("gotica_cheguei_cedo");

            run.Choose(1);

            CollectionAssert.DoesNotContain(run.Lines, GoticaGivesTheKeyLine);
            Assert.IsEmpty(run.Commands);
            Assert.IsTrue(run.Completed);
        }

        [TestCase("peguei_chave", "Peguei uma chave... O que será que ela abre?")]
        [TestCase("porta_trancada", "A porta está trancada.")]
        [TestCase("porta_destrancada", "A chave serviu! Destrancou a porta.")]
        public void NarrationNodes_ShowOneLineWithoutASpeaker_AndEnd(string node, string text)
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            run.Start(node);

            CollectionAssert.AreEqual(new[] { text }, run.Lines);
            Assert.IsTrue(run.Completed);
        }
    }
}
