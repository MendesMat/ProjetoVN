using UnityEngine;

namespace ProjetoVN.Audio
{
    /// <summary>
    /// Um efeito sonoro de objeto de cena, ligado por <c>UnityEvent</c> (D-07): ponha o componente em um filho do objeto e
    /// ligue o <c>Play</c> a um evento, como o <c>OnLocked</c> de uma porta. Não renomeie nem mova a classe depois de ligar:
    /// a ligação guarda o tipo pelo nome.
    /// </summary>
    public sealed class SoundEffect : MonoBehaviour
    {
        [Tooltip("O som que o Play toca. Vazio = o Play não toca nada.")]
        [SerializeField] private AudioClip clip;

        public void Play()
        {
            if (clip == null) return;

            if (AudioManager.Instance == null)
            {
                Debug.LogError($"[SoundEffect] Não há AudioManager (o prefab Managers não foi criado). O som '{clip.name}' não tocou.", this);
                return;
            }

            AudioManager.Instance.PlayEffect(clip);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (clip == null)
                Debug.LogWarning("[SoundEffect] O campo 'clip' está vazio: o Play não toca nada.", this);
        }
#endif
    }
}
