using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjetoVN.UI.Inventory
{
    public sealed class ItemSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
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
