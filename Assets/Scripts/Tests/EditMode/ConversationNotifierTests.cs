using NUnit.Framework;
using ProjetoVN.Core.Messaging;
using ProjetoVN.Dialogue.Logic;
using ProjetoVN.Dialogue.Messaging;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ConversationNotifierTests
    {
        private ConversationNotifier _conversation;
        private int _started;
        private int _ended;

        [SetUp]
        public void SetUp()
        {
            MessageBroker.Clear();
            _conversation = new ConversationNotifier();
            _started = 0;
            _ended = 0;
            MessageBroker.Subscribe<DialogueStartedMessage>(_ => _started++);
            MessageBroker.Subscribe<DialogueEndedMessage>(_ => _ended++);
        }

        [TearDown]
        public void TearDown() => MessageBroker.Clear();

        [Test]
        public void Begin_PublishesStartedOnce_AndOpens()
        {
            _conversation.Begin();

            Assert.AreEqual(1, _started);
            Assert.AreEqual(0, _ended);
            Assert.IsTrue(_conversation.IsOpen);
        }

        [Test]
        public void Begin_WhileOpen_DoesNotPublishAgain()
        {
            _conversation.Begin();
            _conversation.Begin();

            Assert.AreEqual(1, _started, "uma conversa é um Started só, por mais nós que ela atravesse");
        }

        [Test]
        public void End_PublishesEndedOnce_AndCloses()
        {
            _conversation.Begin();

            _conversation.End();
            _conversation.End();

            Assert.AreEqual(1, _ended);
            Assert.IsFalse(_conversation.IsOpen);
        }

        [Test]
        public void End_WithoutBegin_PublishesNothing()
        {
            _conversation.End();

            Assert.AreEqual(0, _ended, "sem Started não pode haver Ended: o GameFlow sairia de um modo em que nunca entrou");
            Assert.IsFalse(_conversation.IsOpen);
        }

        [Test]
        public void Begin_AfterEnd_StartsANewConversation()
        {
            _conversation.Begin();
            _conversation.End();

            _conversation.Begin();

            Assert.AreEqual(2, _started);
            Assert.IsTrue(_conversation.IsOpen);
        }
    }
}
