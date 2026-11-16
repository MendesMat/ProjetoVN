using ProjetoVN.Core;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;

namespace ProjetoVN.GameFlow.Rooms
{
    /// <summary>
    /// A conversa que a sala abre sozinha quando o jogador chega nela. Quem abre é o <see cref="RoomNavigator"/>,
    /// por chamada direta, depois de a tela clarear e de o jogo voltar à exploração. Uma por sala, num objeto da cena.
    /// </summary>
    public sealed class RoomArrivalDialogue : MonoBehaviour
    {
        [Tooltip("Nó do roteiro (.yarn) que a sala abre quando o jogador chega por uma troca de sala " +
                 "(Novo Jogo, saída, Continuar, Reload Scene), depois de a tela clarear, escolhido na lista dos nós de Assets/Roteiro/. " +
                 "(nenhum) = a sala não abre conversa e avisa no console. " +
                 "O nó decide se a conversa se repete nas chegadas seguintes (por variável ou por visited()). " +
                 "Dar Play direto na sala não conta como chegada.")]
        [ScriptNode]
        [SerializeField] private string nodeName;

        internal void Open()
        {
            if (string.IsNullOrWhiteSpace(nodeName))
            {
                Debug.LogWarning("[RoomArrivalDialogue] Nenhum nó de roteiro atribuído (campo Node Name). A sala não abriu conversa.", this);
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[RoomArrivalDialogue] Não há DialogueManager (o prefab Managers não foi criado). Conversa de chegada ignorada.", this);
                return;
            }

            DialogueManager.Instance.StartDialogue(nodeName);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(nodeName))
            {
                Debug.LogWarning("[RoomArrivalDialogue] O campo Node Name está vazio: a sala não abrirá conversa ao chegar.", this);
                return;
            }

            if (ScriptNodeName.FollowsConvention(nodeName)) return;

            Debug.LogWarning($"[RoomArrivalDialogue] O nó '{nodeName}' foge do formato dos nomes de nó: minúsculas sem acento, dígitos e '_' " +
                             "(ex.: onibus_chegada).", this);
        }
#endif
    }
}
