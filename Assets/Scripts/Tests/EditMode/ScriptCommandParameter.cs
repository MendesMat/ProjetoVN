using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>
    /// Se uma palavra do roteiro converte para o tipo de um parâmetro de <c>[YarnCommand]</c>. Repete a regra do
    /// pacote (<c>Actions.CreateConverterFunction</c>): <c>string</c>, <c>GameObject</c> e <c>Component</c> aceitam
    /// qualquer palavra (um nome que não existe vira <c>null</c> e não lança); <c>bool</c> aceita o nome do parâmetro
    /// ou <c>true</c>/<c>false</c>; o resto passa por <c>Convert.ChangeType</c> em formato invariante.
    /// </summary>
    public static class ScriptCommandParameter
    {
        public static bool Accepts(ParameterInfo parameter, string word)
        {
            Type type = ValueTypeOf(parameter);

            if (type == typeof(string) || type == typeof(GameObject) || typeof(Component).IsAssignableFrom(type)) return true;
            if (type == typeof(bool)) return AcceptsBool(parameter, word);
            if (IsDecimalNumber(type) && word.Contains(',')) return false;

            return CanConvert(word, type);
        }

        public static string Describe(ParameterInfo parameter)
        {
            Type type = ValueTypeOf(parameter);

            if (IsDecimalNumber(type)) return "um número";
            if (IsWholeNumber(type)) return "um número inteiro";
            if (type == typeof(bool)) return "true ou false";

            return type.Name;
        }

        /// <summary>
        /// Um número com vírgula decimal. O pacote o lê em formato invariante, onde a vírgula separa milhares:
        /// <c>1,5</c> vira <c>15</c> sem erro nenhum.
        /// </summary>
        public static bool IsDecimalCommaMistake(ParameterInfo parameter, string word)
        {
            if (!IsDecimalNumber(ValueTypeOf(parameter)) || !word.Contains(',')) return false;

            return Accepts(parameter, word.Replace(',', '.'));
        }

        private static Type ValueTypeOf(ParameterInfo parameter) =>
            parameter.ParameterType.IsArray ? parameter.ParameterType.GetElementType() : parameter.ParameterType;

        private static bool AcceptsBool(ParameterInfo parameter, string word) =>
            string.Equals(word, parameter.Name, StringComparison.InvariantCultureIgnoreCase) || bool.TryParse(word, out _);

        private static bool CanConvert(string word, Type type)
        {
            try
            {
                Convert.ChangeType(word, type, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsDecimalNumber(Type type) =>
            type == typeof(float) || type == typeof(double) || type == typeof(decimal);

        private static bool IsWholeNumber(Type type) =>
            type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
            type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte);
    }
}
