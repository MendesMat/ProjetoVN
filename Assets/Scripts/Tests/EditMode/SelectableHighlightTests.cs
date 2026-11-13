using NUnit.Framework;
using ProjetoVN.UI.Components;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class SelectableHighlightTests
    {
        private const bool Interactable = true;
        private const bool SelectOnHover = true;

        private SelectableHighlight _highlight;

        [SetUp]
        public void SetUp() => _highlight = new SelectableHighlight();

        [Test]
        public void StateFor_NothingHappened_IsNormal()
        {
            Assert.AreEqual(SelectableVisualState.Normal, _highlight.StateFor(Interactable));
        }

        [Test]
        public void StateFor_NotInteractable_IsDisabledEvenWhenHoveredSelectedAndPressed()
        {
            _highlight.PointerEntered();
            _highlight.Selected();
            _highlight.PointerPressed();

            Assert.AreEqual(SelectableVisualState.Disabled, _highlight.StateFor(!Interactable));
        }

        [Test]
        public void StateFor_PointerOverOnly_IsHighlighted()
        {
            _highlight.PointerEntered();

            Assert.AreEqual(SelectableVisualState.Highlighted, _highlight.StateFor(Interactable));
        }

        [Test]
        public void StateFor_SelectedOnly_IsHighlighted()
        {
            _highlight.Selected();

            Assert.AreEqual(SelectableVisualState.Highlighted, _highlight.StateFor(Interactable));
        }

        [Test]
        public void StateFor_PressedWithPointerOver_IsPressed()
        {
            _highlight.PointerEntered();
            _highlight.PointerPressed();

            Assert.AreEqual(SelectableVisualState.Pressed, _highlight.StateFor(Interactable));
        }

        [Test]
        public void PointerReleased_WithPointerStillOver_ReturnsToHighlighted()
        {
            _highlight.PointerEntered();
            _highlight.PointerPressed();

            _highlight.PointerReleased();

            Assert.AreEqual(SelectableVisualState.Highlighted, _highlight.StateFor(Interactable));
        }

        [Test]
        public void PointerExited_WhileSelected_KeepsTheHighlight()
        {
            _highlight.PointerEntered();
            _highlight.Selected();

            _highlight.PointerExited();

            Assert.AreEqual(SelectableVisualState.Highlighted, _highlight.StateFor(Interactable));
        }

        [Test]
        public void Deselected_WithSelectOnHoverAndPointerOver_DropsThePointerHighlight()
        {
            _highlight.PointerEntered();
            _highlight.Selected();

            bool droppedPointerHighlight = _highlight.Deselected(SelectOnHover);

            Assert.IsTrue(droppedPointerHighlight);
            Assert.AreEqual(SelectableVisualState.Normal, _highlight.StateFor(Interactable));
        }

        [Test]
        public void Deselected_WithSelectOnHoverAndPointerAway_DropsNothingElse()
        {
            _highlight.Selected();

            bool droppedPointerHighlight = _highlight.Deselected(SelectOnHover);

            Assert.IsFalse(droppedPointerHighlight);
            Assert.AreEqual(SelectableVisualState.Normal, _highlight.StateFor(Interactable));
        }

        [Test]
        public void Deselected_WithoutSelectOnHover_KeepsThePointerHighlight()
        {
            _highlight.PointerEntered();
            _highlight.Selected();

            bool droppedPointerHighlight = _highlight.Deselected(!SelectOnHover);

            Assert.IsFalse(droppedPointerHighlight);
            Assert.AreEqual(SelectableVisualState.Highlighted, _highlight.StateFor(Interactable));
        }

        [Test]
        public void Reset_ToSelected_ClearsPointerAndPressAndKeepsTheSelection()
        {
            _highlight.PointerEntered();
            _highlight.PointerPressed();

            _highlight.Reset(isSelected: true);

            Assert.IsFalse(_highlight.IsPointerOver);
            Assert.IsFalse(_highlight.IsPointerDown);
            Assert.AreEqual(SelectableVisualState.Highlighted, _highlight.StateFor(Interactable));
        }

        [Test]
        public void Reset_ToNotSelected_IsNormal()
        {
            _highlight.PointerEntered();
            _highlight.Selected();
            _highlight.PointerPressed();

            _highlight.Reset(isSelected: false);

            Assert.AreEqual(SelectableVisualState.Normal, _highlight.StateFor(Interactable));
        }
    }
}
