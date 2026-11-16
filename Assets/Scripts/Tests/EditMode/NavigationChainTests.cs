using NUnit.Framework;
using ProjetoVN.UI.Components;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class NavigationChainTests
    {
        private const bool WrapAround = true;
        private const bool Available = true;
        private const bool Unavailable = false;

        [Test]
        public void Below_AllAvailable_IsTheNextItem()
        {
            bool[] column = { Available, Available, Available };

            Assert.AreEqual(1, NavigationChain.Below(column, 0, !WrapAround));
        }

        [Test]
        public void Above_AllAvailable_IsThePreviousItem()
        {
            bool[] column = { Available, Available, Available };

            Assert.AreEqual(0, NavigationChain.Above(column, 1, !WrapAround));
        }

        [Test]
        public void Below_SkipsAnUnavailableItem()
        {
            bool[] column = { Available, Unavailable, Available };

            Assert.AreEqual(2, NavigationChain.Below(column, 0, !WrapAround));
        }

        [Test]
        public void Above_SkipsAnUnavailableItem()
        {
            bool[] column = { Available, Unavailable, Available };

            Assert.AreEqual(0, NavigationChain.Above(column, 2, !WrapAround));
        }

        [Test]
        public void Below_FromTheLast_WithoutWrapAround_IsNone()
        {
            bool[] column = { Available, Available, Available };

            Assert.AreEqual(NavigationChain.None, NavigationChain.Below(column, 2, !WrapAround));
        }

        [Test]
        public void Above_FromTheFirst_WithoutWrapAround_IsNone()
        {
            bool[] column = { Available, Available, Available };

            Assert.AreEqual(NavigationChain.None, NavigationChain.Above(column, 0, !WrapAround));
        }

        [Test]
        public void Below_FromTheLast_WithWrapAround_IsTheFirst()
        {
            bool[] column = { Available, Available, Available };

            Assert.AreEqual(0, NavigationChain.Below(column, 2, WrapAround));
        }

        [Test]
        public void Above_FromTheFirst_WithWrapAround_IsTheLast()
        {
            bool[] column = { Available, Available, Available };

            Assert.AreEqual(2, NavigationChain.Above(column, 0, WrapAround));
        }

        [Test]
        public void Below_WithWrapAround_SkipsAnUnavailableItemAcrossTheEdge()
        {
            bool[] column = { Unavailable, Available, Available };

            Assert.AreEqual(1, NavigationChain.Below(column, 2, WrapAround));
        }

        [Test]
        public void Above_WithWrapAround_SkipsAnUnavailableItemAcrossTheEdge()
        {
            bool[] column = { Available, Available, Unavailable };

            Assert.AreEqual(1, NavigationChain.Above(column, 0, WrapAround));
        }

        [Test]
        public void Below_WithoutWrapAround_WithOnlyUnavailableItemsBelow_IsNone()
        {
            bool[] column = { Available, Available, Unavailable };

            Assert.AreEqual(NavigationChain.None, NavigationChain.Below(column, 1, !WrapAround));
        }

        [Test]
        public void WithWrapAround_WithNoOtherAvailableItem_IsNoneInBothDirections()
        {
            bool[] column = { Available, Unavailable, Unavailable };

            Assert.AreEqual(NavigationChain.None, NavigationChain.Below(column, 0, WrapAround));
            Assert.AreEqual(NavigationChain.None, NavigationChain.Above(column, 0, WrapAround));
        }

        [Test]
        public void FromAnUnavailableItem_FindsTheAvailableNeighbours()
        {
            bool[] column = { Available, Unavailable, Available };

            Assert.AreEqual(2, NavigationChain.Below(column, 1, !WrapAround));
            Assert.AreEqual(0, NavigationChain.Above(column, 1, !WrapAround));
        }
    }
}
