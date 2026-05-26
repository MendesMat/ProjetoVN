using System;
using System.Collections.Generic;

namespace Assets.Scripts.Dialogue.Data
{
    [Serializable]
    public class DialogueChoice
    {
        public string Text;
        public DialogueData TargetDialogue;
        public List<DialogueTrigger> Triggers = new ();
    }
}