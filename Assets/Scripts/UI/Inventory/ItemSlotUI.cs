using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjetoVN.UI.Inventory
{
    public sealed class ItemSlotUI : MonoBehaviour
    {
        [Tooltip("A Image do ícone, desligada quando o item não tem ícone. Vazio = o slot não mostra ícone.")]
        [SerializeField] private Image iconImage;

        [Tooltip("O texto com o nome do item. Vazio = o slot não mostra o nome.")]
        [SerializeField] private TextMeshProUGUI nameLabel;

        public void Setup(string itemName, Sprite icon)
        {
            if (nameLabel != null) nameLabel.text = itemName;

            if (iconImage != null)
            {
                bool hasIcon = icon != null;
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(hasIcon);
            }
        }
    }
}
