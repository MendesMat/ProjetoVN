using System.Collections.Generic;

namespace ProjetoVN.UI.Components
{
    /// <summary>
    /// A regra de navegação de uma coluna de botões, sem Unity: dado quais itens estão disponíveis (ativos e interativos),
    /// diz qual é o vizinho disponível acima e abaixo de um item.
    /// </summary>
    public static class NavigationChain
    {
        public const int None = -1;

        public static int Below(IReadOnlyList<bool> available, int from, bool wrapAround) =>
            Neighbour(available, from, step: 1, wrapAround);

        public static int Above(IReadOnlyList<bool> available, int from, bool wrapAround) =>
            Neighbour(available, from, step: -1, wrapAround);

        private static int Neighbour(IReadOnlyList<bool> available, int from, int step, bool wrapAround)
        {
            int count = available.Count;
            for (int distance = 1; distance < count; distance++)
            {
                int index = from + step * distance;
                bool leftTheColumn = index < 0 || index >= count;
                if (leftTheColumn && !wrapAround) return None;

                int candidate = (index % count + count) % count;
                if (available[candidate]) return candidate;
            }

            return None;
        }
    }
}
