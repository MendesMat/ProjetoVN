# UI (Interface de Usuário)

O módulo **UI** agrupa os elementos visuais de tela (HUDs, menus, janelas e painéis) do jogo.

> Antes de mudar qualquer coisa aqui, leia as [Regras de comunicação](../Core/Messaging/README.md#regras-de-comunicação) e o [`ARCHITECTURE_ROADMAP.md`](../../../ARCHITECTURE_ROADMAP.md).

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

1. No `OnEnable`, assina `ItemCollectedMessage` e `ItemUsedMessage` **e** se reconstrói a partir de `InventoryManager.Instance.Items`.
2. A partir daí, cada `ItemCollectedMessage` adiciona um slot e cada `ItemUsedMessage` remove um.
3. No `OnDisable`, cancela as assinaturas.

O passo 1 é o que faz o painel estar certo quando o jogador coleta um item com o inventário fechado e só depois o abre. É também o que permitirá reconstruir a tela depois de carregar um save.

As mensagens carregam o próprio `ItemDataSO`, então o slot lê nome e ícone direto do asset. **Não** mantenha uma lista de itens no Inspector da UI para procurar ícones: isso seria uma segunda fonte de verdade, e foi justamente o que essa estrutura eliminou.

---

## Dependências

A UI compila no seu próprio assembly, **`ProjetoVN.UI`**, e declara explicitamente o que enxerga: `ProjetoVN.Core`, `ProjetoVN.Inventory`, TextMeshPro e uGUI. Isso deixa de ser uma convenção e passa a ser garantido pelo compilador: um script de UI **não consegue** referenciar `Dialogue`, `PointNClick` ou `GameFlow`, e nenhum módulo referencia a UI. Remover esta pasta não quebra a compilação do jogo.

Se uma tela nova precisar de outro módulo, adicione a referência no `ProjetoVN.UI.asmdef` conscientemente — é esse o ponto de ter o arquivo.

A UI de diálogo é a exceção proposital: ela vive dentro do módulo `Dialogue` (`Dialogue/UI/`), junto da lógica que a alimenta.
