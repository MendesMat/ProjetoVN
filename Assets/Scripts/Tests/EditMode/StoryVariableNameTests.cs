using NUnit.Framework;
using ProjetoVN.Core.State;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class StoryVariableNameTests
    {
        [Test]
        public void IsValid_AcceptsADollarPrefixedName()
        {
            Assert.IsTrue(StoryVariableName.IsValid("$afinidade_gotica"));
        }

        [Test]
        public void IsValid_RejectsNullEmptyWhitespaceMissingPrefixAndBareDollar()
        {
            Assert.IsFalse(StoryVariableName.IsValid(null));
            Assert.IsFalse(StoryVariableName.IsValid(""));
            Assert.IsFalse(StoryVariableName.IsValid("   "));
            Assert.IsFalse(StoryVariableName.IsValid("afinidade_gotica"));
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
            Assert.IsTrue(StoryVariableName.FollowsConvention("$falou_com_gotica"));
            Assert.IsTrue(StoryVariableName.FollowsConvention("$afinidade_gotica2"));
            Assert.IsTrue(StoryVariableName.FollowsConvention("$_x"));
        }

        [Test]
        public void FollowsConvention_RejectsWhatTheInspectorMustNotAccept()
        {
            Assert.IsFalse(StoryVariableName.FollowsConvention("$falou-com-gotica"));
            Assert.IsFalse(StoryVariableName.FollowsConvention("$Falou"));
            Assert.IsFalse(StoryVariableName.FollowsConvention("$afinidade_gótica"));
            Assert.IsFalse(StoryVariableName.FollowsConvention("$1x"));
            Assert.IsFalse(StoryVariableName.FollowsConvention("falou"));
            Assert.IsFalse(StoryVariableName.FollowsConvention("$Yarn.Internal.X"));
            Assert.IsFalse(StoryVariableName.FollowsConvention("$"));
            Assert.IsFalse(StoryVariableName.FollowsConvention(""));
            Assert.IsFalse(StoryVariableName.FollowsConvention(null));
        }
    }
}
