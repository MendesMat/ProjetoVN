using UnityEngine;
using TMPro;

namespace ProjetoVN.UI.Components
{
    public class MenuButtonUI : UISelectableBase
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject decorateImage;

        [Header("Colors")]
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color highlightedTextColor = Color.yellow;
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
            Color targetColor = IsInteractable ? (IsHighlighted ? highlightedTextColor : normalTextColor) : disabledTextColor;
            if (label != null)
            {
                label.color = targetColor;
            }
            if (decorateImage != null)
            {
                decorateImage.SetActive(IsHighlighted && IsInteractable);
            }
        }
    }
}
