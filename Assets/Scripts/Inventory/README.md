# Inventory (Inventário)

O módulo **Inventory** gerencia os itens coletáveis do jogo. Ele foi desenhado para manter a lógica de dados separada da interface visual (UI) e da interação na cena, comunicando-se de forma assíncrona.

---

## Estrutura Principal

- **`ItemDataSO` (ScriptableObject)**: A representação estática de um item (nome, ícone, descrição). Criado por Game Designers no Editor da Unity.
- **`InventoryModel` e `InventoryService`**: Classes em puro C# que lidam com a lógica de armazenar os itens coletados e verificar se o jogador possui um item específico.
- **`InventoryManager` (MonoBehaviour)**: Componente que atua como ponte, expondo a API do inventário para o jogo, instanciando o modelo/serviço e assinando as mensagens do `MessageBroker`.
- **`LockedActionBehaviour`**: Um utilitário de cena que bloqueia uma ação até que um item específico do inventário seja usado.

---

## Fluxo de Interações e Mensagens

Assim como os outros módulos, o **Inventory** depende unicamente do módulo `Core` para usar o `MessageBroker`.

### 1. Coletando um Item
Quando o jogador clica em um item no cenário (via módulo `PointNClick`), o fluxo de coleta se inicia:

1. O objeto da cena publica um **`CollectItemCommandMessage`** contendo o `ItemDataSO`.
2. O `InventoryManager` escuta esse comando e instrui o `InventoryService` a adicionar o item ao `InventoryModel`.
3. Uma vez validado e adicionado com sucesso, o `InventoryManager` publica um **`ItemCollectedMessage`**.
   - *Consequência:* O módulo de `UI` (que possui a gaveta do inventário) assina essa mensagem e adiciona visualmente o novo item na tela, totalmente desacoplado da lógica de coleta.

### 2. Usando um Item
Quando o jogador tenta interagir com um elemento da cena usando um item do inventário (ex: Usar *Chave* na *Porta*):

1. O jogador arrasta o item na interface (UI) sobre o alvo, disparando um **`UseItemCommandMessage`**.
2. O `InventoryManager` intercepta a mensagem, verifica se o item existe no modelo e o consome (se for consumível).
3. Após o consumo, publica a mensagem **`ItemUsedMessage`**.
   - *Consequência:* A UI remove o ícone correspondente e sistemas da cena (como `LockedActionBehaviour`) detectam o uso para destravar portas, ativar animações, etc.

### 3. Consultas de Estado
Sistemas da cena ou fluxos de diálogo que precisem bifurcar dependendo dos itens do jogador utilizam o **`CheckItemRequestMessage`** (ou chamam a API diretamente caso estejam no mesmo escopo) para saber se a condição foi atingida.
