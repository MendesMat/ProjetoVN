using ProjetoVN.Dialogue.Logic;
using ProjetoVN.Inventory;
using UnityEngine;
using Yarn.Unity;

namespace ProjetoVN.GameFlow
{
    /// <summary>
    /// Os comandos e a função de roteiro que ligam o diálogo ao inventário: <c>&lt;&lt;dar_item id&gt;&gt;</c>,
    /// <c>&lt;&lt;remover_item id&gt;&gt;</c> e <c>tem_item("id")</c>. Vive no <c>Managers.prefab</c>. Os métodos são
    /// estáticos porque é assim que o Yarn Spinner os acha sem exigir o nome de um objeto no roteiro. Um id que
    /// não existe loga erro com o nó e a conversa segue.
    /// </summary>
    public sealed class ItemScriptActions : MonoBehaviour
    {
        public static ItemScriptActions Instance { get; private set; }

        [Tooltip("Registro em que os comandos de roteiro procuram o item pelo id. " +
                 "Vazio = dar_item, remover_item e tem_item logam erro e não fazem nada.")]
        [SerializeField] private ItemRegistry itemRegistry;

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

        [YarnCommand("dar_item")]
        public static void GiveItem(string itemId)
        {
            if (!TryFindItem(itemId, $"<<dar_item {itemId}>>", out ItemDataSO item)) return;

            // Retorno ignorado de propósito: os itens são únicos, e dar um item que o jogador já tem não é erro.
            InventoryManager.Instance.Collect(item);
        }

        [YarnCommand("remover_item")]
        public static void RemoveItem(string itemId)
        {
            if (!TryFindItem(itemId, $"<<remover_item {itemId}>>", out ItemDataSO item)) return;

            // Retorno ignorado de propósito: remover um item que o jogador não tem não é erro.
            InventoryManager.Instance.TryUse(item);
        }

        [YarnFunction("tem_item")]
        public static bool HasItem(string itemId)
        {
            return TryFindItem(itemId, $"tem_item(\"{itemId}\")", out ItemDataSO item)
                   && InventoryManager.Instance.HasItem(item);
        }

        private static bool TryFindItem(string itemId, string scriptCall, out ItemDataSO item)
        {
            item = null;
            if (Instance == null || Instance.itemRegistry == null)
            {
                Debug.LogError($"[ItemScriptActions] {scriptCall}: não há ItemScriptActions com o ItemRegistry atribuído " +
                               $"(confira o Managers.prefab) (nó '{CurrentNodeName()}'). Ignorado.", Instance);
                return false;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError($"[ItemScriptActions] {scriptCall}: não há InventoryManager (o prefab Managers não foi criado) " +
                               $"(nó '{CurrentNodeName()}'). Ignorado.", Instance);
                return false;
            }

            if (Instance.itemRegistry.TryGetById(itemId, out item)) return true;

            Debug.LogError($"[ItemScriptActions] {scriptCall}: o item '{itemId}' não existe no ItemRegistry " +
                           $"(nó '{CurrentNodeName()}'). Ignorado.", Instance);
            return false;
        }

        private static string CurrentNodeName()
        {
            DialogueManager dialogue = DialogueManager.Instance;
            return dialogue != null && dialogue.CurrentNodeName != null ? dialogue.CurrentNodeName : "?";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (itemRegistry != null) return;

            Debug.LogWarning("[ItemScriptActions] 'itemRegistry' está vazio: dar_item, remover_item e tem_item não vão funcionar.", this);
        }
#endif
    }
}
