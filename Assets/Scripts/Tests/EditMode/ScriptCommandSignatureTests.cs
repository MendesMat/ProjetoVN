using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ScriptCommandSignatureTests
    {
        private const string CommandName = "comando";

        private static void NoParameters() { }

        private static void OneString(string first) { }

        private static void RequiredAndOptional(string first, string second = "") { }

        private static void RequiredAndParams(string first, params string[] rest) { }

        private sealed class ExampleBehaviour : MonoBehaviour
        {
            public void OneStringOnTheInstance(string first) { }
        }

        private static ScriptCommandSignature SignatureOf(System.Type type, string methodName)
        {
            const BindingFlags all = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            return ScriptCommandSignature.From(CommandName, type.GetMethod(methodName, all));
        }

        private static ScriptCommandSignature SignatureOfStatic(string methodName) =>
            SignatureOf(typeof(ScriptCommandSignatureTests), methodName);

        [Test]
        public void MethodWithOneString_AcceptsOnlyOneWord()
        {
            ScriptCommandSignature signature = SignatureOfStatic(nameof(OneString));

            Assert.IsFalse(signature.Accepts(0));
            Assert.IsTrue(signature.Accepts(1));
            Assert.IsFalse(signature.Accepts(2));
        }

        [Test]
        public void MethodWithoutParameters_AcceptsOnlyZeroWords()
        {
            ScriptCommandSignature signature = SignatureOfStatic(nameof(NoParameters));

            Assert.IsTrue(signature.Accepts(0));
            Assert.IsFalse(signature.Accepts(1));
        }

        [Test]
        public void OptionalParameter_AcceptsWithAndWithoutIt()
        {
            ScriptCommandSignature signature = SignatureOfStatic(nameof(RequiredAndOptional));

            Assert.IsFalse(signature.Accepts(0));
            Assert.IsTrue(signature.Accepts(1));
            Assert.IsTrue(signature.Accepts(2));
            Assert.IsFalse(signature.Accepts(3));
        }

        [Test]
        public void ParamsArray_AcceptsAnyCountFromTheRequired()
        {
            ScriptCommandSignature signature = SignatureOfStatic(nameof(RequiredAndParams));

            Assert.IsFalse(signature.Accepts(0));
            Assert.IsTrue(signature.Accepts(1));
            Assert.IsTrue(signature.Accepts(10));
        }

        [Test]
        public void InstanceMethodOnAComponent_CountsTheGameObjectName()
        {
            ScriptCommandSignature signature = SignatureOf(typeof(ExampleBehaviour), nameof(ExampleBehaviour.OneStringOnTheInstance));

            Assert.IsFalse(signature.Accepts(1));
            Assert.IsTrue(signature.Accepts(2));
            Assert.IsFalse(signature.Accepts(3));
        }

        [Test]
        public void Signature_KeepsTheCommandName()
        {
            Assert.AreEqual(CommandName, SignatureOfStatic(nameof(OneString)).Name);
        }

        [TestCase(nameof(NoParameters), "nenhum parâmetro")]
        [TestCase(nameof(OneString), "1 parâmetro")]
        [TestCase(nameof(RequiredAndOptional), "de 1 a 2 parâmetros")]
        [TestCase(nameof(RequiredAndParams), "1 ou mais parâmetros")]
        public void Describe_SaysWhatTheCommandAccepts(string methodName, string expected)
        {
            Assert.AreEqual(expected, SignatureOfStatic(methodName).Describe());
        }
    }
}
