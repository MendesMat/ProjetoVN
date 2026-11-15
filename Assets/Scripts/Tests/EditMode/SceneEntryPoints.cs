using System.Collections.Generic;
using System.Linq;
using ProjetoVN.GameFlow.Rooms;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>
    /// Os pontos de entrada das cenas de <c>Assets/</c>. Ponto em prefab é ignorado: só a cena da sala diz onde se chega.
    /// </summary>
    public static class SceneEntryPoints
    {
        private const string IdField = "id";

        public static List<ComponentReading<(string ScenePath, string Id)>> ReadAll() =>
            ProjectComponents.ReadAll<RoomEntryPoint, (string ScenePath, string Id)>(ScenePathAndIdOf)
                .Where(reading => !string.IsNullOrEmpty(reading.Value.ScenePath))
                .ToList();

        public static ILookup<string, string> IdsByScenePath() =>
            ReadAll().ToLookup(reading => reading.Value.ScenePath, reading => reading.Value.Id);

        private static (string ScenePath, string Id) ScenePathAndIdOf(RoomEntryPoint entryPoint) =>
            (entryPoint.gameObject.scene.path, SerializedFields.StringOf(entryPoint, IdField));
    }
}
