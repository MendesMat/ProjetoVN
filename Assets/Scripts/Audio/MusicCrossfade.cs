using System;

namespace ProjetoVN.Audio
{
    /// <summary>
    /// Os volumes de entrada e de saída de uma troca de música, por tempo decorrido. A curva é de potência
    /// constante: a soma das potências fica em 1, sem o "buraco" no meio que um crossfade linear abre entre duas músicas.
    /// Os volumes de partida existem para uma troca interrompida recomeçar de onde a anterior parou, sem salto.
    /// </summary>
    public sealed class MusicCrossfade
    {
        private const float QuarterTurn = MathF.PI / 2f;

        private readonly float _seconds;
        private readonly float _incomingFrom;
        private readonly float _outgoingFrom;
        private float _elapsed;

        public MusicCrossfade(float seconds, float incomingFrom, float outgoingFrom)
        {
            if (seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A duração da troca não pode ser negativa.");

            _seconds = seconds;
            _incomingFrom = incomingFrom;
            _outgoingFrom = outgoingFrom;
        }

        public bool IsDone => _elapsed >= _seconds;

        public float IncomingVolume => IsDone ? 1f : _incomingFrom + (1f - _incomingFrom) * MathF.Sin(AngleOfProgress);

        public float OutgoingVolume => IsDone ? 0f : _outgoingFrom * MathF.Cos(AngleOfProgress);

        private float AngleOfProgress => _elapsed / _seconds * QuarterTurn;

        public void Advance(float deltaSeconds)
        {
            _elapsed += deltaSeconds;
        }
    }
}
