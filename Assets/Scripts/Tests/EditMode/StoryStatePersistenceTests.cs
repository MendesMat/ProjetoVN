using NUnit.Framework;
using ProjetoVN.Core.State;
using ProjetoVN.GameFlow.Persistence;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class StoryStatePersistenceTests
    {
        [SetUp]
        public void SetUp() => StoryState.ClearAll();

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        [Test]
        public void Capture_CopiesTheThreeTypesToTheGameState()
        {
            StoryState.SetBool("$falou_com_gotica", true);
            StoryState.SetNumber("$afinidade_gotica", 2.5f);
            StoryState.SetText("$nome_jogador", "Ana");
            var state = new GameState();

            StoryStatePersistence.Capture(state);

            Assert.That(state.storyBools, Is.EqualTo(new[] { new StoryBoolEntry { name = "$falou_com_gotica", value = true } }));
            Assert.That(state.storyNumbers, Is.EqualTo(new[] { new StoryNumberEntry { name = "$afinidade_gotica", value = 2.5f } }));
            Assert.That(state.storyTexts, Is.EqualTo(new[] { new StoryTextEntry { name = "$nome_jogador", value = "Ana" } }));
        }

        [Test]
        public void CaptureThenJsonThenRestore_BringsEverythingBack()
        {
            StoryState.SetBool("$porta_destrancada", false);
            StoryState.SetNumber("$afinidade_gotica", 0.1f);
            StoryState.SetNumber("$humor", -3f);
            StoryState.SetText("$apelido", "Gótica");
            var captured = new GameState();
            StoryStatePersistence.Capture(captured);

            var reloaded = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(captured));
            StoryState.ClearAll();
            StoryStatePersistence.Restore(reloaded);

            Assert.IsTrue(StoryState.TryGetBool("$porta_destrancada", out bool door));
            Assert.IsFalse(door, "um falso gravado precisa voltar como falso gravado");
            Assert.IsTrue(StoryState.TryGetNumber("$afinidade_gotica", out float affinity));
            Assert.AreEqual(0.1f, affinity, "a afinidade não pode perder precisão no save");
            Assert.IsTrue(StoryState.TryGetNumber("$humor", out float mood));
            Assert.AreEqual(-3f, mood);
            Assert.IsTrue(StoryState.TryGetText("$apelido", out string nickname));
            Assert.AreEqual("Gótica", nickname, "acentos precisam sobreviver ao save");
        }

        [Test]
        public void Restore_ReplacesWhatWasInTheSession()
        {
            StoryState.SetBool("$da_sessao_antiga", true);
            var state = new GameState();
            state.storyBools.Add(new StoryBoolEntry { name = "$do_save", value = true });

            StoryStatePersistence.Restore(state);

            Assert.IsFalse(StoryState.IsTrue("$da_sessao_antiga"));
            Assert.IsTrue(StoryState.IsTrue("$do_save"));
        }

        [Test]
        public void Restore_OfAFreshGameState_LeavesTheStoryStateEmpty()
        {
            StoryState.SetBool("$da_sessao_antiga", true);

            Assert.DoesNotThrow(() => StoryStatePersistence.Restore(new GameState()));

            Assert.IsEmpty(StoryState.Bools);
            Assert.IsEmpty(StoryState.Numbers);
            Assert.IsEmpty(StoryState.Texts);
        }

        [Test]
        public void Restore_WithNullLists_TreatsThemAsEmpty()
        {
            StoryState.SetBool("$da_sessao_antiga", true);
            var state = new GameState { storyBools = null, storyNumbers = null, storyTexts = null };

            Assert.DoesNotThrow(() => StoryStatePersistence.Restore(state));

            Assert.IsEmpty(StoryState.Bools);
        }
    }
}
