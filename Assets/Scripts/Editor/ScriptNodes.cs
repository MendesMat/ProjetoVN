using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Yarn.Unity;

namespace ProjetoVN.Editor
{
    /// <summary>
    /// Os nós que o roteiro tem, lidos do programa compilado do <c>YarnProject</c>. Lê a cada chamada:
    /// <c>YarnProject.Program</c> guarda cache e pode mostrar nomes antigos depois de renomear um nó.
    /// </summary>
    public static class ScriptNodes
    {
        // Só declara variáveis, não tem fala: escolhê-lo abriria e fecharia uma conversa vazia.
        private const string VariablesNode = "variaveis";

        public static bool TryLoad(out IReadOnlyList<string> nodes, out string problem)
        {
            nodes = new List<string>();
            if (!TheYarnProject.TryFindImported(out string path, out _, out problem)) return false;

            var project = AssetDatabase.LoadAssetAtPath<YarnProject>(path);
            nodes = Yarn.Program.Parser.ParseFrom(project.compiledYarnProgram).Nodes.Keys
                .Where(name => name != VariablesNode)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            return true;
        }
    }
}
