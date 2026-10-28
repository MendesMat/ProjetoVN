using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjetoVN.Audio
{
    /// <summary>
    /// O dono da música. Toca a música que a sala declara (<see cref="RoomMusic"/>) e faz a troca suave entre duas
    /// músicas com duas <see cref="AudioSource"/>: uma sai enquanto a outra entra. Vive no <c>Managers.prefab</c>.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public AudioClip CurrentMusic { get; private set; }

        [Tooltip("A primeira das duas fontes da música (filho Music do Managers.prefab, no grupo Music do mixer). " +
                 "A troca suave usa as duas: uma sai enquanto a outra entra. " +
                 "Vazio = nenhuma música toca; o AudioManager loga um erro.")]
        [SerializeField] private AudioSource musicSourceA;

        [Tooltip("A segunda fonte da música. Mesmas regras da primeira. Vazio = nenhuma música toca.")]
        [SerializeField] private AudioSource musicSourceB;

        [Tooltip("Duração, em segundos, da troca suave entre duas músicas. 0 = troca seca.")]
        [Min(0f)]
        [SerializeField] private float crossfadeSeconds = 1f;

        private AudioSource _enteringSource;
        private AudioSource _leavingSource;
        private MusicCrossfade _crossfade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => Instance = null;

        private void Awake()
        {
            Instance = this;

            if (musicSourceA == null || musicSourceB == null)
            {
                Debug.LogError("[AudioManager] Faltam as AudioSource da música (campos Music Source A e B). Nenhuma música vai tocar.", this);
                return;
            }

            _enteringSource = musicSourceA;
            _leavingSource = musicSourceB;
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        // Na primeira cena do jogo o sceneLoaded já disparou antes de o Managers.prefab existir.
        private void Start() => PlayMusic(MusicOf(SceneManager.GetActiveScene()));

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_crossfade == null) return;

            _crossfade.Advance(Time.unscaledDeltaTime);
            ApplyCrossfade();
        }

        /// <summary>Troca a música com o crossfade. <c>null</c> leva a música a silêncio; a música que já toca não reinicia.</summary>
        public void PlayMusic(AudioClip clip)
        {
            if (_enteringSource == null) return;
            if (clip == CurrentMusic) return;

            AudioSource nowEntering = _leavingSource;
            AudioSource nowLeaving = _enteringSource;
            PrepareToEnter(nowEntering, clip);

            CurrentMusic = clip;
            _enteringSource = nowEntering;
            _leavingSource = nowLeaving;
            _crossfade = new MusicCrossfade(crossfadeSeconds, _enteringSource.volume, _leavingSource.volume);
            ApplyCrossfade();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlayMusic(MusicOf(scene));

        // Procura nas raízes da cena recebida, e não na cena ativa, para não depender da ordem em que a sala antiga sai.
        private static AudioClip MusicOf(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                RoomMusic roomMusic = root.GetComponentInChildren<RoomMusic>();
                if (roomMusic != null) return roomMusic.Music;
            }

            return null;
        }

        private static void PrepareToEnter(AudioSource source, AudioClip clip)
        {
            bool isAlreadyPlayingIt = clip != null && source.isPlaying && source.clip == clip;
            if (isAlreadyPlayingIt) return;

            source.Stop();
            source.volume = 0f;
            source.clip = clip;
            if (clip != null) source.Play();
        }

        private void ApplyCrossfade()
        {
            _enteringSource.volume = _crossfade.IncomingVolume;
            _leavingSource.volume = _crossfade.OutgoingVolume;

            if (!_crossfade.IsDone) return;

            _leavingSource.Stop();
            _crossfade = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (musicSourceA == null || musicSourceB == null)
                Debug.LogWarning("[AudioManager] Os campos Music Source A e B precisam das duas AudioSource da música: sem elas nenhuma música toca.", this);
        }
#endif
    }
}
