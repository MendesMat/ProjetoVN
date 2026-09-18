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
                currentScene = "TesteCameraPan"
            };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<GameState>(json);

            Assert.That(restored.ownedItemIds, Is.EqualTo(original.ownedItemIds));
            Assert.That(restored.consumedWorldObjectIds, Is.EqualTo(original.consumedWorldObjectIds));
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
            Assert.That(restored.currentScene, Is.EqualTo(""));
        }
    }
}
