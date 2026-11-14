using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjetoVN.GameFlow;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class StoryVariableNameCheckTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void WarnIfOffConvention_WithAnEmptyName_StaysSilent(string emptyName)
        {
            StoryVariableNameCheck.WarnIfOffConvention("Dono", "campo", emptyName, null);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void WarnIfOffConvention_WithAConventionalName_StaysSilent()
        {
            StoryVariableNameCheck.WarnIfOffConvention("Dono", "campo", "$porta_mecanicas_destrancada", null);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void WarnIfOffConvention_WithAnOffConventionName_WarnsOnceNamingOwnerFieldAndValue()
        {
            LogAssert.Expect(LogType.Warning, new Regex(
                @"^\[Dono\] O campo 'campo' tem '\$Porta-Aberta', fora do formato das variáveis de história"));

            StoryVariableNameCheck.WarnIfOffConvention("Dono", "campo", "$Porta-Aberta", null);

            LogAssert.NoUnexpectedReceived();
        }
    }
}
