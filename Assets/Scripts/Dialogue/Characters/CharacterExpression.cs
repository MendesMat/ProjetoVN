using System;
using UnityEngine;

namespace ProjetoVN.Dialogue.Characters
{
    /// <summary>Uma expressão de um personagem: o nome dela e a arte.</summary>
    [Serializable]
    public sealed class CharacterExpression
    {
        [Tooltip("Nome da expressão, como o roteiro a escreve depois do # (ex.: raiva). Minúsculas sem acento, dígitos e _. " +
                 "Vazio ou fora do formato: a expressão não pode ser pedida pelo roteiro.")]
        [SerializeField] private string expression = "";

        [Tooltip("Arte do personagem nesta expressão: de frente, fundo transparente, no tamanho da cena de referência, Pixels Per Unit 100 (ver docs/autoria/salas.md). " +
                 "Vazio: o personagem não aparece nas falas que usam esta expressão.")]
        [SerializeField] private Sprite sprite;

        public string Expression => expression;
        public Sprite Sprite => sprite;
    }
}
