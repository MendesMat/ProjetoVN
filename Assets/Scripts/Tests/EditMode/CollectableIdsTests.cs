using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.Inventory;
using UnityEditor;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class CollectableIdsTests
    {
        private const string PersistentIdField = "persistentId";

        [Test]
        public void Scenes_CollectablePersistentIdsAreFilledAndUniqueAcrossRooms()
        {
            var problems = new List<string>();
            var readings = ProjectComponents
                .ReadAll<CollectableItemBehaviour, (string ScenePath, string Id)>(ScenePathAndIdOf)
                .Where(reading => !string.IsNullOrEmpty(reading.Value.ScenePath))
                .ToList();

            problems.AddRange(readings
                .Where(reading => string.IsNullOrWhiteSpace(reading.Value.Id))
                .Select(reading => $"{reading.Location}: id vazio"));
            problems.AddRange(readings
                .Where(reading => !string.IsNullOrWhiteSpace(reading.Value.Id))
                .GroupBy(reading => reading.Value.Id)
                .Where(group => group.Count() > 1)
                .Select(group => $"'{group.Key}' repetido em: {string.Join("; ", group.Select(reading => reading.Location))}"));

            CollectionAssert.IsEmpty(problems,
                "coletáveis sem id persistente, ou com o mesmo id em mais de um objeto (em qualquer sala)");
        }

        private static (string ScenePath, string Id) ScenePathAndIdOf(CollectableItemBehaviour collectable) =>
            (collectable.gameObject.scene.path, PersistentIdOf(collectable));

        private static string PersistentIdOf(CollectableItemBehaviour collectable)
        {
            SerializedProperty field = new SerializedObject(collectable).FindProperty(PersistentIdField);
            if (field == null)
                Assert.Fail($"O campo '{PersistentIdField}' do {nameof(CollectableItemBehaviour)} foi renomeado; atualize este teste.");

            return field.stringValue;
        }
    }
}
