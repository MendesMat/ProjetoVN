using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjetoVN.Core.State;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class StoryStateTests
    {
        // StoryState é estático e compartilhado entre testes, igual ao MessageBroker.
        [SetUp]
        public void SetUp() => StoryState.ClearAll();

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        private static KeyValuePair<string, T> Entry<T>(string name, T value) => new(name, value);

        [Test]
        public void SetBool_ThenIsTrue_ReturnsTrue()
        {
            Assert.IsTrue(StoryState.SetBool("$falou_com_luna", true));
            Assert.IsTrue(StoryState.IsTrue("$falou_com_luna"));
        }

        [Test]
        public void SetBool_False_IsNotTrue_ButIsRememberedAsWritten()
        {
            StoryState.SetBool("$porta_destrancada", false);

            Assert.IsFalse(StoryState.IsTrue("$porta_destrancada"));
            Assert.IsTrue(StoryState.TryGetBool("$porta_destrancada", out bool value),
                "falso gravado não é o mesmo que nunca gravado: o Yarn precisa distinguir para não voltar ao valor inicial");
            Assert.IsFalse(value);
        }

        [Test]
        public void IsTrue_ForANameNeverWritten_ReturnsFalse()
        {
            StoryState.SetBool("$uma_flag", true);

            Assert.IsFalse(StoryState.IsTrue("$outra_flag"),
                "uma variável que nunca foi gravada não pode destravar conteúdo por engano");
        }

        [Test]
        public void IsTrue_WithNullEmptyOrWhitespace_ReturnsFalse_WithoutLogging()
        {
            Assert.IsFalse(StoryState.IsTrue(null));
            Assert.IsFalse(StoryState.IsTrue(""));
            Assert.IsFalse(StoryState.IsTrue("   "));

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SetBool_WithNullEmptyOrWhitespace_StoresNothing_WithoutLogging()
        {
            Assert.IsFalse(StoryState.SetBool(null, true));
            Assert.IsFalse(StoryState.SetBool("", true));
            Assert.IsFalse(StoryState.SetBool("   ", true));

            Assert.IsEmpty(StoryState.Bools, "um campo vazio no Inspector não pode virar uma variável fantasma");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SetBool_WithoutTheDollarPrefix_StoresNothing_AndLogsAWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"\[StoryState\] Nome de variável inválido: 'sem_cifrao'"));

            Assert.IsFalse(StoryState.SetBool("sem_cifrao", true));

            Assert.IsEmpty(StoryState.Bools);
        }

        [Test]
        public void SetNumberAndSetText_WithAnInvalidName_StoreNothing()
        {
            LogAssert.Expect(LogType.Warning, new Regex("sem_cifrao"));
            LogAssert.Expect(LogType.Warning, new Regex("sem_cifrao"));

            Assert.IsFalse(StoryState.SetNumber("sem_cifrao", 1f));
            Assert.IsFalse(StoryState.SetText("sem_cifrao", "x"));

            Assert.IsEmpty(StoryState.Numbers);
            Assert.IsEmpty(StoryState.Texts);
        }

        [Test]
        public void ReplaceAll_DiscardsWhatWasThereBefore_InAllThreeTypes()
        {
            StoryState.SetBool("$da_sessao_antiga", true);
            StoryState.SetNumber("$afinidade_antiga", 1f);
            StoryState.SetText("$nome_antigo", "Velho");

            StoryState.ReplaceAll(
                new[] { Entry("$do_save", true) },
                new[] { Entry("$afinidade_do_save", 2f) },
                new[] { Entry("$nome_do_save", "Novo") });

            Assert.IsFalse(StoryState.IsTrue("$da_sessao_antiga"), "carregar um save não pode manter a sessão anterior");
            Assert.IsFalse(StoryState.TryGetNumber("$afinidade_antiga", out _));
            Assert.IsFalse(StoryState.TryGetText("$nome_antigo", out _));
            Assert.IsTrue(StoryState.IsTrue("$do_save"));
            Assert.IsTrue(StoryState.TryGetNumber("$afinidade_do_save", out float number));
            Assert.AreEqual(2f, number);
            Assert.IsTrue(StoryState.TryGetText("$nome_do_save", out string text));
            Assert.AreEqual("Novo", text);
        }

        [Test]
        public void ReplaceAll_WithNulls_EmptiesEverything_WithoutThrowing()
        {
            StoryState.SetBool("$da_sessao_antiga", true);

            Assert.DoesNotThrow(() => StoryState.ReplaceAll(null, null, null));

            Assert.IsEmpty(StoryState.Bools);
            Assert.IsEmpty(StoryState.Numbers);
            Assert.IsEmpty(StoryState.Texts);
        }

        [Test]
        public void ReplaceAll_SkipsEntriesWithAnInvalidName_AndKeepsTheValidOnes()
        {
            LogAssert.Expect(LogType.Warning, new Regex("sem_cifrao"));

            StoryState.ReplaceAll(
                new[] { Entry("$valida", true), Entry("sem_cifrao", true), Entry("", true), Entry(null, true) },
                null,
                null);

            Assert.AreEqual(1, StoryState.Bools.Count);
            Assert.IsTrue(StoryState.IsTrue("$valida"));
        }

        [Test]
        public void ClearAll_EmptiesAllThreeTypes()
        {
            StoryState.SetBool("$uma", true);
            StoryState.SetNumber("$outra", 1f);
            StoryState.SetText("$mais_uma", "x");

            StoryState.ClearAll();

            Assert.IsEmpty(StoryState.Bools, "Reset Session precisa apagar tudo, senão o estado sobrevive escondido");
            Assert.IsEmpty(StoryState.Numbers);
            Assert.IsEmpty(StoryState.Texts);
        }

        [Test]
        public void SetNumber_ThenTryGetNumber_ReturnsTheValue_AndRewritingReplacesIt()
        {
            StoryState.SetNumber("$afinidade_luna", 2.5f);
            StoryState.SetNumber("$afinidade_luna", 4f);

            Assert.IsTrue(StoryState.TryGetNumber("$afinidade_luna", out float value));
            Assert.AreEqual(4f, value);
        }

        [Test]
        public void SetText_ThenTryGetText_ReturnsTheText_AndNullBecomesEmpty()
        {
            StoryState.SetText("$nome_jogador", "Ana");
            StoryState.SetText("$apelido", null);

            Assert.IsTrue(StoryState.TryGetText("$nome_jogador", out string name));
            Assert.AreEqual("Ana", name);
            Assert.IsTrue(StoryState.TryGetText("$apelido", out string nickname));
            Assert.AreEqual("", nickname);
        }

        [Test]
        public void ReadingAsADifferentTypeThanWritten_ReturnsFalse()
        {
            StoryState.SetNumber("$afinidade_luna", 1f);

            Assert.IsFalse(StoryState.IsTrue("$afinidade_luna"), "não há conversão silenciosa entre tipos");
            Assert.IsFalse(StoryState.TryGetBool("$afinidade_luna", out _));
            Assert.IsFalse(StoryState.TryGetText("$afinidade_luna", out _));
        }

        [Test]
        public void WritingANameWithAnotherType_LastTypeWins_AndTheNameLeavesTheOthers()
        {
            StoryState.SetBool("$mutavel", true);
            StoryState.SetNumber("$mutavel", 3f);

            Assert.IsFalse(StoryState.Bools.ContainsKey("$mutavel"), "um nome nunca pode ter dois valores");
            Assert.IsTrue(StoryState.TryGetNumber("$mutavel", out float number));
            Assert.AreEqual(3f, number);
        }

        [Test]
        public void TryGetNumberAndTryGetText_ForANameNeverWritten_ReturnFalse()
        {
            Assert.IsFalse(StoryState.TryGetNumber("$nunca", out _));
            Assert.IsFalse(StoryState.TryGetText("$nunca", out _));
        }

        [Test]
        public void Collections_ReflectExactlyWhatWasWritten()
        {
            StoryState.SetBool("$b", true);
            StoryState.SetNumber("$n", 1.5f);
            StoryState.SetText("$t", "texto");

            CollectionAssert.AreEquivalent(new[] { Entry("$b", true) }, StoryState.Bools);
            CollectionAssert.AreEquivalent(new[] { Entry("$n", 1.5f) }, StoryState.Numbers);
            CollectionAssert.AreEquivalent(new[] { Entry("$t", "texto") }, StoryState.Texts);
        }

        [Test]
        public void SetNumber_AcceptsTheInternalNamesYarnStoresItself()
        {
            Assert.IsTrue(StoryState.SetNumber("$Yarn.Internal.Visiting.Inicio", 1f));
            Assert.IsTrue(StoryState.TryGetNumber("$Yarn.Internal.Visiting.Inicio", out float visits));
            Assert.AreEqual(1f, visits);
        }
    }
}
