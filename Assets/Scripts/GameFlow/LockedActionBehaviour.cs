using ProjetoVN.Core.State;
using ProjetoVN.Inventory;
using UnityEngine;
using UnityEngine.Events;

namespace ProjetoVN.GameFlow
{
    /// <summary>
    /// Portão de cena: libera uma ação quando os requisitos são atendidos e <b>lembra</b> que já
    /// liberou. A memória é uma variável booleana de história (<see cref="StoryState"/>), então ela já
    /// entra no save por <c>GameState.storyBools</c> e sobrevive a recarregar a cena.
    /// <para>
    /// Os quatro eventos separam <b>estado</b> de <b>ação</b>, e essa é a distinção que importa:
    /// <c>OnOpened</c> é "este portão está aberto" (dispara ao destrancar e de novo a cada carga de
    /// cena em que ele já esteja aberto), enquanto <c>OnAlreadyUnlocked</c> é "o jogador interagiu
    /// com um portão aberto" (dispara só por clique). Ligar uma troca de cena no primeiro
    /// teleportaria o jogador sozinho no <c>Start()</c>; ela pertence ao segundo.
    /// </para>
    /// <para>
    /// Mora no <c>GameFlow</c>, e não no <c>Inventory</c>, porque combina item (Inventory) com variável
    /// de história (Core): é progressão de história, não regra de inventário. É ligado ao objeto pelo
    /// <c>UnityEvent</c> <c>OnInteract</c> do <c>InteractableItem</c>, igual ao
    /// <see cref="InteractableDialogueTrigger"/>.
    /// </para>
    /// </summary>
    public sealed class LockedActionBehaviour : MonoBehaviour
    {
        [Header("Requisitos (preencha pelo menos um; valem juntos)")]
        [Tooltip("Item exigido. É consumido ao destrancar. Vazio = portão só de flag.")]
        [SerializeField] private ItemDataSO requiredItem;

        [Tooltip("Variável de história exigida, escolhida entre as booleanas declaradas em Assets/Roteiro/variaveis.yarn " +
                 "(ex.: $falou_com_luna). NÃO é consumida. '(nenhuma)' = portão só de item. " +
                 "Uma variável nova entra em variaveis.yarn; um valor que não está lá aparece com aviso.")]
        [SerializeField, StoryFlag] private string requiredFlagId;

        [Header("Memória")]
        [Tooltip("Variável de história ligada ao destrancar, escolhida entre as booleanas declaradas em Assets/Roteiro/variaveis.yarn " +
                 "(ex.: $porta_biblioteca_destrancada). É o que mantém o portão aberto depois. " +
                 "'(nenhuma)' = sem memória: o portão consome o item e volta a trancar.")]
        [SerializeField, StoryFlag] private string unlockedFlagId;

        [Header("Events")]
        [Tooltip("O momento em que destrancou: a narrativa do 'a chave serviu'. Dispara uma vez só.")]
        public UnityEvent OnUnlocked;

        [Tooltip("Faltou o requisito.")]
        public UnityEvent OnLocked;

        [Tooltip("ESTADO, não ação: 'este portão está aberto'. Dispara ao destrancar e de novo no " +
                 "Start() de toda cena em que ele já esteja aberto. Use para aparência e passagem " +
                 "(sprite, collider). Nunca ligue aqui algo iniciado pelo jogador — isso dispararia sozinho.")]
        public UnityEvent OnOpened;

        [Tooltip("AÇÃO do jogador sobre um portão já aberto — atravessar, trocar de cena. " +
                 "É um gancho puro: se nada estiver ligado aqui, nada acontece (de propósito).")]
        public UnityEvent OnAlreadyUnlocked;

        public void Interact()
        {
            if (!HasRequirement())
            {
                Debug.LogError("[LockedActionBehaviour] Nem 'requiredItem' nem 'requiredFlagId' foram preenchidos: este portão não tranca nada. Interação ignorada.", this);
                return;
            }

            // StoryState.IsTrue("") é sempre false, então um 'unlockedFlagId' vazio nunca entra aqui:
            // o portão simplesmente fica sem memória, exatamente como era antes.
            if (StoryState.IsTrue(unlockedFlagId))
            {
                OnAlreadyUnlocked?.Invoke();
                return;
            }

            // A flag é checada ANTES do item de propósito: ela não consome nada. Na ordem inversa,
            // um portão que exige os dois gastaria a chave do jogador só para descobrir que a flag
            // ainda não estava ligada. Não inverta.
            if (!string.IsNullOrWhiteSpace(requiredFlagId) && !StoryState.IsTrue(requiredFlagId))
            {
                OnLocked?.Invoke();
                return;
            }

            if (requiredItem == null)
            {
                Unlock();
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[LockedActionBehaviour] Não há InventoryManager na cena. Interação ignorada.", this);
                return;
            }

            if (!InventoryManager.Instance.TryUse(requiredItem))
            {
                OnLocked?.Invoke();
                return;
            }

            Unlock();
        }

        private bool HasRequirement() =>
            requiredItem != null || !string.IsNullOrWhiteSpace(requiredFlagId);

        /// <summary>
        /// Restaura o estado aberto ao carregar a cena. <c>ManagersBootstrap</c> roda em
        /// <c>AfterSceneLoad</c>, ou seja, antes de qualquer <c>Start()</c>, e <c>StoryState</c> é
        /// estático — então aqui a variável já reflete a sessão (ou o save) atual.
        /// </summary>
        private void Start()
        {
            if (StoryState.IsTrue(unlockedFlagId)) OnOpened?.Invoke();
        }

        /// <summary>Grava a memória e dispara os eventos. <c>SetBool</c> ignora sozinho um nome vazio.</summary>
        private void Unlock()
        {
            StoryState.SetBool(unlockedFlagId, true);

            // Estado antes da narrativa: o portão já está visualmente aberto quando a fala aparece.
            OnOpened?.Invoke();
            OnUnlocked?.Invoke();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!HasRequirement())
                Debug.LogWarning("[LockedActionBehaviour] Nem 'requiredItem' nem 'requiredFlagId' foram preenchidos: este portão abriria sempre. Preencha pelo menos um.", this);

            if (string.IsNullOrWhiteSpace(unlockedFlagId))
                Debug.LogWarning("[LockedActionBehaviour] O campo 'unlockedFlagId' está vazio: este portão não lembra que foi aberto e vai trancar de novo depois de consumir o item. Preencha um nome único (ex.: \"$porta_mecanicas_destrancada\").", this);

            if (!string.IsNullOrWhiteSpace(unlockedFlagId) && unlockedFlagId == requiredFlagId)
                Debug.LogWarning("[LockedActionBehaviour] 'requiredFlagId' e 'unlockedFlagId' são a mesma variável: o portão se considera aberto antes de abrir, e OnUnlocked nunca dispara. Use nomes diferentes.", this);

            StoryVariableNameCheck.WarnIfOffConvention(nameof(LockedActionBehaviour), nameof(requiredFlagId), requiredFlagId, this);
            StoryVariableNameCheck.WarnIfOffConvention(nameof(LockedActionBehaviour), nameof(unlockedFlagId), unlockedFlagId, this);
        }
#endif
    }
}
