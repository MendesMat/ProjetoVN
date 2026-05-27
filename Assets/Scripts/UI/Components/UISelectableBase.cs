using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ProjetoVN.UI.Components
{
    [RequireComponent(typeof(Button))]
    public abstract class UISelectableBase : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Base References")]
        [SerializeField] protected Button button;

        [SerializeField] protected bool selectOnHover = true;

        protected bool isPointerOver;
        protected bool isSelected;

        protected Button Button => button;

        protected bool IsHighlighted => isPointerOver == true || isSelected == true;

        protected bool IsInterectable => button == false || button.interactable;

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

        private void OnEnable()
        {
            RefreshVisual();
        }

        #endregion

        protected abstract void RefreshVisual();

        #region EventSystem
        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            RefreshVisual();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            if (selectOnHover == true && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }

            RefreshVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isSelected = false;
            RefreshVisual();
        }

        public void OnSelect(BaseEventData eventData)
        {
            isSelected = true;
            RefreshVisual();
        }
        #endregion
    }
}
