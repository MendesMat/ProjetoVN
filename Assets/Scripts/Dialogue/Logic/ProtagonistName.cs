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

        // Fica de fora de propósito o que quebra o texto ou o roteiro: [ ] são marcação do Yarn aplicada depois da troca de
        // {$nome_protagonista}, < > são rich text do TMP (o histórico escreve o nome dentro de <b>), e { } # : \ são sintaxe do Yarn.
        public static bool IsAllowedCharacter(char character) =>
            char.IsLetter(character) || character == ' ' || character == '-' || character == '\'';
    }
}
