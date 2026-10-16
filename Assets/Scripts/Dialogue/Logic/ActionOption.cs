using System;

namespace ProjetoVN.Dialogue.Logic
{
    /// <summary>
    /// A opção de ação do roteiro (D-21): <c>-> Assinar e sair #acao</c>. É um gesto do protagonista, não uma fala.
    /// O Yarn Spinner entrega as etiquetas da opção sem o <c>#</c>, e a comparação é exata.
    /// </summary>
    public static class ActionOption
    {
        public const string Tag = "acao";

        public static bool IsMarkedIn(string[] metadata) => metadata != null && Array.IndexOf(metadata, Tag) >= 0;
    }
}
