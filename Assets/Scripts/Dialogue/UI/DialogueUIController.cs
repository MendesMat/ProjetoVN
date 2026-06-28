using Assets.Scripts.Core.Messaging;
using Assets.Scripts.Dialogue.Messaging;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Dialogue.UI
{
    public class DialogueUIController : MonoBehaviour
    {
        [SerializeField] private GameObject dialogueBox;
        [SerializeField] private GameObject dialogueChoices;

        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;

        [SerializeField] private GameObject[] choiceButtonObjects;
        [SerializeField] private TMP_Text[] choiceTexts;

        #region Unity Lifecycle

        private void Start()
        {
            HideAll();
        }

        private void OnEnable()
        {
            MessageBroker.Subscribe<DialogueLineMessage>(OnNewLine);
            MessageBroker.Subscribe<DialogueChoicesMessage>(OnChoicesAvailable);
            MessageBroker.Subscribe<DialogueEndedMessage>(OnDialogueEnded);
        }

        private void OnDisable()
        {
            MessageBroker.Unsubscribe<DialogueLineMessage>(OnNewLine);
            MessageBroker.Unsubscribe<DialogueChoicesMessage>(OnChoicesAvailable);
            MessageBroker.Unsubscribe<DialogueEndedMessage>(OnDialogueEnded);
        }

        #endregion

        #region Message Handlers
        private void OnNewLine(DialogueLineMessage message)
        {
            speakerNameText.text = message.SpeakerName;
            dialogueText.text = message.Text;
            ShowDialogueBox();
            HideChoices();
        }

        private void OnChoicesAvailable(DialogueChoicesMessage message)
        {
            PopulateChoiceButtons(message);
            ShowChoices();
        }

        private void OnDialogueEnded(DialogueEndedMessage message)
        {
            HideAll();
        }
        #endregion

        #region Choice Population
        private void PopulateChoiceButtons(DialogueChoicesMessage message)
        {
            for (int i = 0; i < choiceButtonObjects.Length; i++)
            {
                bool isVisible = i < message.Choices.Count;
                choiceButtonObjects[i].SetActive(isVisible);

                if (isVisible) choiceTexts[i].text = message.Choices[i].Text;
            }
        }
        #endregion

        #region Visibility
        private void ShowDialogueBox() => dialogueBox.SetActive(true);
        private void ShowChoices() => dialogueChoices.SetActive(true);
        private void HideChoices() => dialogueChoices.SetActive(false);

        private void HideAll()
        {
            dialogueBox.SetActive(false);
            dialogueChoices.SetActive(false);
        }
        #endregion
    }
}
