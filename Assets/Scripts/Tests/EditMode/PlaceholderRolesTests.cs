using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.GameFlow.DevTools;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class PlaceholderRolesTests
    {
        [Test]
        public void Pick_OnlyConversation_IsConversation()
        {
            PlaceholderRole role = PlaceholderRoles.Pick(
                hasLockedAction: false, hasExit: false, isCollectable: false, startsConversation: true);

            Assert.AreEqual(PlaceholderRole.Conversation, role);
        }

        [Test]
        public void Pick_Nothing_IsObject()
        {
            PlaceholderRole role = PlaceholderRoles.Pick(
                hasLockedAction: false, hasExit: false, isCollectable: false, startsConversation: false);

            Assert.AreEqual(PlaceholderRole.Object, role);
        }

        [Test]
        public void Pick_ExitWithEmptyTrigger_IsExit()
        {
            PlaceholderRole role = PlaceholderRoles.Pick(
                hasLockedAction: false, hasExit: true, isCollectable: false, startsConversation: false);

            Assert.AreEqual(PlaceholderRole.Exit, role);
        }

        [Test]
        public void Pick_CollectableWithConversation_IsItem()
        {
            PlaceholderRole role = PlaceholderRoles.Pick(
                hasLockedAction: false, hasExit: false, isCollectable: true, startsConversation: true);

            Assert.AreEqual(PlaceholderRole.Item, role);
        }

        [Test]
        public void Pick_LockedDoorThatLeads_IsLockedDoor()
        {
            PlaceholderRole role = PlaceholderRoles.Pick(
                hasLockedAction: true, hasExit: true, isCollectable: false, startsConversation: false);

            Assert.AreEqual(PlaceholderRole.LockedDoor, role);
        }

        [Test]
        public void Pick_LockedDoorWithEverything_IsLockedDoor()
        {
            PlaceholderRole role = PlaceholderRoles.Pick(
                hasLockedAction: true, hasExit: true, isCollectable: true, startsConversation: true);

            Assert.AreEqual(PlaceholderRole.LockedDoor, role);
        }

        [Test]
        public void Pick_ExitAndCollectable_IsExit()
        {
            PlaceholderRole role = PlaceholderRoles.Pick(
                hasLockedAction: false, hasExit: true, isCollectable: true, startsConversation: true);

            Assert.AreEqual(PlaceholderRole.Exit, role);
        }

        [TestCase(PlaceholderRole.LockedDoor, "PORTA TRANCADA")]
        [TestCase(PlaceholderRole.Exit, "SAÍDA")]
        [TestCase(PlaceholderRole.Item, "ITEM")]
        [TestCase(PlaceholderRole.Conversation, "CONVERSA")]
        [TestCase(PlaceholderRole.Object, "OBJETO")]
        public void TitleOf_EveryRole_HasItsPortugueseTitle(PlaceholderRole role, string expectedTitle)
        {
            Assert.AreEqual(expectedTitle, PlaceholderRoles.TitleOf(role));
        }

        [Test]
        public void ColorOf_EveryRole_IsDistinct()
        {
            Color[] colors = AllRoles().Select(PlaceholderRoles.ColorOf).ToArray();

            CollectionAssert.AllItemsAreUnique(colors);
        }

        [Test]
        public void ColorOf_IsNeverTheOpenDoorGreen()
        {
            IEnumerable<PlaceholderRole> greenRoles = AllRoles()
                .Where(role => PlaceholderRoles.ColorOf(role) == Color.green);

            CollectionAssert.IsEmpty(greenRoles, "o verde é o do PlaceholderTint e quer dizer 'porta aberta'");
        }

        [Test]
        public void LabelText_PutsTheTitleAboveTheTrimmedName()
        {
            string text = PlaceholderRoles.LabelText(PlaceholderRole.Exit, "  Loja ");

            Assert.AreEqual("<size=75%>SAÍDA</size>\nLoja", text);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void LabelText_BlankName_ShowsTheMissingNameMarker(string blankName)
        {
            string text = PlaceholderRoles.LabelText(PlaceholderRole.Conversation, blankName);

            Assert.AreEqual("<size=75%>CONVERSA</size>\n(sem nome)", text);
        }

        [TestCase(null, true)]
        [TestCase("", true)]
        [TestCase(" \t", true)]
        [TestCase("Motorista", false)]
        public void IsNameMissing_ChecksForAnyVisibleCharacter(string name, bool expected)
        {
            Assert.AreEqual(expected, PlaceholderRoles.IsNameMissing(name));
        }

        private static IEnumerable<PlaceholderRole> AllRoles() =>
            Enum.GetValues(typeof(PlaceholderRole)).Cast<PlaceholderRole>();
    }
}
