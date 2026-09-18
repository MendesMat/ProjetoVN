using System.Collections.Generic;
using ProjetoVN.GameFlow.Persistence;
using ProjetoVN.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjetoVN.GameFlow.DevTools
{
    public sealed class SaveLoadDebugPanel : MonoBehaviour
    {
        public void Save()
        {
            if (GameSaveManager.Instance == null)
            {
                Debug.LogError("[SaveLoadDebugPanel] Não há GameSaveManager.", this);
                return;
            }

            GameSaveManager.Instance.Save();
        }

        public void Load()
        {
            if (GameSaveManager.Instance == null)
            {
                Debug.LogError("[SaveLoadDebugPanel] Não há GameSaveManager.", this);
                return;
            }

            GameSaveManager.Instance.Load();
        }

        public void ResetSession()
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[SaveLoadDebugPanel] Não há InventoryManager.", this);
                return;
            }

            InventoryManager.Instance.ReplaceAll(new List<ItemDataSO>());
            InventoryManager.Instance.ReplaceConsumedWorldObjectIds(new List<string>());
        }

        public void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
