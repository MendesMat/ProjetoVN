using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjetoVN.Audio;
using ProjetoVN.Core.State;
using ProjetoVN.Dialogue.Logic;
using ProjetoVN.GameFlow;
using ProjetoVN.GameFlow.Menu;
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
    public class SoundTests
    {
        private const string StartRoom = "[Teste] Mecanicas";
        private const string OtherRoom = "[Teste] Sala B";
        private const string EntryPointId = "janela";
        private const string StartRoomMusic = "teste_musica_a";
        private const string OtherRoomMusic = "teste_musica_b";
        private const string AmbienceId = "onibus_estrada";
        private const string EffectId = "freada_batida";
        private const string OneLineConversation = "peguei_chave";
        private const string ConversationWithOptions = "luna_cheguei_cedo";
        private const string ChoiceButtonName = "ChoiceButton0";
        private const string AmbienceChildName = "Ambience";
        private const string ContinueButtonPath = "Pause Window/Panel/Button Continue";
        private const string LockedDoorName = "Door_PortaTrancada";
        private const string HoverSoundName = "interface_hover";
        private const string ClickSoundName = "interface_clique";
        private const string CollectSoundName = "item_coletar";
        private const string LockedDoorSoundName = "porta_trancada";
        private const string OpeningDoorSoundName = "porta_abrindo";
        private const int SampleRate = 44100;
        private const float StubSeconds = 4f;
        private const float Tolerance = 1e-3f;
        private const float WaitTimeoutSeconds = 10f;
        private const float LongerThanAnAudioBlockSeconds = 0.1f;
        private const float LineSettleSeconds = 0.5f;

        private PauseMenu _pauseMenu;
        private AudioClip _stubAmbience;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim de uma troca de sala pendente");

            Assert.IsNotNull(AudioManager.Instance,
                "Não há AudioManager: o Managers.prefab não o traz ou não foi criado pelo ManagersBootstrap.");
            Assert.IsNotNull(SoundScriptActions.Instance,
                "Não há SoundScriptActions: o Managers.prefab não o traz ou não foi criado pelo ManagersBootstrap.");
            ResetInventory();
            StoryState.ClearAll();

            yield return SceneManager.LoadSceneAsync(StartRoom);
            yield return null;

            _pauseMenu = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
            Assert.IsNotNull(_pauseMenu, "Não há PauseMenu: o GameUI.prefab não tem o Canvas_Pause.");
            yield return WaitUntil(() => AmbienceSources().All(source => !source.isPlaying), "o ambiente da sala anterior se calar");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _pauseMenu.Resume();
            DialogueManager.Instance.StopDialogue();
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim de uma troca de sala pendente");

            AudioManager.Instance.PlayAmbience(null);
            yield return WaitUntil(() => AmbienceSources().All(source => !source.isPlaying), "o ambiente se calar");
            if (_stubAmbience != null)
            {
                Object.Destroy(_stubAmbience);
                _stubAmbience = null;
            }

            SoundScriptActions.PlayMusic(StartRoomMusic);
            Assert.AreEqual(1f, Time.timeScale, "O tempo do jogo ficou congelado depois do teste.");
            StoryState.ClearAll();
        }

        [UnityTest]
        public IEnumerator PlayAmbience_FadesInOnALoopingSource_AndTheSameClipDoesNotRestartIt()
        {
            AudioClip ambience = NewStubAmbience();
            AudioManager.Instance.PlayAmbience(ambience);

            yield return WaitUntil(() => PlayingAmbienceSources().Length == 1
                                         && Mathf.Approximately(PlayingAmbienceSources()[0].volume, 1f),
                "o ambiente terminar de entrar");
            AudioSource source = PlayingAmbienceSources().Single();
            Assert.IsTrue(source.loop, "A fonte do ambiente não está em loop.");
            Assert.AreEqual("SFX", source.outputAudioMixerGroup.name, "O ambiente não sai no grupo SFX do mixer.");

            AudioManager.Instance.PlayAmbience(ambience);
            yield return new WaitForSecondsRealtime(LongerThanAnAudioBlockSeconds);

            Assert.AreSame(source, PlayingAmbienceSources().Single(), "O mesmo ambiente trocou de fonte ou ganhou uma segunda.");
            Assert.AreEqual(1f, source.volume, Tolerance);
        }

        [UnityTest]
        public IEnumerator PlayAmbience_Null_FadesOutAndStops()
        {
            AudioManager.Instance.PlayAmbience(NewStubAmbience());
            yield return WaitUntil(() => PlayingAmbienceSources().Length == 1
                                         && Mathf.Approximately(PlayingAmbienceSources()[0].volume, 1f),
                "o ambiente terminar de entrar");
            AudioSource source = PlayingAmbienceSources().Single();

            AudioManager.Instance.PlayAmbience(null);
            yield return WaitUntil(() => source.volume < 1f && source.volume > 0f, "o meio da saída suave");

            Assert.IsNull(AudioManager.Instance.CurrentAmbience);
            yield return WaitUntil(() => !source.isPlaying, "o ambiente parar");
            Assert.AreEqual(0f, source.volume, Tolerance);
        }

        [UnityTest]
        public IEnumerator ChangingRoom_SilencesTheAmbience_AndKeepsTheMusicOfTheNewRoom()
        {
            SoundScriptActions.PlayAmbience(AmbienceId);
            Assert.AreEqual(AmbienceId, AudioManager.Instance.CurrentAmbience.name);

            Assert.IsTrue(RoomNavigator.Instance.ChangeRoom(OtherRoom, EntryPointId), "A troca de sala foi recusada.");
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == OtherRoom && PlayerInputGate.IsEnabled,
                "a sala nova aparecer");

            Assert.IsNull(AudioManager.Instance.CurrentAmbience, "O ambiente atravessou a troca de sala.");
            Assert.AreEqual(OtherRoomMusic, AudioManager.Instance.CurrentMusic.name, "A música da sala nova não é a atual.");
            yield return WaitUntil(() => PlayingAmbienceSources().Length == 0, "o ambiente se calar");
        }

        [UnityTest]
        public IEnumerator ScriptCommands_ReachTheAudioManager_ById()
        {
            SoundScriptActions.PlayAmbience(AmbienceId);
            SoundScriptActions.PlayMusic(OtherRoomMusic);
            int effectsBefore = AudioManager.Instance.EffectCount;
            SoundScriptActions.PlayEffect(EffectId);

            Assert.AreEqual(AmbienceId, AudioManager.Instance.CurrentAmbience.name);
            Assert.AreEqual(OtherRoomMusic, AudioManager.Instance.CurrentMusic.name);
            Assert.AreEqual(EffectId, AudioManager.Instance.LastEffect.name);
            Assert.AreEqual(effectsBefore + 1, AudioManager.Instance.EffectCount);

            SoundScriptActions.StopAmbience();
            Assert.IsNull(AudioManager.Instance.CurrentAmbience);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ScriptCommands_WithAnUnknownId_LogAnErrorAndChangeNothing()
        {
            AudioClip musicBefore = AudioManager.Instance.CurrentMusic;
            int effectsBefore = AudioManager.Instance.EffectCount;
            LogAssert.Expect(LogType.Error, new Regex("tocar_efeito nao_existe.*não existe.*nó '\\?'"));
            LogAssert.Expect(LogType.Error, new Regex("tocar_musica nao_existe.*não existe"));
            LogAssert.Expect(LogType.Error, new Regex("tocar_ambiente nao_existe.*não existe"));

            SoundScriptActions.PlayEffect("nao_existe");
            SoundScriptActions.PlayMusic("nao_existe");
            SoundScriptActions.PlayAmbience("nao_existe");

            Assert.AreEqual(effectsBefore, AudioManager.Instance.EffectCount);
            Assert.AreSame(musicBefore, AudioManager.Instance.CurrentMusic);
            Assert.IsNull(AudioManager.Instance.CurrentAmbience);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseButtons_SoundWithTheGameFrozen()
        {
            _pauseMenu.TogglePause();
            Assert.AreEqual(0f, Time.timeScale, "A pausa não congelou o tempo.");
            Button continueButton = ContinueButton();
            int effectsBefore = AudioManager.Instance.EffectCount;

            ExecuteEvents.Execute(continueButton.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);

            Assert.AreEqual(effectsBefore + 1, AudioManager.Instance.EffectCount, "Passar o mouse no botão da pausa não soou.");
            Assert.AreEqual(HoverSoundName, AudioManager.Instance.LastEffect.name);

            continueButton.onClick.Invoke();

            Assert.AreEqual(effectsBefore + 2, AudioManager.Instance.EffectCount, "O clique no botão da pausa não soou.");
            Assert.AreEqual(ClickSoundName, AudioManager.Instance.LastEffect.name);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledButton_DoesNotSoundOnHover()
        {
            _pauseMenu.TogglePause();
            Button continueButton = ContinueButton();
            int effectsBefore = AudioManager.Instance.EffectCount;
            continueButton.interactable = false;
            try
            {
                ExecuteEvents.Execute(continueButton.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            }
            finally
            {
                continueButton.interactable = true;
            }

            Assert.AreEqual(effectsBefore, AudioManager.Instance.EffectCount, "Um botão desabilitado soou ao passar o mouse.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator AdvancingALine_Sounds_ButNotWhilePaused()
        {
            Assert.IsTrue(DialogueManager.Instance.StartDialogue(OneLineConversation));
            yield return AdvanceUntilItSounds("completar a fala");
            yield return new WaitForSecondsRealtime(LineSettleSeconds);
            Assert.IsTrue(DialogueManager.Instance.IsDialogueActive(), "A conversa de uma linha terminou antes de passar a fala.");
            Assert.AreEqual("fala_avancar", AudioManager.Instance.LastEffect.name);

            _pauseMenu.TogglePause();
            int effectsBefore = AudioManager.Instance.EffectCount;
            DialogueManager.Instance.AdvanceDialogue();
            Assert.AreEqual(effectsBefore, AudioManager.Instance.EffectCount, "O avanço de fala soou com a pausa aberta.");

            _pauseMenu.Resume();
            DialogueManager.Instance.AdvanceDialogue();
            Assert.AreEqual(effectsBefore + 1, AudioManager.Instance.EffectCount, "Passar a fala não soou depois de retomar.");
        }

        [UnityTest]
        public IEnumerator AdvancingWithOptionsOnScreen_DoesNotSound()
        {
            Assert.IsTrue(DialogueManager.Instance.StartDialogue(ConversationWithOptions));
            yield return WaitUntil(() => GameObject.Find(ChoiceButtonName) != null, "as opções aparecerem");
            int effectsBefore = AudioManager.Instance.EffectCount;

            DialogueManager.Instance.AdvanceDialogue();
            yield return new WaitForSecondsRealtime(LongerThanAnAudioBlockSeconds);

            Assert.AreEqual(effectsBefore, AudioManager.Instance.EffectCount, "O avanço de fala soou com as opções na tela.");
            Assert.IsNotNull(GameObject.Find(ChoiceButtonName), "O avanço fechou as opções.");
        }

        [UnityTest]
        public IEnumerator CollectingAnItem_Sounds()
        {
            int effectsBefore = AudioManager.Instance.EffectCount;

            FindTheCollectable().Collect();

            Assert.AreEqual(effectsBefore + 1, AudioManager.Instance.EffectCount, "Coletar o item não soou.");
            Assert.AreEqual(CollectSoundName, AudioManager.Instance.LastEffect.name);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LockedDoor_SoundsLockedWithoutTheKey_AndOpeningWithIt()
        {
            LockedActionBehaviour door = GameObject.Find(LockedDoorName).GetComponent<LockedActionBehaviour>();

            door.Interact();

            Assert.AreEqual(LockedDoorSoundName, AudioManager.Instance.LastEffect.name, "A porta trancada não soou.");
            DialogueManager.Instance.StopDialogue();

            FindTheCollectable().Collect();
            door.Interact();

            Assert.AreEqual(OpeningDoorSoundName, AudioManager.Instance.LastEffect.name, "A porta abrindo não soou.");
            yield return null;
        }

        private static IEnumerator AdvanceUntilItSounds(string expectation)
        {
            int effectsBefore = AudioManager.Instance.EffectCount;
            float deadline = Time.realtimeSinceStartup + WaitTimeoutSeconds;
            while (AudioManager.Instance.EffectCount == effectsBefore)
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Tempo esgotado esperando o som de {expectation}.");

                DialogueManager.Instance.AdvanceDialogue();
                yield return null;
            }

            Assert.AreEqual(effectsBefore + 1, AudioManager.Instance.EffectCount, "Um avanço tocou mais de um som.");
        }

        private Button ContinueButton() => _pauseMenu.transform.Find(ContinueButtonPath).GetComponent<Button>();

        private static AudioSource[] AmbienceSources() =>
            AudioManager.Instance.transform.Find(AmbienceChildName).GetComponentsInChildren<AudioSource>(true);

        private static AudioSource[] PlayingAmbienceSources() => AmbienceSources().Where(source => source.isPlaying).ToArray();

        private AudioClip NewStubAmbience()
        {
            int samples = (int)(SampleRate * StubSeconds);
            _stubAmbience = AudioClip.Create("ambiente_provisorio_do_teste", samples, 1, SampleRate, false);
            _stubAmbience.SetData(new float[samples], 0);
            return _stubAmbience;
        }

        private static void ResetInventory()
        {
            InventoryManager.Instance.ReplaceAll(Array.Empty<ItemDataSO>());
            InventoryManager.Instance.ReplaceConsumedWorldObjectIds(Array.Empty<string>());
        }

        private static CollectableItemBehaviour FindTheCollectable()
        {
            CollectableItemBehaviour[] collectables = Object.FindObjectsByType<CollectableItemBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.AreEqual(1, collectables.Length, $"Esperava um coletável na sala '{SceneManager.GetActiveScene().name}'.");
            return collectables[0];
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string expectation)
        {
            float deadline = Time.realtimeSinceStartup + WaitTimeoutSeconds;

            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Tempo esgotado esperando {expectation}. Sala ativa: '{SceneManager.GetActiveScene().name}', " +
                                $"ambiente atual: '{AudioManager.Instance.CurrentAmbience}', fontes de ambiente tocando: {PlayingAmbienceSources().Length}.");

                yield return null;
            }
        }
    }
}
