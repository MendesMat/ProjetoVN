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
                storyFlagIds = new List<string> { "falou-com-gotica", "porta-destrancada" },
                currentScene = "TesteCameraPan"
            };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<GameState>(json);

            Assert.That(restored.ownedItemIds, Is.EqualTo(original.ownedItemIds));
            Assert.That(restored.consumedWorldObjectIds, Is.EqualTo(original.consumedWorldObjectIds));
            Assert.That(restored.storyFlagIds, Is.EqualTo(original.storyFlagIds));
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
            Assert.That(restored.storyFlagIds, Is.Empty);
            Assert.That(restored.currentScene, Is.EqualTo(""));
        }

        #region Compatibilidade com saves antigos
        [Test]
        public void FromJson_WithoutTheStoryFlagsField_DoesNotThrow_AndLeavesItEmpty()
        {
            // JSON exatamente no formato gravado antes de storyFlagIds existir (ARCH-09).
            const string savedBeforeStoryFlags =
                "{\"ownedItemIds\":[\"item-teste-01\"]," +
                "\"consumedWorldObjectIds\":[\"89633904-2ce7-41ee-8fc1-479a4cb3b85e\"]," +
                "\"currentScene\":\"[Teste] Mecanicas\"}";

            GameState restored = null;
            Assert.DoesNotThrow(() => restored = JsonUtility.FromJson<GameState>(savedBeforeStoryFlags),
                "um save antigo precisa continuar carregando depois que o formato ganha um campo novo");

            Assert.That(restored.ownedItemIds, Is.EqualTo(new List<string> { "item-teste-01" }));
            Assert.That(restored.storyFlagIds, Is.Empty,
                "sem o campo no JSON, a lista vem vazia — não nula — então StoryFlags.ReplaceAll recebe algo seguro");
        }
        #endregion
    }
}
