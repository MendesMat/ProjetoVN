using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjetoVN.Core;
using ProjetoVN.GameFlow;
using ProjetoVN.GameFlow.Rooms;
using UnityEditor;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ScriptNodeFieldsTests
    {
        private const string NodeNameField = "nodeName";

        [Test]
        public void ScriptNodeFields_AreFieldsOfComponents()
        {
            FieldInfo[] fields = ScriptNodeFields();

            CollectionAssert.IsNotEmpty(fields, "nenhum campo [ScriptNode] encontrado");
            IEnumerable<string> outsideComponents = fields
                .Where(field => !typeof(MonoBehaviour).IsAssignableFrom(field.DeclaringType))
                .Select(NameOf);
            CollectionAssert.IsEmpty(outsideComponents,
                "o teste de conteúdo lê só campo direto de componente de cena ou prefab");
        }

        [Test]
        public void ScriptNodeFields_IncludeTheDialogueTriggerAndTheRoomArrival()
        {
            IEnumerable<string> names = ScriptNodeFields().Select(NameOf);

            CollectionAssert.Contains(names, $"{nameof(InteractableDialogueTrigger)}.{NodeNameField}");
            CollectionAssert.Contains(names, $"{nameof(RoomArrivalDialogue)}.{NodeNameField}");
        }

        [Test]
        public void ScenesAndPrefabs_ScriptNodeFieldsNameNodesThatExist()
        {
            var nodesInTheScript = new HashSet<string>(ScriptRun.FromProjectFiles().NodeNames);

            IEnumerable<string> missing = FilledScriptNodes()
                .Where(node => !nodesInTheScript.Contains(node.Value))
                .Select(node => $"{node.Location} nó '{node.Value}'");

            CollectionAssert.IsEmpty(missing, "campos de nó de roteiro apontam para nós que não existem em Assets/Roteiro/");
        }

        private static FieldInfo[] ScriptNodeFields() =>
            TypeCache.GetFieldsWithAttribute<ScriptNodeAttribute>().ToArray();

        private static List<ComponentReading<string>> FilledScriptNodes()
        {
            FieldInfo[] fields = ScriptNodeFields();

            return ProjectComponents
                .ReadAll<MonoBehaviour, (string Field, string Value)[]>(component => ScriptNodesOf(component, fields))
                .SelectMany(reading => reading.Value.Select(node =>
                    new ComponentReading<string>($"{reading.Location}: {node.Field}", node.Value)))
                .Where(node => !string.IsNullOrWhiteSpace(node.Value))
                .ToList();
        }

        private static (string Field, string Value)[] ScriptNodesOf(MonoBehaviour component, FieldInfo[] fields) =>
            fields
                .Where(field => field.DeclaringType.IsInstanceOfType(component))
                .Select(field => (field.Name, SerializedFields.StringOf(component, field.Name)))
                .ToArray();

        private static string NameOf(FieldInfo field) => $"{field.DeclaringType.Name}.{field.Name}";
    }
}
