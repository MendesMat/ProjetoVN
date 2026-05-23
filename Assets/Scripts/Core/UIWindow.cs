using UnityEngine;
using UnityEngine.EventSystems;
namespace ProjetoVN.UIFramework
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
            SaveCurrentSelection();
            root.SetActive(false);
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
        private void SaveCurrentSelection()
        {
            if (EventSystem.current == null) { return; }

            GameObject current = EventSystem.current.currentSelectedGameObject;
            if(current == null) { return; }

            if (current.transform.IsChildOf(Root.transform))
            {
                lastSelected = current;
            }
        }
    }
}
