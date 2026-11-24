using NUnit.Framework;
using ProjetoVN.Dialogue.Logic;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ProtagonistNameTests
    {
        [Test]
        public void VariableAndUnknownMark_AreTheOnesTheScriptDeclares()
        {
            Assert.AreEqual("$nome_protagonista", ProtagonistName.VariableName);
            Assert.AreEqual("???", ProtagonistName.Unknown);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ToDisplay_WithNothingStored_ShowsTheUnknownMark(string stored)
        {
            Assert.AreEqual("???", ProtagonistName.ToDisplay(stored));
        }

        [Test]
        public void ToDisplay_WithAStoredName_ShowsTheName()
        {
            Assert.AreEqual("Conceição", ProtagonistName.ToDisplay("Conceição"));
        }

        [TestCase("  Ana   Clara  ", "Ana Clara")]
        [TestCase("Ana\t\tClara", "Ana Clara")]
        [TestCase("Ana", "Ana")]
        [TestCase(null, "")]
        public void Normalize_TrimsTheEnds_AndCollapsesInnerSpaces(string typed, string expected)
        {
            Assert.AreEqual(expected, ProtagonistName.Normalize(typed));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void TryNormalize_RejectsEmptyOrBlank(string typed)
        {
            Assert.IsFalse(ProtagonistName.TryNormalize(typed, out _));
        }

        [TestCase("João")]
        [TestCase("Conceição")]
        [TestCase("Ângela")]
        [TestCase("Zoë")]
        public void TryNormalize_AcceptsAccentedLetters(string typed)
        {
            Assert.IsTrue(ProtagonistName.TryNormalize(typed, out string name));
            Assert.AreEqual(typed, name);
        }

        [TestCase("Ana-Luísa", "Ana-Luísa")]
        [TestCase("D'Ávila", "D'Ávila")]
        [TestCase("  Ana   Clara ", "Ana Clara")]
        public void TryNormalize_AcceptsHyphenApostropheAndInnerSpace_ReturningTheNormalizedName(string typed, string expected)
        {
            Assert.IsTrue(ProtagonistName.TryNormalize(typed, out string name));
            Assert.AreEqual(expected, name);
        }

        [TestCase("[b]")]
        [TestCase("<b>")]
        [TestCase("{x}")]
        [TestCase("Ana#")]
        [TestCase("Ana:")]
        [TestCase("Ana\\")]
        [TestCase("Ana1")]
        public void TryNormalize_RejectsCharactersThatBreakTheTextOrTheScript(string typed)
        {
            Assert.IsFalse(ProtagonistName.TryNormalize(typed, out _));
        }

        [TestCase("-")]
        [TestCase("'")]
        [TestCase("- '")]
        public void TryNormalize_RequiresALetter(string typed)
        {
            Assert.IsFalse(ProtagonistName.TryNormalize(typed, out _));
        }

        [TestCase('a')]
        [TestCase('ç')]
        [TestCase('Ã')]
        [TestCase(' ')]
        [TestCase('-')]
        [TestCase('\'')]
        public void IsAllowedCharacter_AcceptsLettersSpaceHyphenAndApostrophe(char character)
        {
            Assert.IsTrue(ProtagonistName.IsAllowedCharacter(character));
        }

        [TestCase('1')]
        [TestCase('[')]
        [TestCase(']')]
        [TestCase('<')]
        [TestCase('>')]
        [TestCase('{')]
        [TestCase('}')]
        [TestCase('#')]
        [TestCase(':')]
        [TestCase('\\')]
        [TestCase('\t')]
        [TestCase('\uD83D')]
        public void IsAllowedCharacter_RejectsDigitsMarkupAndHalfASurrogatePair(char character)
        {
            Assert.IsFalse(ProtagonistName.IsAllowedCharacter(character));
        }

        [TestCase("", 0)]
        [TestCase("Ana", 0)]
        public void IsRedundantSpace_AtTheStart_IsTrue(string text, int position)
        {
            Assert.IsTrue(ProtagonistName.IsRedundantSpace(text, position));
        }

        [TestCase("Ana ", 4)]
        [TestCase("Ana Clara", 4)]
        public void IsRedundantSpace_AfterASpace_IsTrue(string text, int position)
        {
            Assert.IsTrue(ProtagonistName.IsRedundantSpace(text, position));
        }

        [TestCase("Ana Clara", 3)]
        [TestCase(" Ana", 0)]
        [TestCase("Ana  ", 3)]
        public void IsRedundantSpace_BeforeASpace_IsTrue(string text, int position)
        {
            Assert.IsTrue(ProtagonistName.IsRedundantSpace(text, position));
        }

        [Test]
        public void IsRedundantSpace_NextToATab_IsTrue()
        {
            Assert.IsTrue(ProtagonistName.IsRedundantSpace("Ana\t", 4));
        }

        [TestCase("Ana", 3)]
        [TestCase("Ana-", 4)]
        [TestCase("D'Ávila", 7)]
        public void IsRedundantSpace_AtTheEndAfterAWord_IsFalse(string text, int position)
        {
            Assert.IsFalse(ProtagonistName.IsRedundantSpace(text, position));
        }

        [TestCase("AnaClara", 3)]
        [TestCase("Ana", 1)]
        public void IsRedundantSpace_InsideAWord_IsFalse(string text, int position)
        {
            Assert.IsFalse(ProtagonistName.IsRedundantSpace(text, position));
        }
    }
}
