using UnityEngine;

namespace ProjetoVN.Inventory
{
    [CreateAssetMenu(fileName = "NewItemSO", menuName = "Items/Item")]
    public sealed class ItemDataSO : ScriptableObject
    {
        [Tooltip("Identificador único do item, usado pelo roteiro (<<dar_item chave_teste>>, tem_item(\"chave_teste\")) e gravado no save: " +
                 "minúsculas sem acento, dígitos e _ (ex.: chave_teste, não Chave Teste). " +
                 "Vazio = o console avisa, e o roteiro e o save não acham o item.")]
        [SerializeField] private string id = "";

        [Tooltip("Nome mostrado no painel de inventário e no histórico da conversa quando o roteiro dá ou tira o item (ex.: Chave Teste). " +
                 "Vazio = o item aparece sem nome, e o console avisa.")]
        [SerializeField] private string itemName = "New Item";

        [Tooltip("Texto de examinar o item (ex.: Uma chave de latão, gasta pelo uso). Ainda não aparece no jogo: examinar pelo painel está fora do escopo. " +
                 "Vazio = nada muda hoje.")]
        [SerializeField] private string description = "Description";

        [Tooltip("Ícone do item no slot do painel de inventário. Vazio = o slot mostra só o nome.")]
        [SerializeField] private Sprite icon = null;

        public string Id => id;
        public string ItemName => itemName;
        public string Description => description;
        public Sprite Icon => icon;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                Debug.LogWarning("[ItemDataSO] O campo 'Id' está vazio. Preencha um Id único.", this);

            if (string.IsNullOrWhiteSpace(itemName))
                Debug.LogWarning("[ItemDataSO] O campo 'Item Name' está vazio: o item aparece sem nome no painel. Preencha o nome.", this);
        }
#endif
    }
}
