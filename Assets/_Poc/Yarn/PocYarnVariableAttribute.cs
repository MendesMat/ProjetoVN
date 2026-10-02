using UnityEngine;

namespace ProjetoVN.PocYarn
{
    public sealed class PocYarnVariableAttribute : PropertyAttribute
    {
        public string ProjectField { get; }

        public PocYarnVariableAttribute(string projectField) => ProjectField = projectField;
    }
}
