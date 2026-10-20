using System.Threading;
using UnityEngine;

namespace ProjetoVN.GameFlow.Rooms
{
    /// <summary>
    /// A apresentação do fade de tela: anima o alfa de um <see cref="CanvasGroup"/> e, enquanto a tela está
    /// escura ou escurecendo, faz o <c>GraphicRaycaster</c> do mesmo canvas engolir os cliques na interface.
    /// Vive no <c>Canvas_Fade</c> do <c>GameUI.prefab</c>. Não decide nada: quem manda é o <see cref="RoomNavigator"/>.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFader : MonoBehaviour
    {
        private const float Transparent = 0f;
        private const float Opaque = 1f;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public async Awaitable FadeOutAsync(float seconds, CancellationToken cancellationToken)
        {
            _canvasGroup.blocksRaycasts = true;
            await AnimateAlphaAsync(Opaque, seconds, cancellationToken);
        }

        public async Awaitable FadeInAsync(float seconds, CancellationToken cancellationToken)
        {
            await AnimateAlphaAsync(Transparent, seconds, cancellationToken);
            _canvasGroup.blocksRaycasts = false;
        }

        private async Awaitable AnimateAlphaAsync(float targetAlpha, float seconds, CancellationToken cancellationToken)
        {
            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / seconds);
            }

            _canvasGroup.alpha = targetAlpha;
        }
    }
}
