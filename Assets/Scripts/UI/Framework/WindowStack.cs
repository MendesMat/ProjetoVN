using System.Collections.Generic;

namespace ProjetoVN.UI.Framework
{
    /// <summary>
    /// A regra da pilha de janelas do <see cref="UIWindowManager"/>, sem Unity. Cada operação devolve as janelas que
    /// deixaram de estar visíveis; quem mostra a janela de cima (<see cref="Current"/>) é o chamador.
    /// </summary>
    public sealed class WindowStack<TWindow> where TWindow : class
    {
        private readonly List<TWindow> _windows = new();

        public TWindow Current => _windows.Count > 0 ? _windows[^1] : null;

        /// <summary>
        /// Abre a janela por cima da atual. Se ela já está na pilha, a pilha volta até ela em vez de crescer.
        /// </summary>
        public TWindow[] Open(TWindow window)
        {
            if (window == Current) return System.Array.Empty<TWindow>();

            if (_windows.Contains(window)) return CloseAbove(window);

            TWindow previous = Current;
            _windows.Add(window);
            return previous == null ? System.Array.Empty<TWindow>() : new[] { previous };
        }

        /// <returns>A janela fechada, ou <c>null</c> se a pilha estava vazia.</returns>
        public TWindow Close()
        {
            TWindow closed = Current;
            if (closed != null) _windows.RemoveAt(_windows.Count - 1);

            return closed;
        }

        /// <returns>Todas as janelas, da de cima para a de baixo.</returns>
        public TWindow[] CloseAll()
        {
            var closed = new List<TWindow>(_windows);
            closed.Reverse();
            _windows.Clear();
            return closed.ToArray();
        }

        private TWindow[] CloseAbove(TWindow window)
        {
            var closed = new List<TWindow>();
            while (Current != window) closed.Add(Close());

            return closed.ToArray();
        }
    }
}
