using NUnit.Framework;

namespace ProjetoVN.PocYarn.Tests
{
    public sealed class YarnScriptTests
    {
        [Test]
        public void Start_WithTwoLines_DeliversThemInOrderAndCompletes()
        {
            var run = new ScriptRun("title: Teste\n---\nPrimeira fala.\nSegunda fala.\n===\n");

            run.Start("Teste");

            Assert.IsEmpty(run.Errors);
            Assert.AreEqual(2, run.Lines.Count);
            StringAssert.Contains("Primeira fala.", run.Lines[0]);
            StringAssert.Contains("Segunda fala.", run.Lines[1]);
            Assert.IsTrue(run.Completed);
        }

        [Test]
        public void Start_WithAChoice_DeliversTheOptionsAndFollowsTheChosenBranch()
        {
            var run = new ScriptRun(
                "title: Teste\n---\n-> Sim\n    Disse sim.\n-> N√£o\n    Disse n√£o.\n-> Talvez\n    Disse talvez.\n===\n");

            run.Start("Teste");

            Assert.AreEqual(3, run.OptionTexts.Count);
            Assert.IsFalse(run.Completed, "a conversa espera a escolha");

            run.Choose(0);

            Assert.AreEqual(1, run.Lines.Count);
            StringAssert.Contains("Disse sim.", run.Lines[0]);
            Assert.IsTrue(run.Completed);
        }

        [Test]
        public void Start_WithACommand_DeliversTheCommandTextToTheGame()
        {
            var run = new ScriptRun("title: Teste\n---\n<<dar_item item-teste-01>>\n===\n");

            run.Start("Teste");

            Assert.IsEmpty(run.Errors);
            CollectionAssert.AreEqual(new[] { "dar_item item-teste-01" }, run.Commands);
        }

        [Test]
        public void Compile_WithDeclaredVariables_ExposesTheirInitialValues()
        {
            var run = new ScriptRun(
                "title: Teste\n---\n<<declare $afinidade_gotica = 0>>\n<<declare $conheceu_gotica = false>>\nOi.\n===\n");

            Assert.IsEmpty(run.Errors);
            Assert.IsTrue(run.Program.TryGetInitialValue("$afinidade_gotica", out float afinidade));
            Assert.AreEqual(0f, afinidade);
            Assert.IsTrue(run.Program.TryGetInitialValue("$conheceu_gotica", out bool conheceu));
            Assert.IsFalse(conheceu);
        }

        [Test]
        public void SetNode_WithAnUnknownNode_Throws()
        {
            var run = new ScriptRun("title: Teste\n---\nOi.\n===\n");

            Assert.Throws<Yarn.DialogueException>(() => run.Dialogue.SetNode("NoQueNaoExiste"));
            Assert.IsFalse(run.Dialogue.NodeExists("NoQueNaoExiste"));
        }

        [Test]
        public void Compile_WithAHyphenatedVariableName_ReportsAnError()
        {
            var run = new ScriptRun("title: Teste\n---\n<<declare $falou-com-gotica = false>>\nOi.\n===\n");

            Assert.IsNotEmpty(run.Errors, "o identificador do Yarn n√£o aceita h√≠fen, ent√£o as flags atuais precisam ser renomeadas");
        }

        [Test]
        public void Start_WithASetCommand_StartsFromTheDeclaredInitialValueAndKeepsCounting()
        {
            const string script =
                "title: Teste\n---\n<<declare $afinidade_gotica = 5>>\n"
                + "<<set $afinidade_gotica to $afinidade_gotica + 1>>\n===\n";
            var run = new ScriptRun(script);

            run.Start("Teste");
            Assert.IsTrue(run.Variables.TryGetValue("$afinidade_gotica", out float first));
            Assert.AreEqual(6f, first, "parte do valor inicial declarado, 5");

            run.Start("Teste");
            Assert.IsTrue(run.Variables.TryGetValue("$afinidade_gotica", out float second));
            Assert.AreEqual(7f, second, "a segunda execuÁ„o lÍ o valor guardado, n„o o inicial");
        }

        [Test]
        public void Start_WithAnIfOnAStoredVariable_ShowsTheLineOnlyWhenTheConditionHolds()
        {
            const string script =
                "title: Teste\n---\n<<declare $afinidade_gotica = 0>>\n"
                + "<<if $afinidade_gotica >= 1>>\n    Ela sorri.\n<<endif>>\nFim.\n===\n";
            var withoutAffinity = new ScriptRun(script);
            var withAffinity = new ScriptRun(script);
            withAffinity.Variables.SetValue("$afinidade_gotica", 1f);

            withoutAffinity.Start("Teste");
            withAffinity.Start("Teste");

            Assert.AreEqual(1, withoutAffinity.Lines.Count);
            Assert.AreEqual(2, withAffinity.Lines.Count);
            StringAssert.Contains("Ela sorri.", withAffinity.Lines[0]);
        }

        [Test]
        public void Start_WithTemItemInACondition_ReadsTheFunctionResult()
        {
            const string script =
                "title: Teste\n---\n<<if tem_item(\"item-teste-01\")>>\n    Peguei uma chave.\n<<endif>>\nFim.\n===\n";
            var withoutItem = new ScriptRun(script);
            var withItem = new ScriptRun(script);
            withItem.OwnedItems.Add(ScriptRun.OwnedItemId);

            withoutItem.Start("Teste");
            withItem.Start("Teste");

            Assert.AreEqual(1, withoutItem.Lines.Count);
            Assert.AreEqual(2, withItem.Lines.Count);
        }
    }
}
