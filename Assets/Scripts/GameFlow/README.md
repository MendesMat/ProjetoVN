# GameFlow

O módulo **GameFlow** é o grande orquestrador do jogo. Ele é responsável por gerenciar o estado global da aplicação e coordenar o funcionamento dos outros módulos (como Diálogos, Point'n'Click e UI) em alto nível.

A principal responsabilidade deste módulo não é implementar as regras de negócio de como um diálogo funciona ou como um item é coletado, mas sim ditar as **regras de estado** (Ex: "Quando o jogo entra em estado de diálogo, o input do jogador no mundo deve ser desativado").

---

## Estrutura Principal

- **`GameStateController`**: O componente vital deste módulo. Ele utiliza a `IStateMachine` (fornecida pelo módulo `Core`) para alternar entre os estados macro do jogo.
- **`States/`**: Pasta contendo os estados concretos, sendo os principais:
  - `GameplayState`: O estado padrão de exploração e interação (Point'n'Click ativo).
  - `DialogueState`: O estado em que a narrativa está sendo conduzida (Point'n'Click inativo, interface de diálogo ativa).

---

## Fluxo de Interações e Arquitetura de Mensagens

O `GameFlow` integra-se com outros módulos de forma **totalmente desacoplada**, utilizando o `MessageBroker` do módulo `Core`. Este fluxo de eventos é essencial para as IAs entenderem como o ciclo do jogo é coordenado.

### 1. Entrando em Diálogo
Sempre que o jogador interage com um NPC ou um objeto narrativo (geralmente oriundo do módulo `PointNClick` ou gatilhos na cena), esse objeto publica uma **`DialogueRequestMessage`**.

**Fluxo:**
1. O `GameStateController` ouve a `DialogueRequestMessage`.
2. Repassa o comando de iniciar o diálogo para o `DialogueManager`.
3. Altera a máquina de estados para **`DialogueState`**.
4. Publica globalmente uma **`TogglePlayerInputMessage(false)`**.
   - *Consequência:* O módulo `PointNClick` intercepta essa mensagem e imediatamente bloqueia os cliques e o *panning* de tela do jogador, impedindo-o de caminhar ou interagir com itens do fundo enquanto a conversa acontece.

### 2. Retornando ao Gameplay
Ao término dos nós de um ScriptableObject de diálogo, a lógica de diálogos encerra sua execução.

**Fluxo:**
1. A camada lógica de diálogos publica a mensagem **`DialogueEndedMessage`**.
2. O `GameStateController` ouve a `DialogueEndedMessage`.
3. Altera a máquina de estados de volta para **`GameplayState`**.
4. Publica globalmente uma **`TogglePlayerInputMessage(true)`**.
   - *Consequência:* O módulo `PointNClick` libera novamente a movimentação e interação do mouse no cenário.

---

## Como Estender
Se você estiver criando um novo módulo (ex: `MiniGame`), você deverá:
1. Criar um novo estado (ex: `MiniGameState`).
2. Fazer o `GameStateController` registrar esse novo estado no `Awake()`.
3. Definir as mensagens que causarão a transição (ex: `MiniGameStartRequestMessage`).
4. Replicar a lógica de bloquear o input (`TogglePlayerInputMessage(false)`) caso o minigame use regras próprias de interface ou mouse.
