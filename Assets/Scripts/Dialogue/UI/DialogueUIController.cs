using System.Collections.Generic;
using ProjetoVN.Core.Messaging;
using ProjetoVN.Dialogue.Logic;
using ProjetoVN.Dialogue.Messaging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace ProjetoVN.Dialogue.UI
{
    /// <summary>
    /// O apresentador do Yarn Spinner: recebe do <c>DialogueRunner</c> cada fala e cada grupo de opções
    /// e os mostra na caixa, na placa de nome e nos botões de escolha. Registra-se no <see cref="DialogueManager"/>
    /// ao iniciar e sai ao ser destruído.
    /// </summary>
    public class DialogueUIController : DialoguePresenterBase
    {
        private const string LastLineTag = "lastline";

        [Header("Caixa de fala")]
        [Tooltip("Objeto ligado enquanto há fala na tela. Vazio: a interface de diálogo não funciona e o console avisa.")]
        [SerializeField] private GameObject dialogueBox;

        [Tooltip("Texto da fala. Vazio: a interface de diálogo não funciona e o console avisa.")]
        [SerializeField] private TMP_Text dialogueText;

        [Tooltip("Texto do nome de quem fala. Vazio: a interface de diálogo não funciona e o console avisa.")]
        [SerializeField] private TMP_Text speakerNameText;

        [Header("Escolhas")]
        [Tooltip("Objeto que contém os botões de escolha, ligado só enquanto há opções. Vazio: a interface de diálogo não funciona e o console avisa.")]
        [SerializeField] private GameObject dialogueChoices;

        [Tooltip("Um objeto por botão de escolha, na ordem, cada um com um componente Button. Uma opção indisponível ocupa um botão, desabilitado. " +
                 "Opções além do tamanho da lista são descartadas, com erro no console (D-11).")]
        [SerializeField] private GameObject[] choiceButtonObjects;

        [Tooltip("O texto de cada botão, na mesma ordem e do mesmo tamanho de Choice Button Objects.")]
        [SerializeField] private TMP_Text[] choiceTexts;

        [Header("Opcionais")]
        [Tooltip("Placa do nome, ligada só quando a fala tem personagem (narração a esconde). Vazio: a placa não é controlada.")]
        [SerializeField] private GameObject speakerNameplate;

        [Tooltip("O '>>' de continuar, ligado enquanto a fala espera o jogador. Vazio: não é controlado.")]
        [SerializeField] private GameObject continueIndicator;

        private readonly List<DialogueOption> _shownOptions = new();
        private Button[] _choiceButtons;
        private YarnTaskCompletionSource<DialogueOption> _selection;

        private void Start()
        {
            CacheChoiceButtons();
            HideAll();
            RegisterOnDialogueManager();
        }

        private void OnEnable() => MessageBroker.Subscribe<DialogueEndedMessage>(OnDialogueEnded);

        private void OnDisable() => MessageBroker.Unsubscribe<DialogueEndedMessage>(OnDialogueEnded);

        private void OnDestroy()
        {
            if (DialogueManager.Instance != null) DialogueManager.Instance.UnregisterPresenter(this);
        }

        public override YarnTask OnDialogueStartedAsync() => YarnTask.CompletedTask;

        public override YarnTask OnDialogueCompleteAsync() => YarnTask.CompletedTask;

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            ShowLine(line);

            if (IsFollowedByOptions(line))
            {
                SetActiveIfAssigned(continueIndicator, false);
                return;
            }

            SetActiveIfAssigned(continueIndicator, true);
            await YarnTask.WaitUntilCanceled(token.NextContentToken).SuppressCancellationThrow();
        }

        public override async YarnTask<DialogueOption> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken token)
        {
            CollectOptions(dialogueOptions);
            if (!HasAvailableOption()) return null;

            ShowOptions();
            var selection = new YarnTaskCompletionSource<DialogueOption>();
            _selection = selection;

            DialogueOption chosen;
            using (token.NextContentToken.Register(() => selection.TrySetResult(null)))
            {
                chosen = await selection.Task;
            }

            if (this == null) return null;

            _selection = null;
            dialogueChoices.SetActive(false);
            return chosen;
        }

        public void Choose(int buttonIndex)
        {
            if (_selection == null) return;
            if (buttonIndex < 0 || buttonIndex >= _shownOptions.Count) return;
            if (!_shownOptions[buttonIndex].IsAvailable) return;

            _selection.TrySetResult(_shownOptions[buttonIndex]);
        }

        private static bool IsFollowedByOptions(LocalizedLine line) => System.Array.IndexOf(line.Metadata, LastLineTag) >= 0;

        private void ShowLine(LocalizedLine line)
        {
            string speaker = line.CharacterName;
            speakerNameText.text = speaker ?? "";
            dialogueText.text = line.TextWithoutCharacterName.Text;
            SetActiveIfAssigned(speakerNameplate, !string.IsNullOrWhiteSpace(speaker));
            dialogueBox.SetActive(true);
            dialogueChoices.SetActive(false);
        }

        private void CacheChoiceButtons()
        {
            _choiceButtons = new Button[choiceButtonObjects.Length];
            for (int i = 0; i < choiceButtonObjects.Length; i++)
                _choiceButtons[i] = choiceButtonObjects[i].GetComponent<Button>();
        }

        private bool HasAvailableOption() => _shownOptions.Exists(option => option.IsAvailable);

        private void CollectOptions(DialogueOption[] dialogueOptions)
        {
            _shownOptions.Clear();
            _shownOptions.AddRange(dialogueOptions);

            if (_shownOptions.Count <= choiceButtonObjects.Length) return;

            Debug.LogError($"[DialogueUIController] O roteiro ofereceu {_shownOptions.Count} opções, mas há só {choiceButtonObjects.Length} botões. " +
                           "Mostrando as primeiras (D-11).", this);
            _shownOptions.RemoveRange(choiceButtonObjects.Length, _shownOptions.Count - choiceButtonObjects.Length);
        }

        private void ShowOptions()
        {
            for (int i = 0; i < choiceButtonObjects.Length; i++)
            {
                bool isVisible = i < _shownOptions.Count;
                choiceButtonObjects[i].SetActive(isVisible);

                if (!isVisible) continue;

                choiceTexts[i].text = _shownOptions[i].Line.TextWithoutCharacterName.Text;
                if (_choiceButtons[i] != null) _choiceButtons[i].interactable = _shownOptions[i].IsAvailable;
            }

            SetActiveIfAssigned(continueIndicator, false);
            dialogueChoices.SetActive(true);
        }

        private void RegisterOnDialogueManager()
        {
            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[DialogueUIController] Não há DialogueManager (o prefab Managers não foi criado). A interface não foi registrada e o diálogo não inicia.", this);
                return;
            }

            DialogueManager.Instance.RegisterPresenter(this);
        }

        private void OnDialogueEnded(DialogueEndedMessage message) => HideAll();

        private void HideAll()
        {
            dialogueBox.SetActive(false);
            dialogueChoices.SetActive(false);
            SetActiveIfAssigned(continueIndicator, false);
        }

        private static void SetActiveIfAssigned(GameObject target, bool active)
        {
            if (target != null) target.SetActive(active);
        }

#if UNITY_EDITOR
        private void WarnAboutChoiceObjectsWithoutButton()
        {
            foreach (GameObject choiceObject in choiceButtonObjects)
            {
                if (choiceObject != null && choiceObject.GetComponent<Button>() == null)
                    Debug.LogWarning($"[DialogueUIController] '{choiceObject.name}' não tem um componente Button: a opção indisponível não aparece desabilitada.", this);
            }
        }

        private void OnValidate()
        {
            if (dialogueBox == null || dialogueText == null || speakerNameText == null || dialogueChoices == null)
                Debug.LogWarning("[DialogueUIController] Caixa, texto da fala, texto do nome e contêiner de escolhas precisam estar atribuídos.", this);

            if (choiceButtonObjects == null || choiceButtonObjects.Length == 0)
                Debug.LogWarning("[DialogueUIController] Choice Button Objects está vazio: as escolhas não aparecem.", this);
            else if (choiceTexts == null || choiceTexts.Length != choiceButtonObjects.Length)
                Debug.LogWarning("[DialogueUIController] Choice Texts precisa ter o mesmo tamanho de Choice Button Objects.", this);
            else
                WarnAboutChoiceObjectsWithoutButton();
        }
#endif
    }
}
