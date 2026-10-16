using System;

namespace ProjetoVN.Dialogue.Logic
{
    /// <summary>
    /// O pensamento do protagonista no roteiro (D-21): a linha <c>Pensamento: texto</c>. <c>Pensamento</c> é um nome
    /// reservado, não um personagem. A comparação é exata, como a dos nomes de personagem.
    /// </summary>
    public static class ThoughtLine
    {
        public const string ReservedName = "Pensamento";

        public static bool Is(string characterName) => string.Equals(characterName, ReservedName, StringComparison.Ordinal);
    }
}
