using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjetoVN.PocYarn.Tests
{
    public sealed class StoryVariablesTests
    {
        [Test]
        public void SetValue_WithFloat_ThenTryGetValue_ReturnsTheFloat()
        {
            var variables = new StoryVariables();

            variables.SetValue("$afinidade_gotica", 2.5f);

            Assert.IsTrue(variables.TryGetValue("$afinidade_gotica", out float value));
            Assert.AreEqual(2.5f, value);
        }

        [Test]
        public void SetValue_WithString_ThenTryGetValue_ReturnsTheString()
        {
            var variables = new StoryVariables();

            variables.SetValue("$nome_jogador", "Ana");

            Assert.IsTrue(variables.TryGetValue("$nome_jogador", out string value));
            Assert.AreEqual("Ana", value);
        }

        [Test]
        public void SetValue_WithBool_ThenTryGetValue_ReturnsTheBool()
        {
            var variables = new StoryVariables();

            variables.SetValue("$conheceu_gotica", true);

            Assert.IsTrue(variables.TryGetValue("$conheceu_gotica", out bool value));
            Assert.IsTrue(value);
        }

        [Test]
        public void TryGetValue_AsADifferentTypeThanStored_ReturnsFalse()
        {
            var variables = new StoryVariables();
            variables.SetValue("$conheceu_gotica", true);

            Assert.IsFalse(variables.TryGetValue("$conheceu_gotica", out float _),
                "não pode haver conversão silenciosa entre bool e float");
        }

        [Test]
        public void TryGetValue_ForAVariableNeverWritten_ReturnsFalse()
        {
            var variables = new StoryVariables();

            Assert.IsFalse(variables.TryGetValue("$nunca_gravada", out float _));
            Assert.IsFalse(variables.Contains("$nunca_gravada"));
        }

        [Test]
        public void Contains_AfterSetValue_ReturnsTrue()
        {
            var variables = new StoryVariables();

            variables.SetValue("$afinidade_gotica", 1f);

            Assert.IsTrue(variables.Contains("$afinidade_gotica"));
        }

        [Test]
        public void Clear_AfterSetValue_RemovesEverything()
        {
            var variables = new StoryVariables();
            variables.SetValue("$afinidade_gotica", 1f);
            variables.SetValue("$conheceu_gotica", true);

            variables.Clear();

            Assert.IsFalse(variables.Contains("$afinidade_gotica"));
            Assert.IsFalse(variables.Contains("$conheceu_gotica"));
        }

        [Test]
        public void SetAllVariables_WithWhatGetAllVariablesReturned_RestoresEveryValue()
        {
            var original = new StoryVariables();
            original.SetValue("$afinidade_gotica", 2f);
            original.SetValue("$nome_jogador", "Ana");
            original.SetValue("$conheceu_gotica", true);
            var (floats, strings, bools) = original.GetAllVariables();

            var restored = new StoryVariables();
            restored.SetAllVariables(floats, strings, bools);

            Assert.IsTrue(restored.TryGetValue("$afinidade_gotica", out float afinidade));
            Assert.AreEqual(2f, afinidade);
            Assert.IsTrue(restored.TryGetValue("$nome_jogador", out string nome));
            Assert.AreEqual("Ana", nome);
            Assert.IsTrue(restored.TryGetValue("$conheceu_gotica", out bool conheceu));
            Assert.IsTrue(conheceu);
        }

        [Test]
        public void SetAllVariables_ByDefault_DropsWhatWasThereBefore()
        {
            var variables = new StoryVariables();
            variables.SetValue("$antiga", 1f);

            variables.SetAllVariables(new(), new(), new());

            Assert.IsFalse(variables.Contains("$antiga"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("afinidade_gotica")]
        public void SetValue_WithAnInvalidName_DoesNotStoreAndDoesNotThrow(string invalidName)
        {
            var variables = new StoryVariables();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[StoryVariables\]"));

            Assert.DoesNotThrow(() => variables.SetValue(invalidName, 1f));

            Assert.AreEqual(0, variables.GetAllVariables().Floats.Count);
        }

        [Test]
        public void GetVariableKind_AfterSetValue_ReturnsStored()
        {
            var variables = new StoryVariables();

            variables.SetValue("$afinidade_gotica", 1f);

            Assert.AreEqual(Yarn.VariableKind.Stored, variables.GetVariableKind("$afinidade_gotica"));
        }

        [Test]
        public void GetVariableKind_ForANameNobodyKnows_ReturnsUnknown()
        {
            var variables = new StoryVariables();

            Assert.AreEqual(Yarn.VariableKind.Unknown, variables.GetVariableKind("$nunca_vista"));
        }

        [Test]
        public void GetVariableKind_ForAVariableOnlyDeclaredInTheScript_ReturnsStored()
        {
            var run = new ScriptRun("title: Teste\n---\n<<declare $afinidade_gotica = 0>>\nOi.\n===\n");

            Assert.AreEqual(Yarn.VariableKind.Stored, run.Variables.GetVariableKind("$afinidade_gotica"),
                "o Yarn consulta o Program para saber de variáveis declaradas e ainda não gravadas");
        }
    }
}
