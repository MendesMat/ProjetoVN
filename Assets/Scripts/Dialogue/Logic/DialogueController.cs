using System.Collections.Generic;
using Assets.Scripts.Core.Messaging;
using Assets.Scripts.Dialogue.Data;
using Assets.Scripts.Dialogue.Messaging;
using UnityEngine;

namespace Assets.Scripts.Dialogue.Logic
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

        private void ClearDialogueState()
        {
            _currentData = null;
        }
        #endregion

        #region Dialogue Controls
        public void StartDialogue(DialogueData data)
        {
            if (data == null || data.DialogueNodes.Count == 0) return;

            Debug.Log($"[DialogueController] Iniciando diálogo: '{data.name}' ({data.DialogueNodes.Count} nós)");

            InitializeDialogueState(data);
            ProcessCurrentNode();
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

            var choice = CurrentNode.Choices[choiceIndex];
            Debug.Log($"[DialogueController] Escolha selecionada [{choiceIndex}]: '{choice.Text}'");

            PublishTriggers(choice.Triggers, "Escolha");
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

        private void ProcessCurrentNode()
        {
            string textPreview = CurrentNode.Text.Length > 40 ? CurrentNode.Text[..40] + "..." : CurrentNode.Text;
            Debug.Log($"[DialogueController] Nó [{_currentNodeIndex}] | {CurrentNode.SpeakerName}: \"{textPreview}\"");

            PublishTriggers(CurrentNode.Triggers, "Nó");
            PublishLineMessage();
            HandleNodeChoices();
        }

        private void FinishCurrentDialogue()
        {
            if (HasNextDialogueData())
            {
                Debug.Log($"[DialogueController] Encadeando para: '{_currentData.NextDialogueData.name}'");

                StartDialogue(_currentData.NextDialogueData);
                return;
            }

            Debug.Log("[DialogueController] Diálogo concluído. → DialogueEndedMessage");

            ClearDialogueState();
            MessageBroker.Publish(new DialogueEndedMessage());
        }

        private void ProcessChoiceTarget(DialogueChoice choice)
        {
            if (choice.TargetDialogue != null)
            {
                StartDialogue(choice.TargetDialogue);
                return;
            }

            _isWaitingForChoice = false;
            NextNode();
        }
        #endregion

        #region Message Publishers
        private void HandleNodeChoices()
        {
            if (!HasChoices(CurrentNode))
            {
                _isWaitingForChoice = false;
                return;
            }

            Debug.Log($"[DialogueController] Aguardando escolha ({CurrentNode.Choices.Count} opções).");

            _isWaitingForChoice = true;
            MessageBroker.Publish(new DialogueChoicesMessage(CurrentNode.Choices));
        }

        private void PublishLineMessage()
        {
            MessageBroker.Publish(new DialogueLineMessage(CurrentNode.SpeakerName, CurrentNode.Text));
        }

        private void PublishTriggers(IEnumerable<DialogueTrigger> triggers, string source)
        {
            if (triggers == null) return;

            foreach (var trigger in triggers)
            {
                Debug.Log($"[DialogueController] → Trigger ({source}): '{trigger.TriggerType}' | Param: '{trigger.Parameter}'");

                MessageBroker.Publish(new DialogueTriggerMessage(trigger.TriggerType, trigger.Parameter));
            }
        }
        #endregion
    }
}
