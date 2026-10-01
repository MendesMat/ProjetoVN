# Point N' Click

O módulo **PointNClick** é responsável pela mecânica central de interação física do jogador com o mundo (cenário) através do mouse. Ele detecta onde o jogador está clicando e como a câmera se movimenta.

> **Antes de mudar qualquer coisa aqui:** [regras de comunicação](../../../docs/arquitetura/visao-geral.md#regras-de-comunicação) · [decisões](../../../docs/arquitetura/decisoes.md) · [fluxo de trabalho](../../../docs/agentes/fluxo-de-trabalho.md)

---

## Estrutura Principal

- **`PointNClickSelector`**: É o "motor" do clique. Ele dispara *Raycasts* a partir da posição do mouse na tela em direção à cena para detectar se o cursor está sobre algum objeto interativo (com *Colliders*). Ele gerencia os eventos de clique, repassando o comando para o objeto clicado.
- **`ScreenPanController`**: Permite que o jogador desloque a visão da câmera para as laterais ao mover o mouse para as bordas da tela, algo clássico em Visual Novels de exploração.
- **`InteractableItem`**: Componente anexado aos objetos da cena que diz ao sistema: "Eu posso ser clicado!".
- **`PlayerInputGate`**: Classe estática que guarda a **fonte única de verdade** sobre se o jogador pode interagir com o mundo agora.

---

## Fluxo de Interações e Bloqueio de Input

A grande sacada deste módulo é que ele obedece cegamente ao módulo `GameFlow` no que diz respeito a quando o jogador *pode* ou *não pode* clicar nas coisas.

### 1. Bloqueio e Liberação de Input (`PlayerInputGate`)
Durante transições de estado (exemplo: iniciar uma conversa), o jogo não pode permitir que o jogador continue clicando em cenários ao fundo.

O gate é **consultado (pull)**, nunca transmitido por mensagem. Isso importa porque um objeto que estava desativado (ou que foi carregado depois) nunca receberia uma mensagem enviada antes de ele existir, mas sempre consegue ler o estado atual.

**Regra de propriedade:** o `PointNClick` é **dono** do gate, mas o `GameFlow` é o seu **único escritor**. Nenhum outro módulo deve chamar `SetEnabled`. Se um dia outro sistema precisar bloquear o input, o caminho é passar pelo `GameFlow`, e não criar uma segunda fonte de verdade.

**API:**
- `PlayerInputGate.IsEnabled` — o input do mundo está liberado?
- `PlayerInputGate.SetEnabled(bool)` — **só o `GameFlow` chama**, e sempre de dentro do `Enter()` de um estado. Ao liberar, registra o frame em que isso aconteceu.
- `PlayerInputGate.CanClickThisFrame` — `IsEnabled`, **e** não estamos no mesmo frame em que o input foi liberado, **e** o ponteiro não está sobre a UI.

**Fluxo:**
1. O `GameFlow` entra em `DialogueState`, cujo `Enter()` chama `PlayerInputGate.SetEnabled(false)`.
2. O `PointNClickSelector` lê `IsEnabled` no seu `Update`, para de processar cliques e **limpa o destaque (hover)** do objeto que estivesse sob o mouse.
3. O `ScreenPanController` lê `IsEnabled` no seu `Update` e trava o movimento lateral da câmera.
4. Ao terminar o diálogo, o `GameFlow` volta para `GameplayState`, cujo `Enter()` chama `PlayerInputGate.SetEnabled(true)`, e tudo volta a funcionar.

Note que **nenhum dos dois desliga o próprio componente** (`enabled = false`). Eles continuam rodando e apenas consultam o gate, o que mantém o estado de hover consistente.

#### Por que `CanClickThisFrame` existe
O input do mouse é processado **antes** do `Update`. Sem essa checagem, o mesmo clique que fecha um diálogo reabriria o diálogo na sequência: o callback de input encerra a conversa e libera o gate, e o `Update` daquele mesmo frame ainda enxerga `wasPressedThisFrame == true` e clica no objeto embaixo do cursor. Ignorar o frame da liberação corta esse "clique fantasma". A checagem de ponteiro sobre a UI (via `EventSystem`) impede que cliques no inventário ou nos menus atravessem para o cenário.

### 2. Disparando Ações de Outros Módulos
O `PointNClickSelector` não sabe *o que* um objeto faz; ele apenas avisa o objeto que ele foi clicado.
A partir daí, componentes instalados no mesmo GameObject (pertencentes a outros módulos) reagem, via o `UnityEvent` `OnInteract` do `InteractableItem`. Exemplos:
- Se o objeto tiver um **`CollectableItemBehaviour`** (do módulo `Inventory`), um clique fará ele coletar o item.
- Se o objeto tiver um **`InteractableDialogueTrigger`** (do módulo `GameFlow`), o clique iniciará o diálogo configurado.

Isso garante que o `PointNClick` nunca conheça a existência de diálogos ou inventários, ele apenas lida com raycasts.

---

## Limitações conhecidas e mudanças planejadas

- O destaque (hover) ainda aparece em um objeto que esteja atrás de um painel de interface; só o clique é barrado. Só vale corrigir se ficar visivelmente errado no jogo.
- `PointNClickSelector` guarda `Camera.main` no `Awake`. Cada sala tem a sua câmera, então isso continua valendo com uma cena por sala.
- **Issue #13:** o ponto de entrada de uma sala define a posição inicial do pan (`ScreenPanController`).
