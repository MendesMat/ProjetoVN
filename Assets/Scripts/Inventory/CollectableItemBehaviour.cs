using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjetoVN.Inventory
{
    public sealed class CollectableItemBehaviour : MonoBehaviour
    {
        [Tooltip("O item que entra no inventário ao coletar. Vazio = o clique só registra um erro no console e nada é coletado.")]
        [SerializeField] private ItemDataSO itemData;

        [Tooltip("Identificador gravado no save quando o objeto é coletado, para ele continuar coletado ao voltar à sala. " +
                 "Gerado sozinho ao acrescentar o componente e único no jogo inteiro, não só na sala. Ao duplicar o objeto ou copiá-lo " +
                 "para outra sala, use o menu de contexto do componente → Regenerate Persistent Id. " +
                 "Vazio = o objeto não lembra que foi coletado, e todos os coletáveis vazios se confundem entre si.")]
        [SerializeField] private string persistentId;

        private void Reset()
        {
            persistentId = System.Guid.NewGuid().ToString();
        }

        [ContextMenu("Regenerate Persistent Id")]
        private void RegeneratePersistentId()
        {
            persistentId = System.Guid.NewGuid().ToString();
        }

        private void Start()
        {
            if (InventoryManager.Instance != null && InventoryManager.Instance.IsWorldObjectConsumed(persistentId))
                gameObject.SetActive(false);
        }

        public void Collect()
        {
            if (itemData == null)
            {
                Debug.LogError("[CollectableItemBehaviour] 'itemData' não foi atribuído.", this);
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[CollectableItemBehaviour] Não há InventoryManager na cena. Coleta ignorada.", this);
                return;
            }

            if (!InventoryManager.Instance.Collect(itemData))
            {
                Debug.LogWarning("[CollectableItemBehaviour] O item já estava no inventário. O objeto continua no mundo.", this);
                return;
            }

            InventoryManager.Instance.MarkWorldObjectConsumed(persistentId);
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (itemData == null)
                Debug.LogWarning("[CollectableItemBehaviour] O campo 'itemData' está vazio: clicar neste objeto não coleta nada. Atribua o item.", this);

            if (string.IsNullOrWhiteSpace(persistentId))
            {
                Debug.LogWarning("[CollectableItemBehaviour] O campo 'persistentId' está vazio: este objeto não lembra que foi coletado. Use o menu de contexto do componente → Regenerate Persistent Id.", this);
                return;
            }

            CollectableItemBehaviour other = OtherInSceneWithSamePersistentId();
            if (other != null)
                Debug.LogWarning($"[CollectableItemBehaviour] O 'persistentId' é o mesmo de '{other.name}': coletar um faz o outro sumir. Use o menu de contexto do componente → Regenerate Persistent Id em um dos dois.", this);
        }

        private CollectableItemBehaviour OtherInSceneWithSamePersistentId()
        {
            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded) return null;

            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<CollectableItemBehaviour>(true))
                .FirstOrDefault(other => other != this && other.persistentId == persistentId);
        }
#endif
    }
}
