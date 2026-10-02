using System.Collections.Generic;
using NUnit.Framework;
using ProjetoVN.GameFlow.Persistence;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class GameStateTests
    {
        [Test]
        public void RoundTrips_ThroughJson()
        {
            var original = new GameState
            {
                ownedItemIds = new List<string> { "chave", "moeda" },
                consumedWorldObjectIds = new List<string> { "obj-1", "obj-2" },
                storyBools = new List<StoryBoolEntry>
                {
                    new() { name = "$falou_com_gotica", value = true },
                    new() { name = "$porta_destrancada", value = false }
                },
                storyNumbers = new List<StoryNumberEntry> { new() { name = "$afinidade_gotica", value = 2.5f } },
                storyTexts = new List<StoryTextEntry> { new() { name = "$nome_jogador", value = "Ana" } },
                currentScene = "SalaDeTeste"
            };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<GameState>(json);

            Assert.That(restored.ownedItemIds, Is.EqualTo(original.ownedItemIds));
            Assert.That(restored.consumedWorldObjectIds, Is.EqualTo(original.consumedWorldObjectIds));
            Assert.That(restored.storyBools, Is.EqualTo(original.storyBools));
            Assert.That(restored.storyNumbers, Is.EqualTo(original.storyNumbers));
            Assert.That(restored.storyTexts, Is.EqualTo(original.storyTexts));
            Assert.That(restored.currentScene, Is.EqualTo(original.currentScene));
        }

        [Test]
        public void RoundTrips_WhenEmpty()
        {
            var original = new GameState();

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<GameState>(json);

            Assert.That(restored.ownedItemIds, Is.Empty);
            Assert.That(restored.consumedWorldObjectIds, Is.Empty);
            Assert.That(restored.storyBools, Is.Empty);
            Assert.That(restored.storyNumbers, Is.Empty);
            Assert.That(restored.storyTexts, Is.Empty);
            Assert.That(restored.currentScene, Is.EqualTo(""));
        }
    }
}
