using UnityEngine;

namespace ProjetoVN.GameFlow.Rooms
{
    /// <summary>
    /// Um marcador na cena de uma sala: o lugar do cenário que fica no centro da tela quando o jogador chega por uma
    /// <see cref="RoomExit"/> que cita o <see cref="Id"/>. Só o X importa. Sem lógica: o <see cref="RoomNavigator"/> o procura.
    /// </summary>
    public sealed class RoomEntryPoint : MonoBehaviour
    {
        private const float GizmoHalfHeight = 6f;

        [Tooltip("O nome do ponto, único nesta sala, citado pelas saídas que chegam aqui (ex.: porta_biblioteca). " +
                 "Vazio = nenhuma saída consegue chegar neste ponto.")]
        [SerializeField] private string id;

        public string Id => id;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                Debug.LogWarning("[RoomEntryPoint] O campo Id está vazio: nenhuma saída consegue chegar neste ponto.", this);
        }

        private void OnDrawGizmos()
        {
            Vector3 position = transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(position + Vector3.down * GizmoHalfHeight, position + Vector3.up * GizmoHalfHeight);
        }
#endif
    }
}
