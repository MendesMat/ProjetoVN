using NUnit.Framework;
using ProjetoVN.Dialogue.Characters;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ConversationStageTests
    {
        private const string Neutral = "neutra";
        private const string Angry = "raiva";

        private ConversationStage _stage;
        private CharacterSO _luna;
        private CharacterSO _protagonist;
        private CharacterSO _teacher;

        [SetUp]
        public void SetUp()
        {
            _stage = new ConversationStage();
            _luna = ScriptableObject.CreateInstance<CharacterSO>();
            _protagonist = ScriptableObject.CreateInstance<CharacterSO>();
            _teacher = ScriptableObject.CreateInstance<CharacterSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_luna);
            Object.DestroyImmediate(_protagonist);
            Object.DestroyImmediate(_teacher);
        }

        [Test]
        public void NewStage_HasBothSidesEmpty()
        {
            Assert.IsTrue(_stage.Occupant(StageSide.Left).IsEmpty);
            Assert.IsTrue(_stage.Occupant(StageSide.Right).IsEmpty);
        }

        [Test]
        public void FirstSpeaker_EntersOnTheLeftHighlightedWithTheLineExpression()
        {
            _stage.Speak(_luna, Angry);

            AssertOccupant(StageSide.Left, _luna, Angry, highlighted: true);
            Assert.IsTrue(_stage.Occupant(StageSide.Right).IsEmpty);
        }

        [Test]
        public void SecondSpeaker_EntersOnTheRightHighlightedAndTheFirstIsDimmed()
        {
            _stage.Speak(_luna, Neutral);
            _stage.Speak(_protagonist, Neutral);

            AssertOccupant(StageSide.Left, _luna, Neutral, highlighted: false);
            AssertOccupant(StageSide.Right, _protagonist, Neutral, highlighted: true);
        }

        [Test]
        public void SpeakerAlreadyOnStage_KeepsTheSideAndTheOtherIsDimmed()
        {
            _stage.Speak(_luna, Neutral);
            _stage.Speak(_protagonist, Neutral);
            _stage.Speak(_luna, Neutral);

            AssertOccupant(StageSide.Left, _luna, Neutral, highlighted: true);
            AssertOccupant(StageSide.Right, _protagonist, Neutral, highlighted: false);
        }

        [Test]
        public void ProtagonistOpeningTheConversation_StaysOnTheLeft()
        {
            _stage.Speak(_protagonist, Neutral);
            _stage.Speak(_luna, Neutral);

            AssertOccupant(StageSide.Left, _protagonist, Neutral, highlighted: false);
            AssertOccupant(StageSide.Right, _luna, Neutral, highlighted: true);
        }

        [Test]
        public void ChoicesOnly_PutTheProtagonistOnTheLeftHighlightedWithTheDefaultExpression()
        {
            _stage.GiveTurnToPlayer(_protagonist, Neutral);

            AssertOccupant(StageSide.Left, _protagonist, Neutral, highlighted: true);
            Assert.IsTrue(_stage.Occupant(StageSide.Right).IsEmpty);
        }

        [Test]
        public void ThirdSpeaker_TakesThePlaceOfWhoSpokeLongestAgo()
        {
            _stage.Speak(_luna, Neutral);
            _stage.Speak(_protagonist, Neutral);
            _stage.Speak(_teacher, Neutral);

            AssertOccupant(StageSide.Left, _teacher, Neutral, highlighted: true);
            AssertOccupant(StageSide.Right, _protagonist, Neutral, highlighted: false);
        }

        [Test]
        public void ThirdSpeaker_AfterTheFirstSpokeAgain_TakesThePlaceOfTheSecond()
        {
            _stage.Speak(_luna, Neutral);
            _stage.Speak(_protagonist, Neutral);
            _stage.Speak(_luna, Neutral);
            _stage.Speak(_teacher, Neutral);

            AssertOccupant(StageSide.Left, _luna, Neutral, highlighted: false);
            AssertOccupant(StageSide.Right, _teacher, Neutral, highlighted: true);
        }

        [Test]
        public void WhoLeft_ComesBackOnTheOtherSideWhenSpeakingAgain()
        {
            _stage.Speak(_luna, Neutral);
            _stage.Speak(_protagonist, Neutral);
            _stage.Speak(_teacher, Neutral);
            _stage.Speak(_luna, Neutral);

            AssertOccupant(StageSide.Left, _teacher, Neutral, highlighted: false);
            AssertOccupant(StageSide.Right, _luna, Neutral, highlighted: true);
        }

        [Test]
        public void DimEveryone_OnAnEmptyStage_KeepsItEmpty()
        {
            _stage.DimEveryone();

            Assert.IsTrue(_stage.Occupant(StageSide.Left).IsEmpty);
            Assert.IsTrue(_stage.Occupant(StageSide.Right).IsEmpty);
        }

        [Test]
        public void DimEveryone_DimsBothKeepingSidesAndExpressions()
        {
            _stage.Speak(_luna, Angry);
            _stage.Speak(_protagonist, Neutral);

            _stage.DimEveryone();

            AssertOccupant(StageSide.Left, _luna, Angry, highlighted: false);
            AssertOccupant(StageSide.Right, _protagonist, Neutral, highlighted: false);
        }

        [Test]
        public void DimEveryone_DoesNotCountAsATurn()
        {
            _stage.Speak(_luna, Neutral);
            _stage.Speak(_protagonist, Neutral);
            _stage.DimEveryone();
            _stage.Speak(_teacher, Neutral);

            AssertOccupant(StageSide.Left, _teacher, Neutral, highlighted: true);
            AssertOccupant(StageSide.Right, _protagonist, Neutral, highlighted: false);
        }

        [Test]
        public void SpeakingAgain_ShowsTheExpressionOfTheNewLine()
        {
            _stage.Speak(_luna, Angry);
            _stage.Speak(_luna, Neutral);

            AssertOccupant(StageSide.Left, _luna, Neutral, highlighted: true);
        }

        [Test]
        public void WhoIsDimmed_KeepsTheLastExpression()
        {
            _stage.Speak(_luna, Angry);
            _stage.Speak(_protagonist, Neutral);

            AssertOccupant(StageSide.Left, _luna, Angry, highlighted: false);
        }

        [Test]
        public void Choices_WithTheProtagonistOffStageAndASideFree_EnterOnTheRightHighlighted()
        {
            _stage.Speak(_luna, Angry);

            _stage.GiveTurnToPlayer(_protagonist, Neutral);

            AssertOccupant(StageSide.Left, _luna, Angry, highlighted: false);
            AssertOccupant(StageSide.Right, _protagonist, Neutral, highlighted: true);
        }

        [Test]
        public void Choices_WithTheProtagonistOnStage_KeepTheSideAndTheLastExpression()
        {
            _stage.Speak(_protagonist, Angry);
            _stage.Speak(_luna, Neutral);

            _stage.GiveTurnToPlayer(_protagonist, Neutral);

            AssertOccupant(StageSide.Left, _protagonist, Angry, highlighted: true);
            AssertOccupant(StageSide.Right, _luna, Neutral, highlighted: false);
        }

        [Test]
        public void Choices_OnAFullStageWithoutTheProtagonist_TakeThePlaceOfWhoSpokeLongestAgo()
        {
            _stage.Speak(_luna, Neutral);
            _stage.Speak(_teacher, Neutral);

            _stage.GiveTurnToPlayer(_protagonist, Neutral);

            AssertOccupant(StageSide.Left, _protagonist, Neutral, highlighted: true);
            AssertOccupant(StageSide.Right, _teacher, Neutral, highlighted: false);
        }

        [Test]
        public void PlayersTurn_CountsAsATurnForTheReplacementRule()
        {
            _stage.Speak(_luna, Neutral);
            _stage.GiveTurnToPlayer(_protagonist, Neutral);
            _stage.Speak(_teacher, Neutral);

            AssertOccupant(StageSide.Left, _teacher, Neutral, highlighted: true);
            AssertOccupant(StageSide.Right, _protagonist, Neutral, highlighted: false);
        }

        [Test]
        public void Clear_EmptiesBothSidesAndTheNextSpeakerEntersOnTheLeft()
        {
            _stage.Speak(_luna, Neutral);
            _stage.Speak(_protagonist, Neutral);

            _stage.Clear();

            Assert.IsTrue(_stage.Occupant(StageSide.Left).IsEmpty);
            Assert.IsTrue(_stage.Occupant(StageSide.Right).IsEmpty);

            _stage.Speak(_teacher, Neutral);
            AssertOccupant(StageSide.Left, _teacher, Neutral, highlighted: true);
        }

        private void AssertOccupant(StageSide side, CharacterSO character, string expression, bool highlighted)
        {
            StageOccupant occupant = _stage.Occupant(side);
            Assert.AreSame(character, occupant.Character, $"quem está em {side}");
            Assert.AreEqual(expression, occupant.Expression, $"expressão em {side}");
            Assert.AreEqual(highlighted, occupant.IsHighlighted, $"destaque em {side}");
        }
    }
}
