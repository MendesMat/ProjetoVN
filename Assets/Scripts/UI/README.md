# UI (Interface de Usuário)

O módulo **UI** agrupa os elementos visuais de tela (HUDs, menus, janelas e painéis) do jogo.

> **Antes de mudar qualquer coisa aqui:** [regras de comunicação](../../../docs/arquitetura/visao-geral.md#regras-de-comunicação) · [decisões](../../../docs/arquitetura/decisoes.md) · [fluxo de trabalho](../../../docs/agentes/fluxo-de-trabalho.md)

---

## Estrutura Principal

- **`Framework/`**: A base estrutural de qualquer tela.
  - **`UIWindow`**: Classe base para um painel que abre, fecha e define qual elemento recebe o foco.
  - **`UIWindowManager`**: Coordena qual janela está aberta (`OpenWindow`, `CloseCurrentWindow`). *Não* trata a tecla ESC nem empilha janelas — se isso for necessário, será preciso implementar.
- **`Components/`**: Elementos de interface reutilizáveis. `UISelectableBase` centraliza o estado de destaque (`isPointerOver` e `isSelected` são independentes: o mouse sair não desfaz a seleção do teclado) e `MenuButtonUI` o aplica às cores e ao ornamento.
  - **`MenuButtonUI` só cuida da aparência.** Clique e Enter se ligam no `Button.onClick`, que é o único caminho (o `MenuButtonUI` não tem `onClick` próprio nem `AddListener`: com os dois, uma ligação dispararia duas vezes no Enter, porque o `ExecuteEvents` entrega o Submit a todos os `ISubmitHandler` do objeto).
  - **Botão desabilitado (#17).** `public void SetInteractable(bool)` liga ou desliga o `Button` e **redesenha na hora** (o visual só era refeito no `OnEnable` e nos eventos de seleção, então desligar o `Button` depois deixaria o texto na cor normal). Desabilitado, o texto vai para `disabledTextColor`, o ornamento (`decorateImage`) não aparece e o mouse por cima **não o seleciona** (`UISelectableBase.OnPointerEnter`). Um `UnityEvent<bool>` do Inspector o liga como método dinâmico (o valor vem de quem invoca).
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

O painel (`InventoryPanel`, com o `InventoryPresenter`) mora em `Assets/Prefabs/UI/GameUI.prefab`, dentro do `Canvas_Game`. Esse prefab é a interface de jogo persistente (D-19): vem aninhado no `Managers.prefab`, é criado uma vez pelo bootstrap e nenhuma cena o contém. O `EventSystem` do jogo mora no mesmo prefab; `Menu.unity` também não tem o seu.

### O menu principal (#17)

`Assets/Scenes/Menu.unity` é a cena 0 do build. O `Title Window` (`UIWindow`) tem os quatro botões, todos `Button` + `MenuButtonUI` com navegação **Explicit**; as ligações moram no `Button.onClick`: NOVO JOGO, CONTINUAR e SAIR chamam o `MainMenu` do módulo `GameFlow`, CONFIGURAÇÕES abre a `Settings Window`. **A UI não enxerga o `GameFlow`** (`ProjetoVN.UI.asmdef`): a ligação é só por `UnityEvent` serializado na cena, e o `MainMenu` chama o `SetInteractable` do CONTINUAR pelo evento `On Save Checked`. A lógica do menu, o que cada botão faz e os campos do `MainMenu` estão no [README do GameFlow](../GameFlow/README.md#6-menu-principal-17).

A `Confirm New Game Window` (`UIWindow`, inativa, irmã da `Settings Window`) é o clone do `Title Window` com a pergunta e dois botões, NOVO JOGO e VOLTAR. Ela abre pelo `On New Game Needs Confirmation` do `MainMenu` (`UIWindowManager.OpenWindow`), tem o VOLTAR como `firstSelected` (a resposta segura sob o Enter) e `rememberLastSelected` desligado, para abrir sempre nele. O `Midle` não controla a altura dos filhos, então o `Confirm Text` tem a altura escrita no próprio `RectTransform` (160): um `LayoutElement` ali não vale e o texto sobreporia os botões.

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
| #18 | Menu de pausa; o `UIWindowManager` ganha uma pilha simples de janelas (decisão D-13) |

A interface do jogo é **uGUI**. Para criar ou alterar telas, use a skill `unity:ui-ugui`.
