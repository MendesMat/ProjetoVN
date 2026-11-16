using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjetoVN.UI.Framework
{
    public class UIWindow : MonoBehaviour
    {
        [Header("Window")]
        [Tooltip("O objeto ligado e desligado ao abrir e fechar a janela. Vazio = a janela usa o próprio objeto deste componente.")]
        [SerializeField] private GameObject root;

        [Tooltip("O botão selecionado ao abrir a janela, para o teclado e o controle começarem por ele (na confirmação, o VOLTAR). " +
                 "Vazio = a janela abre sem nada selecionado.")]
        [SerializeField] private GameObject firstSelected;

        [Tooltip("Ligado = a janela reabre no último botão selecionado quando ela fechou, se ele continuar ativo. " +
                 "Desligado = reabre sempre no First Selected.")]
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
