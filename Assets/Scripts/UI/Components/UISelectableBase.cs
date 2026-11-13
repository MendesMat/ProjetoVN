using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ProjetoVN.UI.Components
{
    [RequireComponent(typeof(Button))]
    public abstract class UISelectableBase : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Base References")]
        [Tooltip("O Button deste objeto, que é quem executa a ação. Vazio: o componente procura o Button no próprio objeto.")]
        [SerializeField] protected Button button;

        [Tooltip("Ligado: o mouse sobre o botão o seleciona, e o destaque passa a ser a seleção (só um botão destacado por vez, " +
                 "mesmo com mouse e teclado). Desligado: ponteiro e seleção destacam cada um por conta própria.")]
        [SerializeField] protected bool selectOnHover = true;

        private readonly SelectableHighlight _highlight = new();

        protected Button Button => button;

        protected bool IsInteractable => button == null || button.interactable;

        protected SelectableVisualState VisualState => _highlight.StateFor(IsInteractable);

        #region Unity
        protected virtual void Reset()
        {
            button = GetComponent<Button>();
        }
        protected virtual void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        // Espelha o Selectable, que restaura a seleção no OnEnable e limpa tudo no OnDisable:
        // assim o sprite (do Button) e a cor do texto (deste componente) nunca discordam.
        private void OnEnable()
        {
            _highlight.Reset(isSelected: IsSelectedInEventSystem());
            RefreshVisual();
        }

        private void OnDisable()
        {
            _highlight.Reset(isSelected: false);
        }

        #endregion

        protected abstract void RefreshVisual();

        #region EventSystem
        public void OnDeselect(BaseEventData eventData)
        {
            bool pointerHighlightDropped = _highlight.Deselected(selectOnHover);
            if (pointerHighlightDropped) ClearPointerStateOfButton();

            RefreshVisual();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _highlight.PointerEntered();
            if (selectOnHover == true && IsInteractable && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }

            RefreshVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _highlight.PointerExited();
            RefreshVisual();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            _highlight.PointerPressed();
            RefreshVisual();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            _highlight.PointerReleased();
            RefreshVisual();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _highlight.Selected();
            RefreshVisual();
        }
        #endregion

        private bool IsSelectedInEventSystem() =>
            EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;

        // O Selectable só zera o "ponteiro dentro" no OnPointerExit; sem isso o sprite de destaque ficaria no botão que perdeu a seleção.
        private void ClearPointerStateOfButton()
        {
            if (button == null || EventSystem.current == null) return;

            button.OnPointerExit(new PointerEventData(EventSystem.current));
        }
    }
}
