using System.Collections.Generic;
using System.Linq;
using Yarn.Unity;
using Yarn.Unity.Editor;

namespace ProjetoVN.Editor
{
    /// <summary>
    /// As variáveis booleanas que o roteiro declara, lidas do que o importador do Yarn Spinner guardou na última
    /// importação do <c>YarnProject</c>. Lê a cada chamada: <c>YarnProject.InitialValues</c> guarda cache.
    /// </summary>
    public static class DeclaredStoryFlags
    {
        private const string BoolTypeName = "Bool";

        public static bool TryLoad(out IReadOnlyList<NameChoice> flags, out string problem)
        {
            flags = new List<NameChoice>();
            if (!TheYarnProject.TryFindImported(out _, out ProjectImportData importData, out problem)) return false;

            flags = importData.serializedDeclarations
                .Where(declaration => declaration.typeName == BoolTypeName && !declaration.isImplicit)
                .Select(declaration => new NameChoice(declaration.name, declaration.description))
                .OrderBy(flag => flag.Name, System.StringComparer.Ordinal)
                .ToList();
            return true;
        }
    }
}
