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
    [TestFixture(LowFrameRate)]
    [TestFixture(UnlimitedFrameRate)]
    public class RoomChangeTests
    {
        private const int LowFrameRate = 10;
        private const int UnlimitedFrameRate = -1;
        private const string StartRoom = "[Teste] Mecanicas";
        private const string OtherRoom = "[Teste] Sala B";
        private const string EntryPointId = "janela";
        private const float TransitionTimeoutSeconds = 10f;
        private const float LongFrameSeconds = 1f;
        private const float LongFrameDetectionRatio = 0.9f;
        private const float FrameCountWindowSeconds = 1f;
        private const int LowFrameRateMinFrames = 5;
        private const int LowFrameRateMaxFrames = 15;
        private const int UnlimitedFrameRateMinFrames = 20;

        private readonly int _targetFrameRate;
        private int _originalTargetFrameRate;
        private int _originalVSyncCount;

        public RoomChangeTests(int targetFrameRate)
        {
            _targetFrameRate = targetFrameRate;
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim de uma troca de sala pendente");

            Assert.IsNotNull(RoomNavigator.Instance,
                "Não há RoomNavigator: o Managers.prefab não foi criado pelo ManagersBootstrap ou foi destruído.");
            ResetInventory();

            yield return SceneManager.LoadSceneAsync(StartRoom);
            yield return null;

            SaveOriginalFrameRateAndApplyFixtureFrameRate();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            RestoreFrameRate();
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim de uma troca de sala pendente");
        }

        [UnityTest]
        public IEnumerator FrameRate_IsTheOneThisFixtureAsked()
        {
            int frames = 0;
            float end = Time.realtimeSinceStartup + FrameCountWindowSeconds;
            while (Time.realtimeSinceStartup < end)
            {
                frames++;
                yield return null;
            }

            if (_targetFrameRate == LowFrameRate)
            {
                Assert.That(frames, Is.InRange(LowFrameRateMinFrames, LowFrameRateMaxFrames),
                    $"Pedido {LowFrameRate} FPS, mas passaram {frames} quadros em {FrameCountWindowSeconds} s.");
                yield break;
            }

            Assert.Greater(frames, UnlimitedFrameRateMinFrames,
                $"Pedido FPS sem limite, mas passaram só {frames} quadros em {FrameCountWindowSeconds} s.");
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

            int callFrame = Time.frameCount;
            Assert.IsTrue(RoomNavigator.Instance.ChangeRoom(OtherRoom, EntryPointId));
            AssertInputIsBlocked(fade);

            LogAssert.Expect(LogType.Warning, new Regex("já há uma troca de sala em curso"));
            Assert.IsFalse(RoomNavigator.Instance.ChangeRoom(StartRoom), "Uma segunda troca foi aceita no meio da primeira.");

            int blockedFramesAfterCall = 0;
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o input voltar depois da troca", () =>
            {
                AssertInputIsBlocked(fade);
                if (Time.frameCount > callFrame)
                    blockedFramesAfterCall++;
            });

            Assert.Greater(blockedFramesAfterCall, 0,
                "Nenhum frame depois da chamada foi conferido com o input bloqueado: o teste não provou o bloqueio durante a troca.");
            Assert.AreEqual(OtherRoom, SceneManager.GetActiveScene().name, "O input voltou antes de a sala nova estar na tela.");
            Assert.IsFalse(fade.blocksRaycasts, "A interface continua barrada depois da troca.");
            Assert.AreEqual(0f, fade.alpha, "A tela continua escura depois da troca.");
        }

        [UnityTest]
        public IEnumerator RoomChange_SurvivesALongFrameWhileFadingOutAndIn()
        {
            CanvasGroup fade = Object.FindFirstObjectByType<ScreenFader>().GetComponent<CanvasGroup>();
            int freezes = 0;
            int longFramesSeen = 0;

            void FreezeOnceWhenTheNewRoomAppears()
            {
                if (freezes != 1 || SceneManager.GetActiveScene().name != OtherRoom)
                    return;

                FreezeMainThread(LongFrameSeconds);
                freezes++;
            }

            Assert.IsTrue(RoomNavigator.Instance.ChangeRoom(OtherRoom, EntryPointId));
            FreezeMainThread(LongFrameSeconds);
            freezes++;

            yield return WaitUntil(() =>
            {
                if (Time.unscaledDeltaTime >= LongFrameDetectionRatio * LongFrameSeconds)
                    longFramesSeen++;
                return PlayerInputGate.IsEnabled;
            }, "o input voltar depois da troca com quadros longos", () =>
            {
                AssertInputIsBlocked(fade);
                FreezeOnceWhenTheNewRoomAppears();
            });

            Assert.AreEqual(2, freezes, "A sala nova só apareceu depois do clarear: o travamento do clarear não aconteceu.");
            Assert.GreaterOrEqual(longFramesSeen, 2, "O jogo não viu os dois quadros longos: o travamento não chegou ao tempo do fade.");
            Assert.AreEqual(OtherRoom, SceneManager.GetActiveScene().name, "A troca não terminou na sala nova.");
            Assert.IsFalse(fade.blocksRaycasts, "A interface continua barrada depois da troca.");
            Assert.AreEqual(0f, fade.alpha, "A tela continua escura depois da troca.");
        }

        private void SaveOriginalFrameRateAndApplyFixtureFrameRate()
        {
            _originalTargetFrameRate = Application.targetFrameRate;
            _originalVSyncCount = QualitySettings.vSyncCount;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _targetFrameRate;
        }

        private void RestoreFrameRate()
        {
            QualitySettings.vSyncCount = _originalVSyncCount;
            Application.targetFrameRate = _originalTargetFrameRate;
        }

        private static void FreezeMainThread(float seconds)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.Elapsed.TotalSeconds < seconds)
            {
            }
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
