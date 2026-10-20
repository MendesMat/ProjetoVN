using System.Collections.Generic;
using System.Linq;
using ProjetoVN.Core;
using UnityEditor;
using UnityEngine.UIElements;

namespace ProjetoVN.Editor
{
    /// <summary>
    /// Desenha um campo <see cref="ScriptNodeAttribute"/> como lista dos nós do roteiro. Só vale quando o
    /// Inspector é de UI Toolkit, como o do <c>InteractableDialogueTrigger</c>.
    /// </summary>
    [CustomPropertyDrawer(typeof(ScriptNodeAttribute))]
    public sealed class ScriptNodeDrawer : PropertyDrawer
    {
        private const string NoneLabel = "(nenhum)";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.String)
                return new HelpBox("[ScriptNode] só vale em um campo string.", HelpBoxMessageType.Error);

            if (!ScriptNodes.TryLoad(out IReadOnlyList<string> nodes, out string problem))
                return NameChoiceField.PlainTextWithWarning(property, problem);

            List<NameChoice> choices = nodes.Select(node => new NameChoice(node, string.Empty)).ToList();
            return new NameChoiceField(property.Copy(), NoneLabel, choices, MissingWarning);
        }

        private static string MissingWarning(string name) =>
            $"O nó '{name}' não existe no roteiro (Assets/Roteiro/).";
    }
}
