using System.Text;
using NUnit.Framework;
using ProjetoVN.Dialogue.Logic;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ProtagonistNameTests
    {
        // Escrita à mão, em ordem de código, a partir da regra da D-21 (#213). Não pode vir da constante de
        // ProtagonistName: um teste que confere a constante contra ela mesma nunca reprova.
        private const string PortugueseLettersInCodeOrder =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "abcdefghijklmnopqrstuvwxyz" +
            "ÀÁÂÃÇÉÊÍÓÔÕÚÜ" +
            "àáâãçéêíóôõúü";

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
        [TestCase("Lúcia")]
        [TestCase("Müller")]
        public void TryNormalize_AcceptsAccentedLetters(string typed)
        {
            Assert.IsTrue(ProtagonistName.TryNormalize(typed, out string name));
            Assert.AreEqual(typed, name);
        }

        [TestCase("Ana-Luísa")]
        [TestCase("D'Ávila")]
        [TestCase("  Ana   Clara ")]
        [TestCase("Zoë")]
        public void TryNormalize_RejectsWhatIsNotAPortugueseLetter(string typed)
        {
            Assert.IsFalse(ProtagonistName.TryNormalize(typed, out _));
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
        [TestCase('Z')]
        [TestCase('ç')]
        [TestCase('Ç')]
        [TestCase('ã')]
        [TestCase('Õ')]
        [TestCase('ü')]
        [TestCase('À')]
        [TestCase('ê')]
        [TestCase('í')]
        public void IsAllowedCharacter_AcceptsThePortugueseLetters(char character)
        {
            Assert.IsTrue(ProtagonistName.IsAllowedCharacter(character));
        }

        [Test]
        public void IsAllowedCharacter_AcceptsExactlyThePortugueseLetters()
        {
            var accepted = new StringBuilder();
            for (int code = char.MinValue; code <= char.MaxValue; code++)
            {
                if (ProtagonistName.IsAllowedCharacter((char)code)) accepted.Append((char)code);
            }

            Assert.AreEqual(PortugueseLettersInCodeOrder, accepted.ToString());
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
        [TestCase(' ')]
        [TestCase('-')]
        [TestCase('\'')]
        [TestCase('ë')]
        [TestCase('ñ')]
        [TestCase('ß')]
        [TestCase('α')]
        [TestCase('Ж')]
        [TestCase('ア')]
        public void IsAllowedCharacter_RejectsWhatIsNotAPortugueseLetter(char character)
        {
            Assert.IsFalse(ProtagonistName.IsAllowedCharacter(character));
        }
    }
}
