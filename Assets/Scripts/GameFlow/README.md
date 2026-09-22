# GameFlow

O módulo **GameFlow** é o orquestrador do jogo. Ele gerencia em qual **modo** a aplicação está e coordena os outros módulos (Diálogos, Point'n'Click e UI) em alto nível.

A responsabilidade deste módulo não é implementar como um diálogo funciona ou como um item é coletado, mas ditar as **regras de modo** (Ex: "enquanto o jogo estiver em diálogo, o input do jogador no mundo fica desativado").

> Antes de mudar qualquer coisa aqui, leia as [Regras de comunicação](../Core/Messaging/README.md#regras-de-comunicação) e o [`ARCHITECTURE_ROADMAP.md`](../../../ARCHITECTURE_ROADMAP.md).

---

## Estrutura Principal

- **`GameStateController`**: O componente vital deste módulo. Ele **possui** uma `StateMachine` (classe C# pura do módulo `Core`), cria os estados com `new` no `Awake` e repassa o `Update` para ela via `Tick()`. Não há nada para configurar no Inspector.
- **`States/`**: Os estados concretos:
  - `GameplayState`: exploração e interação. Seu `Enter()` libera o `PlayerInputGate`.
  - `DialogueState`: narrativa em curso. Seu `Enter()` bloqueia o `PlayerInputGate`.
- **`ManagersBootstrap`**: cria, uma única vez por sessão, o prefab `Assets/Prefabs/Resources/Managers.prefab` (`GameStateController`, `DialogueManager`, `DialogueInputHandler`, `InventoryManager`, `GameSaveManager`) e o marca `DontDestroyOnLoad`. Roda em `RuntimeInitializeOnLoadMethod(AfterSceneLoad)`, então funciona em qualquer cena. **Cenas não devem conter esses managers.** O prefab fica numa pasta `Resources` dentro de `Prefabs` porque `Resources.Load` só encontra arquivos em pastas com esse nome.
- **`Persistence/`**: `GameState` (POCO serializado) e `GameSaveManager` (`Save()`/`Load()` em JSON em `Application.persistentDataPath/savegame.json`). Salva itens, objetos de mundo consumidos e **flags de história** (`StoryFlags`, do módulo `Core`). Ainda não há menu de save; na cena de teste, use o painel de debug.
- **`LockedActionBehaviour`**: Portão de cena. Libera uma ação quando os requisitos são atendidos (um `ItemDataSO`, que é consumido, e/ou uma flag de história, que não é) e **lembra** que liberou, gravando `unlockedFlagId` em `StoryFlags` — que já entra no save, então a porta continua aberta depois de recarregar a cena ou carregar um save. Mora aqui, e não no `Inventory`, porque combina item (Inventory) com flag (Core): é progressão de história, não regra de inventário. Ligado ao objeto pelo `OnInteract` do `InteractableItem`, igual ao `InteractableDialogueTrigger`.
  - Quatro eventos, e a distinção que importa é **estado × ação**:

    | Evento | Quando dispara | Para quê |
    |---|---|---|
    | `OnLocked` | interagiu e faltou o requisito | a fala de "está trancada" |
    | `OnUnlocked` | o instante em que destrancou (uma vez só) | a fala de "a chave serviu" |
    | **`OnOpened`** | **ao destrancar _e_ no `Start()` de toda cena em que já esteja aberto** | **estado**: sprite, collider, passagem liberada |
    | `OnAlreadyUnlocked` | o jogador interagiu com um portão já aberto | **ação**: atravessar, trocar de cena |

  - `OnOpened` é o que mantém o portão aberto: como ele também roda no `Start()`, recarregar a cena ou carregar um save traz o portão de volta já aberto. **Nunca ligue nele algo iniciado pelo jogador** — uma troca de cena ligada ali teleportaria o jogador sozinho ao carregar. Isso pertence ao `OnAlreadyUnlocked`, que só dispara por clique.
  - Ao autorar, a flag é checada **antes** do item, para que um portão que exige os dois não gaste a chave só para descobrir que a flag não estava ligada.
  - *Limitação conhecida:* um `Load()` no meio da cena restaura a flag, mas o `Start()` já rodou, então a aparência do portão só se acerta ao recarregar a cena — o mesmo comportamento do `CollectableItemBehaviour`. É para isso que o painel de debug tem o botão **Reload Scene** ao lado do Load.
- **`DevTools/PlaceholderTint`**: Marcador visual provisório. Hoje está ligado ao **`OnOpened`** das duas portas, pintando-as de verde — ou seja, verde quer dizer "esta porta está aberta", e não "você clicou nela". Existe como componente porque um `UnityEvent` do Inspector não aceita argumento do tipo `Color`, então não dá para ligar `SpriteRenderer.color` direto. Troque pela arte de porta aberta quando existir.
- **`DialogueEffects/`**: As implementações concretas de `DialogueEffectSO` (`GiveItemEffect`, `RemoveItemEffect`, `SetFlagEffect`, `ClearFlagEffect`). Elas moram **aqui**, e não no módulo `Dialogue`, porque precisam enxergar `Inventory` e `Core` — e `Dialogue` não referencia nenhum dos dois. O campo em `DialogueNode`/`DialogueChoice` é da classe base abstrata, então a referência de asset resolve entre assemblies sem inverter a dependência. Ver o [README do Dialogue](../Dialogue/README.md#5-efeitos-effects).
- **`DevTools/`**: `SaveLoadDebugPanel`, ligado aos botões Save / Load / Reset Session / Reload Scene do `Canvas_Debug` da cena `[Teste] Mecanicas`. Ferramenta de teste, não UI de jogo.

---

## Fluxo de Interações e Arquitetura de Mensagens

O `GameFlow` **reage** a notificações dos outros módulos (via `MessageBroker` do `Core`) e, a partir delas, decide o estado do jogo. Ele não é quem inicia diálogos: cada módulo é dono do seu próprio ciclo de vida e apenas **avisa** o que aconteceu.

### 1. Entrando em Diálogo
O jogador clica em um objeto narrativo. O `InteractableDialogueTrigger` (via `UnityEvent` do `InteractableItem`) chama **diretamente** `DialogueManager.Instance.StartDialogue(dialogueData)` — iniciar um diálogo é um **comando** com um único dono, e comando não passa pelo barramento.

**Fluxo:**
1. O `DialogueManager` valida os dados. Se forem inválidos (nulos ou sem nós), ele loga um aviso, retorna `false` e **nada mais acontece**: o jogo segue em `GameplayState` com o input liberado.
2. Sendo válidos, o `DialogueController` publica **`DialogueStartedMessage`** *antes* de processar o primeiro nó.
3. O `GameStateController` ouve a `DialogueStartedMessage`, altera a máquina de estados para **`DialogueState`** e chama `PlayerInputGate.SetEnabled(false)`.
   - *Consequência:* O módulo `PointNClick` lê esse gate no `Update` e bloqueia cliques e *panning* de tela, impedindo o jogador de interagir com o cenário enquanto a conversa acontece.

Como a troca de estado acontece **antes** da primeira fala aparecer, nunca existe um frame em que o texto já está na tela mas o jogo ainda se considera em `GameplayState`.

### 2. Retornando ao Gameplay
Ao término dos nós de um ScriptableObject de diálogo, a lógica de diálogos encerra sua execução.

**Fluxo:**
1. A camada lógica de diálogos publica a mensagem **`DialogueEndedMessage`**.
2. O `GameStateController` ouve a `DialogueEndedMessage`.
3. Altera a máquina de estados de volta para **`GameplayState`**.
4. Chama `PlayerInputGate.SetEnabled(true)`.
   - *Consequência:* O módulo `PointNClick` libera novamente a movimentação e interação do mouse no cenário, **a partir do frame seguinte** (veja abaixo).

### 3. O `PlayerInputGate` (quem escreve o quê)
O input do jogador no mundo **não é transmitido como mensagem**; ele é um estado consultável no `PlayerInputGate`, que mora no módulo `PointNClick`.

**O `GameFlow` é o único escritor desse gate**, e escreve nele sempre de dentro do `Enter()` de um estado. Essa é uma convenção, não algo que o compilador garanta: se um novo sistema precisar bloquear o input, ele deve pedir uma troca de modo ao `GameFlow` em vez de chamar `SetEnabled` por conta própria. Manter um único escritor evita que dois sistemas fiquem sobrescrevendo o `true`/`false` um do outro.

Como o efeito mora no estado, e não em quem manda trocar de estado, é impossível entrar em `DialogueState` e esquecer de travar o input: as duas coisas são a mesma linha de código.

Ao liberar o input, o gate registra o frame da liberação e ignora cliques nesse mesmo frame. Sem isso, o clique que fecha o diálogo reabriria o diálogo imediatamente, porque o input é processado antes do `Update`.

---

## Como Estender
Se você estiver criando um novo módulo (ex: `MiniGame`), você deverá:
1. Criar um novo estado (ex: `MiniGameState`, herdando de `BaseState`), colocando no seu `Enter()`/`Exit()` tudo o que precisa valer enquanto ele estiver ativo — inclusive `PlayerInputGate.SetEnabled(false)`, se o minigame usar regras próprias de mouse.
2. Instanciar esse estado com `new` no `Awake()` do `GameStateController`. Não há registro nem fábrica.
3. Fazer o próprio módulo ser dono do seu ciclo de vida e publicar notificações de início e fim (ex: `MiniGameStartedMessage` / `MiniGameEndedMessage`), que o `GameStateController` assina para chamar `ChangeState`.

Se o novo modo for uma **sobreposição** temporária (inventário, pausa), use `Push`/`Pop` em vez de `ChangeState`: o modo de baixo recebe `Exit` ao ser coberto e `Enter` ao voltar, então os efeitos colaterais se desfazem sozinhos.
