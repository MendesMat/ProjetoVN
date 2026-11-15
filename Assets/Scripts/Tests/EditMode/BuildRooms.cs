using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>Um nome de sala que duas ou mais cenas habilitadas do build têm em comum.</summary>
    public readonly struct RepeatedRoom
    {
        public string Name { get; }

        /// <summary>Os caminhos das cenas com esse nome, na ordem da lista do build.</summary>
        public IReadOnlyList<string> ScenePaths { get; }

        public RepeatedRoom(string name, IReadOnlyList<string> scenePaths)
        {
            Name = name;
            ScenePaths = scenePaths;
        }

        public override string ToString() => $"'{Name}' repetido em: {string.Join("; ", ScenePaths)}";
    }

    /// <summary>
    /// As salas do build: o jogo acha a sala pelo nome da cena, então o nome é a identidade dela. Um nome repetido é
    /// erro de conteúdo, e por isso fica registrado em <see cref="Repeated"/> em vez de lançar exceção, para um teste
    /// só reprovar com os caminhos e os outros continuarem conferindo o resto.
    /// </summary>
    public sealed class BuildRooms
    {
        /// <summary>O caminho de cada sala. Com nome repetido, vale o primeiro da ordem do build.</summary>
        public IReadOnlyDictionary<string, string> ScenePathByRoom { get; }

        public IReadOnlyList<RepeatedRoom> Repeated { get; }

        private BuildRooms(IReadOnlyDictionary<string, string> scenePathByRoom, IReadOnlyList<RepeatedRoom> repeated)
        {
            ScenePathByRoom = scenePathByRoom;
            Repeated = repeated;
        }

        public static BuildRooms In(IEnumerable<string> enabledScenePaths)
        {
            List<IGrouping<string, string>> pathsByName = enabledScenePaths
                .GroupBy(path => Path.GetFileNameWithoutExtension(path))
                .ToList();

            Dictionary<string, string> scenePathByRoom = pathsByName.ToDictionary(paths => paths.Key, paths => paths.First());
            List<RepeatedRoom> repeated = pathsByName
                .Where(paths => paths.Count() > 1)
                .Select(paths => new RepeatedRoom(paths.Key, paths.ToList()))
                .ToList();

            return new BuildRooms(scenePathByRoom, repeated);
        }

        public static BuildRooms OfTheBuildSettings() =>
            In(EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path));
    }
}
