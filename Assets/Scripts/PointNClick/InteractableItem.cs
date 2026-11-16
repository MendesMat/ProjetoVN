using UnityEngine;
using UnityEngine.Events;

namespace ProjetoVN.PointNClick
{
    public class InteractableItem : MonoBehaviour
    {
        [Header("Interaction")]
        [Tooltip("O que acontece no clique: ligue aqui as ações do objeto (Collect, TriggerDialogue, Interact, Leave; ver docs/autoria/salas.md). " +
                 "Vazio = o objeto se destaca com o mouse em cima, mas o clique não faz nada.")]
        public UnityEvent OnInteract;

        [Header("Highlight Settings")]
        [Tooltip("Quanto o objeto cresce com o mouse em cima: 1 = não cresce, 1.15 = 15% maior (o valor do prefab Interactable). " +
                 "Não aceita menos que 1.")]
        [Min(1f)]
        [SerializeField] private float highlightScaleMultiplier = 1.08f;

        [Tooltip("Objeto filho que aparece só com o mouse em cima (o contorno); começa desligado ao entrar na sala. " +
                 "Vazio = o objeto só cresce com o mouse em cima. Não pode ser o próprio objeto nem um pai dele: ele sumiria da sala.")]
        [SerializeField] private GameObject outlineObject;

        private Vector3 _originalScale;
        private bool _isHighlighted;

        private void Awake()
        {
            _originalScale = transform.localScale;
            DisableOutline();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (outlineObject == null) return;

            if (transform.IsChildOf(outlineObject.transform))
                Debug.LogWarning("[InteractableItem] O 'Outline Object' é o próprio objeto ou um pai dele: o objeto se desliga ao entrar na sala e some. Use um objeto filho.", this);
        }
#endif

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
