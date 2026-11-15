using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.GameFlow.Rooms;
using UnityEditor;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class RoomLinksTests
    {
        private const string DestinationRoomField = "destinationRoom";
        private const string EntryPointIdField = "entryPointId";
        private const string IdField = "id";

        [Test]
        public void ScenesAndPrefabs_ExitsNameARoomInTheBuildList()
        {
            IReadOnlyDictionary<string, string> scenePathByRoom = BuildRooms.OfTheBuildSettings().ScenePathByRoom;

            IEnumerable<string> invalid = ProjectComponents
                .ReadAll<RoomExit, string>(DestinationRoomOf)
                .Where(reading => !scenePathByRoom.ContainsKey(reading.Value))
                .Select(reading => $"{reading.Location}: destino '{reading.Value}'");

            CollectionAssert.IsEmpty(invalid,
                "saídas sem destino, ou com destino que não é uma cena habilitada na lista de cenas do build");
        }

        [Test]
        public void ScenesAndPrefabs_ExitsNameAnEntryPointOfTheDestination()
        {
            IReadOnlyDictionary<string, string> scenePathByRoom = BuildRooms.OfTheBuildSettings().ScenePathByRoom;
            ILookup<string, string> entryPointIdsByScene = EntryPointIdsByScenePath();

            IEnumerable<string> missing = ProjectComponents
                .ReadAll<RoomExit, (string Room, string EntryPointId)>(DestinationAndEntryPointOf)
                .Where(reading => scenePathByRoom.ContainsKey(reading.Value.Room))
                .Where(reading => !entryPointIdsByScene[scenePathByRoom[reading.Value.Room]]
                    .Contains(reading.Value.EntryPointId))
                .Select(reading => $"{reading.Location}: ponto '{reading.Value.EntryPointId}' em '{reading.Value.Room}'");

            CollectionAssert.IsEmpty(missing,
                "saídas apontam para um ponto de entrada que a sala de destino não tem (ou deixaram o ponto vazio)");
        }

        [Test]
        public void BuildList_RoomNamesAreUnique()
        {
            IEnumerable<string> repeated = BuildRooms.OfTheBuildSettings().Repeated.Select(room => room.ToString());

            CollectionAssert.IsEmpty(repeated,
                "duas cenas habilitadas na lista do build têm o mesmo nome; a sala é achada pelo nome, então só uma seria alcançável");
        }

        [Test]
        public void Scenes_EntryPointIdsAreFilledAndUniquePerRoom()
        {
            var problems = new List<string>();
            var readings = ProjectComponents.ReadAll<RoomEntryPoint, (string ScenePath, string Id)>(ScenePathAndIdOf)
                .Where(reading => !string.IsNullOrEmpty(reading.Value.ScenePath))
                .ToList();

            problems.AddRange(readings
                .Where(reading => string.IsNullOrWhiteSpace(reading.Value.Id))
                .Select(reading => $"{reading.Location}: id vazio"));
            problems.AddRange(readings
                .Where(reading => !string.IsNullOrWhiteSpace(reading.Value.Id))
                .GroupBy(reading => reading.Value)
                .Where(group => group.Count() > 1)
                .Select(group => $"{group.Key.ScenePath}: '{group.Key.Id}' repetido"));

            CollectionAssert.IsEmpty(problems,
                "pontos de entrada sem id, ou com o mesmo id mais de uma vez na mesma cena");
        }

        private static ILookup<string, string> EntryPointIdsByScenePath() =>
            ProjectComponents.ReadAll<RoomEntryPoint, (string ScenePath, string Id)>(ScenePathAndIdOf)
                .Where(reading => !string.IsNullOrEmpty(reading.Value.ScenePath))
                .ToLookup(reading => reading.Value.ScenePath, reading => reading.Value.Id);

        private static string DestinationRoomOf(RoomExit exit) =>
            StringField(exit, DestinationRoomField, nameof(RoomExit));

        private static (string Room, string EntryPointId) DestinationAndEntryPointOf(RoomExit exit) =>
            (StringField(exit, DestinationRoomField, nameof(RoomExit)),
                StringField(exit, EntryPointIdField, nameof(RoomExit)));

        private static (string ScenePath, string Id) ScenePathAndIdOf(RoomEntryPoint entryPoint) =>
            (entryPoint.gameObject.scene.path, StringField(entryPoint, IdField, nameof(RoomEntryPoint)));

        private static string StringField(UnityEngine.Object target, string fieldName, string typeName)
        {
            SerializedProperty field = new SerializedObject(target).FindProperty(fieldName);
            if (field == null)
                Assert.Fail($"O campo '{fieldName}' do {typeName} foi renomeado; atualize este teste.");

            return field.stringValue;
        }
    }
}
