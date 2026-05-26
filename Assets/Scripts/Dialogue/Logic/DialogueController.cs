using Assets.Scripts.Core.Messaging;
using Assets.Scripts.Dialogue.Data;
using Assets.Scripts.Dialogue.Messaging;

namespace Assets.Scripts.Dialogue.Logic
{
    public class DialogueController
    {
        private DialogueData _currentData;
        private int _currentNodeIndex;
        private bool _isWaitingForChoice;

        public bool IsActive => _currentData != null;

        public void StartDialogue(DialogueData data)
        {
            if (data == null || data.DialogueNodes.Count == 0) return;

            _currentData = data;
            _currentNodeIndex = 0;
            _isWaitingForChoice = false;

            ProcessCurrentNode();
        }

        public void NextNode()
        {
            if (!IsActive || _isWaitingForChoice) return;

            _currentNodeIndex++;

            if (_currentNodeIndex < _currentData.DialogueNodes.Count)
            {
                ProcessCurrentNode();
            }
            else
            {
                FinishCurrentDialogue();
            }
        }

        public void SelectChoice(int choiceIndex)
        {
            if (!IsActive || !_isWaitingForChoice) return;

            var currentNode = _currentData.DialogueNodes[_currentNodeIndex];
            if (choiceIndex < 0 || choiceIndex >= currentNode.Choices.Count) return;

            var choice = currentNode.Choices[choiceIndex];

            foreach (var trigger in choice.Triggers)
            {
                MessageBroker.Publish(new DialogueTriggerMessage(trigger.TriggerType, trigger.Parameter));
            }

            if (choice.TargetDialogue != null)
            {
                StartDialogue(choice.TargetDialogue);
            }
            else
            {
                _isWaitingForChoice = false;
                NextNode();
            }
        }

        private void ProcessCurrentNode()
        {
            var node = _currentData.DialogueNodes[_currentNodeIndex];

            foreach (var trigger in node.Triggers)
            {
                MessageBroker.Publish(new DialogueTriggerMessage(trigger.TriggerType, trigger.Parameter));
            }

            MessageBroker.Publish(new DialogueLineMessage(node.SpeakerName, node.Text));

            if (node.Choices != null && node.Choices.Count > 0)
            {
                _isWaitingForChoice = true;
                MessageBroker.Publish(new DialogueChoicesMessage(node.Choices));
            }
            else
            {
                _isWaitingForChoice = false;
            }
        }

        private void FinishCurrentDialogue()
        {
            if (_currentData.NextDialogueData != null)
            {
                StartDialogue(_currentData.NextDialogueData);
            }
            else
            {
                _currentData = null;
                MessageBroker.Publish(new DialogueLineMessage(string.Empty, string.Empty));
            }
        }
    }
}
