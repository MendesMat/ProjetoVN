using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjetoVN.Audio
{
    /// <summary>
    /// O dono do som do jogo. Toca a música que a sala declara (<see cref="RoomMusic"/>), o ambiente e os efeitos
    /// que o roteiro e a interface pedem. Música e ambiente trocam de faixa com a troca suave, cada um com duas
    /// <see cref="AudioSource"/>. Vive no <c>Managers.prefab</c>.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public AudioClip CurrentMusic => _music?.Current;

        public AudioClip CurrentAmbience => _ambience?.Current;

        /// <summary>O último efeito pedido, tocado ou não até o fim. Existe para a verificação: <c>isPlaying</c> não prova um <c>PlayOneShot</c>.</summary>
        public AudioClip LastEffect { get; private set; }

        /// <summary>Quantos efeitos foram pedidos desde que o jogo abriu.</summary>
        public int EffectCount { get; private set; }

        [Tooltip("A primeira das duas fontes da música (filho Music do Managers.prefab, no grupo Music do mixer). " +
                 "A troca suave usa as duas: uma sai enquanto a outra entra. " +
                 "Vazio = nenhuma música toca; o AudioManager loga um erro.")]
        [SerializeField] private AudioSource musicSourceA;

        [Tooltip("A segunda fonte da música. Mesmas regras da primeira. Vazio = nenhuma música toca.")]
        [SerializeField] private AudioSource musicSourceB;

        [Tooltip("Duração, em segundos, da troca suave entre duas músicas. 0 = troca seca.")]
        [Min(0f)]
        [SerializeField] private float crossfadeSeconds = 1f;

        [Tooltip("A primeira das duas fontes do ambiente (filho Ambience do Managers.prefab, no grupo SFX do mixer, com Loop ligado). " +
                 "Vazio = nenhum ambiente toca; o AudioManager loga um erro.")]
        [SerializeField] private AudioSource ambienceSourceA;

        [Tooltip("A segunda fonte do ambiente. Mesmas regras da primeira. Vazio = nenhum ambiente toca.")]
        [SerializeField] private AudioSource ambienceSourceB;

        [Tooltip("Duração, em segundos, da entrada e da saída suaves do ambiente. 0 = troca seca.")]
        [Min(0f)]
        [SerializeField] private float ambienceFadeSeconds = 1f;

        [Tooltip("A fonte dos efeitos (filho Effects do Managers.prefab, no grupo SFX do mixer, sem Loop). " +
                 "Vazio = nenhum efeito toca; o AudioManager loga um erro.")]
        [SerializeField] private AudioSource effectsSource;

        [Header("Sons da interface")]
        [Tooltip("Passar o mouse sobre um botão habilitado. Vazio = silêncio.")]
        [SerializeField] private AudioClip buttonHoverSound;

        [Tooltip("Clicar em um botão (ou Enter sobre ele). Vazio = silêncio.")]
        [SerializeField] private AudioClip buttonClickSound;

        [Tooltip("Completar ou passar uma fala do diálogo. Vazio = silêncio.")]
        [SerializeField] private AudioClip lineAdvanceSound;

        [Tooltip("Coletar um item, inclusive o que o roteiro entrega com <<dar_item>>. Vazio = silêncio.")]
        [SerializeField] private AudioClip itemCollectedSound;

        private CrossfadeChannel _music;
        private CrossfadeChannel _ambience;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => Instance = null;

        private void Awake()
        {
            Instance = this;

            if (musicSourceA == null || musicSourceB == null)
                Debug.LogError("[AudioManager] Faltam as AudioSource da música (campos Music Source A e B). Nenhuma música vai tocar.", this);
            else
                _music = new CrossfadeChannel(musicSourceA, musicSourceB);

            if (ambienceSourceA == null || ambienceSourceB == null)
                Debug.LogError("[AudioManager] Faltam as AudioSource do ambiente (campos Ambience Source A e B). Nenhum ambiente vai tocar.", this);
            else
                _ambience = new CrossfadeChannel(ambienceSourceA, ambienceSourceB);

            if (effectsSource == null)
                Debug.LogError("[AudioManager] Falta a AudioSource dos efeitos (campo Effects Source). Nenhum efeito vai tocar.", this);
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
            _music?.Advance(Time.unscaledDeltaTime);
            _ambience?.Advance(Time.unscaledDeltaTime);
        }

        /// <summary>Troca a música com o crossfade. <c>null</c> leva a música a silêncio; a música que já toca não reinicia.</summary>
        public void PlayMusic(AudioClip clip) => _music?.Play(clip, crossfadeSeconds);

        /// <summary>Troca o ambiente, que toca em loop. <c>null</c> leva o ambiente a silêncio; o ambiente que já toca não reinicia.</summary>
        public void PlayAmbience(AudioClip clip) => _ambience?.Play(clip, ambienceFadeSeconds);

        /// <summary>Toca um efeito pontual por cima do que já toca. <c>null</c>: nada acontece.</summary>
        public void PlayEffect(AudioClip clip)
        {
            if (clip == null || effectsSource == null) return;

            effectsSource.PlayOneShot(clip);
            LastEffect = clip;
            EffectCount++;
        }

        public void PlayInterfaceSound(InterfaceSound sound) => PlayEffect(ClipOf(sound));

        private AudioClip ClipOf(InterfaceSound sound) => sound switch
        {
            InterfaceSound.ButtonHover => buttonHoverSound,
            InterfaceSound.ButtonClick => buttonClickSound,
            InterfaceSound.LineAdvance => lineAdvanceSound,
            InterfaceSound.ItemCollected => itemCollectedSound,
            _ => null,
        };

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayMusic(MusicOf(scene));
            PlayAmbience(null);
        }

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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (musicSourceA == null || musicSourceB == null)
                Debug.LogWarning("[AudioManager] Os campos Music Source A e B precisam das duas AudioSource da música: sem elas nenhuma música toca.", this);

            if (ambienceSourceA == null || ambienceSourceB == null)
                Debug.LogWarning("[AudioManager] Os campos Ambience Source A e B precisam das duas AudioSource do ambiente: sem elas nenhum ambiente toca.", this);

            if (effectsSource == null)
                Debug.LogWarning("[AudioManager] O campo Effects Source precisa da AudioSource dos efeitos: sem ela nenhum efeito toca.", this);
        }
#endif
    }
}
