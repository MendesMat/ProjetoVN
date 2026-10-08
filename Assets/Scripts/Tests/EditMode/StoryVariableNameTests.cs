using NUnit.Framework;
using ProjetoVN.Core.State;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class StoryVariableNameTests
    {
        [Test]
        public void IsValid_AcceptsADollarPrefixedName()
        {
            Assert.IsTrue(StoryVariableName.IsValid("$afinidade_luna"));
        }

        [Test]
        public void IsValid_RejectsNullEmptyWhitespaceMissingPrefixAndBareDollar()
        {
            Assert.IsFalse(StoryVariableName.IsValid(null));
            Assert.IsFalse(StoryVariableName.IsValid(""));
            Assert.IsFalse(StoryVariableName.IsValid("   "));
            Assert.IsFalse(StoryVariableName.IsValid("afinidade_luna"));
            Assert.IsFalse(StoryVariableName.IsValid("$"));
        }

        [Test]
        public void IsValid_AcceptsTheInternalNamesYarnStoresItself()
        {
            Assert.IsTrue(StoryVariableName.IsValid("$Yarn.Internal.Visiting.Inicio"),
                "o armazenamento não pode recusar o que o Yarn grava para sustentar visited()");
        }

        [Test]
        public void FollowsConvention_AcceptsLowercaseAsciiDigitsAndUnderscore()
        {
            Assert.IsTrue(StoryVariableName.FollowsConvention("$falou_com_luna"));
            Assert.IsTrue(StoryVariableName.FollowsConvention("$afinidade_luna2"));
            Assert.IsTrue(StoryVariableName.FollowsConvention("$_x"));
        }

        [Test]
        public void FollowsConvention_RejectsWhatTheInspectorMustNotAccept()
        {
            Assert.IsFalse(StoryVariableName.FollowsConvention("$falou-com-luna"));
            Assert.IsFalse(StoryVariableName.FollowsConvention("$Falou"));
            Assert.IsFalse(StoryVariableName.FollowsConvention("$afinidade_ação"));
            Assert.IsFalse(StoryVariableName.FollowsConvention("$1x"));
            Assert.IsFalse(StoryVariableName.FollowsConvention("falou"));
            Assert.IsFalse(StoryVariableName.FollowsConvention("$Yarn.Internal.X"));
            Assert.IsFalse(StoryVariableName.FollowsConvention("$"));
            Assert.IsFalse(StoryVariableName.FollowsConvention(""));
            Assert.IsFalse(StoryVariableName.FollowsConvention(null));
        }
    }
}
