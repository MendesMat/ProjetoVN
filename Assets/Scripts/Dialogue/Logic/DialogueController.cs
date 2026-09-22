using System;
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
        private bool _isExecutingEffects;
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
            if (_isExecutingEffects)
            {
                Debug.LogWarning($"[DialogueController] StartDialogue: um efeito tentou iniciar '{(data != null ? data.name : "null")}' " +
                                 "no meio da execução de efeitos. Isso seria sobrescrito logo em seguida. Diálogo ignorado.");
                return false;
            }

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

            // ARCH-11: libera a espera antes dos efeitos, para que um efeito reentrante caia em
            // CanSelectChoice() e seja ignorado de graça, e para que ProcessChoiceTarget não precise mexer nisso.
            _isWaitingForChoice = false;

            // Efeitos da escolha rodam ANTES do alvo resolver: "tome a chave" precisa valer
            // antes da fala de resposta aparecer.
            ExecuteEffects(choice.Effects);
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
            // ARCH-11: o nó é capturado antes de publicar, porque um efeito pode trocar o diálogo atual.
            DialogueNode node = CurrentNode;

            // Ordem deliberada: estado assentado → fala → escolhas → efeitos.
            // Efeitos por último garante que a UI já mostrou o que o jogador precisa ver
            // antes de qualquer sistema reagir à mudança de estado.
            PublishLineMessage();
            HandleNodeChoices();
            ExecuteEffects(node.Effects);
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

        #endregion

        #region Effects
        /// <summary>
        /// Executa os efeitos de um nó ou escolha. Cada efeito é isolado por try/catch pelo mesmo
        /// motivo que MessageBroker.Publish isola handlers: um asset de conteúdo quebrado não pode
        /// travar o fluxo do diálogo.
        /// </summary>
        private void ExecuteEffects(List<DialogueEffectSO> effects)
        {
            if (effects == null || effects.Count == 0) return;

            _isExecutingEffects = true;

            try
            {
                for (int i = 0; i < effects.Count; i++)
                {
                    DialogueEffectSO effect = effects[i];
                    if (effect == null) continue; // slot vazio no Inspector

                    try { effect.Execute(); }
                    catch (Exception exception) { Debug.LogException(exception); }
                }
            }
            finally
            {
                _isExecutingEffects = false;
            }
        }
        #endregion
    }
}
