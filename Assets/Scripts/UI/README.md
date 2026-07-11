# UI (Interface de Usuário)

O módulo **UI** agrupa todos os elementos visuais de tela (HUDs, menus, janelas e painéis) do jogo. Ele foi desenhado seguindo o padrão de *Dumb Views* (Visões "Burras"), o que significa que a UI nunca deve ditar regras de negócio ou buscar dados ativamente.

---

## Estrutura Principal

- **`Framework/`**: Contém a base estrutural de qualquer tela do jogo.
  - **`UIWindow`**: Classe base para qualquer painel que precise abrir/fechar, fazer transições e lidar com foco.
  - **`UIWindowManager`**: Um gerenciador opcional que ajuda a coordenar qual janela está por cima das outras, fechar janelas com a tecla ESC, etc.
- **`Components/`**: Elementos de interface reutilizáveis (botões customizados, barras, alertas).
- **`Inventory/`**: (e outras sub-pastas temáticas): Scripts específicos que conectam a parte visual a um módulo específico.

---

## O Paradigma "Reativo" e o MessageBroker

A regra de ouro deste projeto é que a UI **não se acopla** aos Managers para puxar dados. Em vez disso, ela é reativa: ela apenas "escuta" mensagens do módulo `Core` (`MessageBroker`) e atualiza seus componentes visuais.

Isso é fundamental para as IAs entenderem: se precisarem adicionar uma nova funcionalidade na interface, elas não devem fazer a UI invocar `InventoryManager.Instance.GetItems()`, mas sim fazer a UI assinar os eventos de inventário.

### Exemplo de Fluxo: Atualização do Inventário na UI

Quando o jogador pega um item no cenário, o módulo `Inventory` faz todo o processamento e, ao fim, lança a mensagem **`ItemCollectedMessage`**.

**Fluxo da UI:**
1. O script responsável por desenhar a barra de inventário (dentro de `UI/Inventory`) se inscreve (Subscribe) no `MessageBroker` no seu método `OnEnable()`.
2. Ele fica aguardando passivamente.
3. Quando a `ItemCollectedMessage` chega, o script extrai o dado (ex: ícone do `ItemDataSO`) e apenas cria um novo botão visual na tela.
4. Se o item for consumido depois (`ItemUsedMessage`), a UI apaga aquele botão correspondente.

Essa arquitetura garante que você possa deletar a pasta `UI` inteira e o jogo (PointNClick, GameFlow, Inventory) continuará rodando sem nenhum erro de compilação, mantendo total desacoplamento.
