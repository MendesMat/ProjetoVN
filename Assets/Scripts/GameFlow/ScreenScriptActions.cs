using ProjetoVN.Dialogue.Logic;
using ProjetoVN.GameFlow.ScreenEffects;
using UnityEngine;
using Yarn.Unity;

namespace ProjetoVN.GameFlow
{
    /// <summary>
    /// Os comandos de roteiro de tela: <c>&lt;&lt;escurecer&gt;&gt;</c>, <c>&lt;&lt;clarear&gt;&gt;</c>,
    /// <c>&lt;&lt;mostrar_imagem id&gt;&gt;</c> e <c>&lt;&lt;esconder_imagem&gt;&gt;</c>, todos com a duração em segundos
    /// como último parâmetro, opcional. Vive no <c>Managers.prefab</c>. Os métodos são estáticos porque é assim que o
    /// Yarn Spinner os acha sem exigir o nome de um objeto no roteiro. O roteiro espera o efeito terminar antes da
    /// linha seguinte. Um erro de conteúdo loga com o nó e a conversa segue.
    /// </summary>
    public sealed class ScreenScriptActions : MonoBehaviour
    {
        private const float DefaultSeconds = 1f;

        public static ScreenScriptActions Instance { get; private set; }

        [Tooltip("A camada de tela que os comandos comandam (objeto ScriptScreen dentro do GameUI.prefab). " +
                 "Vazio = escurecer, clarear, mostrar_imagem e esconder_imagem logam erro e não fazem nada.")]
        [SerializeField] private ScriptScreen scriptScreen;

        [Tooltip("Registro em que mostrar_imagem procura a imagem pelo id. " +
                 "Vazio = mostrar_imagem loga erro e não mostra nada.")]
        [SerializeField] private PictureRegistry pictureRegistry;

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

        [YarnCommand("escurecer")]
        public static YarnTask Darken(float seconds = DefaultSeconds)
        {
            if (!TryGetScreen("<<escurecer>>", out ScriptScreen screen)) return YarnTask.CompletedTask;

            return screen.DarkenAsync(NonNegative("<<escurecer>>", seconds));
        }

        [YarnCommand("clarear")]
        public static YarnTask Brighten(float seconds = DefaultSeconds)
        {
            if (!TryGetScreen("<<clarear>>", out ScriptScreen screen)) return YarnTask.CompletedTask;

            return screen.BrightenAsync(NonNegative("<<clarear>>", seconds));
        }

        [YarnCommand("mostrar_imagem")]
        public static YarnTask ShowPicture(string pictureId, float seconds = DefaultSeconds)
        {
            string scriptCall = $"<<mostrar_imagem {pictureId}>>";
            if (!TryGetScreen(scriptCall, out ScriptScreen screen)) return YarnTask.CompletedTask;
            if (!TryGetRegistry(scriptCall, out PictureRegistry registry)) return YarnTask.CompletedTask;

            if (!registry.TryGet(pictureId, out Sprite sprite))
            {
                LogUnknownPicture(scriptCall, pictureId, registry);
                return YarnTask.CompletedTask;
            }

            return screen.ShowPictureAsync(sprite, NonNegative(scriptCall, seconds));
        }

        [YarnCommand("esconder_imagem")]
        public static YarnTask HidePicture(float seconds = DefaultSeconds)
        {
            if (!TryGetScreen("<<esconder_imagem>>", out ScriptScreen screen)) return YarnTask.CompletedTask;

            return screen.HidePictureAsync(NonNegative("<<esconder_imagem>>", seconds));
        }

        private static bool TryGetScreen(string scriptCall, out ScriptScreen screen)
        {
            screen = Instance != null ? Instance.scriptScreen : null;
            if (screen != null) return true;

            Debug.LogError($"[ScreenScriptActions] {scriptCall}: não há ScreenScriptActions com o ScriptScreen atribuído " +
                           $"(nó '{CurrentNodeName()}'; confira o Managers.prefab). Ignorado.", Instance);
            return false;
        }

        private static bool TryGetRegistry(string scriptCall, out PictureRegistry registry)
        {
            registry = Instance != null ? Instance.pictureRegistry : null;
            if (registry != null) return true;

            Debug.LogError($"[ScreenScriptActions] {scriptCall}: não há ScreenScriptActions com o PictureRegistry atribuído " +
                           $"(nó '{CurrentNodeName()}'; confira o Managers.prefab). Ignorado.", Instance);
            return false;
        }

        private static float NonNegative(string scriptCall, float seconds)
        {
            if (seconds >= 0f) return seconds;

            Debug.LogError($"[ScreenScriptActions] {scriptCall}: a duração {seconds} é negativa (nó '{CurrentNodeName()}'). " +
                           "Valeu 0, o efeito acontece na hora.", Instance);
            return 0f;
        }

        private static void LogUnknownPicture(string scriptCall, string pictureId, PictureRegistry registry)
        {
            Debug.LogError($"[ScreenScriptActions] {scriptCall}: a imagem '{pictureId}' não existe no PictureRegistry " +
                           $"(nó '{CurrentNodeName()}'; as que existem: {registry.DescribeIds()}). Ignorado.", Instance);
        }

        private static string CurrentNodeName()
        {
            DialogueManager dialogue = DialogueManager.Instance;
            return dialogue != null && dialogue.CurrentNodeName != null ? dialogue.CurrentNodeName : "?";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (scriptScreen == null)
                Debug.LogWarning("[ScreenScriptActions] 'scriptScreen' está vazio: escurecer, clarear, mostrar_imagem e esconder_imagem não vão funcionar.", this);

            if (pictureRegistry == null)
                Debug.LogWarning("[ScreenScriptActions] 'pictureRegistry' está vazio: mostrar_imagem não vai funcionar.", this);
        }
#endif
    }
}
