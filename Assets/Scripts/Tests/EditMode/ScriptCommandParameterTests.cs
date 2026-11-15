using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ScriptCommandParameterTests
    {
        private static void OneString(string text) { }

        private static void OneFloat(float duration) { }

        private static void OneInt(int amount) { }

        private static void OneBool(bool enabled) { }

        private static void OneGameObject(GameObject target) { }

        private static void OneTransform(Transform target) { }

        private static void ParamsFloats(params float[] values) { }

        private static ParameterInfo ParameterOf(string methodName)
        {
            MethodInfo method = typeof(ScriptCommandParameterTests).GetMethod(
                methodName, BindingFlags.Static | BindingFlags.NonPublic);
            return method.GetParameters()[0];
        }

        [TestCase("abc")]
        [TestCase("1,5")]
        [TestCase("{0}")]
        public void String_AcceptsAnyWord(string word)
        {
            Assert.IsTrue(ScriptCommandParameter.Accepts(ParameterOf(nameof(OneString)), word));
        }

        [TestCase("1.5")]
        [TestCase(".5")]
        [TestCase("2")]
        [TestCase("-1")]
        public void Float_AcceptsADotDecimalAndAnInteger(string word)
        {
            Assert.IsTrue(ScriptCommandParameter.Accepts(ParameterOf(nameof(OneFloat)), word));
        }

        [TestCase("abc")]
        [TestCase("")]
        public void Float_RejectsText(string word)
        {
            Assert.IsFalse(ScriptCommandParameter.Accepts(ParameterOf(nameof(OneFloat)), word));
        }

        [TestCase("1,5")]
        [TestCase("1,000")]
        public void Float_RejectsADecimalComma(string word)
        {
            Assert.IsFalse(ScriptCommandParameter.Accepts(ParameterOf(nameof(OneFloat)), word));
        }

        [TestCase("3", true)]
        [TestCase("1.5", false)]
        [TestCase("abc", false)]
        public void Int_AcceptsOnlyWholeNumbers(string word, bool expected)
        {
            Assert.AreEqual(expected, ScriptCommandParameter.Accepts(ParameterOf(nameof(OneInt)), word));
        }

        [TestCase("true")]
        [TestCase("False")]
        [TestCase("ENABLED")]
        public void Bool_AcceptsTrueAndFalseInAnyCaseAndTheParameterName(string word)
        {
            Assert.IsTrue(ScriptCommandParameter.Accepts(ParameterOf(nameof(OneBool)), word));
        }

        [TestCase("sim")]
        [TestCase("1")]
        public void Bool_RejectsAnyOtherWord(string word)
        {
            Assert.IsFalse(ScriptCommandParameter.Accepts(ParameterOf(nameof(OneBool)), word));
        }

        [TestCase(nameof(OneGameObject))]
        [TestCase(nameof(OneTransform))]
        public void GameObjectAndComponent_AcceptAnyName(string methodName)
        {
            Assert.IsTrue(ScriptCommandParameter.Accepts(ParameterOf(methodName), "qualquer_nome"));
        }

        [TestCase("1.5", true)]
        [TestCase("abc", false)]
        public void ParamsArray_IsCheckedByItsElementType(string word, bool expected)
        {
            Assert.AreEqual(expected, ScriptCommandParameter.Accepts(ParameterOf(nameof(ParamsFloats)), word));
        }

        [TestCase(nameof(OneFloat), "um número")]
        [TestCase(nameof(OneInt), "um número inteiro")]
        [TestCase(nameof(OneBool), "true ou false")]
        [TestCase(nameof(ParamsFloats), "um número")]
        [TestCase(nameof(OneString), "String")]
        public void Describe_SaysTheTypeInPortuguese(string methodName, string expected)
        {
            Assert.AreEqual(expected, ScriptCommandParameter.Describe(ParameterOf(methodName)));
        }

        [TestCase("1,5", true)]
        [TestCase("1,000", true)]
        [TestCase("1.5", false)]
        [TestCase("abc,def", false)]
        public void DecimalCommaMistake_IsACommaThatADotWouldFix(string word, bool expected)
        {
            Assert.AreEqual(expected, ScriptCommandParameter.IsDecimalCommaMistake(ParameterOf(nameof(OneFloat)), word));
        }

        [Test]
        public void DecimalCommaMistake_IsNeverAMistakeForAnInteger()
        {
            Assert.IsFalse(ScriptCommandParameter.IsDecimalCommaMistake(ParameterOf(nameof(OneInt)), "1,5"));
        }
    }
}
