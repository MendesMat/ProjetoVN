using ProjetoVN.Core.Messaging;
using ProjetoVN.Dialogue.Messaging;
using ProjetoVN.Inventory;
using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;

namespace ProjetoVN.PocYarn
{
    public sealed class PocYarnLauncher : MonoBehaviour
    {
        [SerializeField, Tooltip("Runner que executa o roteiro. Vazio: nada funciona e o erro aparece no Start.")]
        private DialogueRunner runner;
        [SerializeField, Tooltip("Registro de itens usado pelo comando dar_item e pela função tem_item. Vazio: os dois logam erro.")]
        private ItemRegistry itemRegistry;

        private void Awake() => PocCommands.Registry = itemRegistry;

        private void Start()
        {
            if (runner != null) return;

            Debug.LogError("[PocYarnLauncher] O campo runner não foi atribuído.", this);
            enabled = false;
        }

        private void OnEnable()
        {
            if (runner == null) return;

            runner.onDialogueStart ??= new UnityEvent();
            runner.onDialogueComplete ??= new UnityEvent();
            runner.onDialogueStart.AddListener(PublishStarted);
            runner.onDialogueComplete.AddListener(PublishEnded);
        }

        private void OnDisable()
        {
            if (runner == null) return;

            runner.onDialogueStart.RemoveListener(PublishStarted);
            runner.onDialogueComplete.RemoveListener(PublishEnded);
        }

        public bool StartNode(string nodeName)
        {
            if (!runner.Dialogue.NodeExists(nodeName))
            {
                Debug.LogError($"[PocYarnLauncher] O nó '{nodeName}' não existe no projeto Yarn.", this);
                return false;
            }

            runner.StartDialogue(nodeName).Forget();
            return true;
        }

        public void StartNodeUnguarded(string nodeName) => runner.StartDialogue(nodeName).Forget();

        public void Advance() => runner.RequestNextLine();

        private static void PublishStarted() => MessageBroker.Publish(new DialogueStartedMessage());

        private static void PublishEnded() => MessageBroker.Publish(new DialogueEndedMessage());
    }
}
