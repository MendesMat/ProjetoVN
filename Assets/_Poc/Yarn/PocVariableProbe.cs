using UnityEngine;
using Yarn.Unity;

namespace ProjetoVN.PocYarn
{
    public sealed class PocVariableProbe : MonoBehaviour
    {
        [SerializeField, Tooltip("Projeto Yarn de onde vem a lista de variáveis. Vazio: o seletor avisa e não lista nada.")]
        private YarnProject project;

        [SerializeField, PocYarnVariable(nameof(project)), Tooltip("Variável declarada no roteiro. Vazio: nenhuma variável escolhida.")]
        private string variableName;
    }
}
