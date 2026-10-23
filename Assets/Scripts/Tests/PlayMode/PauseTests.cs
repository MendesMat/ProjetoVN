using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjetoVN.Core.State;
using ProjetoVN.Dialogue.Logic;
using ProjetoVN.GameFlow.Menu;
using ProjetoVN.GameFlow.Persistence;
using ProjetoVN.GameFlow.Rooms;
using ProjetoVN.Inventory;
using ProjetoVN.PointNClick;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ProjetoVN.Tests.PlayMode
{
    public class PauseTests
    {
        private const string StartRoom = "[Teste] Mecanicas";
        private const string OtherRoom = "[Teste] Sala B";
        private const string MenuScene = "Menu";
        private const string EntryPointId = "janela";
        private const string OneLineConversation = "peguei_chave";
        private const string StoryVariable = "$sessao_anterior";
        private const string PauseWindowName = "Pause Window";
        private const string ConfirmWindowName = "Confirm Return Window";
        private const string SavedLabelPath = "Pause Window/Panel/Saved Label";
        private const string SaveButtonPath = "Pause Window/Panel/Button Save";
        private const string ReturnButtonPath = "Pause Window/Panel/Button Return";
        private const string BackButtonPath = "Confirm Return Window/Panel/Button Back";
        private const string ContinueButtonName = "Button Continue";
        private const string BackButtonName = "Button Back";
        private const float WaitTimeoutSeconds = 10f;

        private PauseMenu _pauseMenu;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim de uma troca de sala pendente");

            Assert.IsNotNull(RoomNavigator.Instance,
                "Não há RoomNavigator: o Managers.prefab não foi criado pelo ManagersBootstrap ou foi destruído.");
            ResetInventory();

            yield return SceneManager.LoadSceneAsync(StartRoom);
            yield return null;

            _pauseMenu = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
            Assert.IsNotNull(_pauseMenu, "Não há PauseMenu: o GameUI.prefab não tem o Canvas_Pause.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _pauseMenu.Resume();
            DialogueManager.Instance.StopDialogue();
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim de uma troca de sala pendente");

            Assert.AreEqual(1f, Time.timeScale, "O tempo do jogo ficou congelado depois do teste.");
            StoryState.ClearAll();
            if (SceneManager.GetActiveScene().name == MenuScene)
            {
                yield return SceneManager.LoadSceneAsync(StartRoom);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Pause_InExploration_BlocksTheWorldAndFreezesTime()
        {
            _pauseMenu.TogglePause();

            Assert.IsFalse(PlayerInputGate.IsEnabled, "A pausa não fechou o input do mundo.");
            Assert.AreEqual(0f, Time.timeScale, "A pausa não congelou o tempo.");
            Assert.IsTrue(DialogueManager.Instance.IsPaused, "A pausa não bloqueou o diálogo.");
            Assert.IsTrue(PauseWindow().activeSelf, "A janela de pausa não abriu.");
            Assert.AreEqual(ContinueButtonName, EventSystem.current.currentSelectedGameObject.name,
                "A pausa não abriu com o Continuar selecionado.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Resume_GivesTheWorldAndTheTimeBack()
        {
            _pauseMenu.TogglePause();

            _pauseMenu.TogglePause();

            Assert.IsTrue(PlayerInputGate.IsEnabled, "Retomar não devolveu o input do mundo.");
            Assert.AreEqual(1f, Time.timeScale, "Retomar não devolveu o tempo.");
            Assert.IsFalse(DialogueManager.Instance.IsPaused, "Retomar não liberou o diálogo.");
            Assert.IsFalse(PauseWindow().activeSelf, "A janela de pausa continua aberta.");
            Assert.IsNull(EventSystem.current.currentSelectedGameObject, "Um botão da pausa continua selecionado, escondido.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pause_DuringAConversation_DoesNotAdvanceTheLine()
        {
            Assert.IsTrue(DialogueManager.Instance.StartDialogue(OneLineConversation));
            yield return null;
            _pauseMenu.TogglePause();

            DialogueManager.Instance.AdvanceDialogue();
            yield return null;
            DialogueManager.Instance.AdvanceDialogue();
            yield return null;
            DialogueManager.Instance.MakeChoice(0);

            Assert.IsTrue(DialogueManager.Instance.IsDialogueActive(), "A fala avançou com a pausa aberta.");

            _pauseMenu.TogglePause();
            yield return AdvanceUntilTheConversationEnds();

            Assert.IsTrue(PlayerInputGate.IsEnabled, "O jogo não voltou à exploração depois da conversa.");
        }

        [UnityTest]
        public IEnumerator Resume_DuringAConversation_KeepsTheWorldBlocked()
        {
            Assert.IsTrue(DialogueManager.Instance.StartDialogue(OneLineConversation));
            yield return null;
            _pauseMenu.TogglePause();

            _pauseMenu.TogglePause();

            Assert.IsTrue(DialogueManager.Instance.IsDialogueActive());
            Assert.IsFalse(PlayerInputGate.IsEnabled, "Retomar sobre a conversa liberou o mundo.");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(DialogueManager.Instance.IsPaused);
        }

        [UnityTest]
        public IEnumerator Pause_IsRefusedDuringARoomChange()
        {
            Assert.IsTrue(RoomNavigator.Instance.ChangeRoom(OtherRoom, EntryPointId));

            _pauseMenu.TogglePause();

            Assert.AreEqual(1f, Time.timeScale, "A pausa abriu no meio da troca de sala.");
            Assert.IsFalse(PauseWindow().activeSelf);
            Assert.IsFalse(DialogueManager.Instance.IsPaused);
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim da troca de sala");
            Assert.IsFalse(PauseWindow().activeSelf, "A pausa abriu quando a troca de sala acabou.");
        }

        [UnityTest]
        public IEnumerator SaveGame_FromThePause_WritesTheRoomAndTheEntryPoint()
        {
            FindTheCollectable().Collect();
            ItemDataSO collectedItem = InventoryManager.Instance.Items[0];
            yield return ChangeRoomAndWait(OtherRoom);
            ResetInventory();
            _pauseMenu.TogglePause();

            _pauseMenu.SaveGame();

            InventoryManager.Instance.ReplaceAll(new[] { collectedItem });
            _pauseMenu.Resume();
            Assert.IsTrue(GameSaveManager.Instance.Load(), "O Load foi recusado: não havia save.");
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim da troca do Load");

            Assert.AreEqual(OtherRoom, SceneManager.GetActiveScene().name, "O save manual não guardou a sala.");
            Assert.AreEqual(EntryPointId, RoomNavigator.Instance.CurrentEntryPointId, "O save manual não guardou o ponto de entrada.");
            Assert.IsEmpty(InventoryManager.Instance.Items, "O save da pausa não gravou o inventário do momento (o do save automático voltou).");
        }

        [UnityTest]
        public IEnumerator SaveGame_ShowsTheSavedMessageOnlyWhenItSaved()
        {
            _pauseMenu.TogglePause();
            Assert.IsTrue(SaveButton().interactable, "O Salvar está desabilitado na exploração.");
            Assert.IsFalse(SavedLabel().activeSelf);

            _pauseMenu.SaveGame();

            Assert.IsTrue(SavedLabel().activeSelf, "Salvar não mostrou a mensagem.");
            _pauseMenu.TogglePause();
            _pauseMenu.TogglePause();
            Assert.IsFalse(SavedLabel().activeSelf, "A mensagem de salvo sobrou na pausa seguinte.");

            _pauseMenu.TogglePause();
            Assert.IsTrue(DialogueManager.Instance.StartDialogue(OneLineConversation));
            yield return null;
            _pauseMenu.TogglePause();
            Assert.IsFalse(SaveButton().interactable, "O Salvar está habilitado durante a conversa.");

            LogAssert.Expect(LogType.Warning, new Regex("Save recusado: há uma conversa aberta"));
            _pauseMenu.SaveGame();

            Assert.IsFalse(SavedLabel().activeSelf, "A mensagem apareceu com o salvar recusado.");
        }

        [UnityTest]
        public IEnumerator ReturnButton_AsksForConfirmationAndBackGoesToThePause()
        {
            _pauseMenu.TogglePause();

            ButtonAt(ReturnButtonPath).onClick.Invoke();

            Assert.IsTrue(WindowNamed(ConfirmWindowName).activeSelf, "O Voltar ao menu não abriu a confirmação.");
            Assert.IsFalse(PauseWindow().activeSelf, "A pausa continua à mostra atrás da confirmação.");
            Assert.AreEqual(BackButtonName, EventSystem.current.currentSelectedGameObject.name,
                "A confirmação não abriu com o VOLTAR selecionado.");

            ButtonAt(BackButtonPath).onClick.Invoke();

            Assert.IsTrue(PauseWindow().activeSelf, "O VOLTAR da confirmação não devolveu à pausa.");
            Assert.IsFalse(WindowNamed(ConfirmWindowName).activeSelf);
            Assert.AreEqual(ContinueButtonName, EventSystem.current.currentSelectedGameObject.name,
                "A pausa não voltou com o CONTINUAR selecionado.");
            Assert.AreEqual(StartRoom, SceneManager.GetActiveScene().name, "Voltar da confirmação saiu da sala.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Resume_WithTheConfirmationOpen_ClosesBothWindows()
        {
            _pauseMenu.TogglePause();
            ButtonAt(ReturnButtonPath).onClick.Invoke();

            _pauseMenu.TogglePause();

            Assert.IsFalse(WindowNamed(ConfirmWindowName).activeSelf, "A confirmação continua aberta depois de retomar.");
            Assert.IsFalse(PauseWindow().activeSelf, "A pausa continua aberta depois de retomar.");
            Assert.IsTrue(PlayerInputGate.IsEnabled);
            Assert.AreEqual(1f, Time.timeScale);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReturnToMainMenu_DoesNotOverwriteTheSave()
        {
            Assert.IsTrue(GameSaveManager.Instance.Save(), "O Save foi recusado.");
            _pauseMenu.TogglePause();

            _pauseMenu.ReturnToMainMenu();
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == MenuScene && PlayerInputGate.IsEnabled,
                "o menu principal aparecer");

            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(DialogueManager.Instance.IsPaused);
            Assert.IsFalse(GameUIVisibility.Instance.IsShown, "A interface de jogo ficou à mostra no menu.");

            Assert.IsTrue(GameSaveManager.Instance.Load(), "O Load foi recusado.");
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim da troca do Load");
            Assert.AreEqual(StartRoom, SceneManager.GetActiveScene().name,
                "Voltar ao menu gravou o menu como sala: o Continuar não abre a sala do último save.");
        }

        [UnityTest]
        public IEnumerator ReturnToMainMenu_DuringAConversation_EndsIt()
        {
            Assert.IsTrue(DialogueManager.Instance.StartDialogue(OneLineConversation));
            yield return null;
            _pauseMenu.TogglePause();

            _pauseMenu.ReturnToMainMenu();
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == MenuScene && PlayerInputGate.IsEnabled,
                "o menu principal aparecer");

            Assert.IsFalse(DialogueManager.Instance.IsDialogueActive(), "A conversa continua aberta no menu.");
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator NewGame_AfterReturningToTheMenu_InheritsNothing()
        {
            FindTheCollectable().Collect();
            StoryState.SetBool(StoryVariable, true);
            _pauseMenu.TogglePause();
            _pauseMenu.ReturnToMainMenu();
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == MenuScene && PlayerInputGate.IsEnabled,
                "o menu principal aparecer");

            Assert.IsTrue(GameSaveManager.Instance.StartNewGame(StartRoom, null), "O StartNewGame foi recusado.");
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == StartRoom && PlayerInputGate.IsEnabled,
                "a sala inicial do novo jogo aparecer");

            Assert.IsEmpty(InventoryManager.Instance.Items, "O inventário não está vazio.");
            Assert.IsEmpty(InventoryManager.Instance.ConsumedWorldObjectIds, "Há objetos consumidos.");
            Assert.AreEqual(0, StoryState.Bools.Count, "O StoryState não está vazio.");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(DialogueManager.Instance.IsPaused);
            Assert.IsTrue(PlayerInputGate.IsEnabled);
        }

        [UnityTest]
        public IEnumerator Pause_IsRefusedInTheMainMenu()
        {
            yield return SceneManager.LoadSceneAsync(MenuScene);
            yield return null;

            _pauseMenu.TogglePause();

            Assert.AreEqual(1f, Time.timeScale, "A pausa abriu no menu principal.");
            Assert.IsFalse(PauseWindow().activeSelf);
            Assert.IsFalse(DialogueManager.Instance.IsPaused);
        }

        private GameObject PauseWindow() => WindowNamed(PauseWindowName);

        private GameObject SavedLabel() => _pauseMenu.transform.Find(SavedLabelPath).gameObject;

        private Button SaveButton() => ButtonAt(SaveButtonPath);

        private Button ButtonAt(string path) => _pauseMenu.transform.Find(path).GetComponent<Button>();

        private GameObject WindowNamed(string name) => _pauseMenu.transform.Find(name).gameObject;

        private static IEnumerator AdvanceUntilTheConversationEnds()
        {
            float deadline = Time.realtimeSinceStartup + WaitTimeoutSeconds;
            while (DialogueManager.Instance.IsDialogueActive())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail("A conversa não terminou depois de retomar a pausa.");

                DialogueManager.Instance.AdvanceDialogue();
                yield return null;
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

        private static CollectableItemBehaviour FindTheCollectable()
        {
            CollectableItemBehaviour[] collectables = Object.FindObjectsByType<CollectableItemBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.AreEqual(1, collectables.Length, $"Esperava um coletável na sala '{SceneManager.GetActiveScene().name}'.");
            return collectables[0];
        }
    }
}
