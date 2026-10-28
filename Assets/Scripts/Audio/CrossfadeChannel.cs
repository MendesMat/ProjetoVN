using UnityEngine;

namespace ProjetoVN.Audio
{
    /// <summary>
    /// Um canal de som que troca de faixa com a troca suave: duas <see cref="AudioSource"/>, uma sai enquanto a outra
    /// entra. É a mesma conta para a música e para o ambiente. As regras de <see cref="Play"/> estão no README do módulo.
    /// </summary>
    internal sealed class CrossfadeChannel
    {
        private AudioSource _entering;
        private AudioSource _leaving;
        private MusicCrossfade _crossfade;

        public AudioClip Current { get; private set; }

        public CrossfadeChannel(AudioSource first, AudioSource second)
        {
            _entering = first;
            _leaving = second;
        }

        /// <summary><c>null</c> leva o canal a silêncio; a faixa que já toca não reinicia.</summary>
        public void Play(AudioClip clip, float fadeSeconds)
        {
            if (clip == Current) return;

            AudioSource nowEntering = _leaving;
            AudioSource nowLeaving = _entering;
            PrepareToEnter(nowEntering, clip);

            Current = clip;
            _entering = nowEntering;
            _leaving = nowLeaving;
            _crossfade = new MusicCrossfade(fadeSeconds, _entering.volume, _leaving.volume);
            Apply();
        }

        public void Advance(float deltaSeconds)
        {
            if (_crossfade == null) return;

            _crossfade.Advance(deltaSeconds);
            Apply();
        }

        private static void PrepareToEnter(AudioSource source, AudioClip clip)
        {
            bool isAlreadyPlayingIt = clip != null && source.isPlaying && source.clip == clip;
            if (isAlreadyPlayingIt) return;

            source.Stop();
            source.volume = 0f;
            source.clip = clip;
            if (clip != null) source.Play();
        }

        private void Apply()
        {
            _entering.volume = _crossfade.IncomingVolume;
            _leaving.volume = _crossfade.OutgoingVolume;

            if (!_crossfade.IsDone) return;

            _leaving.Stop();
            _crossfade = null;
        }
    }
}
