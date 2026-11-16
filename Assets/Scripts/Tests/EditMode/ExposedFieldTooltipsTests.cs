using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ExposedFieldTooltipsTests
    {
        [Test]
        public void ProjectTypes_AreFound()
        {
            CollectionAssert.IsNotEmpty(ProjectTypes(), "nenhum componente ou ScriptableObject do projeto encontrado");
        }

        [Test]
        public void ProjectComponentsAndAssets_ExposedFieldsHaveTooltips()
        {
            IEnumerable<string> withoutTooltip = ProjectTypes()
                .SelectMany(ExposedFields.WithoutTooltip)
                .Distinct()
                .OrderBy(field => field, StringComparer.Ordinal);

            CollectionAssert.IsEmpty(withoutTooltip,
                "campos que o Inspector mostra sem [Tooltip] (regra 8 do CLAUDE.md): o tooltip diz o que o campo é, "
                + "dá um exemplo e diz o que acontece quando ele fica vazio");
        }

        private static List<Type> ProjectTypes() =>
            TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                .Concat(TypeCache.GetTypesDerivedFrom<ScriptableObject>())
                .Where(type => ExposedFields.IsProjectAssembly(type.Assembly))
                .ToList();
    }
}
