using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjetoVN.Core.State;
using ProjetoVN.GameFlow.DialogueEffects;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>
    /// Cobre os efeitos que só dependem de StoryState. GiveItemEffect e RemoveItemEffect ficam de
    /// fora de propósito: exigiriam um InventoryManager num GameObject, quebrando a regra
    /// "C# puro, sem GameObjects" da suíte por um null-check e uma linha que delega.
    /// Esses dois são verificados em Play Mode.
    /// </summary>
    public sealed class StoryFlagEffectsTests
    {
        [SetUp]
        public void SetUp() => StoryState.ClearAll();

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        // flagId é [SerializeField] private, então preenchemos pelo SerializedObject —
        // legal aqui porque a Tests.asmdef é Editor-only.
        private static T EffectWithFlag<T>(string flagId) where T : ScriptableObject
        {
            var effect = ScriptableObject.CreateInstance<T>();

            var serialized = new UnityEditor.SerializedObject(effect);
            serialized.FindProperty("flagId").stringValue = flagId;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return effect;
        }

        [Test]
        public void SetFlagEffect_Execute_SetsTheFlag()
        {
            var effect = EffectWithFlag<SetFlagEffect>("$falou_com_gotica");

            effect.Execute();

            Assert.IsTrue(StoryState.IsTrue("$falou_com_gotica"));
        }

        [Test]
        public void SetFlagEffect_Executed_Twice_LeavesTheFlagSet()
        {
            var effect = EffectWithFlag<SetFlagEffect>("$falou_com_gotica");

            effect.Execute();
            effect.Execute();

            Assert.IsTrue(StoryState.IsTrue("$falou_com_gotica"),
                "rejogar um diálogo não pode desligar o que ele tinha ligado");
        }

        [Test]
        public void SetFlagEffect_WithEmptyFlagId_LogsError_AndSetsNothing()
        {
            LogAssert.Expect(LogType.Error, new Regex("flagId"));

            var effect = EffectWithFlag<SetFlagEffect>("");

            effect.Execute();

            Assert.IsEmpty(StoryState.Bools, "um efeito mal configurado precisa reclamar, não criar uma variável vazia");
        }

        [Test]
        public void ClearFlagEffect_Execute_WritesFalse()
        {
            StoryState.SetBool("$porta_destrancada", true);
            var effect = EffectWithFlag<ClearFlagEffect>("$porta_destrancada");

            effect.Execute();

            Assert.IsFalse(StoryState.IsTrue("$porta_destrancada"));
            Assert.IsTrue(StoryState.TryGetBool("$porta_destrancada", out bool value),
                "desligar grava falso, como um <<set $x to false>> do roteiro faria");
            Assert.IsFalse(value);
        }

        [Test]
        public void ClearFlagEffect_WithEmptyFlagId_LogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("flagId"));

            var effect = EffectWithFlag<ClearFlagEffect>("");

            effect.Execute();

            Assert.IsEmpty(StoryState.Bools);
        }
    }
}
