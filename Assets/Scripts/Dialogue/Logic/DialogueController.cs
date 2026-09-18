using System.Collections.Generic;
using ProjetoVN.Core.Messaging;
using ProjetoVN.Dialogue.Data;
using ProjetoVN.Dialogue.Messaging;
using UnityEngine;

namespace ProjetoVN.Dialogue.Logic
{
    public class DialogueController
    {
        #region Fields
        private DialogueData _currentData;
        private int _currentNodeIndex;
        private bool _isWaitingForChoice;
        #endregion

        #region Properties
        public bool IsActive => _currentData != null;
        private DialogueNode CurrentNode => _currentData.DialogueNodes[_currentNodeIndex];
        private bool HasMoreNodes => _currentNodeIndex < _currentData.DialogueNodes.Count;
        #endregion

        #region Helpers
        private bool CanAdvance() => IsActive && !_isWaitingForChoice;
        private bool CanSelectChoice() => IsActive && _isWaitingForChoice;
        private bool IsValidChoiceIndex(int index) => index >= 0 && index < CurrentNode.Choices.Count;
        private bool HasChoices(DialogueNode node) => node.Choices != null && node.Choices.Count > 0;
        private bool HasNextDialogueData() => _currentData.NextDialogueData != null;

        private static bool IsPlayable(DialogueData data, string context)
        {
            if (data == null)
            {
                Debug.LogWarning($"[DialogueController] {context}: DialogueData nulo. Diálogo ignorado.");
                return false;
            }

            if (data.DialogueNodes == null || data.DialogueNodes.Count == 0)
            {
                Debug.LogWarning($"[DialogueController] {context}: '{data.name}' não tem nós. Diálogo ignorado.");
                return false;
            }

            return true;
        }

        private void ClearDialogueState()
        {
            _currentData = null;
            _isWaitingForChoice = false;
        }
        #endregion

        #region Dialogue Controls
        public bool StartDialogue(DialogueData data)
        {
            if (!IsPlayable(data, "StartDialogue")) return false;

            bool isNewSession = !IsActive;

            InitializeDialogueState(data);

            if (isNewSession) MessageBroker.Publish(new DialogueStartedMessage());

            ProcessCurrentNode();
            return true;
        }

        public void NextNode()
        {
            if (!CanAdvance()) return;

            _currentNodeIndex++;
            if (!HasMoreNodes)
            {
                FinishCurrentDialogue();
                return;
            }

            ProcessCurrentNode();
        }

        public void SelectChoice(int choiceIndex)
        {
            if (!CanSelectChoice()) return;
            if (!IsValidChoiceIndex(choiceIndex)) return;

            DialogueChoice choice = CurrentNode.Choices[choiceIndex];

            PublishTriggers(choice.Triggers);
            ProcessChoiceTarget(choice);
        }
        #endregion

        #region Flow Logic
        private void InitializeDialogueState(DialogueData data)
        {
            _currentData = data;
            _currentNodeIndex = 0;
            _isWaitingForChoice = false;
        }

        private void FinishCurrentDialogue()
        {
            if (HasNextDialogueData() && StartDialogue(_currentData.NextDialogueData)) return;

            EndDialogue();
        }

        private void ProcessChoiceTarget(DialogueChoice choice)
        {
            if (choice.TargetDialogue != null)
            {
                if (!StartDialogue(choice.TargetDialogue)) EndDialogue();
                return;
            }

            _isWaitingForChoice = false;
            NextNode();
        }

        private void EndDialogue()
        {
            ClearDialogueState();
            MessageBroker.Publish(new DialogueEndedMessage());
        }
        #endregion

        #region Message Publishers
        private void ProcessCurrentNode()
        {
            PublishTriggers(CurrentNode.Triggers);
            PublishLineMessage();
            HandleNodeChoices();
        }

        private void HandleNodeChoices()
        {
            if (!HasChoices(CurrentNode))
            {
                _isWaitingForChoice = false;
                return;
            }

            _isWaitingForChoice = true;
            MessageBroker.Publish(new DialogueChoicesMessage(CurrentNode.Choices));
        }

        private void PublishLineMessage()
        {
            MessageBroker.Publish(new DialogueLineMessage(CurrentNode.SpeakerName, CurrentNode.Text));
        }

        private void PublishTriggers(IEnumerable<DialogueTrigger> triggers)
        {
            if (triggers == null) return;

            foreach (DialogueTrigger trigger in triggers)
                MessageBroker.Publish(new DialogueTriggerMessage(trigger.TriggerType, trigger.Parameter));
        }
        #endregion
    }
}
