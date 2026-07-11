using UnityEngine;

namespace ProjetoVN.Inventory
{
    [CreateAssetMenu(fileName = "NewItemSO", menuName = "Items/Item")]
    public sealed class ItemDataSO : ScriptableObject
    {
        [SerializeField] private string id = "default-id";
        [SerializeField] private string itemName = "New Item";
        [SerializeField] private string description = "Description";
        [SerializeField] private Sprite icon = null;

        public string Id => id;
        public Sprite Icon => icon;

        public Item ToDomainItem() => new Item(id, itemName, description);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                Debug.LogWarning($"[ItemDataSO] O campo 'Id' do asset '{name}' está vazio. Preencha um Id único.");
        }
#endif
    }
}
