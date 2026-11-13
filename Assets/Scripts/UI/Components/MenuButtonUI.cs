using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace ProjetoVN.UI.Components
{
    public class MenuButtonUI : UISelectableBase
    {
        [Tooltip("O texto do botão, que muda de cor conforme o estado. Vazio: o texto fica sempre na cor que tiver.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("Ícone do botão, pintado com a mesma cor do texto em todo estado. Vazio: o botão não tem ícone.")]
        [SerializeField] private Graphic icon;

        [Tooltip("Ornamento que aparece só com o botão destacado ou apertado. Vazio: o botão não tem ornamento.")]
        [SerializeField] private GameObject decorateImage;

        [Header("Colors")]
        [Tooltip("Cor do texto em repouso.")]
        [SerializeField] private Color normalTextColor = Color.white;

        [Tooltip("Cor do texto com o mouse em cima ou o botão selecionado.")]
        [SerializeField] private Color highlightedTextColor = Color.yellow;

        [Tooltip("Cor do texto enquanto o botão esquerdo do mouse está apertado sobre o botão.")]
        [SerializeField] private Color pressedTextColor = Color.yellow;

        [Tooltip("Cor do texto quando o botão está desabilitado.")]
        [SerializeField] private Color disabledTextColor = Color.gray;

        protected override void Reset()
        {
            base.Reset();
            if (decorateImage != null)
            {
                decorateImage.SetActive(false);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button == null) { return; }
            button.interactable = interactable;
            RefreshVisual();
        }

        protected override void RefreshVisual()
        {
            SelectableVisualState state = VisualState;
            Color color = TextColorOf(state);
            if (label != null)
            {
                label.color = color;
            }
            if (icon != null)
            {
                icon.color = color;
            }
            if (decorateImage != null)
            {
                decorateImage.SetActive(state == SelectableVisualState.Highlighted || state == SelectableVisualState.Pressed);
            }
        }

        private Color TextColorOf(SelectableVisualState state) => state switch
        {
            SelectableVisualState.Highlighted => highlightedTextColor,
            SelectableVisualState.Pressed => pressedTextColor,
            SelectableVisualState.Disabled => disabledTextColor,
            _ => normalTextColor
        };

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (label == null)
                Debug.LogWarning($"[MenuButtonUI] '{name}' está sem Label: o texto do botão não muda de cor com o estado.", this);
        }
#endif
    }
}
