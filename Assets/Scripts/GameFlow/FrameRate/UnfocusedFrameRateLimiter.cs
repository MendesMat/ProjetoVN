using UnityEngine;

namespace ProjetoVN.GameFlow.FrameRate
{
    /// <summary>
    /// Limita o FPS enquanto a janela do jogo está sem foco e devolve o VSync e o FPS de antes quando o foco volta.
    /// Vive no <c>Managers.prefab</c>. No Editor, "foco" é o da Game view.
    /// </summary>
    public sealed class UnfocusedFrameRateLimiter : MonoBehaviour
    {
        [Tooltip("FPS máximo enquanto a janela do jogo está sem foco (atrás de outra janela, minimizada ou em outro monitor). " +
                 "Enquanto isso o VSync fica desligado; com o foco de volta, VSync e FPS voltam ao que eram. " +
                 "No Editor, vale o foco da Game view.")]
        [Min(1)]
        [SerializeField] private int unfocusedFrameRate = 30;

        private readonly UnfocusedFrameRate _limit = new UnfocusedFrameRate();

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Apply(_limit.Limit(ReadCurrent(), unfocusedFrameRate));
                return;
            }

            if (_limit.TryRestore(out var original)) Apply(original);
        }

        private static FrameRateSettings ReadCurrent() =>
            new FrameRateSettings(QualitySettings.vSyncCount, Application.targetFrameRate);

        // O VSync vai primeiro: com ele ligado, o Unity ignora o targetFrameRate.
        private static void Apply(FrameRateSettings settings)
        {
            QualitySettings.vSyncCount = settings.VSyncCount;
            Application.targetFrameRate = settings.TargetFrameRate;
        }
    }
}
