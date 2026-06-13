using UnityEngine;

namespace ProjetoVN.PointNClick
{
    public class InteractableItem : MonoBehaviour
    {
        [Header("Highlight Settings")]
        [SerializeField] private float highlightScaleMultiplier = 1.08f;
        [SerializeField] private GameObject outlineObject;

        private Vector3 originalScale;
        private bool isHighlighted;

        private void Awake()
        {
            originalScale = transform.localScale;
            DisableOutline();
        }

        public void OnHoverEnter() => ApplyHighlight();
        public void OnHoverExit() => RemoveHighlight();
        public void OnClick() => Debug.Log($"Interagiu com: {gameObject.name}");

        private void ApplyHighlight()
        {
            if (isHighlighted) return;

            isHighlighted = true;
            transform.localScale = originalScale * highlightScaleMultiplier;
            EnableOutline();
        }

        private void RemoveHighlight()
        {
            if (!isHighlighted) return;

            isHighlighted = false;
            transform.localScale = originalScale;
            DisableOutline();
        }

        private void EnableOutline()
        {
            if (outlineObject != null) outlineObject.SetActive(true);
        }

        private void DisableOutline()
        {
            if (outlineObject != null) outlineObject.SetActive(false);
        }
    }
}
