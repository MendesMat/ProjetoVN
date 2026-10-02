using UnityEngine;

namespace ProjetoVN.GameFlow.DevTools
{
    /// <summary>
    /// Marcador visual para um evento que ainda não tem implementação de verdade.
    /// Ligue <see cref="Apply"/> a um <c>UnityEvent</c> para enxergar que ele disparou —
    /// por exemplo o <c>OnOpened</c> de uma porta, que fica tingida enquanto a arte de
    /// porta aberta não existe.
    /// <para>
    /// Existe porque um <c>UnityEvent</c> do Inspector não aceita um argumento do tipo
    /// <c>Color</c>, então não dá para ligar <c>SpriteRenderer.color</c> direto.
    /// </para>
    /// Ferramenta de desenvolvimento: troque pelo evento real e remova o componente.
    /// </summary>
    public sealed class PlaceholderTint : MonoBehaviour
    {
        [Tooltip("Vazio = usa o SpriteRenderer deste próprio GameObject.")]
        [SerializeField] private SpriteRenderer target;

        [SerializeField] private Color tint = Color.green;

        private Color _originalColor;
        private bool _hasOriginal;

        private SpriteRenderer Target => target != null ? target : GetComponent<SpriteRenderer>();

        public void Apply()
        {
            SpriteRenderer renderer = Target;
            if (renderer == null)
            {
                Debug.LogError("[PlaceholderTint] Não há SpriteRenderer para tingir.", this);
                return;
            }

            if (!_hasOriginal)
            {
                _originalColor = renderer.color;
                _hasOriginal = true;
            }

            renderer.color = tint;
        }

        /// <summary>Volta à cor original. Útil ao resetar a sessão durante os testes.</summary>
        public void Restore()
        {
            if (!_hasOriginal) return;

            SpriteRenderer renderer = Target;
            if (renderer != null) renderer.color = _originalColor;
        }
    }
}
