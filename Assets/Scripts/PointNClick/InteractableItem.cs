using UnityEngine;
using UnityEngine.Events;

namespace ProjetoVN.PointNClick
{
    public class InteractableItem : MonoBehaviour
    {
        [Header("Interaction")]
        public UnityEvent OnInteract;

        [Header("Highlight Settings")]
        [SerializeField] private float highlightScaleMultiplier = 1.08f;
        [SerializeField] private GameObject outlineObject;

        private Vector3 _originalScale;
        private bool _isHighlighted;

        private void Awake()
        {
            _originalScale = transform.localScale;
            DisableOutline();
        }

        public void OnHoverEnter() => ApplyHighlight();
        public void OnHoverExit() => RemoveHighlight();
        public void OnClick() => OnInteract?.Invoke();

        private void ApplyHighlight()
        {
            if (_isHighlighted) return;

            _isHighlighted = true;
            transform.localScale = _originalScale * highlightScaleMultiplier;
            EnableOutline();
        }

        private void RemoveHighlight()
        {
            if (!_isHighlighted) return;

            _isHighlighted = false;
            transform.localScale = _originalScale;
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
