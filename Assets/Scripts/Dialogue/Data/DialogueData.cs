using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Dialogue.Data
{
    [CreateAssetMenu(fileName = "DialogueDataSO", menuName = "Dialogue/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        public List<DialogueNode> DialogueNodes = new();

        [Tooltip("Opcional. Se definido, este diálogo começará logo após o término dos nós atuais.")]
        public DialogueData NextDialogueData;
    }
}
