using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Audio
{
    /// <summary>
    /// Os sons que o roteiro pode pedir, cada um por um id. Somente leitura em runtime (D-02): a música e o ambiente
    /// que estão tocando moram no <see cref="AudioManager"/>. Um som novo de roteiro é uma linha aqui.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundRegistry", menuName = "Audio/Sound Registry")]
    public sealed class SoundRegistry : ScriptableObject
    {
        [Serializable]
        public sealed class SoundEntry
        {
            [Tooltip("O nome que o roteiro digita (<<tocar_efeito freada_batida>>): minúsculas sem acento, dígitos e _. " +
                     "Único no registro inteiro. Vazio ou fora do formato = o som não pode ser pedido.")]
            [SerializeField] private string id;

            [Tooltip("O som. Vazio = o roteiro pede o id e nada toca.")]
            [SerializeField] private AudioClip clip;

            public string Id => id;

            public AudioClip Clip => clip;
        }

        [Tooltip("Músicas do <<tocar_musica id>>. Importe a faixa com Load Type = Streaming.")]
        [SerializeField] private List<SoundEntry> music = new();

        [Tooltip("Sons de ambiente do <<tocar_ambiente id>>: tocam em loop, por baixo da música. " +
                 "Importe com Load Type = Streaming.")]
        [SerializeField] private List<SoundEntry> ambience = new();

        [Tooltip("Efeitos pontuais do <<tocar_efeito id>>. Importe com Load Type = Decompress On Load.")]
        [SerializeField] private List<SoundEntry> effects = new();

        public IReadOnlyList<SoundEntry> Music => music;

        public IReadOnlyList<SoundEntry> Ambience => ambience;

        public IReadOnlyList<SoundEntry> Effects => effects;

        public bool TryGetMusic(string id, out AudioClip clip) => TryGet(music, id, out clip);

        public bool TryGetAmbience(string id, out AudioClip clip) => TryGet(ambience, id, out clip);

        public bool TryGetEffect(string id, out AudioClip clip) => TryGet(effects, id, out clip);

        private static bool TryGet(List<SoundEntry> entries, string id, out AudioClip clip)
        {
            SoundEntry entry = entries.Find(candidate => candidate != null && candidate.Id == id);
            clip = entry?.Clip;
            return clip != null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<string>();
            foreach (SoundEntry entry in AllEntries())
            {
                if (entry == null) continue;

                if (!SoundId.FollowsConvention(entry.Id))
                    Debug.LogWarning($"[SoundRegistry] O id '{entry.Id}' está vazio ou fora do formato (minúsculas sem acento, dígitos e _).", this);
                else if (!seen.Add(entry.Id))
                    Debug.LogWarning($"[SoundRegistry] Id repetido: '{entry.Id}'. Cada id vale uma vez no registro inteiro.", this);

                if (entry.Clip == null)
                    Debug.LogWarning($"[SoundRegistry] O som '{entry.Id}' está sem AudioClip: o roteiro o pede e nada toca.", this);
            }
        }

        private IEnumerable<SoundEntry> AllEntries()
        {
            foreach (SoundEntry entry in music) yield return entry;
            foreach (SoundEntry entry in ambience) yield return entry;
            foreach (SoundEntry entry in effects) yield return entry;
        }
#endif
    }
}
