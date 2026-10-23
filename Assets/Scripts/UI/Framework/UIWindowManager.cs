using UnityEngine;

namespace ProjetoVN.UI.Framework
{
    public class UIWindowManager : MonoBehaviour
    {
        [SerializeField] private UIWindow startingWindow;

        private readonly WindowStack<UIWindow> _windows = new();

        private void Start()
        {
            if (startingWindow != null) OpenWindow(startingWindow);
        }

        public void OpenWindow(UIWindow newWindow)
        {
            if (newWindow == null) return;

            if (newWindow == _windows.Current)
            {
                newWindow.SelectDefault();
                return;
            }

            HideAll(_windows.Open(newWindow));
            newWindow.Show();
        }

        public void CloseCurrentWindow()
        {
            UIWindow closed = _windows.Close();
            if (closed == null) return;

            closed.Hide();
            ShowCurrent();
        }

        public void CloseAllWindows() => HideAll(_windows.CloseAll());

        private void ShowCurrent()
        {
            if (_windows.Current != null) _windows.Current.Show();
        }

        private static void HideAll(UIWindow[] windows)
        {
            foreach (UIWindow window in windows) window.Hide();
        }
    }
}
