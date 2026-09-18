using System;
using NUnit.Framework;
using ProjetoVN.Core.Messaging;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class MessageBrokerTests
    {
        private readonly struct ProbeMessage : IMessage
        {
            public readonly int Value;

            public ProbeMessage(int value)
            {
                Value = value;
            }
        }

        [SetUp]
        public void SetUp() => MessageBroker.Clear();

        [TearDown]
        public void TearDown() => MessageBroker.Clear();

        [Test]
        public void Publish_DeliversMessageToSubscriber()
        {
            int received = 0;
            Action<ProbeMessage> handler = message => received = message.Value;

            MessageBroker.Subscribe(handler);
            MessageBroker.Publish(new ProbeMessage(42));

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNothing()
        {
            Assert.DoesNotThrow(() => MessageBroker.Publish(new ProbeMessage(1)));
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int calls = 0;
            Action<ProbeMessage> handler = _ => calls++;

            MessageBroker.Subscribe(handler);
            MessageBroker.Unsubscribe(handler);
            MessageBroker.Publish(new ProbeMessage(1));

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void DuplicateSubscribe_DeliversOnce_AndOneUnsubscribeRemovesIt()
        {
            int calls = 0;
            Action<ProbeMessage> handler = _ => calls++;

            MessageBroker.Subscribe(handler);
            MessageBroker.Subscribe(handler);
            MessageBroker.Publish(new ProbeMessage(1));

            Assert.AreEqual(1, calls, "assinar duas vezes deve registrar um único handler");

            MessageBroker.Unsubscribe(handler);
            MessageBroker.Publish(new ProbeMessage(2));

            Assert.AreEqual(1, calls, "um único Unsubscribe deve bastar para remover a assinatura");
        }

        [Test]
        public void ThrowingSubscriber_IsLogged_AndOtherSubscribersStillRun()
        {
            bool secondRan = false;
            Action<ProbeMessage> throwing = _ => throw new InvalidOperationException("falha proposital");
            Action<ProbeMessage> healthy = _ => secondRan = true;

            MessageBroker.Subscribe(throwing);
            MessageBroker.Subscribe(healthy);

            LogAssert.Expect(LogType.Exception, "InvalidOperationException: falha proposital");

            Assert.DoesNotThrow(() => MessageBroker.Publish(new ProbeMessage(1)),
                "a exceção de um assinante não pode subir para quem publicou");
            Assert.IsTrue(secondRan, "os demais assinantes devem receber a mensagem mesmo assim");
        }

        [Test]
        public void Clear_RemovesEveryHandler()
        {
            int calls = 0;
            Action<ProbeMessage> handler = _ => calls++;

            MessageBroker.Subscribe(handler);
            MessageBroker.Clear();
            MessageBroker.Publish(new ProbeMessage(1));

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void SubscribingDuringDispatch_DoesNotAffectTheInFlightMessage()
        {
            int lateCalls = 0;
            Action<ProbeMessage> late = _ => lateCalls++;
            Action<ProbeMessage> early = null;
            early = _ => MessageBroker.Subscribe(late);

            MessageBroker.Subscribe(early);
            MessageBroker.Publish(new ProbeMessage(1));

            Assert.AreEqual(0, lateCalls, "quem assina durante o despacho só recebe a próxima mensagem");

            MessageBroker.Publish(new ProbeMessage(2));

            Assert.AreEqual(1, lateCalls);
        }
    }
}
