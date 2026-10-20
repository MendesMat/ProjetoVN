using System.Collections.Generic;
using System.Linq;
using ProjetoVN.Core.State;
using UnityEditor;
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
                return NameChoiceField.PlainTextWithWarning(property, problem);

            List<NameChoice> choices = flags.Select(flag => new NameChoice(flag.Name, flag.Description)).ToList();
            return new NameChoiceField(property.Copy(), NoneLabel, choices, UndeclaredWarning);
        }

        private static string UndeclaredWarning(string name) =>
            $"A variável '{name}' não está declarada como booleana em variaveis.yarn.";
    }
}
