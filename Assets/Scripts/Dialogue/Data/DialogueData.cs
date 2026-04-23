using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Dialogue.Data
{
    [CreateAssetMenu(fileName = "NewDialogueData", menuName = "Dialogue/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        public List<DialogueNode> Nodes = new List<DialogueNode>();
        
        [Tooltip("Optional. Se definido, este diálogo começará logo após o término dos nós atuais.")]
        public DialogueData NextDialogue;
    }
}
