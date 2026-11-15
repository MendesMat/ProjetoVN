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

        private static void StringAndFloat(string first, float second) { }

        private static void OneFloat(float first) { }

        private static void StringAndParamsOfFloat(string first, params float[] rest) { }

        private sealed class ExampleBehaviour : MonoBehaviour
        {
            public void OneStringOnTheInstance(string first) { }

            public void OneFloatOnTheInstance(float first) { }
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

        [Test]
        public void WordsOfTheRightType_HaveNoTypeProblem()
        {
            ScriptCommandSignature signature = SignatureOfStatic(nameof(StringAndFloat));

            Assert.IsNull(signature.TypeProblemOf(new[] { "a", "1.5" }));
        }

        [Test]
        public void WordOfTheWrongType_IsReportedWithItsPosition()
        {
            ScriptCommandSignature signature = SignatureOfStatic(nameof(StringAndFloat));

            Assert.AreEqual("pede um número no parâmetro 2, recebeu 'abc'", signature.TypeProblemOf(new[] { "a", "abc" }));
        }

        [TestCase("{0}")]
        [TestCase("a{0}b")]
        public void WordFromAnExpression_IsNotChecked(string word)
        {
            ScriptCommandSignature signature = SignatureOfStatic(nameof(OneFloat));

            Assert.IsNull(signature.TypeProblemOf(new[] { word }));
        }

        [Test]
        public void InstanceMethod_DoesNotCheckTheGameObjectName()
        {
            ScriptCommandSignature signature = SignatureOf(typeof(ExampleBehaviour), nameof(ExampleBehaviour.OneFloatOnTheInstance));

            Assert.IsNull(signature.TypeProblemOf(new[] { "Porta", "1.5" }));
            Assert.AreEqual("pede um número no parâmetro 1, recebeu 'abc'", signature.TypeProblemOf(new[] { "Porta", "abc" }));
        }

        [Test]
        public void ParamsArray_ChecksEveryExtraWord()
        {
            ScriptCommandSignature signature = SignatureOfStatic(nameof(StringAndParamsOfFloat));

            Assert.IsNull(signature.TypeProblemOf(new[] { "a", "1", "2" }));
            Assert.AreEqual("pede um número no parâmetro 2, recebeu 'abc'", signature.TypeProblemOf(new[] { "a", "1", "abc" }));
        }

        [Test]
        public void DecimalComma_IsReportedAskingForADot()
        {
            ScriptCommandSignature signature = SignatureOfStatic(nameof(OneFloat));

            Assert.AreEqual("pede um número com ponto no parâmetro 1 (1.5, não 1,5), recebeu '1,5'",
                signature.TypeProblemOf(new[] { "1,5" }));
        }

        [Test]
        public void SeveralWrongWords_ReportTheFirst()
        {
            ScriptCommandSignature signature = SignatureOfStatic(nameof(StringAndParamsOfFloat));

            Assert.AreEqual("pede um número no parâmetro 2, recebeu 'x'", signature.TypeProblemOf(new[] { "a", "x", "y" }));
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
