using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.Core.State;
using ProjetoVN.Dialogue.Characters;
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
        public void ProjectScripts_ContainTheTestNodes()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            CollectionAssert.IsSubsetOf(ExpectedNodes, run.NodeNames.ToArray());
        }

        [Test]
        public void ProjectScripts_AllNodesFollowTheNamingConvention()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            Assert.IsEmpty(run.NodeNames.Where(node => !ScriptNodeName.FollowsConvention(node)),
                "o nome de nó é digitado na cena: minúsculas sem acento, dígitos e _");
        }

        [Test]
        public void ProjectScripts_CiteOnlyItemIdsThatExistInTheItemRegistry()
        {
            ItemRegistry registry = LoadTheOnly<ItemRegistry>();
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

        [Test]
        public void ProjectScripts_NameOnlyCharactersInTheCharacterRegistry()
        {
            CharacterRegistry registry = LoadTheOnly<CharacterRegistry>();
            ScriptSpeakers speakers = ProjectSpeakers();

            Assert.IsEmpty(
                speakers.Spoken.Where(line => !registry.TryGetByScriptName(line.Character, out _))
                    .Select(line => $"{line}: '{line.Character}'"),
                "todo nome antes dos dois-pontos precisa ser o Display Name de um personagem do CharacterRegistry; " +
                "se for narração com dois-pontos, escape com \\:");
        }

        [Test]
        public void ProjectScripts_UseOnlyExpressionsTheSpeakerHas()
        {
            CharacterRegistry registry = LoadTheOnly<CharacterRegistry>();
            ScriptSpeakers speakers = ProjectSpeakers();

            Assert.IsEmpty(speakers.TaggedWithoutSpeaker.Select(line => $"{line}: #{string.Join(" #", line.Expressions)}"),
                "etiqueta de expressão só vale em fala com nome, nunca em narração nem em opção");
            Assert.IsEmpty(speakers.Spoken.Where(line => line.Expressions.Length > 1).Select(line => line.ToString()),
                "uma fala tem no máximo uma etiqueta de expressão");
            Assert.IsEmpty(speakers.Spoken.SelectMany(line => ExpressionsTheSpeakerLacks(registry, line)),
                "a expressão precisa existir nos retratos de quem fala");
        }

        [Test]
        public void CharacterRegistry_HasUniqueIdsAndNamesInTheConvention()
        {
            CharacterRegistry registry = LoadTheOnly<CharacterRegistry>();
            Assert.IsEmpty(registry.Characters.Where(character => character == null).Select(_ => "(vazio)"),
                "o registro não pode ter elemento vazio");

            Assert.IsEmpty(registry.Characters.Select(character => character.Id).Where(id => !ScriptNodeName.FollowsConvention(id)),
                "o id é minúsculas sem acento, dígitos e _");
            Assert.IsEmpty(Repeated(registry.Characters.Select(character => character.Id)), "ids únicos");
            Assert.IsEmpty(registry.Characters.Where(character => string.IsNullOrWhiteSpace(character.DisplayName)).Select(character => character.Id),
                "todo personagem tem nome exibido");
            Assert.IsEmpty(Repeated(registry.Characters.Select(character => character.DisplayName)), "nomes exibidos únicos");
            Assert.IsEmpty(registry.Characters.SelectMany(PortraitProblems), "cada retrato tem uma expressão no formato, única, e um sprite");
        }

        [Test]
        public void GoticaRespostaNao_AsksForTheAngryExpression()
        {
            ScriptSpeakers speakers = ProjectSpeakers();

            ScriptSpeakerLine line = speakers.Spoken.Single(l => l.NodeName == "gotica_resposta_nao");

            Assert.AreEqual("Gótica", line.Character);
            CollectionAssert.AreEqual(new[] { "raiva" }, line.Expressions);
        }

        private static ScriptSpeakers ProjectSpeakers() => ScriptSpeakers.In(ScriptRun.FromProjectFiles().StringTable.Values);

        private static T LoadTheOnly<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            Assert.AreEqual(1, guids.Length, $"o projeto tem um único {typeof(T).Name}");

            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static IEnumerable<string> ExpressionsTheSpeakerLacks(CharacterRegistry registry, ScriptSpeakerLine line)
        {
            if (!registry.TryGetByScriptName(line.Character, out CharacterSO character)) return Enumerable.Empty<string>();

            return line.Expressions.Where(expression => !character.HasExpression(expression))
                .Select(expression => $"{line}: {character.DisplayName} não tem a expressão '{expression}'");
        }

        private static IEnumerable<string> PortraitProblems(CharacterSO character)
        {
            var problems = new List<string>();
            problems.AddRange(character.Portraits.Where(p => !ScriptNodeName.FollowsConvention(p.Expression))
                .Select(p => $"{character.Id}: expressão '{p.Expression}' fora do formato"));
            problems.AddRange(Repeated(character.Portraits.Select(p => p.Expression)).Select(e => $"{character.Id}: expressão '{e}' repetida"));
            problems.AddRange(character.Portraits.Where(p => p.Sprite == null).Select(p => $"{character.Id}: expressão '{p.Expression}' sem sprite"));
            return problems;
        }

        private static IEnumerable<string> Repeated(IEnumerable<string> values) =>
            values.GroupBy(value => value).Where(group => group.Count() > 1).Select(group => group.Key);

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
