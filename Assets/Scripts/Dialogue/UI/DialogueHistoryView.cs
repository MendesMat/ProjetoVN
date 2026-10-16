using System.Collections.Generic;
using ProjetoVN.Dialogue.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjetoVN.Dialogue.UI
{
    /// <summary>
    /// O painel do histórico de falas (D-30): um balão por entrada, como num app de mensagens. Só mostra as entradas
    /// que o apresentador entrega e rola até a mais recente. Fica no próprio objeto do painel, que é um
    /// <c>Canvas</c> aninhado: abrir e fechar ligam e desligam esse componente, e o objeto fica sempre ativo.
    /// Reaproveita os balões (D-11): cria os que faltam e desliga os que sobram. Uma reabertura acrescenta só as
    /// entradas novas; as já mostradas não são tocadas.
    /// </summary>
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    public sealed class DialogueHistoryView : MonoBehaviour
    {
        [Header("Painel")]
        [Tooltip("A área de rolagem das entradas. Vazio: o histórico não abre e o console avisa.")]
        [SerializeField] private ScrollRect scrollRect;

        [Tooltip("O conteúdo da área de rolagem, onde os balões são criados, com um Vertical Layout Group. Vazio: o histórico não abre e o console avisa.")]
        [SerializeField] private RectTransform content;

        [Tooltip("Modelo de um balão, desligado, dentro do conteúdo: uma linha com Horizontal Layout Group, e dentro dela o balão com Image, " +
                 "Vertical Layout Group e Layout Element, e o texto. Os balões são cópias dele. Vazio ou incompleto: o histórico não abre e o console avisa.")]
        [SerializeField] private RectTransform bubbleTemplate;

        [Tooltip("Botão que fecha o histórico. Vazio: não há como fechar o histórico, e o console avisa.")]
        [SerializeField] private Button closeButton;

        [Header("Balões")]
        [Tooltip("Cor do balão de narração e de item (uma cor translúcida e escura combina com a caixa de diálogo). " +
                 "O balão de um personagem usa a Name Color dele.")]
        [SerializeField] private Color neutralBubbleColor = new Color32(0x24, 0x11, 0x1F, 0xD1);

        [Tooltip("Opacidade do balão do pensamento do protagonista, tingido pela cor de pensamento do Dialogue UI Controller. " +
                 "Acima de 0,3 o texto perde contraste com o balão.")]
        [Range(0.05f, 0.3f)]
        [SerializeField] private float thoughtBubbleOpacity = 0.2f;

        [Tooltip("Largura máxima de um balão, como fração da largura do conteúdo. Um texto maior quebra em linhas.")]
        [Range(0.3f, 1f)]
        [SerializeField] private float maxBubbleWidth = 0.65f;

        [Tooltip("Tamanho da fonte dos itens que o jogador recebeu ou entregou. Falas e narração usam o tamanho do modelo.")]
        [Min(1f)]
        [SerializeField] private float itemFontSize = 28f;

        private readonly List<BubbleRow> _rows = new();
        private Canvas _panelCanvas;
        private int _shownCount;

        internal bool IsOpen => _panelCanvas != null && _panelCanvas.enabled;

        private void Awake()
        {
            _panelCanvas = GetComponent<Canvas>();
            _panelCanvas.enabled = false;
        }

        private void OnEnable()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        internal void Open(IReadOnlyList<ConversationEntry> entries)
        {
            if (!HasRequiredReferences()) return;

            bool layoutChanged = ShowNewEntries(entries) | HideSurplusRows(entries.Count);
            ScrollToMostRecent(layoutChanged);
            _panelCanvas.enabled = true;
            ClearUiSelection();
        }

        // Desligar o Canvas, e não o objeto: reativar as entradas faria o TMP refazer o texto de todas (~100 ms com 200 falas).
        internal void Close()
        {
            if (_panelCanvas != null) _panelCanvas.enabled = false;
            ClearUiSelection();
        }

        /// <summary>Uma conversa nova começou: os balões que existem são de outra conversa e serão reescritos.</summary>
        internal void ForgetShownEntries() => _shownCount = 0;

        private bool ShowNewEntries(IReadOnlyList<ConversationEntry> entries)
        {
            for (int i = _shownCount; i < entries.Count; i++)
            {
                if (i == _rows.Count) _rows.Add(new BubbleRow(Instantiate(bubbleTemplate, content)));

                Show(_rows[i], entries[i]);
            }

            bool addedAny = entries.Count > _shownCount;
            _shownCount = entries.Count;
            return addedAny;
        }

        private bool HideSurplusRows(int entryCount)
        {
            bool hidAny = false;
            for (int i = entryCount; i < _rows.Count; i++)
            {
                if (!_rows[i].GameObject.activeSelf) continue;

                _rows[i].GameObject.SetActive(false);
                hidAny = true;
            }

            return hidAny;
        }

        private void Show(BubbleRow row, ConversationEntry entry)
        {
            Color bubbleColor = BubbleColorOf(entry);

            row.Layout.childAlignment = AlignmentOf(entry);
            row.Background.color = bubbleColor;
            row.Text.color = TextColorOf(entry, bubbleColor);
            row.Text.fontSize = entry.Kind == ConversationEntryKind.Item ? itemFontSize : row.SpeechFontSize;
            row.Text.fontStyle = IsItalic(entry) ? FontStyles.Italic : FontStyles.Normal;
            row.Text.text = TextOf(entry);
            row.Size.preferredWidth = WidthOf(row);
            row.GameObject.SetActive(true);
        }

        private Color BubbleColorOf(ConversationEntry entry)
        {
            if (entry.Kind == ConversationEntryKind.Speech) return entry.SpeakerColor;
            if (entry.Kind == ConversationEntryKind.Thought) return new Color(entry.SpeakerColor.r, entry.SpeakerColor.g, entry.SpeakerColor.b, thoughtBubbleOpacity);

            return neutralBubbleColor;
        }

        private static Color TextColorOf(ConversationEntry entry, Color bubbleColor) =>
            entry.Kind == ConversationEntryKind.Thought ? entry.SpeakerColor : BubbleTextColor.For(bubbleColor);

        private static bool IsItalic(ConversationEntry entry) =>
            entry.Kind == ConversationEntryKind.Narration || entry.Kind == ConversationEntryKind.Thought;

        private static TextAnchor AlignmentOf(ConversationEntry entry)
        {
            if (entry.Kind == ConversationEntryKind.Thought) return TextAnchor.UpperRight;
            if (entry.Kind != ConversationEntryKind.Speech) return TextAnchor.UpperCenter;

            return entry.IsProtagonist ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        }

        private static string TextOf(ConversationEntry entry) =>
            entry.ShowsName ? $"<b>{entry.SpeakerName}</b>\n{entry.Text}" : entry.Text;

        private float WidthOf(BubbleRow row)
        {
            float maxWidth = content.rect.width * maxBubbleWidth;
            float textMaxWidth = maxWidth - row.HorizontalPadding;
            float textWidth = row.Text.GetPreferredValues(row.Text.text, textMaxWidth, 0f).x;

            return Mathf.Min(textWidth + row.HorizontalPadding, maxWidth);
        }

        // Sem reconstruir o layout antes, o conteúdo ainda tem o tamanho antigo e a rolagem não para na entrada mais recente.
        private void ScrollToMostRecent(bool layoutChanged)
        {
            if (layoutChanged) LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        // Um botão clicado fica selecionado, e o Enter (Submit) o aciona de novo: seria um atalho de teclado.
        private static void ClearUiSelection()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private bool HasRequiredReferences()
        {
            if (_panelCanvas != null && scrollRect != null && content != null && TemplateIsComplete()) return true;

            Debug.LogError("[DialogueHistoryView] Scroll Rect, Content e Bubble Template (completo) precisam estar atribuídos, e o objeto do painel precisa ficar ativo no prefab. O histórico não abre.", this);
            return false;
        }

        private bool TemplateIsComplete() =>
            bubbleTemplate != null
            && bubbleTemplate.GetComponent<HorizontalLayoutGroup>() != null
            && bubbleTemplate.GetComponentInChildren<Image>(true) != null
            && bubbleTemplate.GetComponentInChildren<VerticalLayoutGroup>(true) != null
            && bubbleTemplate.GetComponentInChildren<LayoutElement>(true) != null
            && bubbleTemplate.GetComponentInChildren<TMP_Text>(true) != null;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (scrollRect == null || content == null || !TemplateIsComplete())
                Debug.LogWarning("[DialogueHistoryView] Scroll Rect, Content e Bubble Template precisam estar atribuídos, e o modelo precisa ter Horizontal Layout Group na raiz e, dentro, Image, Vertical Layout Group, Layout Element e TMP_Text: sem isso o histórico não abre.", this);

            if (closeButton == null)
                Debug.LogWarning("[DialogueHistoryView] Close Button está vazio: não há como fechar o histórico.", this);

            if (!gameObject.activeSelf)
                Debug.LogWarning("[DialogueHistoryView] O objeto do painel está desligado: o histórico não abre. Deixe-o ativo; o painel se esconde sozinho (Canvas desligado).", this);
        }
#endif

        /// <summary>As partes de um balão, achadas uma vez na criação para não repetir <c>GetComponent</c> a cada abertura.</summary>
        private sealed class BubbleRow
        {
            public BubbleRow(RectTransform instance)
            {
                GameObject = instance.gameObject;
                Layout = instance.GetComponent<HorizontalLayoutGroup>();
                Background = instance.GetComponentInChildren<Image>(true);
                Size = instance.GetComponentInChildren<LayoutElement>(true);
                Text = instance.GetComponentInChildren<TMP_Text>(true);
                SpeechFontSize = Text.fontSize;
                HorizontalPadding = instance.GetComponentInChildren<VerticalLayoutGroup>(true).padding.horizontal;
            }

            public GameObject GameObject { get; }
            public HorizontalLayoutGroup Layout { get; }
            public Image Background { get; }
            public LayoutElement Size { get; }
            public TMP_Text Text { get; }
            public float SpeechFontSize { get; }
            public float HorizontalPadding { get; }
        }
    }
}
