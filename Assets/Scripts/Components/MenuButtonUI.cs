using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

namespace ProjetoVN.UIFramework.Components
{
    public class MenuButtonUI : UISelectableBase, ISubmitHandler
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject decorateImage;

        [Header("Colors")]
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color highlightedTextColor = Color.yellow;
        [SerializeField] private Color disabledTextColor = Color.gray;


        [Header("Behavior")]
        [SerializeField] private bool invokeClickOnSubmit = true;
        [Header("Events")]
        [SerializeField] private UnityEvent onClick;

        protected override void Reset()
        {
            base.Reset();
            if (decorateImage != null)
            {
                decorateImage.SetActive(false);
            }
        }
        protected override void Awake()
        {
            base.Awake();
            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }

        }
        void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }

        }
        private void HandleClick()
        {
            if (button != null && button.interactable == false) { return; }
            onClick?.Invoke();
        }
        public void OnSubmit(BaseEventData eventData)
        {
            if (invokeClickOnSubmit == false || button == null || button.interactable == false) { return; }
            onClick?.Invoke();
        }

        protected override void RefreshVisual()
        {
            Color targetColor = IsInterectable == true ? (IsHighlighted == true ? highlightedTextColor : normalTextColor) : disabledTextColor;
            if (label != null)
            {
                label.color = targetColor;
            }
            if (decorateImage != null)
            {
                decorateImage.SetActive(IsHighlighted);
            }
        }
        public void AddListener(UnityAction callback)
        {
            onClick.AddListener(callback);
        }
        public void RemoveListener(UnityAction callback)
        {
            onClick.RemoveListener(callback);
        }
    }
}
