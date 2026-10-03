namespace ProjetoVN.Dialogue.Logic
{
    /// <summary>
    /// O formato dos nomes de nó do roteiro (D-22): minúsculas sem acento, dígitos e <c>_</c>, começando
    /// por letra ou <c>_</c> (ex.: <c>porta_trancada</c>). É o que quem monta cena pode digitar.
    /// </summary>
    public static class ScriptNodeName
    {
        public static bool FollowsConvention(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!IsLowercaseOrUnderscore(name[0])) return false;

            for (int i = 1; i < name.Length; i++)
            {
                if (!IsLowercaseOrUnderscore(name[i]) && !IsDigit(name[i])) return false;
            }

            return true;
        }

        private static bool IsLowercaseOrUnderscore(char c) => c == '_' || (c >= 'a' && c <= 'z');

        private static bool IsDigit(char c) => c >= '0' && c <= '9';
    }
}
