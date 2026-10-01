# UI (Interface de Usuário)

O módulo **UI** agrupa os elementos visuais de tela (HUDs, menus, janelas e painéis) do jogo.

> **Antes de mudar qualquer coisa aqui:** [regras de comunicação](../../../docs/arquitetura/visao-geral.md#regras-de-comunicação) · [decisões](../../../docs/arquitetura/decisoes.md) · [fluxo de trabalho](../../../docs/agentes/fluxo-de-trabalho.md)

---

## Estrutura Principal

- **`Framework/`**: A base estrutural de qualquer tela.
  - **`UIWindow`**: Classe base para um painel que abre, fecha e define qual elemento recebe o foco.
  - **`UIWindowManager`**: Coordena qual janela está aberta (`OpenWindow`, `CloseCurrentWindow`). *Não* trata a tecla ESC nem empilha janelas — se isso for necessário, será preciso implementar.
- **`Components/`**: Elementos de interface reutilizáveis. `UISelectableBase` centraliza o estado de destaque (`isPointerOver` e `isSelected` são independentes: o mouse sair não desfaz a seleção do teclado) e `MenuButtonUI` o aplica às cores e ao ornamento.
- **`Inventory/`** (e outras sub-pastas temáticas): scripts que conectam a parte visual a um módulo específico.

---

## A regra: a UI **lê** estado, mas só **modifica** chamando o dono

A UI pode consultar os managers à vontade — `InventoryManager.Instance.Items` é uma leitura legítima. O que ela não pode é **decidir regras de negócio** ou mudar estado por conta própria: para mudar algo, ela chama o método do dono daquele estado.

Uma UI que se monta **apenas** a partir de eventos fica errada dependendo de quando foi habilitada: ela perde tudo o que aconteceu enquanto estava escondida. O padrão correto neste projeto é **reconstruir do estado + aplicar os eventos incrementalmente**.

### Exemplo de Fluxo: o painel de inventário

O `InventoryPresenter` faz exatamente isso:

1. No `OnEnable`, assina `ItemCollectedMessage`, `ItemUsedMessage` e `InventoryReplacedMessage`.
2. Reconstrói-se a partir de `InventoryManager.Instance.Items` — **no `Start` na primeira vez**, e no `OnEnable` nas ativações seguintes. A primeira reconstrução não pode ser no `OnEnable`: os managers são criados pelo `ManagersBootstrap` só depois do `Awake`/`OnEnable` da cena, e o `Start` é o primeiro ponto garantido depois disso.
3. A partir daí, cada `ItemCollectedMessage` adiciona um slot, cada `ItemUsedMessage` remove um e cada `InventoryReplacedMessage` (publicada quando um save é carregado) reconstrói tudo.
4. No `OnDisable`, cancela as assinaturas.

O passo 2 é o que faz o painel estar certo quando o jogador coleta um item com o inventário fechado e só depois o abre.

### O prefab do slot

`Assets/Prefabs/UI/SlotUI.prefab`: raiz com `Image` de fundo, `LayoutElement` (140×140) e `ItemSlotUI`; filhos `Icon` (`Image`, escondido quando o item não tem ícone) e `Name` (TMP). As referências `iconImage` e `nameLabel` do `ItemSlotUI` precisam estar ligadas no prefab. Slots devolvidos ao pool são **desativados, não destruídos**, então `transform.childCount` do painel não diz quantos itens estão visíveis — conte os filhos ativos.

As mensagens carregam o próprio `ItemDataSO`, então o slot lê nome e ícone direto do asset. **Não** mantenha uma lista de itens no Inspector da UI para procurar ícones: isso seria uma segunda fonte de verdade, e foi justamente o que essa estrutura eliminou.

---

## Dependências

A UI compila no seu próprio assembly, **`ProjetoVN.UI`**, e declara explicitamente o que enxerga: `ProjetoVN.Core`, `ProjetoVN.Inventory`, TextMeshPro e uGUI. Isso deixa de ser uma convenção e passa a ser garantido pelo compilador: um script de UI **não consegue** referenciar `Dialogue`, `PointNClick` ou `GameFlow`, e nenhum módulo referencia a UI. Remover esta pasta não quebra a compilação do jogo.

Se uma tela nova precisar de outro módulo, adicione a referência no `ProjetoVN.UI.asmdef` conscientemente — é esse o ponto de ter o arquivo.

A UI de diálogo é a exceção proposital: ela vive dentro do módulo `Dialogue` (`Dialogue/UI/`), junto da lógica que a alimenta.

---

## Mudanças planejadas

| Issue | O que muda neste módulo |
|---|---|
| #9 | O painel de inventário sai das cenas e passa a morar no prefab persistente da interface de jogo |
| #17 | `Menu.unity` vira a primeira cena; Novo Jogo, Continuar e Sair passam a funcionar |
| #18 | Menu de pausa; o `UIWindowManager` ganha uma pilha simples de janelas (decisão D-13) |

A interface do jogo é **uGUI**. Para criar ou alterar telas, use a skill `unity:ui-ugui`.
