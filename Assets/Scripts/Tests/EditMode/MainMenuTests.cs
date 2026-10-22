using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.GameFlow.Menu;
using ProjetoVN.GameFlow.Rooms;
using UnityEditor;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class MainMenuTests
    {
        private const string StartRoomField = "startRoom";
        private const string StartEntryPointIdField = "startEntryPointId";
        private const string IdField = "id";

        [Test]
        public void BuildList_StartsWithTheMainMenu()
        {
            EditorBuildSettingsScene firstScene = EditorBuildSettings.scenes.FirstOrDefault(scene => scene.enabled);
            Assert.IsNotNull(firstScene, "A lista de cenas do build não tem nenhuma cena habilitada.");

            bool firstSceneHasAMainMenu = ProjectComponents
                .ReadAll<MainMenu, string>(menu => menu.gameObject.scene.path)
                .Any(reading => reading.Value == firstScene.path);

            Assert.IsTrue(firstSceneHasAMainMenu,
                $"A primeira cena do build ('{firstScene.path}') não tem um MainMenu: o jogo não começa no menu.");
        }

        [Test]
        public void Scenes_HaveASingleMainMenu()
        {
            IEnumerable<string> found = ProjectComponents
                .ReadAll<MainMenu, string>(menu => menu.name)
                .Select(reading => reading.Location);

            Assert.AreEqual(1, found.Count(),
                $"A sala inicial do Novo Jogo é configurada em um único MainMenu. Achados: {string.Join("; ", found)}");
        }

        [Test]
        public void MainMenu_StartRoomIsARoomOfTheBuildList()
        {
            Dictionary<string, string> scenePathByRoom = EnabledRoomsOfTheBuild();
            ILookup<string, string> entryPointIdsByScene = EntryPointIdsByScenePath();

            IEnumerable<string> problems = ProjectComponents
                .ReadAll<MainMenu, MenuStart>(StartOf)
                .Select(reading => ProblemWithTheStart(reading, scenePathByRoom, entryPointIdsByScene))
                .Where(problem => problem != null);

            CollectionAssert.IsEmpty(problems,
                "a sala inicial do Novo Jogo precisa estar preenchida, ser uma sala do build (não o próprio menu) " +
                "e ter o ponto de entrada citado");
        }

        private static string ProblemWithTheStart(ComponentReading<MenuStart> reading,
            Dictionary<string, string> scenePathByRoom, ILookup<string, string> entryPointIdsByScene)
        {
            MenuStart start = reading.Value;
            bool isInTheBuildList = scenePathByRoom.TryGetValue(start.Room, out string roomScenePath);
            if (!isInTheBuildList)
                return $"{reading.Location}: sala '{start.Room}'";

            if (roomScenePath == start.MenuScenePath)
                return $"{reading.Location}: sala '{start.Room}' é o próprio menu";

            bool citesAnEntryPoint = !string.IsNullOrEmpty(start.EntryPointId);
            if (citesAnEntryPoint && !entryPointIdsByScene[roomScenePath].Contains(start.EntryPointId))
                return $"{reading.Location}: ponto '{start.EntryPointId}' em '{start.Room}'";

            return null;
        }

        private static Dictionary<string, string> EnabledRoomsOfTheBuild() =>
            EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .ToDictionary(scene => Path.GetFileNameWithoutExtension(scene.path), scene => scene.path);

        private static ILookup<string, string> EntryPointIdsByScenePath() =>
            ProjectComponents.ReadAll<RoomEntryPoint, (string ScenePath, string Id)>(ScenePathAndIdOf)
                .Where(reading => !string.IsNullOrEmpty(reading.Value.ScenePath))
                .ToLookup(reading => reading.Value.ScenePath, reading => reading.Value.Id);

        private static MenuStart StartOf(MainMenu menu) =>
            new MenuStart(
                menu.gameObject.scene.path,
                StringField(menu, StartRoomField, nameof(MainMenu)),
                StringField(menu, StartEntryPointIdField, nameof(MainMenu)));

        private static (string ScenePath, string Id) ScenePathAndIdOf(RoomEntryPoint entryPoint) =>
            (entryPoint.gameObject.scene.path, StringField(entryPoint, IdField, nameof(RoomEntryPoint)));

        private static string StringField(UnityEngine.Object target, string fieldName, string typeName)
        {
            SerializedProperty field = new SerializedObject(target).FindProperty(fieldName);
            if (field == null)
                Assert.Fail($"O campo '{fieldName}' do {typeName} foi renomeado; atualize este teste.");

            return field.stringValue;
        }

        private readonly struct MenuStart
        {
            public string MenuScenePath { get; }
            public string Room { get; }
            public string EntryPointId { get; }

            public MenuStart(string menuScenePath, string room, string entryPointId)
            {
                MenuScenePath = menuScenePath;
                Room = room;
                EntryPointId = entryPointId;
            }
        }
    }
}
