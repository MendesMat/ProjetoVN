using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>
    /// O que um <c>[YarnCommand]</c> aceita depois do nome. Repete a regra do pacote (<c>Actions.ParameterCount</c>
    /// e o conversor de parâmetros): opcional e <c>params</c> baixam o mínimo, <c>params</c> tira o máximo, cada
    /// palavra precisa converter para o tipo do seu parâmetro, e um método de instância de <c>Component</c> recebe o
    /// nome do GameObject como primeira palavra.
    /// </summary>
    public sealed class ScriptCommandSignature
    {
        private const char ExpressionMarker = '{';

        private readonly int _minimumWords;
        private readonly int _maximumWords;
        private readonly ParameterInfo[] _parameters;
        private readonly int _targetWords;

        public string Name { get; }

        private ScriptCommandSignature(string name, int minimumWords, int maximumWords, ParameterInfo[] parameters, int targetWords)
        {
            Name = name;
            _minimumWords = minimumWords;
            _maximumWords = maximumWords;
            _parameters = parameters;
            _targetWords = targetWords;
        }

        public static ScriptCommandSignature From(string name, MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            int paramsArrays = parameters.Count(IsParamsArray);
            int optional = parameters.Count(parameter => parameter.IsOptional);

            int minimum = parameters.Length - optional - paramsArrays;
            int maximum = paramsArrays > 0 ? int.MaxValue : parameters.Length;
            int targetWords = method.IsStatic ? 0 : 1;

            return new ScriptCommandSignature(name, minimum + targetWords, AddWithoutOverflow(maximum, targetWords), parameters, targetWords);
        }

        public bool Accepts(int wordCount) => wordCount >= _minimumWords && wordCount <= _maximumWords;

        /// <summary>
        /// O primeiro problema de tipo entre as palavras, ou <c>null</c>. Um valor de expressão (<c>{0}</c>, que o
        /// programa compilado guarda no lugar de <c>{$x}</c>) só existe em runtime e não é conferido.
        /// </summary>
        public string TypeProblemOf(IReadOnlyList<string> words)
        {
            for (int index = _targetWords; index < words.Count; index++)
            {
                string problem = TypeProblemOfWord(words[index], index - _targetWords);
                if (problem != null) return problem;
            }

            return null;
        }

        public string Describe()
        {
            if (_maximumWords == int.MaxValue) return $"{_minimumWords} ou mais parâmetros";
            if (_minimumWords != _maximumWords) return $"de {_minimumWords} a {_maximumWords} parâmetros";
            if (_maximumWords == 0) return "nenhum parâmetro";
            return _maximumWords == 1 ? "1 parâmetro" : $"{_maximumWords} parâmetros";
        }

        private string TypeProblemOfWord(string word, int parameterIndex)
        {
            if (word.Contains(ExpressionMarker)) return null;
            if (_parameters.Length == 0) return null;

            int position = Math.Min(parameterIndex, _parameters.Length - 1);
            ParameterInfo parameter = _parameters[position];
            if (ScriptCommandParameter.Accepts(parameter, word)) return null;

            return ScriptCommandParameter.IsDecimalCommaMistake(parameter, word)
                ? $"pede {ScriptCommandParameter.Describe(parameter)} com ponto no parâmetro {position + 1} (1.5, não 1,5), recebeu '{word}'"
                : $"pede {ScriptCommandParameter.Describe(parameter)} no parâmetro {position + 1}, recebeu '{word}'";
        }

        private static bool IsParamsArray(ParameterInfo parameter) =>
            parameter.ParameterType.IsArray && parameter.GetCustomAttribute<ParamArrayAttribute>() != null;

        private static int AddWithoutOverflow(int value, int increment) =>
            value == int.MaxValue ? value : value + increment;
    }
}
