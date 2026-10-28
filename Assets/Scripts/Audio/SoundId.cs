namespace ProjetoVN.Audio
{
    /// <summary>
    /// O formato dos ids de som que o roteiro cita (D-22): minúsculas sem acento, dígitos e <c>_</c>, começando por
    /// letra ou <c>_</c> (ex.: <c>onibus_estrada</c>). O mesmo dos nomes de nó e dos ids de item.
    /// </summary>
    public static class SoundId
    {
        public static bool FollowsConvention(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (!IsLowercaseOrUnderscore(id[0])) return false;

            for (int i = 1; i < id.Length; i++)
            {
                if (!IsLowercaseOrUnderscore(id[i]) && !IsDigit(id[i])) return false;
            }

            return true;
        }

        private static bool IsLowercaseOrUnderscore(char c) => c == '_' || (c >= 'a' && c <= 'z');

        private static bool IsDigit(char c) => c >= '0' && c <= '9';
    }
}
