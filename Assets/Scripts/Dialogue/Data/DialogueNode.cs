using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Dialogue.Data
{
    [Serializable]
    public class DialogueNode
    {
        public string SpeakerName;
        [TextArea(0, 200)]
        public string Text;
        public List<DialogueChoice> Choices = new ();
        public List<DialogueTrigger> Triggers = new ();
    }
}
