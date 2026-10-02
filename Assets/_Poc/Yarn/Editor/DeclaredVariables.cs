using System.Collections.Generic;
using System.Linq;
using Yarn;
using Yarn.Unity;

namespace ProjetoVN.PocYarn.Editor
{
    public static class DeclaredVariables
    {
        // O YarnProject guarda Program e InitialValues em cache e só os invalida no Awake;
        // o que a importação gravou de verdade está em compiledYarnProgram.
        public static List<string> NamesOf(YarnProject project)
        {
            if (project == null || project.compiledYarnProgram == null) return new List<string>();

            Program program = Program.Parser.ParseFrom(project.compiledYarnProgram);
            return program.InitialValues.Keys.OrderBy(name => name).ToList();
        }
    }
}
