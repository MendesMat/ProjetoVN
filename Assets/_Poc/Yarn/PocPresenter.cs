#pragma warning disable CS8632
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace ProjetoVN.PocYarn
{
    public sealed class PocPresenter : DialoguePresenterBase
    {
        [Header("Caixa de fala")]
        [SerializeField, Tooltip("Objeto ligado enquanto há fala na tela. Vazio: a fala não aparece.")]
        private GameObject dialogueBox;
        [SerializeField, Tooltip("Texto da fala. Vazio: a fala não aparece.")]
        private TMP_Text dialogueText;
        [SerializeField, Tooltip("Texto do nome de quem fala. Vazio: o nome não aparece.")]
        private TMP_Text speakerNameText;
        [SerializeField, Tooltip("Placa do nome, ligada só quando a fala tem personagem. Vazio: a placa não é controlada.")]
        private GameObject nameplate;
        [SerializeField, Tooltip("Indicador de continuar, ligado enquanto a fala espera o jogador. Vazio: não é controlado.")]
        private GameObject continueIndicator;

        [Header("Escolhas")]
        [SerializeField, Tooltip("Objeto que contém os botões de escolha. Vazio: as escolhas não aparecem.")]
        private GameObject choicesContainer;
        [SerializeField, Tooltip("Um botão por opção possível, na ordem. Opções além do tamanho da lista são descartadas.")]
        private Button[] choiceButtons;
        [SerializeField, Tooltip("O texto de cada botão, na mesma ordem de Choice Buttons.")]
        private TMP_Text[] choiceTexts;

        [Header("Registro")]
        [SerializeField, Tooltip("Runner que usa este apresentador. Vazio: procura um na cena e, se não achar, loga um erro.")]
        private DialogueRunner runner;

        private DialogueOption[] _currentOptions = System.Array.Empty<DialogueOption>();
        private YarnTaskCompletionSource<DialogueOption?> _selection;

        private void Awake() => WireChoiceButtons();

        private void Start()
        {
            HideAll();
            RegisterOnRunner();
        }

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;

        public override YarnTask OnDialogueCompleteAsync()
        {
            HideAll();
            return YarnTask.CompletedTask;
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            string speaker = line.CharacterName;
            speakerNameText.text = speaker;
            dialogueText.text = line.TextWithoutCharacterName.Text;
            SetActive(nameplate, !string.IsNullOrWhiteSpace(speaker));
            SetActive(choicesContainer, false);
            SetActive(dialogueBox, true);
            SetActive(continueIndicator, true);

            await YarnTask.WaitUntilCanceled(token.NextContentToken).SuppressCancellationThrow();

            SetActive(continueIndicator, false);
        }

        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken token)
        {
            _currentOptions = dialogueOptions;
            _selection = new YarnTaskCompletionSource<DialogueOption?>();
            ShowOptions(dialogueOptions);

            DialogueOption? chosen = await _selection.Task;

            SetActive(choicesContainer, false);
            return chosen;
        }

        public void Choose(int optionIndex)
        {
            if (_selection == null || optionIndex < 0 || optionIndex >= _currentOptions.Length) return;
            if (!_currentOptions[optionIndex].IsAvailable) return;

            _selection.TrySetResult(_currentOptions[optionIndex]);
        }

        private void ShowOptions(DialogueOption[] options)
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                bool isVisible = i < options.Length && options[i].IsAvailable;
                choiceButtons[i].gameObject.SetActive(isVisible);
                if (isVisible) choiceTexts[i].text = options[i].Line.TextWithoutCharacterName.Text;
            }

            SetActive(continueIndicator, false);
            SetActive(choicesContainer, true);
        }

        private void WireChoiceButtons()
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int optionIndex = i;
                choiceButtons[i].onClick.AddListener(() => Choose(optionIndex));
            }
        }

        private void RegisterOnRunner()
        {
            if (runner == null) runner = FindFirstObjectByType<DialogueRunner>();
            if (runner == null)
            {
                Debug.LogError("[PocPresenter] Não há DialogueRunner na cena. O apresentador não foi registrado.", this);
                return;
            }

            runner.DialoguePresenters = new DialoguePresenterBase[] { this };
        }

        private void HideAll()
        {
            SetActive(dialogueBox, false);
            SetActive(choicesContainer, false);
            SetActive(continueIndicator, false);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null) target.SetActive(active);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (dialogueBox == null || dialogueText == null || speakerNameText == null)
                Debug.LogWarning("[PocPresenter] Caixa, texto da fala e texto do nome precisam estar atribuídos.", this);

            if (choicesContainer == null || choiceButtons == null || choiceButtons.Length == 0)
                Debug.LogWarning("[PocPresenter] O contêiner e os botões de escolha precisam estar atribuídos.", this);
            else if (choiceTexts == null || choiceTexts.Length != choiceButtons.Length)
                Debug.LogWarning("[PocPresenter] Choice Texts precisa ter o mesmo tamanho de Choice Buttons.", this);
        }
#endif
    }
}
