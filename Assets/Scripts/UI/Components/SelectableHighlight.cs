namespace ProjetoVN.UI.Components
{
    public enum SelectableVisualState
    {
        Normal,
        Highlighted,
        Pressed,
        Disabled
    }

    /// <summary>
    /// A regra de destaque de um botão, sem Unity: ponteiro em cima, seleção e botão apertado viram um único estado visual.
    /// </summary>
    public sealed class SelectableHighlight
    {
        public bool IsPointerOver { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsPointerDown { get; private set; }

        public void PointerEntered() => IsPointerOver = true;

        public void PointerExited() => IsPointerOver = false;

        public void PointerPressed() => IsPointerDown = true;

        public void PointerReleased() => IsPointerDown = false;

        public void Selected() => IsSelected = true;

        /// <summary>
        /// Com <paramref name="selectOnHover"/> o destaque é a seleção: perdê-la derruba também o destaque do ponteiro,
        /// senão o botão sob o mouse e o selecionado pelo teclado apareceriam destacados ao mesmo tempo.
        /// </summary>
        /// <returns><c>true</c> se o destaque do ponteiro caiu e o botão do Unity precisa ser avisado.</returns>
        public bool Deselected(bool selectOnHover)
        {
            IsSelected = false;

            bool dropsPointerHighlight = selectOnHover && IsPointerOver;
            if (dropsPointerHighlight) IsPointerOver = false;
            return dropsPointerHighlight;
        }

        public void Reset(bool isSelected)
        {
            IsPointerOver = false;
            IsPointerDown = false;
            IsSelected = isSelected;
        }

        public SelectableVisualState StateFor(bool interactable)
        {
            if (!interactable) return SelectableVisualState.Disabled;
            if (IsPointerDown) return SelectableVisualState.Pressed;
            if (IsPointerOver || IsSelected) return SelectableVisualState.Highlighted;
            return SelectableVisualState.Normal;
        }
    }
}
