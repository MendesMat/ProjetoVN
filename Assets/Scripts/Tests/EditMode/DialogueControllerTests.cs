using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjetoVN.Core.Messaging;
using ProjetoVN.Dialogue.Data;
using ProjetoVN.Dialogue.Logic;
using ProjetoVN.Dialogue.Messaging;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class DialogueControllerTests
    {
        private DialogueController _controller;
        private List<DialogueLineMessage> _lines;
        private List<DialogueChoicesMessage> _choices;
        private int _startedCount;
        private int _endedCount;

        #region Setup

        [SetUp]
        public void SetUp()
        {
            MessageBroker.Clear();

            _controller = new DialogueController();
            _lines = new List<DialogueLineMessage>();
            _choices = new List<DialogueChoicesMessage>();
            _startedCount = 0;
            _endedCount = 0;

            MessageBroker.Subscribe<DialogueLineMessage>(message => _lines.Add(message));
            MessageBroker.Subscribe<DialogueChoicesMessage>(message => _choices.Add(message));
            MessageBroker.Subscribe<DialogueStartedMessage>(_ => _startedCount++);
            MessageBroker.Subscribe<DialogueEndedMessage>(_ => _endedCount++);
        }

        [TearDown]
        public void TearDown() => MessageBroker.Clear();

        private static DialogueData Dialogue(string assetName, params string[] texts)
        {
            var data = ScriptableObject.CreateInstance<DialogueData>();
            data.name = assetName;

            foreach (string text in texts)
                data.DialogueNodes.Add(new DialogueNode { SpeakerName = assetName, Text = text });

            return data;
        }

        private static DialogueData EmptyDialogue(string assetName)
        {
            var data = ScriptableObject.CreateInstance<DialogueData>();
            data.name = assetName;
            return data;
        }

        #endregion

        #region Dados inválidos

        [Test]
        public void StartDialogue_WithNull_ReturnsFalse_AndPublishesNothing()
        {
            LogAssert.Expect(LogType.Warning, new Regex("DialogueData nulo"));

            Assert.IsFalse(_controller.StartDialogue(null));
            Assert.IsFalse(_controller.IsActive);
            Assert.AreEqual(0, _startedCount);
            Assert.AreEqual(0, _endedCount, "sem Started não pode haver Ended, senão o GameFlow destrava sozinho");
        }

        [Test]
        public void StartDialogue_WithNoNodes_ReturnsFalse_AndLeavesGameplayUntouched()
        {
            LogAssert.Expect(LogType.Warning, new Regex("não tem nós"));

            Assert.IsFalse(_controller.StartDialogue(EmptyDialogue("Vazio")));
            Assert.IsFalse(_controller.IsActive);
            Assert.AreEqual(0, _startedCount);
            Assert.AreEqual(0, _endedCount);
            Assert.IsEmpty(_lines);
        }

        #endregion

        #region Avanço sequencial

        [Test]
        public void StartDialogue_PublishesStartedBeforeTheFirstLine()
        {
            var order = new List<string>();
            MessageBroker.Clear();
            MessageBroker.Subscribe<DialogueStartedMessage>(_ => order.Add("started"));
            MessageBroker.Subscribe<DialogueLineMessage>(_ => order.Add("line"));

            _controller.StartDialogue(Dialogue("Cena", "Olá"));

            Assert.AreEqual(new[] { "started", "line" }, order.ToArray(),
                "o modo precisa trocar antes de qualquer conteúdo aparecer");
        }

        [Test]
        public void NextNode_AdvancesThroughEveryNode_ThenEndsOnce()
        {
            Assert.IsTrue(_controller.StartDialogue(Dialogue("Cena", "Um", "Dois", "Três")));

            Assert.AreEqual(1, _lines.Count);
            Assert.AreEqual("Um", _lines[0].Text);

            _controller.NextNode();
            Assert.AreEqual("Dois", _lines[1].Text);

            _controller.NextNode();
            Assert.AreEqual("Três", _lines[2].Text);
            Assert.AreEqual(0, _endedCount, "ainda há um nó sendo exibido");

            _controller.NextNode();
            Assert.AreEqual(1, _endedCount);
            Assert.IsFalse(_controller.IsActive);
            Assert.AreEqual(3, _lines.Count, "o fim não deve publicar uma quarta linha");
        }

        [Test]
        public void NextNode_AfterTheDialogueEnded_DoesNothing()
        {
            _controller.StartDialogue(Dialogue("Cena", "Única"));
            _controller.NextNode();

            _controller.NextNode();

            Assert.AreEqual(1, _endedCount, "avançar um diálogo encerrado não pode republicar Ended");
            Assert.AreEqual(1, _lines.Count);
        }

        #endregion

        #region Encadeamento

        [Test]
        public void Chaining_PublishesStartedOnce_AndEndedOnce()
        {
            DialogueData second = Dialogue("Segundo", "B1");
            DialogueData first = Dialogue("Primeiro", "A1");
            first.NextDialogueData = second;

            _controller.StartDialogue(first);
            _controller.NextNode();

            Assert.AreEqual("B1", _lines[1].Text, "o encadeamento deve seguir para o próximo diálogo");
            Assert.AreEqual(1, _startedCount, "encadear não é um novo diálogo para o GameFlow");
            Assert.AreEqual(0, _endedCount);

            _controller.NextNode();

            Assert.AreEqual(1, _startedCount);
            Assert.AreEqual(1, _endedCount);
        }

        [Test]
        public void Chaining_ToInvalidData_EndsTheDialogue_InsteadOfLocking()
        {
            DialogueData first = Dialogue("Primeiro", "A1");
            first.NextDialogueData = EmptyDialogue("VazioEncadeado");

            LogAssert.Expect(LogType.Warning, new Regex("não tem nós"));

            _controller.StartDialogue(first);
            _controller.NextNode();

            Assert.AreEqual(1, _endedCount, "dados inválidos no meio do caminho não podem travar o jogador");
            Assert.IsFalse(_controller.IsActive);
        }

        #endregion

        #region Escolhas

        [Test]
        public void SelectChoice_WithTargetDialogue_JumpsToIt()
        {
            DialogueData target = Dialogue("Alvo", "Resposta");
            DialogueData root = Dialogue("Raiz", "Pergunta");
            root.DialogueNodes[0].Choices.Add(new DialogueChoice { Text = "Sim", TargetDialogue = target });

            _controller.StartDialogue(root);

            Assert.AreEqual(1, _choices.Count, "um nó com escolhas deve publicar DialogueChoicesMessage");

            _controller.SelectChoice(0);

            Assert.AreEqual("Resposta", _lines[1].Text);
            Assert.AreEqual(1, _startedCount, "saltar para outro diálogo não republica Started");
            Assert.AreEqual(0, _endedCount);
        }

        [Test]
        public void SelectChoice_WithoutTarget_ContinuesInTheSameDialogue()
        {
            DialogueData root = Dialogue("Raiz", "Pergunta", "Continuação");
            root.DialogueNodes[0].Choices.Add(new DialogueChoice { Text = "Seguir", TargetDialogue = null });

            _controller.StartDialogue(root);
            _controller.SelectChoice(0);

            Assert.AreEqual("Continuação", _lines[1].Text);
            Assert.AreEqual(0, _endedCount);
        }

        [Test]
        public void SelectChoice_WithInvalidIndex_IsIgnored()
        {
            DialogueData root = Dialogue("Raiz", "Pergunta");
            root.DialogueNodes[0].Choices.Add(new DialogueChoice { Text = "Só esta" });

            _controller.StartDialogue(root);
            _controller.SelectChoice(7);

            Assert.IsTrue(_controller.IsActive);
            Assert.AreEqual(1, _lines.Count);
            Assert.AreEqual(0, _endedCount);
        }

        [Test]
        public void NextNode_WhileWaitingForAChoice_IsIgnored()
        {
            DialogueData root = Dialogue("Raiz", "Pergunta", "NaoDeveAparecer");
            root.DialogueNodes[0].Choices.Add(new DialogueChoice { Text = "Escolha" });

            _controller.StartDialogue(root);
            _controller.NextNode();

            Assert.AreEqual(1, _lines.Count, "clicar para avançar não pode pular uma escolha pendente");
        }

        #endregion
    }
}
