using NUnit.Framework;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ConversationLogTests
    {
        private static readonly Color LunaColor = new Color32(0xD9, 0xA6, 0xFF, 0xFF);
        private static readonly Color ProtagonistColor = new Color32(0xFF, 0xF3, 0xFB, 0xFF);

        private static readonly Color ThoughtColor = new Color32(0xC9, 0xDC, 0xFF, 0xFF);

        private ConversationLog _log;

        [SetUp]
        public void SetUp() => _log = new ConversationLog();

        [Test]
        public void NewLog_IsEmpty()
        {
            Assert.AreEqual(0, _log.Entries.Count);
        }

        [Test]
        public void AddSpokenLine_KeepsTheSpeakerColorAndText()
        {
            _log.AddSpokenLine("Luna", LunaColor, "Oi.");

            ConversationEntry entry = _log.Entries[0];
            Assert.AreEqual(ConversationEntryKind.Speech, entry.Kind);
            Assert.AreEqual("Luna", entry.SpeakerName);
            Assert.AreEqual(LunaColor, entry.SpeakerColor);
            Assert.AreEqual("Oi.", entry.Text);
            Assert.IsFalse(entry.IsProtagonist);
        }

        [Test]
        public void AddSpokenLine_StoresTheColorOpaque()
        {
            _log.AddSpokenLine("Luna", new Color32(0xD9, 0xA6, 0xFF, 0x40), "Oi.");

            Assert.AreEqual(LunaColor, _log.Entries[0].SpeakerColor);
        }

        [Test]
        public void AddProtagonistLine_IsMarkedAsTheProtagonists()
        {
            _log.AddProtagonistLine("Protagonista", ProtagonistColor, "Sim");

            ConversationEntry entry = _log.Entries[0];
            Assert.AreEqual(ConversationEntryKind.Speech, entry.Kind);
            Assert.IsTrue(entry.IsProtagonist);
            Assert.AreEqual("Sim", entry.Text);
        }

        [Test]
        public void AddNarration_HasOnlyText()
        {
            _log.AddNarration("A porta rangeu.");

            ConversationEntry entry = _log.Entries[0];
            Assert.AreEqual(ConversationEntryKind.Narration, entry.Kind);
            Assert.AreEqual("A porta rangeu.", entry.Text);
            Assert.IsNull(entry.SpeakerName);
            Assert.IsFalse(entry.ShowsName);
        }

        [Test]
        public void AddItemReceived_SaysYouReceivedTheItem()
        {
            _log.AddItemReceived("Chave");

            Assert.AreEqual(ConversationEntryKind.Item, _log.Entries[0].Kind);
            Assert.AreEqual("Você recebeu: Chave", _log.Entries[0].Text);
        }

        [Test]
        public void AddItemHandedOver_SaysYouHandedOverTheItem()
        {
            _log.AddItemHandedOver("Chave");

            Assert.AreEqual(ConversationEntryKind.Item, _log.Entries[0].Kind);
            Assert.AreEqual("Você entregou: Chave", _log.Entries[0].Text);
        }

        [Test]
        public void FirstLineOfASpeaker_ShowsTheName()
        {
            _log.AddSpokenLine("Luna", LunaColor, "Oi.");

            Assert.IsTrue(_log.Entries[0].ShowsName);
        }

        [Test]
        public void NextLineOfTheSameSpeaker_HidesTheName()
        {
            _log.AddSpokenLine("Luna", LunaColor, "Oi.");
            _log.AddSpokenLine("Luna", LunaColor, "Tudo bem?");

            Assert.IsFalse(_log.Entries[1].ShowsName);
        }

        [Test]
        public void LineOfAnotherSpeaker_ShowsTheName()
        {
            _log.AddSpokenLine("Luna", LunaColor, "Oi.");
            _log.AddProtagonistLine("Protagonista", ProtagonistColor, "Oi!");
            _log.AddSpokenLine("Luna", LunaColor, "Tudo bem?");

            Assert.IsTrue(_log.Entries[0].ShowsName);
            Assert.IsTrue(_log.Entries[1].ShowsName);
            Assert.IsTrue(_log.Entries[2].ShowsName);
        }

        [Test]
        public void ProtagonistAndCharacter_WithTheSameName_BothShowTheName()
        {
            _log.AddProtagonistLine("Luna", ProtagonistColor, "Oi.");
            _log.AddSpokenLine("Luna", LunaColor, "Oi, eu também.");

            Assert.IsTrue(_log.Entries[0].ShowsName);
            Assert.IsTrue(_log.Entries[1].ShowsName);
        }

        [Test]
        public void NarrationBetweenLinesOfTheSameSpeaker_ShowsTheNameAgain()
        {
            _log.AddSpokenLine("Luna", LunaColor, "Oi.");
            _log.AddNarration("Ela sorriu.");
            _log.AddSpokenLine("Luna", LunaColor, "Tudo bem?");

            Assert.IsTrue(_log.Entries[2].ShowsName);
        }

        [Test]
        public void ItemBetweenLinesOfTheSameSpeaker_ShowsTheNameAgain()
        {
            _log.AddSpokenLine("Luna", LunaColor, "Toma.");
            _log.AddItemReceived("Chave");
            _log.AddSpokenLine("Luna", LunaColor, "Cuide bem dela.");

            Assert.IsTrue(_log.Entries[2].ShowsName);
        }

        [Test]
        public void ChosenOptionAfterAProtagonistLine_HidesTheName()
        {
            _log.AddProtagonistLine("Protagonista", ProtagonistColor, "Oi.");
            _log.AddProtagonistLine("Protagonista", ProtagonistColor, "Sim");

            Assert.IsFalse(_log.Entries[1].ShowsName);
        }

        [Test]
        public void AddThought_HasTextAndColorButNoName()
        {
            _log.AddThought("Que dia.", ThoughtColor);

            ConversationEntry entry = _log.Entries[0];
            Assert.AreEqual(ConversationEntryKind.Thought, entry.Kind);
            Assert.IsNull(entry.SpeakerName);
            Assert.IsFalse(entry.ShowsName);
            Assert.IsFalse(entry.IsProtagonist);
            Assert.AreEqual("Que dia.", entry.Text);
            Assert.AreEqual(ThoughtColor, entry.SpeakerColor);
        }

        [Test]
        public void AddThought_StoresTheColorOpaque()
        {
            _log.AddThought("Que dia.", new Color32(0xC9, 0xDC, 0xFF, 0x40));

            Assert.AreEqual(ThoughtColor, _log.Entries[0].SpeakerColor);
        }

        [Test]
        public void ProtagonistLineAfterAThought_ShowsTheName()
        {
            _log.AddProtagonistLine("Protagonista", ProtagonistColor, "Oi.");
            _log.AddThought("Que dia.", ThoughtColor);
            _log.AddProtagonistLine("Protagonista", ProtagonistColor, "Tudo bem?");

            Assert.IsTrue(_log.Entries[2].ShowsName);
        }

        [Test]
        public void ChosenOptionAfterAThought_ShowsTheName()
        {
            _log.AddThought("Que dia.", ThoughtColor);
            _log.AddProtagonistLine("Protagonista", ProtagonistColor, "Sim");

            Assert.IsTrue(_log.Entries[1].ShowsName);
        }

        [Test]
        public void ThoughtNeverShowsAName_EvenAfterAnotherThought()
        {
            _log.AddThought("Que dia.", ThoughtColor);
            _log.AddThought("Que calor.", ThoughtColor);

            Assert.IsFalse(_log.Entries[0].ShowsName);
            Assert.IsFalse(_log.Entries[1].ShowsName);
        }

        [Test]
        public void Entries_KeepTheOrderInWhichTheyHappened()
        {
            _log.AddSpokenLine("Luna", LunaColor, "Toma.");
            _log.AddNarration("Ela sorriu.");
            _log.AddProtagonistLine("Protagonista", ProtagonistColor, "Obrigado.");
            _log.AddItemReceived("Chave");

            Assert.AreEqual(ConversationEntryKind.Speech, _log.Entries[0].Kind);
            Assert.AreEqual("Toma.", _log.Entries[0].Text);
            Assert.AreEqual(ConversationEntryKind.Narration, _log.Entries[1].Kind);
            Assert.AreEqual("Ela sorriu.", _log.Entries[1].Text);
            Assert.AreEqual(ConversationEntryKind.Speech, _log.Entries[2].Kind);
            Assert.AreEqual("Obrigado.", _log.Entries[2].Text);
            Assert.AreEqual(ConversationEntryKind.Item, _log.Entries[3].Kind);
            Assert.AreEqual("Você recebeu: Chave", _log.Entries[3].Text);
        }

        [Test]
        public void Clear_EmptiesTheLogAndTheNextLineShowsTheName()
        {
            _log.AddSpokenLine("Luna", LunaColor, "Primeira conversa.");

            _log.Clear();
            _log.AddSpokenLine("Luna", LunaColor, "Segunda conversa.");

            Assert.AreEqual(1, _log.Entries.Count);
            Assert.AreEqual("Segunda conversa.", _log.Entries[0].Text);
            Assert.IsTrue(_log.Entries[0].ShowsName);
        }

        [Test]
        public void TwoHundredLines_AreAllKeptInOrder()
        {
            for (int i = 1; i <= 200; i++) _log.AddNarration($"Fala {i}");

            Assert.AreEqual(200, _log.Entries.Count);
            Assert.AreEqual("Fala 1", _log.Entries[0].Text);
            Assert.AreEqual("Fala 200", _log.Entries[199].Text);
        }
    }
}
