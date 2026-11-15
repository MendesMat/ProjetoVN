using System.Linq;
using NUnit.Framework;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class BuildRoomsTests
    {
        private const string FirstCorridor = "Assets/Scenes/Capitulo1/Corredor.unity";
        private const string SecondCorridor = "Assets/Scenes/Capitulo2/Corredor.unity";

        [Test]
        public void In_NamesEachRoomByTheSceneFileName()
        {
            const string path = "Assets/Scenes/[Teste] Sala B.unity";

            BuildRooms rooms = BuildRooms.In(new[] { path });

            Assert.AreEqual(path, rooms.ScenePathByRoom["[Teste] Sala B"]);
        }

        [Test]
        public void In_WithDistinctNames_MapsEveryRoomAndRepeatsNone()
        {
            BuildRooms rooms = BuildRooms.In(new[]
            {
                "Assets/Scenes/Menu.unity",
                "Assets/Scenes/Onibus.unity",
                "Assets/Scenes/Posto.unity",
            });

            Assert.AreEqual(3, rooms.ScenePathByRoom.Count);
            CollectionAssert.IsEmpty(rooms.Repeated);
        }

        [Test]
        public void In_WithTheSameNameInTwoFolders_DoesNotThrowAndKeepsTheFirstPath()
        {
            BuildRooms rooms = null;

            Assert.DoesNotThrow(() => rooms = BuildRooms.In(new[] { FirstCorridor, SecondCorridor }));

            Assert.AreEqual(1, rooms.ScenePathByRoom.Count);
            Assert.AreEqual(FirstCorridor, rooms.ScenePathByRoom["Corredor"]);
        }

        [Test]
        public void In_WithTheSameNameInTwoFolders_ListsTheNameAndBothPaths()
        {
            BuildRooms rooms = BuildRooms.In(new[] { FirstCorridor, SecondCorridor });

            RepeatedRoom repeated = rooms.Repeated.Single();

            Assert.AreEqual("Corredor", repeated.Name);
            CollectionAssert.AreEqual(new[] { FirstCorridor, SecondCorridor }, repeated.ScenePaths);
            Assert.AreEqual($"'Corredor' repetido em: {FirstCorridor}; {SecondCorridor}", repeated.ToString());
        }

        [Test]
        public void In_WithANameThreeTimes_ListsItOnceWithTheThreePaths()
        {
            const string thirdCorridor = "Assets/Scenes/Capitulo3/Corredor.unity";

            BuildRooms rooms = BuildRooms.In(new[] { FirstCorridor, SecondCorridor, thirdCorridor });

            RepeatedRoom repeated = rooms.Repeated.Single();
            CollectionAssert.AreEqual(new[] { FirstCorridor, SecondCorridor, thirdCorridor }, repeated.ScenePaths);
        }

        [Test]
        public void In_WithNoScenes_IsEmpty()
        {
            BuildRooms rooms = BuildRooms.In(Enumerable.Empty<string>());

            CollectionAssert.IsEmpty(rooms.ScenePathByRoom);
            CollectionAssert.IsEmpty(rooms.Repeated);
        }
    }
}
