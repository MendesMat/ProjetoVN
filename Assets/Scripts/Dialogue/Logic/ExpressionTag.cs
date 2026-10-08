using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoVN.Dialogue.Logic
{
    /// <summary>
    /// A etiqueta de expressão de uma fala (<c>Luna: Sai daqui. #raiva</c>). O Yarn Spinner entrega as etiquetas
    /// da linha sem o <c>#</c>, misturadas com as que ele mesmo põe (<c>lastline</c> e <c>line:…</c>); esta classe
    /// separa as do roteirista.
    /// </summary>
    public static class ExpressionTag
    {
        private const string LastLineTag = "lastline";
        private const string LineIdPrefix = "line:";

        public static IEnumerable<string> AllIn(string[] metadata)
        {
            if (metadata == null) return Enumerable.Empty<string>();

            return metadata.Where(tag => !IsYarnTag(tag));
        }

        public static string FirstIn(string[] metadata) => AllIn(metadata).FirstOrDefault();

        private static bool IsYarnTag(string tag) =>
            tag == LastLineTag || tag.StartsWith(LineIdPrefix, StringComparison.Ordinal);
    }
}
