using UnityEngine;

namespace ProjetoVN.Core.State
{
    /// <summary>
    /// Marca um campo <c>string</c> que guarda o nome de uma variável booleana de história. No Inspector o
    /// campo vira uma lista das variáveis booleanas declaradas em <c>Assets/Roteiro/variaveis.yarn</c>.
    /// O desenho da lista mora no módulo Editor; este atributo é só o marcador.
    /// </summary>
    public sealed class StoryFlagAttribute : PropertyAttribute
    {
    }
}
