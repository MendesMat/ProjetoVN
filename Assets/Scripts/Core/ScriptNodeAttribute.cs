using UnityEngine;

namespace ProjetoVN.Core
{
    /// <summary>
    /// Marca um campo <c>string</c> que guarda o nome de um nó do roteiro. No Inspector o campo vira uma lista
    /// dos nós de <c>Assets/Roteiro/</c>. O desenho da lista mora no módulo Editor; este atributo é só o marcador.
    /// </summary>
    public sealed class ScriptNodeAttribute : PropertyAttribute
    {
    }
}
