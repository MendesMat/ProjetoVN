using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Yarn.Unity;
using Yarn.Unity.Editor;

namespace ProjetoVN.Editor
{
    public readonly struct StoryFlagDeclaration
    {
        public string Name { get; }
        public string Description { get; }

        public StoryFlagDeclaration(string name, string description)
        {
            Name = name;
            Description = description ?? string.Empty;
        }
    }

    /// <summary>
    /// As variáveis booleanas que o roteiro declara, lidas do que o importador do Yarn Spinner guardou na última
    /// importação do <c>YarnProject</c>. Lê a cada chamada: <c>YarnProject.InitialValues</c> guarda cache.
    /// </summary>
    public static class DeclaredStoryFlags
    {
        private const string BoolTypeName = "Bool";

        public static bool TryLoad(out IReadOnlyList<StoryFlagDeclaration> flags, out string problem)
        {
            flags = new List<StoryFlagDeclaration>();
            if (!TryFindImportData(out ProjectImportData importData, out problem)) return false;

            flags = importData.serializedDeclarations
                .Where(declaration => declaration.typeName == BoolTypeName && !declaration.isImplicit)
                .Select(declaration => new StoryFlagDeclaration(declaration.name, declaration.description))
                .OrderBy(flag => flag.Name, System.StringComparer.Ordinal)
                .ToList();
            return true;
        }

        private static bool TryFindImportData(out ProjectImportData importData, out string problem)
        {
            importData = null;
            string[] guids = AssetDatabase.FindAssets("t:YarnProject");
            if (guids.Length != 1)
            {
                problem = guids.Length == 0
                    ? "Não há um YarnProject no projeto."
                    : $"Há {guids.Length} YarnProject no projeto; o seletor espera um só.";
                return false;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            importData = (AssetImporter.GetAtPath(path) as YarnProjectImporter)?.ImportData;
            if (importData == null)
            {
                problem = $"Não foi possível ler as declarações de {path}.";
                return false;
            }

            if (importData.ImportStatus != ProjectImportData.ImportStatusCode.Succeeded)
            {
                problem = $"O roteiro de {path} não compila; corrija os erros e a lista volta.";
                return false;
            }

            problem = null;
            return true;
        }
    }
}
