using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Dialogue.Data
{
    [Serializable]
    public class DialogueNode
    {
        public string SpeakerName;
        [TextArea(3, 10)]
        public string Text;
        public List<DialogueChoice> Choices = new List<DialogueChoice>();
        public List<DialogueTrigger> Triggers = new List<DialogueTrigger>();
    }
}
