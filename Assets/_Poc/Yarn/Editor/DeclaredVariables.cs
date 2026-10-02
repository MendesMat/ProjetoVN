using System.Collections.Generic;
using System.Linq;
using Yarn.Unity;

namespace ProjetoVN.PocYarn.Editor
{
    public static class DeclaredVariables
    {
        public static List<string> NamesOf(YarnProject project)
        {
            if (project == null) return new List<string>();

            return project.InitialValues.Keys.OrderBy(name => name).ToList();
        }
    }
}
