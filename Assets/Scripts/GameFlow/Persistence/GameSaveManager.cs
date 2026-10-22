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

        [SerializeField] private ItemRegistry itemRegistry;

        private static string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

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

            File.WriteAllText(SavePath, JsonUtility.ToJson(state, prettyPrint: true));
            return true;
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

        public bool StartNewGame(string roomName, string entryPointId) => false;

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
