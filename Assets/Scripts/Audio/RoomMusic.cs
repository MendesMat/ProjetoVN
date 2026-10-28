using UnityEngine;

namespace ProjetoVN.Audio
{
    /// <summary>
    /// A música de uma sala, declarada na cena dela. Sem lógica: o <see cref="AudioManager"/> a procura quando a sala carrega.
    /// No máximo um por cena. Sala sem este componente, ou com o campo vazio, fica em silêncio.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomMusic : MonoBehaviour
    {
        [Tooltip("A música desta sala. Vazio = silêncio nesta sala. " +
                 "Duas salas com a mesma música não a reiniciam na troca. Importe a faixa com Load Type = Streaming.")]
        [SerializeField] private AudioClip music;

        public AudioClip Music => music;
    }
}
