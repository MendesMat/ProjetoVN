using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using Yarn.Unity;

namespace ProjetoVN.PocYarn.Editor
{
    [CustomPropertyDrawer(typeof(PocYarnVariableAttribute))]
    public sealed class PocYarnVariableDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            if (property.propertyType != SerializedPropertyType.String)
            {
                root.Add(new HelpBox("PocYarnVariable só funciona em campo de texto.", HelpBoxMessageType.Error));
                return root;
            }

            YarnProject project = FindProject(property);
            if (project == null)
            {
                root.Add(new HelpBox("Atribua o projeto Yarn para listar as variáveis.", HelpBoxMessageType.Warning));
                return root;
            }

            List<string> names = DeclaredVariables.NamesOf(project);
            var dropdown = new DropdownField(property.displayName, names, names.IndexOf(property.stringValue));
            dropdown.RegisterValueChangedCallback(change => Store(property, change.newValue));
            root.Add(dropdown);

            if (IsOrphan(property.stringValue, names))
                root.Add(new HelpBox($"A variável '{property.stringValue}' não está declarada no projeto Yarn.", HelpBoxMessageType.Warning));

            return root;
        }

        private YarnProject FindProject(SerializedProperty property)
        {
            string projectField = ((PocYarnVariableAttribute)attribute).ProjectField;
            return property.serializedObject.FindProperty(projectField)?.objectReferenceValue as YarnProject;
        }

        private static bool IsOrphan(string current, List<string> declaredNames)
            => !string.IsNullOrEmpty(current) && !declaredNames.Contains(current);

        private static void Store(SerializedProperty property, string value)
        {
            property.stringValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
