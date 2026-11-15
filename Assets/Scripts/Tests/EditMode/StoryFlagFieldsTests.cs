using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjetoVN.Core.State;
using UnityEditor;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class StoryFlagFieldsTests
    {
        private const string BoolTypeName = "Bool";

        [Test]
        public void StoryFlagFields_AreFieldsOfComponents()
        {
            FieldInfo[] fields = StoryFlagFields();

            CollectionAssert.IsNotEmpty(fields, "nenhum campo [StoryFlag] encontrado");
            IEnumerable<string> outsideComponents = fields
                .Where(field => !typeof(MonoBehaviour).IsAssignableFrom(field.DeclaringType))
                .Select(field => $"{field.DeclaringType.Name}.{field.Name}");
            CollectionAssert.IsEmpty(outsideComponents,
                "o teste de conteúdo lê só campo direto de componente de cena ou prefab");
        }

        [Test]
        public void ScenesAndPrefabs_StoryFlagFieldsFollowTheNamingConvention()
        {
            IEnumerable<string> outOfFormat = FilledStoryFlags()
                .Where(flag => !StoryVariableName.FollowsConvention(flag.Value))
                .Select(Describe);

            CollectionAssert.IsEmpty(outOfFormat,
                "campos de variável de história fora do formato: '$' seguido de minúsculas sem acento, dígitos e '_'");
        }

        [Test]
        public void ScenesAndPrefabs_StoryFlagFieldsNameDeclaredBoolVariables()
        {
            HashSet<string> declaredBools = DeclaredBoolVariables();

            IEnumerable<string> undeclared = FilledStoryFlags()
                .Where(flag => StoryVariableName.FollowsConvention(flag.Value))
                .Where(flag => !declaredBools.Contains(flag.Value))
                .Select(Describe);

            CollectionAssert.IsEmpty(undeclared,
                "campos de variável de história com uma variável que não está declarada como booleana em "
                + "Assets/Roteiro/variaveis.yarn");
        }

        private static FieldInfo[] StoryFlagFields() =>
            TypeCache.GetFieldsWithAttribute<StoryFlagAttribute>().ToArray();

        private static HashSet<string> DeclaredBoolVariables() =>
            new(ScriptRun.FromProjectFiles().Declarations
                .Where(declaration => declaration.Type.Name == BoolTypeName)
                .Select(declaration => declaration.Name));

        private static List<ComponentReading<string>> FilledStoryFlags()
        {
            FieldInfo[] fields = StoryFlagFields();

            return ProjectComponents
                .ReadAll<MonoBehaviour, (string Field, string Value)[]>(component => StoryFlagsOf(component, fields))
                .SelectMany(reading => reading.Value.Select(flag =>
                    new ComponentReading<string>($"{reading.Location}: {flag.Field}", flag.Value)))
                .Where(flag => !string.IsNullOrWhiteSpace(flag.Value))
                .ToList();
        }

        private static (string Field, string Value)[] StoryFlagsOf(MonoBehaviour component, FieldInfo[] fields) =>
            fields
                .Where(field => field.DeclaringType.IsInstanceOfType(component))
                .Select(field => (field.Name, SerializedFields.StringOf(component, field.Name)))
                .ToArray();

        private static string Describe(ComponentReading<string> flag) => $"{flag.Location} '{flag.Value}'";
    }
}
