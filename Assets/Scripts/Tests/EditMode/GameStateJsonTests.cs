using System.Collections.Generic;
using NUnit.Framework;
using ProjetoVN.GameFlow.Persistence;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class GameStateJsonTests
    {
        [Test]
        public void TryRead_OfWhatJsonUtilityWrote_GivesBackEveryField()
        {
            var original = new GameState
            {
                ownedItemIds = new List<string> { "chave_teste" },
                consumedWorldObjectIds = new List<string> { "obj-1" },
                storyBools = new List<StoryBoolEntry> { new() { name = "$porta_destrancada", value = true } },
                storyNumbers = new List<StoryNumberEntry> { new() { name = "$afinidade_luna", value = 2.5f } },
                storyTexts = new List<StoryTextEntry> { new() { name = "$nome_jogador", value = "Ana" } },
                currentScene = "[Teste] Sala B",
                currentEntryPointId = "janela"
            };

            bool read = GameStateJson.TryRead(JsonUtility.ToJson(original), out GameState state);

            Assert.IsTrue(read);
            Assert.That(state.ownedItemIds, Is.EqualTo(original.ownedItemIds));
            Assert.That(state.consumedWorldObjectIds, Is.EqualTo(original.consumedWorldObjectIds));
            Assert.That(state.storyBools, Is.EqualTo(original.storyBools));
            Assert.That(state.storyNumbers, Is.EqualTo(original.storyNumbers));
            Assert.That(state.storyTexts, Is.EqualTo(original.storyTexts));
            Assert.That(state.currentScene, Is.EqualTo(original.currentScene));
            Assert.That(state.currentEntryPointId, Is.EqualTo(original.currentEntryPointId));
        }

        [Test]
        public void TryRead_OfMalformedJson_ReturnsFalse()
        {
            bool read = GameStateJson.TryRead("{garbage", out GameState state);

            Assert.IsFalse(read);
            Assert.IsNull(state);
        }

        [TestCase("null")]
        [TestCase("[1,2]")]
        public void TryRead_OfJsonThatIsNotAnObject_ReturnsFalse(string json)
        {
            bool read = GameStateJson.TryRead(json, out GameState state);

            Assert.IsFalse(read);
            Assert.IsNull(state);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void TryRead_OfEmptyOrBlankText_ReturnsFalse(string text)
        {
            bool read = GameStateJson.TryRead(text, out GameState state);

            Assert.IsFalse(read);
            Assert.IsNull(state);
        }

        [Test]
        public void TryRead_OfAnEmptyObject_ReturnsTheDefaults()
        {
            bool read = GameStateJson.TryRead("{}", out GameState state);

            Assert.IsTrue(read);
            Assert.That(state.ownedItemIds, Is.Empty);
            Assert.That(state.consumedWorldObjectIds, Is.Empty);
            Assert.That(state.storyBools, Is.Empty);
            Assert.That(state.storyNumbers, Is.Empty);
            Assert.That(state.storyTexts, Is.Empty);
            Assert.That(state.currentScene, Is.EqualTo(""));
            Assert.That(state.currentEntryPointId, Is.EqualTo(""));
        }
    }
}
