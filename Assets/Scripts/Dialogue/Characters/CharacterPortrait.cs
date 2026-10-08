using System;
using UnityEngine;

namespace ProjetoVN.Dialogue.Characters
{
    /// <summary>Um retrato de um personagem: o nome da expressão e a imagem dela.</summary>
    [Serializable]
    public sealed class CharacterPortrait
    {
        [Tooltip("Nome da expressão, como o roteiro a escreve depois do # (ex.: raiva). Minúsculas sem acento, dígitos e _. " +
                 "Vazio ou fora do formato: a expressão não pode ser pedida pelo roteiro.")]
        [SerializeField] private string expression = "";

        [Tooltip("Imagem do retrato, de 300x300 pixels. Vazio: o retrato some nas falas que usam esta expressão.")]
        [SerializeField] private Sprite sprite;

        public string Expression => expression;
        public Sprite Sprite => sprite;
    }
}
