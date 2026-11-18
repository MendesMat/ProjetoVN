using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjetoVN.Dialogue.Logic;
using ProjetoVN.GameFlow.Rooms;
using ProjetoVN.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjetoVN.GameFlow.Persistence
{
    public sealed class GameSaveManager : MonoBehaviour
    {
        public static GameSaveManager Instance { get; private set; }

        [Tooltip("O registro de itens (Assets/Scripts/ScriptableObjects/Items/ItemRegistry.asset), que traduz os ids gravados no save em itens. " +
                 "Vazio = carregar o save dá erro e nada é restaurado.")]
        [SerializeField] private ItemRegistry itemRegistry;

        private static string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

        private static string TemporarySavePath => SavePath + ".tmp";

        public bool HasSave => File.Exists(SavePath) && GameStateJson.TryRead(File.ReadAllText(SavePath), out _);

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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (itemRegistry == null)
                Debug.LogWarning("[GameSaveManager] O campo 'Item Registry' está vazio: carregar o save dá erro e nada é restaurado. Atribua o ItemRegistry.", this);
        }
#endif

        public bool Save()
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[GameSaveManager] Não há InventoryManager na cena. Save ignorado.", this);
                return false;
            }

            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
            {
                Debug.LogWarning("[GameSaveManager] Save recusado: há uma conversa aberta (D-20).", this);
                return false;
            }

            if (RoomNavigator.Instance == null)
            {
                Debug.LogError("[GameSaveManager] Não há RoomNavigator na cena. Save ignorado.", this);
                return false;
            }

            var state = new GameState
            {
                ownedItemIds = InventoryManager.Instance.Items.Select(item => item.Id).ToList(),
                consumedWorldObjectIds = InventoryManager.Instance.ConsumedWorldObjectIds.ToList(),
                currentScene = SceneManager.GetActiveScene().name,
                currentEntryPointId = RoomNavigator.Instance.CurrentEntryPointId ?? ""
            };
            StoryStatePersistence.Capture(state);

            WriteSaveFile(JsonUtility.ToJson(state, prettyPrint: true));
            return true;
        }

        // O save anterior só é trocado depois de o novo estar inteiro no disco: se o jogo morrer no meio, sobra o
        // anterior. O backup é null de propósito: com ele sobraria no disco um segundo arquivo que nada lê (slot
        // único, D-20).
        private static void WriteSaveFile(string json)
        {
            WriteTemporarySaveFile(json);

            if (File.Exists(SavePath))
                File.Replace(TemporarySavePath, SavePath, null);
            else
                File.Move(TemporarySavePath, SavePath);
        }

        // O Flush(true) pede ao sistema que grave no disco antes da troca: sem ele, uma queda de energia logo
        // depois da troca pode deixar o save novo com o conteúdo ainda no cache do sistema, ou seja, vazio.
        private static void WriteTemporarySaveFile(string json)
        {
            using var stream = new FileStream(TemporarySavePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write(json);
            writer.Flush();
            stream.Flush(flushToDisk: true);
        }

        public bool Load()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning("[GameSaveManager] Nenhum save encontrado.", this);
                return false;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[GameSaveManager] Não há InventoryManager na cena. Load ignorado.", this);
                return false;
            }

            if (itemRegistry == null)
            {
                Debug.LogError("[GameSaveManager] 'itemRegistry' não foi atribuído.", this);
                return false;
            }

            if (RoomNavigator.Instance == null)
            {
                Debug.LogError("[GameSaveManager] Não há RoomNavigator na cena. Load ignorado.", this);
                return false;
            }

            if (!GameStateJson.TryRead(File.ReadAllText(SavePath), out GameState state))
            {
                Debug.LogWarning("[GameSaveManager] O save está corrompido e não pôde ser lido. Load ignorado.", this);
                return false;
            }

            // Restaurar só depois de a troca ser aceita mantém o Load tudo ou nada. A cena nova ainda não foi
            // ativada neste ponto, então o Start() dos objetos dela já lê o estado restaurado.
            if (!RoomNavigator.Instance.ChangeRoom(state.currentScene, state.currentEntryPointId))
            {
                Debug.LogWarning($"[GameSaveManager] Load ignorado: a troca para a sala salva '{state.currentScene}' " +
                                 "foi recusada (motivo acima).", this);
                return false;
            }

            Restore(state);
            return true;
        }

        public bool StartNewGame(string roomName, string entryPointId)
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[GameSaveManager] Não há InventoryManager na cena. StartNewGame ignorado.", this);
                return false;
            }

            if (RoomNavigator.Instance == null)
            {
                Debug.LogError("[GameSaveManager] Não há RoomNavigator na cena. StartNewGame ignorado.", this);
                return false;
            }

            if (!RoomNavigator.Instance.ChangeRoom(roomName, entryPointId))
            {
                Debug.LogWarning($"[GameSaveManager] StartNewGame ignorado: a troca para a sala inicial '{roomName}' " +
                                 "foi recusada (motivo acima).", this);
                return false;
            }

            Restore(new GameState());
            return true;
        }

        private void Restore(GameState state)
        {
            var restoredItems = new List<ItemDataSO>();
            foreach (string id in state.ownedItemIds ?? new List<string>())
            {
                if (itemRegistry.TryGetById(id, out ItemDataSO item))
                    restoredItems.Add(item);
                else
                    Debug.LogError($"[GameSaveManager] Id de item desconhecido no save: '{id}'.", this);
            }

            InventoryManager.Instance.ReplaceAll(restoredItems);
            InventoryManager.Instance.ReplaceConsumedWorldObjectIds(state.consumedWorldObjectIds ?? new List<string>());
            StoryStatePersistence.Restore(state);
        }
    }
}
