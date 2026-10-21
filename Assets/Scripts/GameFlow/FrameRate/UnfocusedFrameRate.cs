using System;

namespace ProjetoVN.GameFlow.FrameRate
{
    /// <summary>
    /// A regra do limite de FPS sem foco: guarda o par de antes do limite uma vez só e o devolve uma vez só.
    /// C# puro; quem lê e escreve <c>QualitySettings</c> e <c>Application</c> é o <see cref="UnfocusedFrameRateLimiter"/>.
    /// </summary>
    public sealed class UnfocusedFrameRate
    {
        private FrameRateSettings _original;

        public bool IsLimiting { get; private set; }

        public FrameRateSettings Limit(FrameRateSettings current, int maxFrameRate)
        {
            if (maxFrameRate < 1)
                throw new ArgumentOutOfRangeException(nameof(maxFrameRate), maxFrameRate, "O limite de FPS precisa ser ao menos 1.");

            if (!IsLimiting)
            {
                _original = current;
                IsLimiting = true;
            }

            return new FrameRateSettings(0, maxFrameRate);
        }

        public bool TryRestore(out FrameRateSettings original)
        {
            original = _original;
            if (!IsLimiting) return false;

            IsLimiting = false;
            return true;
        }
    }
}
