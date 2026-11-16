using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjetoVN.Core.State;
using ProjetoVN.Dialogue.Logic;
using ProjetoVN.GameFlow.Persistence;
using ProjetoVN.GameFlow.Rooms;
using ProjetoVN.Inventory;
using ProjetoVN.PointNClick;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ProjetoVN.Tests.PlayMode
{
    /// <summary>
    /// Caracteriza a conversa de chegada na primeira cena <c>[Fatia]</c> da suíte (autorizada pelo programador na #127):
    /// o prólogo abre sozinho no Novo Jogo, só depois de a tela clarear e de o jogo sair da transição.
    /// </summary>
    public class RoomArrivalTests
    {
        private const string StartRoom = "[Teste] Mecanicas";
        private const string ArrivalRoom = "[Fatia] Onibus";
        private const string ArrivalConversation = "onibus_viagem";
        private const string PrologueDone = "$prologo_concluido";
        private const float WaitTimeoutSeconds = 10f;
        private const int FramesWatchedAfterOpening = 5;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim de uma troca de sala pendente");

            Assert.IsNotNull(RoomNavigator.Instance,
                "Não há RoomNavigator: o Managers.prefab não foi criado pelo ManagersBootstrap ou foi destruído.");
            InventoryManager.Instance.ReplaceAll(Array.Empty<ItemDataSO>());
            InventoryManager.Instance.ReplaceConsumedWorldObjectIds(Array.Empty<string>());

            yield return SceneManager.LoadSceneAsync(StartRoom);
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
        public IEnumerator NewGame_OpensThePrologueOnlyAfterTheScreenClears()
        {
            CanvasGroup fade = Object.FindFirstObjectByType<ScreenFader>().GetComponent<CanvasGroup>();

            Assert.IsTrue(GameSaveManager.Instance.StartNewGame(ArrivalRoom, null), "O StartNewGame foi recusado.");
            yield return WaitUntil(
                () => DialogueManager.Instance.IsDialogueActive(), "a conversa de chegada abrir",
                () => Assert.IsFalse(DialogueManager.Instance.IsDialogueActive() && IsFading(fade),
                    "A conversa de chegada abriu antes de a tela clarear."));

            Assert.AreEqual(ArrivalRoom, SceneManager.GetActiveScene().name);
            Assert.IsFalse(IsFading(fade), "A conversa abriu com o fade ainda ativo.");
            yield return WaitUntil(() => DialogueManager.Instance.CurrentNodeName == ArrivalConversation,
                "o prólogo começar (o nó de chegada chama onibus_viagem)");
            yield return WatchTheGameStayInTheConversation();
        }

        [UnityTest]
        public IEnumerator NewGame_SavesTheArrivalBeforeTheConversation_AndRefusesToSaveDuringIt()
        {
            Assert.IsTrue(GameSaveManager.Instance.StartNewGame(ArrivalRoom, null), "O StartNewGame foi recusado.");
            yield return WaitUntil(() => DialogueManager.Instance.IsDialogueActive(), "a conversa de chegada abrir");

            GameState saved = ReadTheSaveFile();
            Assert.AreEqual(ArrivalRoom, saved.currentScene, "O save da chegada não guardou a sala nova.");
            Assert.IsFalse(saved.storyBools.Any(entry => entry.name == PrologueDone && entry.value),
                "O save da chegada já traz o prólogo concluído: foi gravado depois da conversa.");

            LogAssert.Expect(LogType.Warning, new Regex("Save recusado"));
            Assert.IsFalse(GameSaveManager.Instance.Save(), "O Save foi aceito com a conversa de chegada aberta.");
        }

        [UnityTest]
        public IEnumerator ArrivalConversation_WhenItEnds_ReturnsTheGameToExploration()
        {
            Assert.IsTrue(GameSaveManager.Instance.StartNewGame(ArrivalRoom, null), "O StartNewGame foi recusado.");
            yield return WaitUntil(() => DialogueManager.Instance.IsDialogueActive(), "a conversa de chegada abrir");

            DialogueManager.Instance.StopDialogue();
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o jogo voltar à exploração");

            Assert.IsFalse(DialogueManager.Instance.IsDialogueActive(), "A conversa de chegada continuou aberta.");
        }

        private static IEnumerator WatchTheGameStayInTheConversation()
        {
            for (int frame = 0; frame < FramesWatchedAfterOpening; frame++)
            {
                Assert.IsTrue(DialogueManager.Instance.IsDialogueActive(), "A conversa de chegada se fechou sozinha.");
                Assert.IsFalse(PlayerInputGate.IsEnabled,
                    "O jogo voltou à exploração com a conversa de chegada aberta (a abertura veio antes do fim da transição).");
                yield return null;
            }
        }

        private static bool IsFading(CanvasGroup fade) => fade.alpha > 0f || fade.blocksRaycasts;

        private static GameState ReadTheSaveFile()
        {
            string path = Path.Combine(Application.persistentDataPath, "savegame.json");
            Assert.IsTrue(GameStateJson.TryRead(File.ReadAllText(path), out GameState state), "O save da chegada está ilegível.");
            return state;
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string expectation, Action checkEachFrame = null)
        {
            float deadline = Time.realtimeSinceStartup + WaitTimeoutSeconds;

            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Tempo esgotado esperando {expectation}. Sala ativa: '{SceneManager.GetActiveScene().name}', " +
                                $"input liberado: {PlayerInputGate.IsEnabled}.");

                checkEachFrame?.Invoke();
                yield return null;
            }

            checkEachFrame?.Invoke();
        }
    }
}
