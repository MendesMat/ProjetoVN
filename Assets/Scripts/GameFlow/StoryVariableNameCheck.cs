using ProjetoVN.Core.State;
using UnityEngine;

namespace ProjetoVN.GameFlow
{
    /// <summary>O aviso de autoria que os campos de variável de história mostram no <c>OnValidate</c>.</summary>
    public static class StoryVariableNameCheck
    {
        public static void WarnIfOffConvention(string owner, string field, string name, Object context)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (StoryVariableName.FollowsConvention(name)) return;

            Debug.LogWarning($"[{owner}] O campo '{field}' tem '{name}', fora do formato das variáveis de história: " +
                             "'$' seguido de minúsculas sem acento, dígitos e '_' (ex.: \"$porta_biblioteca_destrancada\").", context);
        }
    }
}
