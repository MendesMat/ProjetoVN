using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>Um nome de sala que duas ou mais cenas habilitadas do build têm em comum.</summary>
    public readonly struct RepeatedRoom
    {
        public string Name { get; }
        public IReadOnlyList<string> ScenePaths { get; }

        public RepeatedRoom(string name, IReadOnlyList<string> scenePaths)
        {
            Name = name;
            ScenePaths = scenePaths;
        }

        public override string ToString() => string.Empty;
    }

    public sealed class BuildRooms
    {
        public IReadOnlyDictionary<string, string> ScenePathByRoom { get; } = new Dictionary<string, string>();

        public IReadOnlyList<RepeatedRoom> Repeated { get; } = new List<RepeatedRoom>();

        public static BuildRooms In(IEnumerable<string> enabledScenePaths) => new BuildRooms();

        public static BuildRooms OfTheBuildSettings() =>
            In(EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path));
    }
}
