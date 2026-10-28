using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.Audio;
using ProjetoVN.GameFlow.Menu;
using ProjetoVN.GameFlow.Rooms;
using ProjetoVN.PointNClick;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ProjetoVN.Tests.PlayMode
{
    public class MusicTests
    {
        private const string StartRoom = "[Teste] Mecanicas";
        private const string OtherRoom = "[Teste] Sala B";
        private const string MenuScene = "Menu";
        private const string EntryPointId = "janela";
        private const string StartRoomMusic = "teste_musica_a";
        private const string OtherRoomMusic = "teste_musica_b";
        private const int SampleRate = 44100;
        private const float StubMusicSeconds = 4f;
        private const float Tolerance = 1e-3f;
        private const float WaitTimeoutSeconds = 10f;
        private const float LongerThanTheCrossfadeSeconds = 2f;
        private const float LongerThanAnAudioBlockSeconds = 0.1f;
        private const float LoopStartMarginSeconds = 1f;
        private const float LoopEndMarginSeconds = 3f;

        private AudioClip _startRoomClip;
        private AudioClip _stubMusic;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim de uma troca de sala pendente");

            Assert.IsNotNull(AudioManager.Instance,
                "Não há AudioManager: o Managers.prefab não o traz ou não foi criado pelo ManagersBootstrap.");

            yield return SceneManager.LoadSceneAsync(StartRoom);
            yield return null;
            yield return WaitUntil(() => HasSettledOn(StartRoomMusic), $"a música '{StartRoomMusic}' terminar de entrar");
            _startRoomClip = AudioManager.Instance.CurrentMusic;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include).Resume();
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim de uma troca de sala pendente");
            yield return DestroyStubMusic();

            Assert.AreEqual(1f, Time.timeScale, "O tempo do jogo ficou congelado depois do teste.");
            if (SceneManager.GetActiveScene().name == MenuScene)
            {
                yield return SceneManager.LoadSceneAsync(StartRoom);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ChangingToARoomWithOtherMusic_CrossfadesToIt()
        {
            AudioSource leaving = PlayingSourceOf(StartRoomMusic);
            Assert.IsTrue(RoomNavigator.Instance.ChangeRoom(OtherRoom, EntryPointId), "A troca de sala foi recusada.");

            yield return WaitUntil(() => AudioManager.Instance.CurrentMusic != null
                                         && AudioManager.Instance.CurrentMusic.name == OtherRoomMusic,
                "a música da sala nova ser a atual");
            AudioSource entering = PlayingSourceOf(OtherRoomMusic);
            yield return WaitUntil(() => entering.volume > 0.2f && entering.volume < 0.8f, "o meio do crossfade");

            Assert.IsTrue(leaving.isPlaying, "A música antiga parou no meio do crossfade.");
            Assert.Less(leaving.volume, 1f, "A música antiga não está descendo.");
            Assert.Greater(leaving.volume, 0f, "A música antiga já está muda no meio do crossfade.");

            yield return WaitUntil(() => HasSettledOn(OtherRoomMusic), $"a música '{OtherRoomMusic}' terminar de entrar");
            Assert.IsFalse(leaving.isPlaying, "A música antiga continua tocando depois do crossfade.");
            Assert.AreEqual(1f, entering.volume, Tolerance);
        }

        [UnityTest]
        public IEnumerator ReloadingTheRoom_DoesNotRestartTheMusic()
        {
            AudioSource source = PlayingSourceOf(StartRoomMusic);
            yield return WaitUntilTimeIsAwayFromTheLoopEnds(source);
            float timeBefore = source.time;

            Assert.IsTrue(RoomNavigator.Instance.ChangeRoom(SceneManager.GetActiveScene().name), "O Reload foi recusado.");
            yield return WaitUntil(() => PlayerInputGate.IsEnabled, "o fim da troca");

            Assert.AreEqual(StartRoomMusic, AudioManager.Instance.CurrentMusic.name);
            Assert.IsTrue(source.isPlaying, "A música parou ao recarregar a sala.");
            Assert.Greater(source.time, timeBefore, "A música reiniciou ao recarregar a sala.");
            Assert.AreEqual(1, PlayingSources().Length, "Uma segunda fonte começou a tocar a mesma música ao recarregar a sala.");
            Assert.AreEqual(1f, source.volume, Tolerance, "A música baixou de volume ao recarregar a sala.");
        }

        [UnityTest]
        public IEnumerator PlayMusic_WithTheCurrentClip_DoesNotRestartIt()
        {
            AudioSource source = PlayingSourceOf(StartRoomMusic);
            yield return WaitUntilTimeIsAwayFromTheLoopEnds(source);
            float timeBefore = source.time;

            AudioManager.Instance.PlayMusic(AudioManager.Instance.CurrentMusic);
            yield return new WaitForSecondsRealtime(LongerThanAnAudioBlockSeconds);

            Assert.AreSame(source, PlayingSourceOf(StartRoomMusic), "A música trocou de fonte.");
            Assert.Greater(source.time, timeBefore, "A música reiniciou.");
            Assert.AreEqual(1, PlayingSources().Length, "Uma segunda fonte começou a tocar.");
        }

        [UnityTest]
        public IEnumerator ReturnToMainMenu_FadesToSilence_WithoutErrors()
        {
            Assert.IsTrue(RoomNavigator.Instance.GoToMainMenu(MenuScene), "A volta ao menu foi recusada.");
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == MenuScene && PlayerInputGate.IsEnabled,
                "o menu principal aparecer");

            Assert.IsNull(AudioManager.Instance.CurrentMusic, "O menu não tem música, mas há uma música atual.");

            yield return WaitUntil(() => PlayingSources().Length == 0, "a música se calar");
        }

        [UnityTest]
        public IEnumerator GoingBackDuringAChange_DoesNotRestartTheLeavingMusic()
        {
            AudioSource leaving = PlayingSourceOf(StartRoomMusic);
            yield return WaitUntilTimeIsAwayFromTheLoopEnds(leaving);
            AudioManager.Instance.PlayMusic(NewStubMusic());
            yield return WaitUntil(() => leaving.volume < 0.8f && leaving.volume > 0.2f, "o meio do crossfade");
            float timeBefore = leaving.time;

            AudioManager.Instance.PlayMusic(_startRoomClip);
            yield return new WaitForSecondsRealtime(LongerThanAnAudioBlockSeconds);

            Assert.IsTrue(leaving.isPlaying, "A música que voltou parou.");
            Assert.Greater(leaving.time, timeBefore, "A música que voltou reiniciou.");
            yield return WaitUntil(() => HasSettledOn(StartRoomMusic), "a música voltar a ser a única tocando");
            Assert.AreSame(leaving, PlayingSourceOf(StartRoomMusic), "A música trocou de fonte ao voltar.");
            Assert.AreEqual(1f, leaving.volume, Tolerance);
        }

        [UnityTest]
        public IEnumerator TheChangeDoesNotFreezeWhilePaused_AndTheMusicKeepsPlaying()
        {
            var pauseMenu = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
            AudioManager.Instance.PlayMusic(NewStubMusic());
            yield return null;
            pauseMenu.TogglePause();
            Assert.AreEqual(0f, Time.timeScale, "A pausa não congelou o tempo.");

            yield return new WaitForSecondsRealtime(LongerThanTheCrossfadeSeconds);

            Assert.AreEqual(1, PlayingSources().Length, "A troca ficou parada no meio durante a pausa.");
            AudioSource source = PlayingSources().Single();
            Assert.AreSame(_stubMusic, source.clip);
            Assert.AreEqual(1f, source.volume, Tolerance);
        }

        private static AudioSource[] MusicSources() =>
            AudioManager.Instance.GetComponentsInChildren<AudioSource>(true);

        private static AudioSource[] PlayingSources() => MusicSources().Where(source => source.isPlaying).ToArray();

        private static AudioSource PlayingSourceOf(string clipName)
        {
            AudioSource source = PlayingSources().FirstOrDefault(candidate => candidate.clip.name == clipName);
            Assert.IsNotNull(source, $"Nenhuma fonte toca '{clipName}'.");
            return source;
        }

        private static bool HasSettledOn(string clipName)
        {
            AudioClip current = AudioManager.Instance.CurrentMusic;
            if (current == null || current.name != clipName) return false;

            AudioSource[] playing = PlayingSources();
            return playing.Length == 1 && Mathf.Approximately(playing[0].volume, 1f);
        }

        private AudioClip NewStubMusic()
        {
            _stubMusic = AudioClip.Create("musica_provisoria_do_teste", (int)(SampleRate * StubMusicSeconds), 1, SampleRate, false);
            _stubMusic.SetData(new float[(int)(SampleRate * StubMusicSeconds)], 0);
            return _stubMusic;
        }

        private IEnumerator DestroyStubMusic()
        {
            if (_stubMusic == null) yield break;

            AudioManager.Instance.PlayMusic(_startRoomClip);
            yield return WaitUntil(() => HasSettledOn(StartRoomMusic), $"a música '{StartRoomMusic}' voltar a ser a única tocando");
            Object.Destroy(_stubMusic);
            _stubMusic = null;
        }

        private static IEnumerator WaitUntilTimeIsAwayFromTheLoopEnds(AudioSource source) =>
            WaitUntil(() => source.time >= LoopStartMarginSeconds && source.time <= source.clip.length - LoopEndMarginSeconds,
                "a música estar longe das pontas do loop");

        private static IEnumerator WaitUntil(Func<bool> condition, string expectation)
        {
            float deadline = Time.realtimeSinceStartup + WaitTimeoutSeconds;

            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Tempo esgotado esperando {expectation}. Sala ativa: '{SceneManager.GetActiveScene().name}', " +
                                $"música atual: '{AudioManager.Instance.CurrentMusic}', fontes tocando: {PlayingSources().Length}.");

                yield return null;
            }
        }
    }
}
