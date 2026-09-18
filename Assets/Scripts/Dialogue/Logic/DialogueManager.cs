using ProjetoVN.Dialogue.Data;
using UnityEngine;

namespace ProjetoVN.Dialogue.Logic
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        private DialogueController _controller;

        private void Awake()
        {
            Instance = this;
            _controller = new DialogueController();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool StartDialogue(DialogueData data)
        {
            return _controller != null && _controller.StartDialogue(data);
        }

        public void AdvanceDialogue()
        {
            if (_controller != null && _controller.IsActive)
            {
                _controller.NextNode();
            }
        }

        public void MakeChoice(int index)
        {
            if (_controller != null && _controller.IsActive)
            {
                _controller.SelectChoice(index);
            }
        }

        public bool IsDialogueActive()
        {
            return _controller != null && _controller.IsActive;
        }
    }
}
