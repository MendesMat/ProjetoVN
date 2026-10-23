using System.IO;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.GameFlow.Menu;
using UnityEditor;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class PauseMenuTests
    {
        private const string GameUIPrefabPath = "Assets/Prefabs/UI/GameUI.prefab";
        private const string ManagersPrefabPath = "Assets/Prefabs/Resources/Managers.prefab";
        private const string MainMenuSceneField = "mainMenuScene";

        [Test]
        public void Project_HasASinglePauseMenu_InTheGameUIPrefab()
        {
            var locations = ProjectComponents
                .ReadAll<PauseMenu, string>(menu => menu.name)
                .Select(reading => reading.Location)
                .ToList();

            int inTheGameUI = locations.Count(location => location.StartsWith(GameUIPrefabPath));
            string[] outsideThePrefabs = locations
                .Where(location => !location.StartsWith(GameUIPrefabPath) && !location.StartsWith(ManagersPrefabPath))
                .ToArray();

            Assert.AreEqual(1, inTheGameUI, $"A pausa mora no GameUI.prefab. Achados: {string.Join("; ", locations)}");
            CollectionAssert.IsEmpty(outsideThePrefabs,
                "o PauseMenu é da interface de jogo (D-19) e não pode estar numa cena nem em outro prefab");
        }

        [Test]
        public void PauseMenu_MainMenuSceneIsTheFirstSceneOfTheBuild()
        {
            EditorBuildSettingsScene firstScene = EditorBuildSettings.scenes.FirstOrDefault(scene => scene.enabled);
            Assert.IsNotNull(firstScene, "A lista de cenas do build não tem nenhuma cena habilitada.");
            string menuScene = Path.GetFileNameWithoutExtension(firstScene.path);

            var wrongScenes = ProjectComponents
                .ReadAll<PauseMenu, string>(MainMenuSceneOf)
                .Where(reading => reading.Value != menuScene)
                .Select(reading => $"{reading.Location}: cena '{reading.Value}'");

            CollectionAssert.IsEmpty(wrongScenes,
                $"o Voltar ao menu precisa carregar '{menuScene}', a primeira cena do build, que tem o MainMenu");
        }

        private static string MainMenuSceneOf(PauseMenu menu)
        {
            SerializedProperty field = new SerializedObject(menu).FindProperty(MainMenuSceneField);
            if (field == null)
                Assert.Fail($"O campo '{MainMenuSceneField}' do PauseMenu foi renomeado; atualize este teste.");

            return field.stringValue;
        }
    }
}
