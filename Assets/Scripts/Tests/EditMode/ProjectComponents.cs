using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjetoVN.Tests.EditMode
{
    public readonly struct ComponentReading<TResult>
    {
        public string Location { get; }
        public TResult Value { get; }

        public ComponentReading(string location, TResult value)
        {
            Location = location;
            Value = value;
        }
    }

    /// <summary>
    /// Percorre os componentes de um tipo em todas as cenas e prefabs de <c>Assets/</c> (os pacotes ficam de fora).
    /// O que se quer saber de cada componente sai por <c>read</c>, que roda enquanto a cena está aberta: o
    /// componente deixa de existir quando ela fecha. Uma cena já aberta é lida como está e nunca é fechada.
    /// </summary>
    public static class ProjectComponents
    {
        private static readonly string[] AssetsFolder = { "Assets" };

        public static List<ComponentReading<TResult>> ReadAll<TComponent, TResult>(Func<TComponent, TResult> read)
            where TComponent : Component
        {
            var readings = new List<ComponentReading<TResult>>();
            foreach (string scenePath in PathsOf("t:Scene"))
                readings.AddRange(ReadScene(scenePath, read));
            foreach (string prefabPath in PathsOf("t:Prefab"))
                readings.AddRange(ReadPrefab(prefabPath, read));

            return readings;
        }

        private static IEnumerable<string> PathsOf(string filter) =>
            AssetDatabase.FindAssets(filter, AssetsFolder).Select(AssetDatabase.GUIDToAssetPath);

        private static List<ComponentReading<TResult>> ReadScene<TComponent, TResult>(
            string scenePath, Func<TComponent, TResult> read) where TComponent : Component
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.isLoaded) return ReadRoots(scenePath, scene.GetRootGameObjects(), read);

            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                return ReadRoots(scenePath, scene.GetRootGameObjects(), read);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static List<ComponentReading<TResult>> ReadPrefab<TComponent, TResult>(
            string prefabPath, Func<TComponent, TResult> read) where TComponent : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return ReadRoots(prefabPath, new[] { prefab }, read);
        }

        private static List<ComponentReading<TResult>> ReadRoots<TComponent, TResult>(
            string assetPath, IEnumerable<GameObject> roots, Func<TComponent, TResult> read)
            where TComponent : Component
        {
            return roots
                .SelectMany(root => root.GetComponentsInChildren<TComponent>(true))
                .Select(component => new ComponentReading<TResult>(
                    $"{assetPath}: {HierarchyPathOf(component.transform)}", read(component)))
                .ToList();
        }

        private static string HierarchyPathOf(Transform transform) =>
            transform.parent == null ? transform.name : $"{HierarchyPathOf(transform.parent)}/{transform.name}";
    }
}
