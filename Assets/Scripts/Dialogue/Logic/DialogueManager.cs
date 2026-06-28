using Assets.Scripts.Dialogue.Data;
using UnityEngine;

namespace Assets.Scripts.Dialogue.Logic
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        private DialogueController _controller;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[DialogueManager] Duplicata encontrada em {gameObject.name}. Destruindo objeto.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _controller = new DialogueController();
            DontDestroyOnLoad(gameObject);
        }

        public void StartDialogue(DialogueData data)
        {
            if (data == null)
            {
                Debug.LogWarning("DialogueManager: Tried to start a null DialogueData.");
                return;
            }

            _controller.StartDialogue(data);
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
