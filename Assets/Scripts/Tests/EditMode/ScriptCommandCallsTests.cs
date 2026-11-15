using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjetoVN.Core.State;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ScriptCommandCallsTests
    {
        private const string Node = "no_teste";

        [SetUp]
        public void SetUp() => StoryState.ClearAll();

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        private static void OneParameter(string first) { }

        private static ScriptCommandCalls CallsIn(string source)
        {
            ScriptRun run = ScriptRun.FromText(source);
            Assert.IsNotNull(run.Program, "o roteiro do teste precisa compilar");
            return ScriptCommandCalls.In(run.Program);
        }

        private static ScriptCommandCalls CallsInNodeBody(string body) =>
            CallsIn($"title: {Node}\n---\n{body}\n===\n");

        private static void OneNumber(float first) { }

        private static ScriptCommandSignature OneParameterSignature(string commandName) =>
            SignatureOf(commandName, nameof(OneParameter));

        private static ScriptCommandSignature OneNumberSignature(string commandName) =>
            SignatureOf(commandName, nameof(OneNumber));

        private static ScriptCommandSignature SignatureOf(string commandName, string methodName)
        {
            MethodInfo method = typeof(ScriptCommandCallsTests).GetMethod(
                methodName, BindingFlags.Static | BindingFlags.NonPublic);
            return ScriptCommandSignature.From(commandName, method);
        }

        [Test]
        public void Command_IsReadWithItsNodeNameAndWordCount()
        {
            ScriptCommandCall call = CallsInNodeBody("<<dar_item chave>>\nfim").All.Single();

            Assert.AreEqual(Node, call.NodeName);
            Assert.AreEqual("dar_item", call.Name);
            Assert.AreEqual(1, call.WordCount);
            Assert.AreEqual("dar_item chave", call.Text);
        }

        [Test]
        public void Command_KeepsItsWords()
        {
            CollectionAssert.AreEqual(new[] { "a b" }, CallsInNodeBody("<<dar_item \"a b\">>\nfim").All.Single().Words);
            CollectionAssert.AreEqual(new[] { "a", "b" }, CallsInNodeBody("<<dar_item a b>>\nfim").All.Single().Words);
        }

        [Test]
        public void CommandWithoutParameters_HasZeroWords()
        {
            Assert.AreEqual(0, CallsInNodeBody("<<dar_item>>\nfim").All.Single().WordCount);
        }

        [Test]
        public void CommandWithTwoWords_HasTwo()
        {
            Assert.AreEqual(2, CallsInNodeBody("<<dar_item a b>>\nfim").All.Single().WordCount);
        }

        [Test]
        public void QuotedTextWithASpace_IsOneWord()
        {
            Assert.AreEqual(1, CallsInNodeBody("<<dar_item \"a b\">>\nfim").All.Single().WordCount);
        }

        [Test]
        public void ValueFromAnExpression_IsOneWord()
        {
            ScriptCommandCalls calls = CallsInNodeBody("<<declare $x = \"chave\">>\n<<dar_item {$x}>>\nfim");

            Assert.AreEqual(1, calls.All.Single().WordCount);
        }

        [Test]
        public void CommandInsideAComment_IsNotRead()
        {
            Assert.IsEmpty(CallsInNodeBody("// <<dar_item chave>>\nfim").All);
        }

        [Test]
        public void JumpDetourSetDeclareAndStop_AreNotCommands()
        {
            ScriptCommandCalls calls = CallsIn(
                "title: " + Node + "\n---\n<<declare $x = 0>>\n<<set $x to 1>>\n<<detour outro>>\n<<jump outro>>\n===\n" +
                "title: outro\n---\nfim\n<<stop>>\n===\n");

            Assert.IsEmpty(calls.All);
        }

        [Test]
        public void CommandsInTwoNodes_EachCarryTheirOwnNode()
        {
            ScriptCommandCalls calls = CallsIn(
                "title: primeiro\n---\n<<tocar_musica tema>>\n===\n" +
                "title: segundo\n---\n<<tocar_ambiente chuva>>\n===\n");

            CollectionAssert.AreEquivalent(
                new[] { "primeiro:tocar_musica", "segundo:tocar_ambiente" },
                calls.All.Select(call => $"{call.NodeName}:{call.Name}"));
        }

        [Test]
        public void Problems_ListOnlyCallsOutsideTheSignature()
        {
            ScriptCommandCalls calls = CallsInNodeBody("<<dar_item>>\n<<dar_item chave>>\n<<dar_item a b>>\nfim");

            string[] problems = calls.Problems(new[] { OneParameterSignature("dar_item") }).ToArray();

            CollectionAssert.AreEqual(new[]
            {
                $"nó '{Node}': <<dar_item>>: dar_item pede 1 parâmetro, recebeu 0",
                $"nó '{Node}': <<dar_item a b>>: dar_item pede 1 parâmetro, recebeu 2",
            }, problems);
        }

        [Test]
        public void Problems_ListAnUnknownCommand()
        {
            ScriptCommandCalls calls = CallsInNodeBody("<<dar_iten chave>>\nfim");

            string[] problems = calls.Problems(new[] { OneParameterSignature("dar_item") }).ToArray();

            CollectionAssert.AreEqual(new[] { $"nó '{Node}': <<dar_iten chave>>: o comando 'dar_iten' não existe" }, problems);
        }

        [Test]
        public void Problems_ListAWordOfTheWrongType()
        {
            ScriptCommandCalls calls = CallsInNodeBody("<<esperar abc>>\n<<esperar 1.5>>\nfim");

            string[] problems = calls.Problems(new[] { OneNumberSignature("esperar") }).ToArray();

            CollectionAssert.AreEqual(new[]
            {
                $"nó '{Node}': <<esperar abc>>: esperar pede um número no parâmetro 1, recebeu 'abc'",
            }, problems);
        }

        [Test]
        public void Problems_IgnoreAValueFromAnExpression()
        {
            ScriptCommandCalls calls = CallsInNodeBody("<<declare $x = 1>>\n<<esperar {$x}>>\nfim");

            Assert.IsEmpty(calls.Problems(new[] { OneNumberSignature("esperar") }));
        }

        [Test]
        public void Problems_ReportTheCountBeforeTheType()
        {
            ScriptCommandCalls calls = CallsInNodeBody("<<esperar abc def>>\nfim");

            string[] problems = calls.Problems(new[] { OneNumberSignature("esperar") }).ToArray();

            CollectionAssert.AreEqual(new[]
            {
                $"nó '{Node}': <<esperar abc def>>: esperar pede 1 parâmetro, recebeu 2",
            }, problems);
        }

        [Test]
        public void Problems_AreEmptyWhenEveryCallMatches()
        {
            ScriptCommandCalls calls = CallsInNodeBody("<<dar_item chave>>\nfim");

            Assert.IsEmpty(calls.Problems(new[] { OneParameterSignature("dar_item") }));
        }
    }
}
