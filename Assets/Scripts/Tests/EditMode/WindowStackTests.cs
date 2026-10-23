using NUnit.Framework;
using ProjetoVN.UI.Framework;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class WindowStackTests
    {
        private const string Title = "title";
        private const string Settings = "settings";
        private const string Confirm = "confirm";

        private WindowStack<string> _stack;

        [SetUp]
        public void SetUp() => _stack = new WindowStack<string>();

        [Test]
        public void Open_FirstWindow_BecomesCurrentAndHidesNothing()
        {
            string[] toHide = _stack.Open(Title);

            Assert.AreEqual(Title, _stack.Current);
            CollectionAssert.IsEmpty(toHide);
        }

        [Test]
        public void Open_AnotherWindow_HidesThePreviousAndKeepsItBelow()
        {
            _stack.Open(Title);

            string[] toHide = _stack.Open(Settings);

            Assert.AreEqual(Settings, _stack.Current);
            CollectionAssert.AreEqual(new[] { Title }, toHide);
            _stack.Close();
            Assert.AreEqual(Title, _stack.Current);
        }

        [Test]
        public void Open_TheCurrentWindow_ChangesNothing()
        {
            _stack.Open(Title);
            _stack.Open(Settings);

            string[] toHide = _stack.Open(Settings);

            Assert.AreEqual(Settings, _stack.Current);
            CollectionAssert.IsEmpty(toHide);
            _stack.Close();
            Assert.AreEqual(Title, _stack.Current);
        }

        [Test]
        public void Open_AWindowAlreadyBelow_ClosesEveryWindowAboveIt()
        {
            _stack.Open(Title);
            _stack.Open(Settings);
            _stack.Open(Confirm);

            string[] toHide = _stack.Open(Title);

            Assert.AreEqual(Title, _stack.Current);
            CollectionAssert.AreEqual(new[] { Confirm, Settings }, toHide);
            _stack.Close();
            Assert.IsNull(_stack.Current, "o título devia ser a única janela que sobrou na pilha");
        }

        [Test]
        public void Close_ReturnsTheTopAndShowsTheOneBelow()
        {
            _stack.Open(Title);
            _stack.Open(Settings);

            string closed = _stack.Close();

            Assert.AreEqual(Settings, closed);
            Assert.AreEqual(Title, _stack.Current);
        }

        [Test]
        public void Close_WhenEmpty_ReturnsNothing()
        {
            string closed = _stack.Close();

            Assert.IsNull(closed);
            Assert.IsNull(_stack.Current);
        }

        [Test]
        public void CloseAll_ReturnsEveryWindowTopFirstAndEmpties()
        {
            _stack.Open(Title);
            _stack.Open(Settings);
            _stack.Open(Confirm);

            string[] closed = _stack.CloseAll();

            CollectionAssert.AreEqual(new[] { Confirm, Settings, Title }, closed);
            Assert.IsNull(_stack.Current);
        }
    }
}
