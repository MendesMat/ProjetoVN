using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public void Save()
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[GameSaveManager] Não há InventoryManager na cena. Save ignorado.", this);
                return;
            }

            var state = new GameState
            {
                ownedItemIds = InventoryManager.Instance.Items.Select(item => item.Id).ToList(),
                consumedWorldObjectIds = InventoryManager.Instance.ConsumedWorldObjectIds.ToList(),
                currentScene = SceneManager.GetActiveScene().name
            };

            File.WriteAllText(SavePath, JsonUtility.ToJson(state, prettyPrint: true));
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

            var state = JsonUtility.FromJson<GameState>(File.ReadAllText(SavePath));

            var restoredItems = new List<ItemDataSO>();
            foreach (string id in state.ownedItemIds)
            {
                if (itemRegistry.TryGetById(id, out ItemDataSO item))
                    restoredItems.Add(item);
                else
                    Debug.LogError($"[GameSaveManager] Id de item desconhecido no save: '{id}'.", this);
            }

            InventoryManager.Instance.ReplaceAll(restoredItems);
            InventoryManager.Instance.ReplaceConsumedWorldObjectIds(state.consumedWorldObjectIds);
            return true;
        }
    }
}
