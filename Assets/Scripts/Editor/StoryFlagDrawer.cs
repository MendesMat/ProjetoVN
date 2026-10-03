using System.Collections.Generic;
using System.Linq;
using ProjetoVN.Core.State;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ProjetoVN.Editor
{
    /// <summary>
    /// Desenha um campo <see cref="StoryFlagAttribute"/> como lista das variáveis booleanas declaradas em
    /// <c>variaveis.yarn</c>. Só vale quando o Inspector é de UI Toolkit, como o do <c>LockedActionBehaviour</c>.
    /// </summary>
    [CustomPropertyDrawer(typeof(StoryFlagAttribute))]
    public sealed class StoryFlagDrawer : PropertyDrawer
    {
        private const string NoneLabel = "(nenhuma)";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.String)
                return new HelpBox("[StoryFlag] só vale em um campo string.", HelpBoxMessageType.Error);

            if (!DeclaredStoryFlags.TryLoad(out IReadOnlyList<StoryFlagDeclaration> flags, out string problem))
                return PlainTextFieldWithWarning(property, problem);

            return new StoryFlagField(property.Copy(), flags);
        }

        private static VisualElement PlainTextFieldWithWarning(SerializedProperty property, string problem)
        {
            var root = new VisualElement();
            root.Add(new HelpBox(problem + " O campo vale como texto comum.", HelpBoxMessageType.Warning));
            root.Add(new TextField(property.displayName) { bindingPath = property.propertyPath, tooltip = property.tooltip });
            return root;
        }

        private sealed class StoryFlagField : VisualElement
        {
            private readonly SerializedProperty _property;
            private readonly IReadOnlyList<StoryFlagDeclaration> _flags;
            private readonly DropdownField _dropdown;
            private readonly Label _description = new();
            private readonly HelpBox _undeclaredWarning = new(string.Empty, HelpBoxMessageType.Warning);

            public StoryFlagField(SerializedProperty property, IReadOnlyList<StoryFlagDeclaration> flags)
            {
                _property = property;
                _flags = flags;

                _dropdown = new DropdownField(property.displayName) { tooltip = property.tooltip };
                _dropdown.AddToClassList(BaseField<string>.alignedFieldUssClassName);
                _dropdown.RegisterValueChangedCallback(OnChosen);

                _description.style.whiteSpace = WhiteSpace.Normal;
                _description.style.opacity = 0.7f;

                Add(_dropdown);
                Add(_description);
                Add(_undeclaredWarning);

                this.TrackPropertyValue(_property, _ => Refresh());
                Refresh();
            }

            private void OnChosen(ChangeEvent<string> evt)
            {
                _property.stringValue = evt.newValue == NoneLabel ? string.Empty : evt.newValue;
                _property.serializedObject.ApplyModifiedProperties();
                Refresh();
            }

            private void Refresh()
            {
                string current = _property.stringValue;
                bool hasValue = !string.IsNullOrEmpty(current);
                bool isUndeclared = hasValue && _flags.All(flag => flag.Name != current);

                var choices = new List<string> { NoneLabel };
                choices.AddRange(_flags.Select(flag => flag.Name));
                if (isUndeclared) choices.Add(current);

                _dropdown.choices = choices;
                _dropdown.SetValueWithoutNotify(hasValue ? current : NoneLabel);

                ShowDescriptionOf(current);
                ShowUndeclaredWarning(isUndeclared, current);
            }

            private void ShowDescriptionOf(string name)
            {
                string description = _flags.FirstOrDefault(flag => flag.Name == name).Description;
                _description.text = description ?? string.Empty;
                _description.style.display = string.IsNullOrEmpty(description) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            private void ShowUndeclaredWarning(bool isUndeclared, string name)
            {
                _undeclaredWarning.text = $"A variável '{name}' não está declarada como booleana em variaveis.yarn.";
                _undeclaredWarning.style.display = isUndeclared ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
