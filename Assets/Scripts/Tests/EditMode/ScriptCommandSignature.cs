using System;
using System.Linq;
using System.Reflection;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>
    /// Quantas palavras um <c>[YarnCommand]</c> aceita depois do nome. Repete a regra do pacote
    /// (<c>Actions.ParameterCount</c>): opcional e <c>params</c> baixam o mínimo, <c>params</c> tira o máximo e um
    /// método de instância de <c>Component</c> recebe o nome do GameObject como primeira palavra.
    /// </summary>
    public sealed class ScriptCommandSignature
    {
        private readonly int _minimumWords;
        private readonly int _maximumWords;

        public string Name { get; }

        private ScriptCommandSignature(string name, int minimumWords, int maximumWords)
        {
            Name = name;
            _minimumWords = minimumWords;
            _maximumWords = maximumWords;
        }

        public static ScriptCommandSignature From(string name, MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            int paramsArrays = parameters.Count(IsParamsArray);
            int optional = parameters.Count(parameter => parameter.IsOptional);

            int minimum = parameters.Length - optional - paramsArrays;
            int maximum = paramsArrays > 0 ? int.MaxValue : parameters.Length;
            int targetWords = method.IsStatic ? 0 : 1;

            return new ScriptCommandSignature(name, minimum + targetWords, AddWithoutOverflow(maximum, targetWords));
        }

        public bool Accepts(int wordCount) => wordCount >= _minimumWords && wordCount <= _maximumWords;

        public string Describe()
        {
            if (_maximumWords == int.MaxValue) return $"{_minimumWords} ou mais parâmetros";
            if (_minimumWords != _maximumWords) return $"de {_minimumWords} a {_maximumWords} parâmetros";
            if (_maximumWords == 0) return "nenhum parâmetro";
            return _maximumWords == 1 ? "1 parâmetro" : $"{_maximumWords} parâmetros";
        }

        private static bool IsParamsArray(ParameterInfo parameter) =>
            parameter.ParameterType.IsArray && parameter.GetCustomAttribute<ParamArrayAttribute>() != null;

        private static int AddWithoutOverflow(int value, int increment) =>
            value == int.MaxValue ? value : value + increment;
    }
}
