using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjetoVN.Core.State;
using ProjetoVN.GameFlow.DialogueEffects;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>
    /// Cobre os efeitos que só dependem de StoryFlags. GiveItemEffect e RemoveItemEffect ficam de
    /// fora de propósito: exigiriam um InventoryManager num GameObject, quebrando a regra
    /// "C# puro, sem GameObjects" da suíte por um null-check e uma linha que delega.
    /// Esses dois são verificados em Play Mode.
    /// </summary>
    public sealed class StoryFlagEffectsTests
    {
        #region Setup
        [SetUp]
        public void SetUp() => StoryFlags.ClearAll();

        [TearDown]
        public void TearDown() => StoryFlags.ClearAll();

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
        #endregion

        #region SetFlagEffect
        [Test]
        public void SetFlagEffect_Execute_SetsTheFlag()
        {
            var effect = EffectWithFlag<SetFlagEffect>("falou-com-gotica");

            effect.Execute();

            Assert.IsTrue(StoryFlags.IsSet("falou-com-gotica"));
        }

        [Test]
        public void SetFlagEffect_Executed_Twice_LeavesTheFlagSet()
        {
            var effect = EffectWithFlag<SetFlagEffect>("falou-com-gotica");

            effect.Execute();
            effect.Execute();

            Assert.IsTrue(StoryFlags.IsSet("falou-com-gotica"),
                "rejogar um diálogo não pode desligar o que ele tinha ligado");
        }

        [Test]
        public void SetFlagEffect_WithEmptyFlagId_LogsError_AndSetsNothing()
        {
            LogAssert.Expect(LogType.Error, new Regex("flagId"));

            var effect = EffectWithFlag<SetFlagEffect>("");

            effect.Execute();

            Assert.IsEmpty(StoryFlags.All, "um efeito mal configurado precisa reclamar, não criar uma flag vazia");
        }
        #endregion

        #region ClearFlagEffect
        [Test]
        public void ClearFlagEffect_Execute_RemovesTheFlag()
        {
            StoryFlags.Set("porta-destrancada");
            var effect = EffectWithFlag<ClearFlagEffect>("porta-destrancada");

            effect.Execute();

            Assert.IsFalse(StoryFlags.IsSet("porta-destrancada"));
        }

        [Test]
        public void ClearFlagEffect_WithEmptyFlagId_LogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("flagId"));

            var effect = EffectWithFlag<ClearFlagEffect>("");

            effect.Execute();

            Assert.IsEmpty(StoryFlags.All);
        }
        #endregion
    }
}
