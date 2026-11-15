using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.GameFlow.Rooms;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class RoomLinksTests
    {
        private const string DestinationRoomField = "destinationRoom";
        private const string EntryPointIdField = "entryPointId";

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
            ILookup<string, string> entryPointIdsByScene = SceneEntryPoints.IdsByScenePath();

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
            List<ComponentReading<(string ScenePath, string Id)>> readings = SceneEntryPoints.ReadAll();

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

        private static string DestinationRoomOf(RoomExit exit) =>
            SerializedFields.StringOf(exit, DestinationRoomField);

        private static (string Room, string EntryPointId) DestinationAndEntryPointOf(RoomExit exit) =>
            (SerializedFields.StringOf(exit, DestinationRoomField),
                SerializedFields.StringOf(exit, EntryPointIdField));
    }
}
