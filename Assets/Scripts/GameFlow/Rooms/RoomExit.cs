using UnityEngine;

namespace ProjetoVN.GameFlow.Rooms
{
    /// <summary>
    /// Uma saída da sala: guarda a sala de destino e o ponto de entrada. Liga-se no <c>OnInteract</c> de um objeto ou no
    /// <c>OnAlreadyUnlocked</c> de uma porta. <see cref="Leave"/> é <c>void</c> para o <c>UnityEvent</c> do Inspector listá-lo (D-07).
    /// </summary>
    public sealed class RoomExit : MonoBehaviour
    {
        [Tooltip("O nome da cena de destino, exatamente como o arquivo (ex.: [Teste] Sala B). Precisa estar na lista de cenas do build. " +
                 "Vazio = o clique avisa no console e nada acontece.")]
        [SerializeField] private string destinationRoom;

        [Tooltip("O id de um Room Entry Point da sala de destino: onde o jogador chega. " +
                 "Vazio = chega na posição em que a cena foi salva, com aviso no Inspector.")]
        [SerializeField] private string entryPointId;

        public void Leave()
        {
            if (RoomNavigator.Instance == null)
            {
                Debug.LogError("[RoomExit] Não há RoomNavigator (o prefab Managers não foi criado). Saída ignorada.", this);
                return;
            }

            RoomNavigator.Instance.ChangeRoom(destinationRoom, entryPointId);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(destinationRoom))
                Debug.LogWarning("[RoomExit] O campo Destination Room está vazio: o clique não leva a lugar nenhum.", this);

            if (string.IsNullOrWhiteSpace(entryPointId))
                Debug.LogWarning("[RoomExit] O campo Entry Point Id está vazio: o jogador chega na posição em que a cena foi salva.", this);
        }
#endif
    }
}
