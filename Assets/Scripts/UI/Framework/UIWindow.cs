using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjetoVN.UI.Framework
{
    public class UIWindow : MonoBehaviour
    {
        [Header("Window")]
        [SerializeField] private GameObject root;
        [SerializeField] private GameObject firstSelected;
        [SerializeField] private bool rememberLastSelected = true;

        private GameObject lastSelected;

        public GameObject Root => root != null ? root : gameObject;

        public void Show()
        {
            Root.SetActive(true);
            SelectDefault();
        }

        public void Hide()
        {
            ReleaseSelectionInsideWindow();
            Root.SetActive(false);
        }

        public void SelectDefault()
        {
            if (EventSystem.current == null) { return; }

            GameObject target = firstSelected;

            if ((rememberLastSelected == true && lastSelected != null && lastSelected.activeInHierarchy == true))
            {
                target = lastSelected;
            }

            if (target != null)
            {
                EventSystem.current.SetSelectedGameObject(target);
            }
        }

        // Sem soltar a seleção, o botão escondido continuaria selecionado e um Submit seguinte o acionaria.
        private void ReleaseSelectionInsideWindow()
        {
            if (EventSystem.current == null) { return; }

            GameObject current = EventSystem.current.currentSelectedGameObject;
            if(current == null) { return; }

            if (current.transform.IsChildOf(Root.transform))
            {
                lastSelected = current;
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }
}
