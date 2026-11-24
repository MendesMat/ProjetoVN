using System.Linq;
using System.Text;

namespace ProjetoVN.Dialogue.Logic
{
    /// <summary>
    /// O nome do protagonista, escolhido pelo jogador ao assinar a lista de passageiros (#26, D-21). As regras do que
    /// o campo aceita e o que a placa de nome e o histórico mostram antes e depois da assinatura.
    /// </summary>
    public static class ProtagonistName
    {
        public const string VariableName = "$nome_protagonista";
        public const string Unknown = "???";

        private const string AccentedPortugueseLetters = "áàâãéêíóôõúüçÁÀÂÃÉÊÍÓÔÕÚÜÇ";

        public static string ToDisplay(string stored) => string.IsNullOrWhiteSpace(stored) ? Unknown : stored;

        public static string Normalize(string typed)
        {
            if (string.IsNullOrEmpty(typed)) return "";

            var normalized = new StringBuilder(typed.Length);
            foreach (string word in typed.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries))
            {
                if (normalized.Length > 0) normalized.Append(' ');
                normalized.Append(word);
            }

            return normalized.ToString();
        }

        public static bool TryNormalize(string typed, out string name)
        {
            name = Normalize(typed);
            return name.Any(char.IsLetter) && name.All(IsAllowedCharacter);
        }

        // Só as letras do português, e nenhum espaço, hífen ou apóstrofo (decidido na #213). Como a lista é fechada, nada
        // que quebre o texto ou o roteiro entra. As duas caixas estão escritas, sem ToLower/ToUpper, para a regra não
        // depender de cultura.
        public static bool IsAllowedCharacter(char character) =>
            IsUnaccentedLetter(character) || AccentedPortugueseLetters.IndexOf(character) >= 0;

        private static bool IsUnaccentedLetter(char character) =>
            character is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    }
}
