using System;
using System.Collections.Generic;

namespace ProjetoVN.Dialogue.Data
{
    [Serializable]
    public class DialogueChoice
    {
        public string Text;
        public DialogueData TargetDialogue;
        public List<DialogueTrigger> Triggers = new ();
    }
}