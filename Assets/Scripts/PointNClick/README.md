# Point N' Click

O módulo **PointNClick** é responsável pela mecânica central de interação física do jogador com o mundo (cenário) através do mouse. Ele detecta onde o jogador está clicando e como a câmera se movimenta.

---

## Estrutura Principal

- **`PointNClickSelector`**: É o "motor" do clique. Ele dispara *Raycasts* a partir da posição do mouse na tela em direção à cena para detectar se o cursor está sobre algum objeto interativo (com *Colliders*). Ele gerencia os eventos de clique, repassando o comando para o objeto clicado.
- **`ScreenPanController`**: Permite que o jogador desloque a visão da câmera para as laterais ao mover o mouse para as bordas da tela, algo clássico em Visual Novels de exploração.
- **`InteractableItem`**: Componente anexado aos objetos da cena que diz ao sistema: "Eu posso ser clicado!".

---

## Fluxo de Interações e Bloqueio de Input

A grande sacada deste módulo é que ele obedece cegamente aos comandos do módulo `GameFlow` no que diz respeito a quando o jogador *pode* ou *não pode* clicar nas coisas.

### 1. Bloqueio e Liberação de Input (Reativo)
Durante transições de estado (exemplo: iniciar uma conversa), o jogo não pode permitir que o jogador continue clicando em cenários ao fundo. 

**Fluxo:**
1. O módulo `GameFlow` publica a mensagem **`TogglePlayerInputMessage(false)`**.
2. O `PointNClickSelector` e o `ScreenPanController` assinam e escutam essa mensagem.
3. Ao receber `false`, eles imediatamente ignoram novos cliques e travam o movimento lateral da câmera.
4. Quando o diálogo termina, uma nova mensagem **`TogglePlayerInputMessage(true)`** é enviada, destrancando os sistemas.

### 2. Disparando Ações de Outros Módulos
O `PointNClickSelector` não sabe *o que* um objeto faz; ele apenas avisa o objeto que ele foi clicado. 
A partir daí, componentes instalados no mesmo GameObject (pertencentes a outros módulos) reagem. Exemplos:
- Se o objeto tiver um **`CollectableItemBehaviour`** (do módulo `Inventory`), um clique fará ele disparar o **`CollectItemCommandMessage`**.
- Se o objeto tiver um **`InteractableDialogueTrigger`** (do módulo `GameFlow`/`Dialogue`), o clique disparará um **`DialogueRequestMessage`**.

Isso garante que o `PointNClick` nunca conheça a existência de diálogos ou inventários, ele apenas lida com raycasts.
