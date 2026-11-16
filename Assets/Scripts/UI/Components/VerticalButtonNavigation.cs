using UnityEngine;
using UnityEngine.UI;

namespace ProjetoVN.UI.Components
{
    /// <summary>
    /// Refaz a navegação de cima e de baixo de uma coluna de botões pulando o que está inativo ou desabilitado. Com a
    /// navegação Explicit o uGUI seleciona o vizinho mesmo desabilitado, e a Vertical procuraria o vizinho em todos os
    /// botões ativos do jogo, de qualquer canvas, e sairia da janela.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VerticalButtonNavigation : MonoBehaviour
    {
        [Tooltip("Os botões da coluna, de cima para baixo. A navegação de cima e de baixo de cada um é refeita no jogo, " +
                 "pulando o desabilitado. Menos de dois botões: o componente não tem o que ligar.")]
        [SerializeField] private Selectable[] items;

        [Tooltip("Ligado: de baixo do último vai para o primeiro, e de cima do primeiro para o último. " +
                 "Desligado: a navegação para nas pontas.")]
        [SerializeField] private bool wrapAround;

        private void OnEnable() => Rebuild();

        // Fora da janela aberta os botões estão inativos e todos pareceriam indisponíveis: o OnEnable refaz tudo ao abrir.
        public void Rebuild()
        {
            if (!isActiveAndEnabled || items == null) return;

            bool[] available = AvailabilityOfItems();
            for (int index = 0; index < items.Length; index++)
            {
                if (items[index] == null) continue;

                items[index].navigation = NavigationOf(index, available);
            }
        }

        private bool[] AvailabilityOfItems()
        {
            var available = new bool[items.Length];
            for (int index = 0; index < items.Length; index++)
            {
                available[index] = IsAvailable(items[index]);
            }

            return available;
        }

        // Não é Selectable.IsActive(): no OnEnable da coluna os botões filhos ainda não rodaram o próprio OnEnable,
        // e o IsActive() deles é falso mesmo com a janela aberta.
        private static bool IsAvailable(Selectable item) =>
            item != null && item.gameObject.activeInHierarchy && item.IsInteractable();

        private Navigation NavigationOf(int index, bool[] available)
        {
            Navigation navigation = items[index].navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = ItemAt(NavigationChain.Above(available, index, wrapAround));
            navigation.selectOnDown = ItemAt(NavigationChain.Below(available, index, wrapAround));
            return navigation;
        }

        private Selectable ItemAt(int index) => index == NavigationChain.None ? null : items[index];

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (items == null || items.Length < 2)
            {
                Debug.LogWarning($"[VerticalButtonNavigation] '{name}' tem menos de dois botões na lista: não há navegação a refazer.", this);
                return;
            }

            for (int index = 0; index < items.Length; index++)
            {
                if (items[index] == null)
                    Debug.LogWarning($"[VerticalButtonNavigation] '{name}' tem um botão vazio na posição {index} da lista.", this);
                else if (System.Array.IndexOf(items, items[index]) != index)
                    Debug.LogWarning($"[VerticalButtonNavigation] '{name}' repete o botão '{items[index].name}' na lista.", this);
            }
        }
#endif
    }
}
