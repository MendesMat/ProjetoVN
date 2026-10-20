using UnityEditor;
using Yarn.Unity;
using Yarn.Unity.Editor;

namespace ProjetoVN.Editor
{
    /// <summary>
    /// O único <c>YarnProject</c> do projeto e o que o importador do Yarn Spinner guardou na última importação
    /// dele. Quem lê o roteiro no Inspector parte daqui.
    /// </summary>
    public static class TheYarnProject
    {
        public static bool TryFindImported(out string path, out ProjectImportData importData, out string problem)
        {
            path = null;
            importData = null;
            string[] guids = AssetDatabase.FindAssets("t:YarnProject");
            if (guids.Length != 1)
            {
                problem = guids.Length == 0
                    ? "Não há um YarnProject no projeto."
                    : $"Há {guids.Length} YarnProject no projeto; o seletor espera um só.";
                return false;
            }

            path = AssetDatabase.GUIDToAssetPath(guids[0]);
            importData = (AssetImporter.GetAtPath(path) as YarnProjectImporter)?.ImportData;
            if (importData == null)
            {
                problem = $"Não foi possível ler a importação de {path}.";
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
