using ProjetoVN.Dialogue.UI;
using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;

namespace ProjetoVN.Dialogue.Logic
{
    /// <summary>
    /// O adaptador entre o Yarn Spinner e o resto do jogo. Vive no <c>Managers.prefab</c>, no mesmo objeto do
    /// <c>DialogueRunner</c>. Iniciar, avançar e escolher são chamadas diretas aqui (D-05); para fora, publica
    /// <c>DialogueStartedMessage</c> e <c>DialogueEndedMessage</c> uma vez por conversa. Conteúdo inválido
    /// (nó inexistente, roteiro com erro, comando desconhecido) nunca trava o jogo: loga e segue em exploração.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        private readonly ConversationNotifier _conversation = new();
        private DialogueRunner _runner;
        private DialogueUIController _presenter;

        private void Awake()
        {
            Instance = this;
            _runner = GetComponent<DialogueRunner>();
            if (_runner == null)
                Debug.LogError("[DialogueManager] Não há DialogueRunner no mesmo objeto. O diálogo não vai funcionar.", this);
        }

        private void OnEnable()
        {
            if (_runner == null) return;

            _runner.onDialogueStart ??= new UnityEvent();
            _runner.onDialogueComplete ??= new UnityEvent();
            _runner.onDialogueStart.AddListener(_conversation.Begin);
            _runner.onDialogueComplete.AddListener(_conversation.End);
        }

        private void OnDisable()
        {
            if (_runner == null) return;

            _runner.onDialogueStart?.RemoveListener(_conversation.Begin);
            _runner.onDialogueComplete?.RemoveListener(_conversation.End);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // O Yarn não chama o fim da conversa quando um <<jump>> aponta para um nó que não existe: a VM lança
        // dentro do Continue() e para. Sem esta rede o GameFlow ficaria em DialogueState para sempre. Só vale
        // porque o apresentador termina o início e o fim da conversa na hora (são síncronos): do contrário
        // uma conversa que acabou de abrir pareceria parada.
        private void Update()
        {
            if (_runner == null || !_conversation.IsOpen || _runner.IsDialogueRunning) return;

            Debug.LogError("[DialogueManager] O roteiro parou sem terminar a conversa. A causa mais comum é um <<jump>> para um nó que não existe. " +
                           "Encerrando a conversa.", this);
            _conversation.End();
        }

        /// <returns><c>false</c>, com o motivo no console, se a conversa não pôde começar.</returns>
        public bool StartDialogue(string nodeName)
        {
            if (!CanStart(nodeName)) return false;

            _runner.StartDialogue(nodeName).Forget();
            return true;
        }

        public void AdvanceDialogue()
        {
            if (_runner != null) _runner.RequestNextLine();
        }

        public void MakeChoice(int index)
        {
            if (_presenter != null) _presenter.Choose(index);
        }

        public bool IsDialogueActive() => _conversation.IsOpen;

        /// <summary>O nó em que o roteiro está; <c>null</c> fora de uma conversa.</summary>
        public string CurrentNodeName => _runner != null && _conversation.IsOpen ? _runner.Dialogue.CurrentNode : null;

        // Ligado ao onUnhandledCommand do runner por ligação persistente no Managers.prefab. Sem isto o runner
        // loga o erro e para sem chamar Continue(): o jogo prenderia na conversa.
        public void SkipUnknownCommand(string commandText)
        {
            Debug.LogError($"[DialogueManager] Comando desconhecido no roteiro: <<{commandText}>> (nó '{_runner.Dialogue.CurrentNode}'). Ignorado.", this);
            _runner.Dialogue.SignalContentComplete();
        }

        // internal de propósito: um membro público com tipo derivado do Yarn obrigaria o GameFlow a referenciar o pacote.
        internal void RegisterPresenter(DialogueUIController presenter)
        {
            _presenter = presenter;
            if (_runner != null) _runner.DialoguePresenters = new DialoguePresenterBase[] { presenter };
        }

        internal void UnregisterPresenter(DialogueUIController presenter)
        {
            if (_presenter != presenter) return;

            _presenter = null;
            if (_runner == null) return;

            _runner.DialoguePresenters = System.Array.Empty<DialoguePresenterBase>();
            if (_conversation.IsOpen) _runner.Stop().Forget();
        }

        private bool CanStart(string nodeName)
        {
            if (string.IsNullOrWhiteSpace(nodeName))
            {
                Debug.LogWarning("[DialogueManager] Nome de nó vazio. A conversa não começou.", this);
                return false;
            }

            if (_runner == null) return false;

            if (_conversation.IsOpen)
            {
                Debug.LogWarning($"[DialogueManager] Já há uma conversa em curso (nó '{_runner.Dialogue.CurrentNode}'); '{nodeName}' foi ignorado.", this);
                return false;
            }

            if (_presenter == null)
            {
                Debug.LogError($"[DialogueManager] Nenhuma interface de diálogo registrada. A conversa '{nodeName}' não começou.", this);
                return false;
            }

            return ScriptIsValid() && NodeExists(nodeName);
        }

        private bool ScriptIsValid()
        {
            YarnProject project = _runner.YarnProject;
            if (project == null)
            {
                Debug.LogError("[DialogueManager] O DialogueRunner não tem um Yarn Project. A conversa não começou.", this);
                return false;
            }

            if (project.compiledYarnProgram == null || project.compiledYarnProgram.Length == 0)
            {
                Debug.LogWarning($"[DialogueManager] O roteiro tem erro de compilação ({project.name}). A conversa não começou; veja o erro na importação do Roteiro.yarnproject.", this);
                return false;
            }

            return true;
        }

        private bool NodeExists(string nodeName)
        {
            if (_runner.YarnProject.Program.Nodes.ContainsKey(nodeName)) return true;

            Debug.LogWarning($"[DialogueManager] O nó '{nodeName}' não existe no roteiro. A conversa não começou.", this);
            return false;
        }
    }
}
