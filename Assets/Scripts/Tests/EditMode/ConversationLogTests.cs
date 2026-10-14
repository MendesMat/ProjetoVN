using NUnit.Framework;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ConversationLogTests
    {
        private static readonly Color LunaColor = new Color32(0xD9, 0xA6, 0xFF, 0xFF);
        private static readonly Color ProtagonistColor = new Color32(0xFF, 0xF3, 0xFB, 0xFF);

        private ConversationLog _log;

        [SetUp]
        public void SetUp() => _log = new ConversationLog();

        [Test]
        public void NewLog_IsEmpty()
        {
            Assert.AreEqual(0, _log.Entries.Count);
        }

        [Test]
        public void AddSpokenLine_ShowsTheNameInItsColorThenTheText()
        {
            _log.AddSpokenLine("Luna", LunaColor, "Oi.");

            Assert.AreEqual("<color=#D9A6FF>Luna</color>: Oi.", _log.Entries[0]);
        }

        [Test]
        public void AddSpokenLine_IgnoresTheAlphaOfTheColor()
        {
            _log.AddSpokenLine("Luna", new Color32(0xD9, 0xA6, 0xFF, 0x40), "Oi.");

            Assert.AreEqual("<color=#D9A6FF>Luna</color>: Oi.", _log.Entries[0]);
        }

        [Test]
        public void AddNarration_ShowsOnlyTheText()
        {
            _log.AddNarration("A porta rangeu.");

            Assert.AreEqual("A porta rangeu.", _log.Entries[0]);
        }

        [Test]
        public void AddItemReceived_SaysYouReceivedTheItem()
        {
            _log.AddItemReceived("Chave");

            Assert.AreEqual("Você recebeu: Chave", _log.Entries[0]);
        }

        [Test]
        public void AddItemHandedOver_SaysYouHandedOverTheItem()
        {
            _log.AddItemHandedOver("Chave");

            Assert.AreEqual("Você entregou: Chave", _log.Entries[0]);
        }

        [Test]
        public void Entries_KeepTheOrderInWhichTheyHappened()
        {
            _log.AddSpokenLine("Luna", LunaColor, "Toma.");
            _log.AddNarration("Ela sorriu.");
            _log.AddSpokenLine("Protagonista", ProtagonistColor, "Obrigado.");
            _log.AddItemReceived("Chave");

            CollectionAssert.AreEqual(new[]
            {
                "<color=#D9A6FF>Luna</color>: Toma.",
                "Ela sorriu.",
                "<color=#FFF3FB>Protagonista</color>: Obrigado.",
                "Você recebeu: Chave",
            }, _log.Entries);
        }

        [Test]
        public void Clear_EmptiesTheLogAndItAcceptsNewEntries()
        {
            _log.AddNarration("Primeira conversa.");

            _log.Clear();
            _log.AddNarration("Segunda conversa.");

            CollectionAssert.AreEqual(new[] { "Segunda conversa." }, _log.Entries);
        }

        [Test]
        public void TwoHundredLines_AreAllKeptInOrder()
        {
            for (int i = 1; i <= 200; i++) _log.AddNarration($"Fala {i}");

            Assert.AreEqual(200, _log.Entries.Count);
            Assert.AreEqual("Fala 1", _log.Entries[0]);
            Assert.AreEqual("Fala 200", _log.Entries[199]);
        }
    }
}
