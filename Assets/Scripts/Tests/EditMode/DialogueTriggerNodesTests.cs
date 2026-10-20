using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.GameFlow;
using UnityEditor;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class DialogueTriggerNodesTests
    {
        private const string NodeNameField = "nodeName";

        [Test]
        public void ScenesAndPrefabs_TriggerOnlyNodesThatExistInTheScript()
        {
            var nodesInTheScript = new HashSet<string>(ScriptRun.FromProjectFiles().NodeNames);

            IEnumerable<string> missing = ProjectComponents
                .ReadAll<InteractableDialogueTrigger, string>(NodeNameOf)
                .Where(reading => !string.IsNullOrWhiteSpace(reading.Value))
                .Where(reading => !nodesInTheScript.Contains(reading.Value))
                .Select(reading => $"{reading.Location}: nó '{reading.Value}'");

            CollectionAssert.IsEmpty(missing,
                "gatilhos de diálogo apontam para nós que não existem em Assets/Roteiro/");
        }

        private static string NodeNameOf(InteractableDialogueTrigger trigger)
        {
            SerializedProperty nodeName = new SerializedObject(trigger).FindProperty(NodeNameField);
            if (nodeName == null)
                Assert.Fail($"O campo '{NodeNameField}' do InteractableDialogueTrigger foi renomeado; atualize este teste.");

            return nodeName.stringValue;
        }
    }
}
