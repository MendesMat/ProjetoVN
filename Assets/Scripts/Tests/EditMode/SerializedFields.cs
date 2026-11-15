using NUnit.Framework;
using UnityEditor;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>
    /// Lê campos privados de componentes nos testes de conteúdo, com os overrides de prefab já resolvidos.
    /// </summary>
    public static class SerializedFields
    {
        /// <summary>
        /// O valor de um campo de texto serializado. Reprova o teste se o campo não existe mais (foi renomeado).
        /// </summary>
        public static string StringOf(UnityEngine.Object target, string fieldName)
        {
            SerializedProperty field = new SerializedObject(target).FindProperty(fieldName);
            if (field == null)
                Assert.Fail($"O campo '{fieldName}' do {target.GetType().Name} foi renomeado; atualize este teste.");

            return field.stringValue;
        }
    }
}
