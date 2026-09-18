using UnityEngine;

namespace ProjetoVN.UI.Framework
{
    public class UIWindowManager : MonoBehaviour
    {
        [SerializeField] private UIWindow startingWindow;

        private UIWindow currentWindow;

        private void Start()
        {
            if (startingWindow != null) OpenWindow(startingWindow);
        }

        public void OpenWindow(UIWindow newWindow)
        {
            if (newWindow == null) return;

            if (newWindow == currentWindow)
            {
                currentWindow.SelectDefault();
                return;
            }

            if (currentWindow != null) currentWindow.Hide();

            currentWindow = newWindow;
            currentWindow.Show();
        }

        public void CloseCurrentWindow()
        {
            if (currentWindow == null) return;

            currentWindow.Hide();
            currentWindow = null;
        }
    }
}
