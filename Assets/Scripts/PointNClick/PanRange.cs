using System;

namespace ProjetoVN.PointNClick
{
    /// <summary>O intervalo de posições x permitidas ao cenário no pan.</summary>
    public readonly struct PanRange
    {
        public float Min { get; }
        public float Max { get; }

        public PanRange(float min, float max)
        {
            if (min > max)
                throw new ArgumentException($"O limite mínimo ({min}) é maior que o máximo ({max}).");

            Min = min;
            Max = max;
        }

        public float Clamp(float x) => Math.Min(Math.Max(x, Min), Max);
    }
}
