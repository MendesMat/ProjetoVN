using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ProjetoVN.Editor
{
    public readonly struct NameChoice
    {
        public string Name { get; }
        public string Description { get; }

        public NameChoice(string name, string description)
        {
            Name = name;
            Description = description ?? string.Empty;
        }
    }

    /// <summary>
    /// Um campo <c>string</c> do Inspector desenhado como lista de nomes. O primeiro item da lista é o rótulo do
    /// vazio e grava texto vazio. Um nome gravado que a lista não traz continua no campo, entra no fim da lista
    /// e ganha um aviso.
    /// </summary>
    public sealed class NameChoiceField : VisualElement
    {
        private readonly SerializedProperty _property;
        private readonly string _noneLabel;
        private readonly IReadOnlyList<NameChoice> _choices;
        private readonly Func<string, string> _missingWarningFor;
        private readonly DropdownField _dropdown;
        private readonly Label _description = new();
        private readonly HelpBox _missingWarning = new(string.Empty, HelpBoxMessageType.Warning);

        public NameChoiceField(SerializedProperty property, string noneLabel, IReadOnlyList<NameChoice> choices,
            Func<string, string> missingWarningFor)
        {
            _property = property;
            _noneLabel = noneLabel;
            _choices = choices;
            _missingWarningFor = missingWarningFor;

            _dropdown = new DropdownField(property.displayName) { tooltip = property.tooltip };
            _dropdown.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            _dropdown.RegisterValueChangedCallback(OnChosen);

            _description.style.whiteSpace = WhiteSpace.Normal;
            _description.style.opacity = 0.7f;

            Add(_dropdown);
            Add(_description);
            Add(_missingWarning);

            this.TrackPropertyValue(_property, _ => Refresh());
            Refresh();
        }

        public static VisualElement PlainTextWithWarning(SerializedProperty property, string problem)
        {
            var root = new VisualElement();
            root.Add(new HelpBox(problem + " O campo vale como texto comum.", HelpBoxMessageType.Warning));
            root.Add(new TextField(property.displayName) { bindingPath = property.propertyPath, tooltip = property.tooltip });
            return root;
        }

        private void OnChosen(ChangeEvent<string> evt)
        {
            _property.stringValue = evt.newValue == _noneLabel ? string.Empty : evt.newValue;
            _property.serializedObject.ApplyModifiedProperties();
            Refresh();
        }

        private void Refresh()
        {
            string current = _property.stringValue;
            bool hasValue = !string.IsNullOrEmpty(current);
            bool isMissing = hasValue && _choices.All(choice => choice.Name != current);

            var names = new List<string> { _noneLabel };
            names.AddRange(_choices.Select(choice => choice.Name));
            if (isMissing) names.Add(current);

            _dropdown.choices = names;
            _dropdown.SetValueWithoutNotify(hasValue ? current : _noneLabel);

            ShowDescriptionOf(current);
            ShowMissingWarning(isMissing, current);
        }

        private void ShowDescriptionOf(string name)
        {
            string description = _choices.FirstOrDefault(choice => choice.Name == name).Description;
            _description.text = description ?? string.Empty;
            _description.style.display = string.IsNullOrEmpty(description) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void ShowMissingWarning(bool isMissing, string name)
        {
            _missingWarning.text = isMissing ? _missingWarningFor(name) : string.Empty;
            _missingWarning.style.display = isMissing ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
