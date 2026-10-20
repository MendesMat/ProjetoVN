using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjetoVN.Dialogue.Logic;
using ProjetoVN.Dialogue.UI;
using ProjetoVN.GameFlow;
using ProjetoVN.GameFlow.Persistence;
using ProjetoVN.GameFlow.Rooms;
using ProjetoVN.Inventory;
using ProjetoVN.PointNClick;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ProjetoVN.Tests.PlayMode
{
    public class RoomChangeTests
    {
        private const string StartRoom = "[Teste] Mecanicas";
        private const string OtherRoom = "[Teste] Sala B";
        private const string EntryPointId = "janela";
        private const float TransitionTimeoutSeconds = 10f;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim de uma troca de sala pendente");

            Assert.IsNotNull(RoomNavigator.Instance, "O ManagersBootstrap não criou o Managers.prefab.");
            ResetInventory();

            yield return SceneManager.LoadSceneAsync(StartRoom);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim de uma troca de sala pendente");
        }

        [UnityTest]
        public IEnumerator RoomChange_KeepsASingleInstanceOfEachManager()
        {
            GameObject managersRoot = RoomNavigator.Instance.transform.root.gameObject;

            yield return ChangeRoomAndWait(OtherRoom);
            yield return ChangeRoomAndWait(StartRoom);
            yield return ChangeRoomAndWait(OtherRoom);
            yield return ChangeRoomAndWait(StartRoom);

            AssertSingleInstanceUnder<GameStateController>(managersRoot);
            AssertSingleInstanceUnder<RoomNavigator>(managersRoot);
            AssertSingleInstanceUnder<InventoryManager>(managersRoot);
            AssertSingleInstanceUnder<GameSaveManager>(managersRoot);
            AssertSingleInstanceUnder<DialogueManager>(managersRoot);
            AssertSingleInstanceUnder<DialogueUIController>(managersRoot);
            AssertSingleInstanceUnder<ScreenFader>(managersRoot);
            AssertSingleInstanceUnder<EventSystem>(managersRoot);
        }

        [UnityTest]
        public IEnumerator CollectedItem_StaysCollectedAfterLeavingAndComingBack()
        {
            FindTheCollectable().Collect();
            Assert.AreEqual(1, InventoryManager.Instance.Items.Count, "A coleta não pôs o item no inventário.");
            ItemDataSO collectedItem = InventoryManager.Instance.Items[0];

            yield return ChangeRoomAndWait(OtherRoom);
            yield return ChangeRoomAndWait(StartRoom);

            Assert.IsFalse(FindTheCollectable().gameObject.activeSelf, "O coletável voltou ao mundo depois de recolhido.");
            Assert.AreEqual(1, InventoryManager.Instance.Items.Count, "O inventário mudou durante a ida e volta.");
            Assert.AreSame(collectedItem, InventoryManager.Instance.Items[0]);
        }

        [UnityTest]
        public IEnumerator RoomChange_BlocksInputUntilTheNewRoomIsShown()
        {
            CanvasGroup fade = Object.FindFirstObjectByType<ScreenFader>().GetComponent<CanvasGroup>();
            Assert.IsTrue(PlayerInputGate.IsEnabled, "O jogo deveria começar em exploração, com o input liberado.");

            Assert.IsTrue(RoomNavigator.Instance.ChangeRoom(OtherRoom, EntryPointId));
            AssertInputIsBlocked(fade);

            LogAssert.Expect(LogType.Warning, new Regex("já há uma troca de sala em curso"));
            Assert.IsFalse(RoomNavigator.Instance.ChangeRoom(StartRoom), "Uma segunda troca foi aceita no meio da primeira.");

            int framesInTransition = 0;
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o input voltar depois da troca", () =>
            {
                AssertInputIsBlocked(fade);
                framesInTransition++;
            });

            Assert.Greater(framesInTransition, 0, "A troca terminou sem passar por nenhum frame: o teste não provou o bloqueio.");
            Assert.AreEqual(OtherRoom, SceneManager.GetActiveScene().name, "O input voltou antes de a sala nova estar na tela.");
            Assert.IsFalse(fade.blocksRaycasts, "A interface continua barrada depois da troca.");
            Assert.AreEqual(0f, fade.alpha, "A tela continua escura depois da troca.");
        }

        private static void ResetInventory()
        {
            InventoryManager.Instance.ReplaceAll(Array.Empty<ItemDataSO>());
            InventoryManager.Instance.ReplaceConsumedWorldObjectIds(Array.Empty<string>());
        }

        private static IEnumerator ChangeRoomAndWait(string room)
        {
            Assert.IsTrue(RoomNavigator.Instance.ChangeRoom(room, EntryPointId), $"A troca para '{room}' foi recusada.");

            yield return WaitUntil(
                () => SceneManager.GetActiveScene().name == room && PlayerInputGate.IsEnabled,
                $"a sala '{room}' aparecer com o input liberado");
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string expectation, Action checkEachFrame = null)
        {
            float deadline = Time.realtimeSinceStartup + TransitionTimeoutSeconds;

            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Tempo esgotado esperando {expectation}. Sala ativa: '{SceneManager.GetActiveScene().name}', " +
                                $"input liberado: {PlayerInputGate.IsEnabled}.");

                checkEachFrame?.Invoke();
                yield return null;
            }
        }

        private static void AssertInputIsBlocked(CanvasGroup fade)
        {
            Assert.IsFalse(PlayerInputGate.IsEnabled, "O PlayerInputGate está aberto durante a troca.");
            Assert.IsFalse(PlayerInputGate.CanClickThisFrame, "O clique no mundo seria aceito durante a troca.");
            Assert.IsTrue(fade.blocksRaycasts, "O fade não está barrando os cliques da interface durante a troca.");
        }

        private static CollectableItemBehaviour FindTheCollectable()
        {
            CollectableItemBehaviour[] collectables = Object.FindObjectsByType<CollectableItemBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.AreEqual(1, collectables.Length, $"Esperava um coletável na sala '{SceneManager.GetActiveScene().name}'.");
            return collectables[0];
        }

        private static void AssertSingleInstanceUnder<T>(GameObject managersRoot) where T : Component
        {
            T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.AreEqual(1, found.Length, $"Esperava 1 {typeof(T).Name}, achou {found.Length}.");
            Assert.AreSame(managersRoot, found[0].transform.root.gameObject,
                $"O {typeof(T).Name} não está no Managers.prefab original: foi recriado ou veio de uma cena.");
        }
    }
}
