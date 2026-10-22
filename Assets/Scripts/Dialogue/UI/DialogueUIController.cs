using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ProjetoVN.Core.Messaging;
using ProjetoVN.Dialogue.Characters;
using ProjetoVN.Dialogue.Logic;
using ProjetoVN.Dialogue.Messaging;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

        [Tooltip("Lista dos personagens que o roteiro pode citar. Vazio: todo nome aparece em texto puro, sem cor e sem sprite, e o console avisa.")]
        [SerializeField] private CharacterRegistry characterRegistry;

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

        [Tooltip("A tela de personagens, desenhada atrás da caixa. Vazio: os personagens não aparecem na tela.")]
        [SerializeField] private CharacterStageView characterStage;

        [Tooltip("O '>>' de continuar, ligado só quando a fala terminou de aparecer e espera o jogador. Vazio: não é controlado.")]
        [SerializeField] private GameObject continueIndicator;

        [Header("Histórico")]
        [Tooltip("Botão da caixa de diálogo que abre o histórico, desabilitado enquanto a fala é revelada. Vazio: o jogador não consegue abrir o histórico.")]
        [SerializeField] private Button openHistoryButton;

        [Tooltip("O painel do histórico, desenhado por cima da caixa e das opções. Vazio: o jogador não consegue abrir o histórico.")]
        [SerializeField] private DialogueHistoryView historyView;

        [Header("Pensamento")]
        [Tooltip("Cor do texto do pensamento do protagonista (Pensamento: no roteiro), na caixa e no histórico. O pensamento aparece também em itálico, " +
                 "sem placa de nome. Uma cor clara: o texto precisa de contraste com a caixa e com o balão do histórico.")]
        [SerializeField] private Color thoughtColor = new Color32(0xC9, 0xDC, 0xFF, 0xFF);

        [Header("Revelação do texto")]
        [Tooltip("Quantos caracteres da fala aparecem por segundo. É o único lugar onde se ajusta a velocidade do texto. Mínimo 1.")]
        [Min(1f)]
        [SerializeField] private float charactersPerSecond = 40f;

        private readonly List<DialogueOption> _shownOptions = new();
        private readonly ConversationStage _stage = new();
        private readonly ConversationLog _history = new();
        private Button[] _choiceButtons;
        private YarnTaskCompletionSource<DialogueOption> _selection;
        private Color _defaultNameColor;
        private Color _defaultTextColor;
        private FontStyles _defaultTextStyle;
        private FontStyles[] _defaultChoiceStyles;
        private LetterTypewriter _typewriter;

        /// <summary>A fala está aparecendo aos poucos; um pedido de pressa a completa.</summary>
        internal bool IsRevealingLine { get; private set; }

        /// <summary>O histórico está aberto por cima do diálogo; avançar a fala e escolher opção ficam bloqueados.</summary>
        internal bool IsHistoryOpen => historyView != null && historyView.IsOpen;

        private CharacterSO Protagonist => characterRegistry != null ? characterRegistry.Protagonist : null;

        private void Start()
        {
            _typewriter = new LetterTypewriter { TextElement = dialogueText };
            _defaultNameColor = speakerNameText.color;
            _defaultTextColor = dialogueText.color;
            _defaultTextStyle = dialogueText.fontStyle;
            CacheChoiceButtons();
            CacheChoiceTextStyles();
            ListenToOpenHistoryButton();
            HideAll();
            WarnAboutMissingCharacterRegistry();
            RegisterOnDialogueManager();
        }

        private void OnEnable() => MessageBroker.Subscribe<DialogueEndedMessage>(OnDialogueEnded);

        private void OnDisable() => MessageBroker.Unsubscribe<DialogueEndedMessage>(OnDialogueEnded);

        private void OnDestroy()
        {
            if (DialogueManager.Instance != null) DialogueManager.Instance.UnregisterPresenter(this);
        }

        // Devolve na hora de propósito: a rede de segurança do DialogueManager depende de o início ser síncrono.
        public override YarnTask OnDialogueStartedAsync()
        {
            _history.Clear();
            if (historyView != null) historyView.ForgetShownEntries();
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync() => YarnTask.CompletedTask;

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            SetActiveIfAssigned(continueIndicator, false);
            ShowLine(line);

            await RevealAsync(line, token.HurryUpToken);
            if (this == null) return;
            if (IsFollowedByOptions(line)) return;

            SetActiveIfAssigned(continueIndicator, true);
            await YarnTask.WaitUntilCanceled(token.NextContentToken).SuppressCancellationThrow();
        }

        // Aguardada dentro do RunLineAsync de propósito: o runner só entrega a fala seguinte quando ele termina,
        // então duas revelações nunca escrevem no mesmo texto ao mesmo tempo.
        private async YarnTask RevealAsync(LocalizedLine line, CancellationToken hurryUpToken)
        {
            using var reveal = CancellationTokenSource.CreateLinkedTokenSource(hurryUpToken, destroyCancellationToken);
            _typewriter.CharactersPerSecond = charactersPerSecond;
            SetRevealing(true);
            try
            {
                await _typewriter.RunTypewriter(line.TextWithoutCharacterName, reveal.Token).SuppressCancellationThrow();
            }
            finally
            {
                SetRevealing(false);
            }
        }

        private void SetRevealing(bool isRevealing)
        {
            IsRevealingLine = isRevealing;
            if (openHistoryButton != null) openHistoryButton.interactable = !isRevealing;
        }

        public override async YarnTask<DialogueOption> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken token)
        {
            CollectOptions(dialogueOptions);
            if (!HasAvailableOption()) return null;

            GiveTurnToProtagonist();
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
            if (IsHistoryOpen) return;

            RecordChosenOption(_shownOptions[buttonIndex]);
            _selection.TrySetResult(_shownOptions[buttonIndex]);
        }

        /// <summary>Abre o histórico se há conversa e a fala já terminou de aparecer; do contrário, não faz nada.</summary>
        public void OpenHistory()
        {
            if (historyView == null || IsHistoryOpen || IsRevealingLine) return;
            if (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive()) return;

            historyView.Open(_history.Entries);
        }

        internal void RecordItemReceived(string itemName) => _history.AddItemReceived(itemName);

        internal void RecordItemHandedOver(string itemName) => _history.AddItemHandedOver(itemName);

        private void RecordChosenOption(DialogueOption option)
        {
            string text = option.Line.TextWithoutCharacterName.Text;
            if (IsAction(option) || Protagonist == null)
            {
                _history.AddNarration(text);
                return;
            }

            RecordCharacterLine(Protagonist, text);
        }

        private static bool IsFollowedByOptions(LocalizedLine line) => System.Array.IndexOf(line.Metadata, LastLineTag) >= 0;

        private void ShowLine(LocalizedLine line)
        {
            ShowSpeaker(line);
            _typewriter.PrepareForContent(line.TextWithoutCharacterName);
            dialogueBox.SetActive(true);
            dialogueChoices.SetActive(false);
        }

        private void ShowSpeaker(LocalizedLine line)
        {
            string speaker = line.CharacterName;
            bool isNarration = string.IsNullOrWhiteSpace(speaker);
            bool isThought = ThoughtLine.Is(speaker);
            SetActiveIfAssigned(speakerNameplate, !isNarration && !isThought);
            ApplyTextStyle(isThought);

            string text = line.TextWithoutCharacterName.Text;

            if (isNarration)
            {
                speakerNameText.text = "";
                _history.AddNarration(text);
                DimStage();
                return;
            }

            // Antes de TryResolveCharacter: o nome reservado não é um personagem e não deve gerar o aviso de personagem desconhecido.
            if (isThought)
            {
                speakerNameText.text = "";
                _history.AddThought(text, thoughtColor);
                DimStage();
                return;
            }

            if (!TryResolveCharacter(speaker, out CharacterSO character))
            {
                speakerNameText.text = speaker;
                speakerNameText.color = _defaultNameColor;
                _history.AddSpokenLine(speaker, _defaultNameColor, text);
                DimStage();
                return;
            }

            speakerNameText.text = NameToDisplay(character);
            speakerNameText.color = character.NameColor;
            RecordCharacterLine(character, text);
            PutOnStage(character, line);
        }

        // A caixa é a mesma entre as falas: o estilo do pensamento precisa ser desfeito na fala seguinte.
        private void ApplyTextStyle(bool isThought)
        {
            dialogueText.fontStyle = isThought ? FontStyles.Italic : _defaultTextStyle;
            dialogueText.color = isThought ? thoughtColor : _defaultTextColor;
        }

        private void RecordCharacterLine(CharacterSO character, string text)
        {
            if (character == Protagonist) _history.AddProtagonistLine(NameToDisplay(character), character.NameColor, text);
            else _history.AddSpokenLine(NameToDisplay(character), character.NameColor, text);
        }

        // A troca do nome do protagonista pelo que o jogador escolheu (#26) entra aqui, sem mexer em nenhum roteiro.
        private static string NameToDisplay(CharacterSO character) => character.DisplayName;

        private bool TryResolveCharacter(string scriptName, out CharacterSO character)
        {
            character = null;
            if (characterRegistry == null) return false;
            if (characterRegistry.TryGetByScriptName(scriptName, out character)) return true;

            Debug.LogWarning($"[DialogueUIController] Personagem desconhecido no roteiro: '{scriptName}' (nó '{CurrentNodeName()}'). " +
                             "O nome aparece em texto puro, sem cor e sem sprite.", this);
            return false;
        }

        private void PutOnStage(CharacterSO character, LocalizedLine line)
        {
            string expression = ExpressionFor(character, line);
            if (expression == null)
            {
                DimStage();
                return;
            }

            _stage.Speak(character, expression);
            DrawStage();
        }

        private string ExpressionFor(CharacterSO character, LocalizedLine line)
        {
            string requested = ExpressionTag.FirstIn(line.Metadata);
            if (requested == null) return character.DefaultExpression;
            if (character.HasExpression(requested)) return requested;

            Debug.LogWarning($"[DialogueUIController] Expressão desconhecida: '{requested}' não existe em '{character.DisplayName}' (nó '{CurrentNodeName()}'). " +
                             "Mostrando a expressão padrão.", this);
            return character.DefaultExpression;
        }

        private void GiveTurnToProtagonist()
        {
            string expression = Protagonist != null ? Protagonist.DefaultExpression : null;
            if (expression == null)
            {
                DimStage();
                return;
            }

            _stage.GiveTurnToPlayer(Protagonist, expression);
            DrawStage();
        }

        private void DimStage()
        {
            _stage.DimEveryone();
            DrawStage();
        }

        private void DrawStage()
        {
            if (characterStage != null) characterStage.Show(_stage);
        }

        private static string CurrentNodeName() =>
            DialogueManager.Instance != null ? DialogueManager.Instance.CurrentNodeName : null;

        private void WarnAboutMissingCharacterRegistry()
        {
            if (characterRegistry == null)
                Debug.LogError("[DialogueUIController] Character Registry não está atribuído: todo nome aparece em texto puro, sem cor e sem sprite, e as opções não trazem o protagonista.", this);
        }

        private void CacheChoiceButtons()
        {
            _choiceButtons = new Button[choiceButtonObjects.Length];
            for (int i = 0; i < choiceButtonObjects.Length; i++)
                _choiceButtons[i] = choiceButtonObjects[i].GetComponent<Button>();
        }

        private void CacheChoiceTextStyles() =>
            _defaultChoiceStyles = choiceTexts.Select(choiceText => choiceText.fontStyle).ToArray();

        private static bool IsAction(DialogueOption option) => ActionOption.IsMarkedIn(option.Line.Metadata);

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
                choiceTexts[i].fontStyle = IsAction(_shownOptions[i]) ? FontStyles.Italic : _defaultChoiceStyles[i];
                if (_choiceButtons[i] != null) _choiceButtons[i].interactable = _shownOptions[i].IsAvailable;
            }

            SetActiveIfAssigned(continueIndicator, false);
            dialogueChoices.SetActive(true);
        }

        private void ListenToOpenHistoryButton()
        {
            if (openHistoryButton != null) openHistoryButton.onClick.AddListener(OpenHistory);
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
            if (historyView != null) historyView.Close();
            ClearSelectionInsideDialogue();
            _stage.Clear();
            DrawStage();
        }

        // A opção clicada continua selecionada, escondida, até o fim da conversa; uma seleção de fora (o menu) não é dela.
        private void ClearSelectionInsideDialogue()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return;

            GameObject selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(transform))
                eventSystem.SetSelectedGameObject(null);
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

            if (characterRegistry == null)
                Debug.LogWarning("[DialogueUIController] Character Registry está vazio: todo nome aparece em texto puro, sem cor e sem sprite.", this);

            if (openHistoryButton == null || historyView == null)
                Debug.LogWarning("[DialogueUIController] Open History Button e History View estão vazios: o jogador não consegue abrir o histórico.", this);

            if (thoughtColor.a < 1f)
                Debug.LogWarning("[DialogueUIController] Thought Color tem alfa menor que 1: o pensamento ficaria translúcido na caixa e opaco no histórico. Use alfa 255.", this);

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
