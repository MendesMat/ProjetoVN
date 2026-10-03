using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjetoVN.Core.State;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class StoryStateVariablesTests
    {
        private StoryStateVariables _variables;

        [SetUp]
        public void SetUp()
        {
            StoryState.ClearAll();
            _variables = new StoryStateVariables();
        }

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        [Test]
        public void SetValue_Bool_WritesABoolToStoryState()
        {
            _variables.SetValue("$falou_com_gotica", true);

            Assert.IsTrue(StoryState.TryGetBool("$falou_com_gotica", out bool value));
            Assert.IsTrue(value);
        }

        [Test]
        public void SetValue_Number_WritesANumberToStoryState()
        {
            _variables.SetValue("$afinidade_gotica", 3f);

            Assert.IsTrue(StoryState.TryGetNumber("$afinidade_gotica", out float value));
            Assert.AreEqual(3f, value);
        }

        [Test]
        public void SetValue_Text_WritesATextToStoryState()
        {
            _variables.SetValue("$nome_jogador", "Ana");

            Assert.IsTrue(StoryState.TryGetText("$nome_jogador", out string value));
            Assert.AreEqual("Ana", value);
        }

        [Test]
        public void TryGetValue_ReadsWhatWasWrittenOutsideTheAdapter()
        {
            StoryState.SetBool("$porta_aberta", true);
            StoryState.SetNumber("$afinidade_gotica", 2f);
            StoryState.SetText("$nome_jogador", "Ana");

            Assert.IsTrue(_variables.TryGetValue("$porta_aberta", out bool flag));
            Assert.IsTrue(flag);
            Assert.IsTrue(_variables.TryGetValue("$afinidade_gotica", out float number));
            Assert.AreEqual(2f, number);
            Assert.IsTrue(_variables.TryGetValue("$nome_jogador", out string text));
            Assert.AreEqual("Ana", text);
        }

        [Test]
        public void TryGetValue_AsIConvertible_ReturnsTheValueOfAllThreeTypes()
        {
            StoryState.SetBool("$flag", true);
            StoryState.SetNumber("$numero", 5f);
            StoryState.SetText("$texto", "oi");

            Assert.IsTrue(_variables.TryGetValue("$flag", out IConvertible flag));
            Assert.AreEqual(true, flag);
            Assert.IsTrue(_variables.TryGetValue("$numero", out IConvertible number));
            Assert.AreEqual(5f, number);
            Assert.IsTrue(_variables.TryGetValue("$texto", out IConvertible text));
            Assert.AreEqual("oi", text);
        }

        [Test]
        public void TryGetValue_AsADifferentTypeThanWritten_ReturnsFalse()
        {
            StoryState.SetNumber("$afinidade_gotica", 2f);

            Assert.IsFalse(_variables.TryGetValue("$afinidade_gotica", out bool _),
                "converter em silêncio esconderia um roteiro que usa a variável com o tipo errado");
            Assert.IsFalse(_variables.TryGetValue("$afinidade_gotica", out string _));
        }

        [Test]
        public void NeverWrittenVariable_WithoutProgram_IsNotFound_WithoutThrowing()
        {
            Assert.IsFalse(_variables.TryGetValue("$nunca_gravada", out bool _));
            Assert.IsFalse(_variables.Contains("$nunca_gravada"));
        }

        [Test]
        public void Contains_AndGetVariableKind_ReportStoredForEachType()
        {
            StoryState.SetBool("$flag", false);
            StoryState.SetNumber("$numero", 0f);
            StoryState.SetText("$texto", "");

            foreach (string name in new[] { "$flag", "$numero", "$texto" })
            {
                Assert.IsTrue(_variables.Contains(name), name);
                Assert.AreEqual(VariableKind.Stored, _variables.GetVariableKind(name), name);
            }
        }

        [Test]
        public void GetVariableKind_ForANeverWrittenName_WithoutProgram_IsUnknown()
        {
            Assert.AreEqual(VariableKind.Unknown, _variables.GetVariableKind("$nunca_gravada"));
        }

        [Test]
        public void SetValue_WithANameWithoutDollar_WritesNothing_AndWarns()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Nome de variável inválido"));

            _variables.SetValue("sem_cifrao", true);

            Assert.AreEqual(0, StoryState.Bools.Count);
        }

        [Test]
        public void Clear_EmptiesTheStoryState()
        {
            StoryState.SetBool("$flag", true);
            StoryState.SetNumber("$numero", 1f);
            StoryState.SetText("$texto", "oi");

            _variables.Clear();

            Assert.AreEqual(0, StoryState.Bools.Count + StoryState.Numbers.Count + StoryState.Texts.Count);
        }
    }
}
