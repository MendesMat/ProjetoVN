using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.Core.State;
using ProjetoVN.Dialogue.Logic;
using ProjetoVN.GameFlow.Rooms;
using ProjetoVN.Inventory;
using ProjetoVN.PointNClick;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProjetoVN.Tests.PlayMode
{
    /// <summary>
    /// Caracteriza o <c>HiddenUntilStoryFlag</c> no <c>Exit_Passagem</c> da cena de referência, escondido até
    /// <c>$falou_com_luna</c> ligar (#128).
    /// </summary>
    public class HiddenUntilStoryFlagTests
    {
        private const string Room = "[Teste] Mecanicas";
        private const string EntryPoint = "janela";
        private const string HiddenObject = "Exit_Passagem";
        private const string TalkedToLuna = "$falou_com_luna";
        private const string LunaConversation = "luna_cheguei_cedo";
        private const float WaitTimeoutSeconds = 10f;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim de uma troca de sala pendente");

            Assert.IsNotNull(RoomNavigator.Instance,
                "Não há RoomNavigator: o Managers.prefab não foi criado pelo ManagersBootstrap ou foi destruído.");
            InventoryManager.Instance.ReplaceAll(Array.Empty<ItemDataSO>());
            InventoryManager.Instance.ReplaceConsumedWorldObjectIds(Array.Empty<string>());
            StoryState.ClearAll();

            yield return SceneManager.LoadSceneAsync(Room);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (DialogueManager.Instance.IsDialogueActive()) DialogueManager.Instance.StopDialogue();

            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o jogo voltar à exploração");
            StoryState.ClearAll();
        }

        [UnityTest]
        public IEnumerator HiddenObject_WhileTheFlagIsFalse_IsInvisibleAndOutOfReach()
        {
            GameObject hidden = FindTheHiddenObject();

            Assert.IsTrue(hidden.GetComponentsInChildren<MeshRenderer>(true).Any(),
                "O objeto não tem o rótulo (um MeshRenderer filho): o teste não cobriria o rótulo escondido.");
            Assert.IsTrue(AllRenderersAre(hidden, enabled: false), "Um renderer do objeto, o do rótulo incluso, continuou visível com a variável falsa.");
            Assert.IsFalse(hidden.GetComponent<BoxCollider2D>().enabled, "O objeto continuou com collider com a variável falsa.");
            Assert.IsNull(Physics2D.OverlapPoint(hidden.transform.position),
                "O seletor ainda acharia o objeto escondido: haveria destaque e clique.");
            yield break;
        }

        [UnityTest]
        public IEnumerator HiddenObject_AppearsWhenTheConversationThatSetsTheFlagEnds()
        {
            GameObject hidden = FindTheHiddenObject();

            Assert.IsTrue(DialogueManager.Instance.StartDialogue(LunaConversation), "A conversa da Luna foi recusada.");
            yield return WaitUntil(() => DialogueManager.Instance.IsDialogueActive(), "a conversa abrir");

            StoryState.SetBool(TalkedToLuna, true);
            yield return null;
            Assert.IsTrue(AllRenderersAre(hidden, enabled: false),
                "O objeto apareceu no meio da conversa, atrás da caixa de diálogo.");

            DialogueManager.Instance.StopDialogue();
            yield return null;

            Assert.IsTrue(AllRenderersAre(hidden, enabled: true), "Um renderer do objeto, o do rótulo incluso, continuou invisível depois da conversa.");
            Assert.IsTrue(hidden.GetComponent<BoxCollider2D>().enabled, "O objeto continuou sem collider depois da conversa.");
            Assert.AreSame(hidden.GetComponent<BoxCollider2D>(), Physics2D.OverlapPoint(hidden.transform.position),
                "O seletor não acha o objeto revelado.");
        }

        [UnityTest]
        public IEnumerator RevealedObject_StaysVisible_WhenTheRoomLoadsAgain()
        {
            StoryState.SetBool(TalkedToLuna, true);

            Assert.IsTrue(RoomNavigator.Instance.ChangeRoom(Room, EntryPoint), "A troca de sala foi recusada.");
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "a troca de sala terminar");

            GameObject revealed = FindTheHiddenObject();
            Assert.IsTrue(AllRenderersAre(revealed, enabled: true), "Um renderer do objeto, o do rótulo incluso, chegou invisível com a variável ligada.");
            Assert.IsTrue(revealed.GetComponent<BoxCollider2D>().enabled, "O objeto chegou sem collider com a variável ligada.");
        }

        private static GameObject FindTheHiddenObject()
        {
            GameObject hidden = GameObject.Find(HiddenObject);
            Assert.IsNotNull(hidden, $"A cena '{Room}' não tem o objeto '{HiddenObject}', ou ele está desativado.");
            return hidden;
        }

        private static bool AllRenderersAre(GameObject target, bool enabled) =>
            target.GetComponentsInChildren<Renderer>(true).All(renderer => renderer.enabled == enabled);

        private static IEnumerator WaitUntil(Func<bool> condition, string expectation)
        {
            float deadline = Time.realtimeSinceStartup + WaitTimeoutSeconds;

            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Tempo esgotado esperando {expectation}. Sala ativa: '{SceneManager.GetActiveScene().name}', " +
                                $"input liberado: {PlayerInputGate.IsEnabled}.");

                yield return null;
            }
        }
    }
}
