using System.Collections.Generic;
using ProjetoVN.Dialogue.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjetoVN.Dialogue.UI
{
    /// <summary>
    /// O painel do histórico de falas (D-30): só mostra as entradas que o apresentador entrega e rola até a mais
    /// recente. Fica no próprio objeto do painel, que é um <c>Canvas</c> aninhado: abrir e fechar ligam e desligam
    /// esse componente, e o objeto fica sempre ativo. Reaproveita as <c>TMP_Text</c> das entradas (D-11): cria as
    /// que faltam e desliga as que sobram.
    /// </summary>
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    public sealed class DialogueHistoryView : MonoBehaviour
    {
        [Header("Painel")]
        [Tooltip("A área de rolagem das entradas. Vazio: o histórico não abre e o console avisa.")]
        [SerializeField] private ScrollRect scrollRect;

        [Tooltip("O conteúdo da área de rolagem, onde as entradas são criadas, com um Vertical Layout Group. Vazio: o histórico não abre e o console avisa.")]
        [SerializeField] private RectTransform content;

        [Tooltip("Modelo de uma entrada, desligado, dentro do conteúdo. As entradas são cópias dele. Vazio: o histórico não abre e o console avisa.")]
        [SerializeField] private TMP_Text entryTemplate;

        [Tooltip("Botão que fecha o histórico. Vazio: não há como fechar o histórico, e o console avisa.")]
        [SerializeField] private Button closeButton;

        private readonly List<TMP_Text> _entryTexts = new();
        private Canvas _panelCanvas;

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

            ShowEntries(entries);
            ScrollToMostRecent();
            _panelCanvas.enabled = true;
            ClearUiSelection();
        }

        // Desligar o Canvas, e não o objeto: reativar as entradas faria o TMP refazer o texto de todas (~100 ms com 200 falas).
        internal void Close()
        {
            if (_panelCanvas != null) _panelCanvas.enabled = false;
            ClearUiSelection();
        }

        private void ShowEntries(IReadOnlyList<ConversationEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (i == _entryTexts.Count) _entryTexts.Add(Instantiate(entryTemplate, content));

                _entryTexts[i].text = entries[i].Text;
                _entryTexts[i].gameObject.SetActive(true);
            }

            for (int i = entries.Count; i < _entryTexts.Count; i++)
                _entryTexts[i].gameObject.SetActive(false);
        }

        // Sem reconstruir o layout antes, o conteúdo ainda tem o tamanho antigo e a rolagem não para na entrada mais recente.
        private void ScrollToMostRecent()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
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
            if (_panelCanvas != null && scrollRect != null && content != null && entryTemplate != null) return true;

            Debug.LogError("[DialogueHistoryView] Scroll Rect, Content e Entry Template precisam estar atribuídos, e o objeto do painel precisa ficar ativo no prefab. O histórico não abre.", this);
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (scrollRect == null || content == null || entryTemplate == null)
                Debug.LogWarning("[DialogueHistoryView] Scroll Rect, Content e Entry Template precisam estar atribuídos: sem eles o histórico não abre.", this);

            if (closeButton == null)
                Debug.LogWarning("[DialogueHistoryView] Close Button está vazio: não há como fechar o histórico.", this);

            if (!gameObject.activeSelf)
                Debug.LogWarning("[DialogueHistoryView] O objeto do painel está desligado: o histórico não abre. Deixe-o ativo; o painel se esconde sozinho (Canvas desligado).", this);
        }
#endif
    }
}
