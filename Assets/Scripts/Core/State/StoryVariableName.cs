namespace ProjetoVN.Core.State
{
    /// <summary>
    /// Duas regras de nome de propósito diferentes. <see cref="IsValid"/> é o que o armazenamento aceita,
    /// e é frouxa porque o Yarn Spinner grava nomes internos fora da convenção (<c>$Yarn.Internal.Visiting.…</c>).
    /// <see cref="FollowsConvention"/> é o que quem monta cena pode digitar, e todo nome que passa nela
    /// é uma variável válida no roteiro.
    /// </summary>
    public static class StoryVariableName
    {
        private const char Prefix = '$';

        public static bool IsValid(string name) =>
            !string.IsNullOrWhiteSpace(name) && name.Length > 1 && name[0] == Prefix;

        public static bool FollowsConvention(string name)
        {
            if (!IsValid(name)) return false;
            if (!IsLowercaseOrUnderscore(name[1])) return false;

            for (int i = 2; i < name.Length; i++)
            {
                if (!IsLowercaseOrUnderscore(name[i]) && !IsDigit(name[i])) return false;
            }

            return true;
        }

        private static bool IsLowercaseOrUnderscore(char c) => c == '_' || (c >= 'a' && c <= 'z');

        private static bool IsDigit(char c) => c >= '0' && c <= '9';
    }
}
