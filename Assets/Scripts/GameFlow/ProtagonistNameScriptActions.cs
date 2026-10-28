using ProjetoVN.Dialogue.Logic;
using UnityEngine;
using Yarn.Unity;

namespace ProjetoVN.GameFlow
{
    /// <summary>
    /// O comando de roteiro que pede o nome do protagonista: <c>&lt;&lt;assinar_lista&gt;&gt;</c>. Não é um componente:
    /// o Yarn Spinner acha o método estático sozinho, e não há campo de Inspector para ligar.
    /// </summary>
    public static class ProtagonistNameScriptActions
    {
        [YarnCommand("assinar_lista")]
        public static YarnTask SignPassengerList()
        {
            DialogueManager dialogue = DialogueManager.Instance;
            if (dialogue != null) return dialogue.AskProtagonistName();

            Debug.LogError("[ProtagonistNameScriptActions] <<assinar_lista>>: não há DialogueManager (o prefab Managers não foi criado). " +
                           "O nome do protagonista continua '???'.");
            return YarnTask.CompletedTask;
        }
    }
}
