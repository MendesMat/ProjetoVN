namespace ProjetoVN.GameFlow.FrameRate
{
    /// <summary>O par de valores de apresentação que o limite sem foco guarda e devolve.</summary>
    public readonly struct FrameRateSettings
    {
        public int VSyncCount { get; }
        public int TargetFrameRate { get; }

        public FrameRateSettings(int vSyncCount, int targetFrameRate)
        {
            VSyncCount = vSyncCount;
            TargetFrameRate = targetFrameRate;
        }
    }
}
