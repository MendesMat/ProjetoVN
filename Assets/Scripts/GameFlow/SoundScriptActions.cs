using ProjetoVN.Audio;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;
using Yarn.Unity;

namespace ProjetoVN.GameFlow
{
    /// <summary>
    /// Os comandos de roteiro de som: <c>&lt;&lt;tocar_musica id&gt;&gt;</c>, <c>&lt;&lt;tocar_efeito id&gt;&gt;</c>,
    /// <c>&lt;&lt;tocar_ambiente id&gt;&gt;</c> e <c>&lt;&lt;parar_ambiente&gt;&gt;</c>. Vive no <c>Managers.prefab</c>. Os métodos
    /// são estáticos porque é assim que o Yarn Spinner os acha sem exigir o nome de um objeto no roteiro. Um id que
    /// não existe loga erro com o nó e a conversa segue. Nenhum comando espera o som acabar.
    /// </summary>
    public sealed class SoundScriptActions : MonoBehaviour
    {
        public static SoundScriptActions Instance { get; private set; }

        [Tooltip("Registro em que os comandos de roteiro procuram o som pelo id. " +
                 "Vazio = tocar_musica, tocar_efeito e tocar_ambiente logam erro e não tocam nada.")]
        [SerializeField] private SoundRegistry soundRegistry;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => Instance = null;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        [YarnCommand("tocar_musica")]
        public static void PlayMusic(string soundId)
        {
            string scriptCall = $"<<tocar_musica {soundId}>>";
            if (!TryGetAudioManager(scriptCall, out AudioManager audio)) return;
            if (!TryGetRegistry(scriptCall, out SoundRegistry registry)) return;

            if (registry.TryGetMusic(soundId, out AudioClip clip)) audio.PlayMusic(clip);
            else LogUnknownSound(scriptCall, soundId, "músicas");
        }

        [YarnCommand("tocar_efeito")]
        public static void PlayEffect(string soundId)
        {
            string scriptCall = $"<<tocar_efeito {soundId}>>";
            if (!TryGetAudioManager(scriptCall, out AudioManager audio)) return;
            if (!TryGetRegistry(scriptCall, out SoundRegistry registry)) return;

            if (registry.TryGetEffect(soundId, out AudioClip clip)) audio.PlayEffect(clip);
            else LogUnknownSound(scriptCall, soundId, "efeitos");
        }

        [YarnCommand("tocar_ambiente")]
        public static void PlayAmbience(string soundId)
        {
            string scriptCall = $"<<tocar_ambiente {soundId}>>";
            if (!TryGetAudioManager(scriptCall, out AudioManager audio)) return;
            if (!TryGetRegistry(scriptCall, out SoundRegistry registry)) return;

            if (registry.TryGetAmbience(soundId, out AudioClip clip)) audio.PlayAmbience(clip);
            else LogUnknownSound(scriptCall, soundId, "ambientes");
        }

        [YarnCommand("parar_ambiente")]
        public static void StopAmbience()
        {
            if (!TryGetAudioManager("<<parar_ambiente>>", out AudioManager audio)) return;

            audio.PlayAmbience(null);
        }

        private static bool TryGetAudioManager(string scriptCall, out AudioManager audio)
        {
            audio = AudioManager.Instance;
            if (audio != null) return true;

            Debug.LogError($"[SoundScriptActions] {scriptCall}: não há AudioManager (o prefab Managers não foi criado) " +
                           $"(nó '{CurrentNodeName()}'). Ignorado.", Instance);
            return false;
        }

        private static bool TryGetRegistry(string scriptCall, out SoundRegistry registry)
        {
            registry = Instance != null ? Instance.soundRegistry : null;
            if (registry != null) return true;

            Debug.LogError($"[SoundScriptActions] {scriptCall}: não há SoundScriptActions com o SoundRegistry atribuído " +
                           $"(confira o Managers.prefab) (nó '{CurrentNodeName()}'). Ignorado.", Instance);
            return false;
        }

        private static void LogUnknownSound(string scriptCall, string soundId, string listName)
        {
            Debug.LogError($"[SoundScriptActions] {scriptCall}: o som '{soundId}' não existe na lista de {listName} do SoundRegistry " +
                           $"(nó '{CurrentNodeName()}'). Ignorado.", Instance);
        }

        private static string CurrentNodeName()
        {
            DialogueManager dialogue = DialogueManager.Instance;
            return dialogue != null && dialogue.CurrentNodeName != null ? dialogue.CurrentNodeName : "?";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (soundRegistry != null) return;

            Debug.LogWarning("[SoundScriptActions] 'soundRegistry' está vazio: tocar_musica, tocar_efeito e tocar_ambiente não vão funcionar.", this);
        }
#endif
    }
}
