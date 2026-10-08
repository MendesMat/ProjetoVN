# GameFlow

O módulo **GameFlow** é o orquestrador do jogo. Ele gerencia em qual **modo** a aplicação está e coordena os outros módulos (Diálogos, Point'n'Click e UI) em alto nível.

A responsabilidade deste módulo não é implementar como um diálogo funciona ou como um item é coletado, mas ditar as **regras de modo** (Ex: "enquanto o jogo estiver em diálogo, o input do jogador no mundo fica desativado").

> **Antes de mudar qualquer coisa aqui:** [regras de comunicação](../../../docs/arquitetura/visao-geral.md#regras-de-comunicação) · [decisões](../../../docs/arquitetura/decisoes.md) · [fluxo de trabalho](../../../docs/agentes/fluxo-de-trabalho.md)

---

## Estrutura Principal

- **`GameStateController`**: O componente vital deste módulo. Ele **possui** uma `StateMachine` (classe C# pura do módulo `Core`), cria os estados com `new` no `Awake` e repassa o `Update` para ela via `Tick()`. Não há nada para configurar no Inspector.
- **`States/`**: Os estados concretos:
  - `GameplayState`: exploração e interação. Seu `Enter()` libera o `PlayerInputGate`.
  - `DialogueState`: narrativa em curso. Seu `Enter()` bloqueia o `PlayerInputGate`.
- **`ManagersBootstrap`**: cria, uma única vez por sessão, o prefab `Assets/Prefabs/Resources/Managers.prefab` (`GameStateController`, `DialogueManager`, `DialogueRunner`, `StoryStateVariableStorage`, `DialogueInputHandler`, `InventoryManager`, `GameSaveManager`, `ItemScriptActions`) e o marca `DontDestroyOnLoad`. O prefab traz aninhada a interface de jogo (`Assets/Prefabs/UI/GameUI.prefab`: `Canvas_Game` e `EventSystem`), que nasce e persiste junto, sem código no bootstrap (D-19); o `GameFlow` não referencia o módulo `UI`, a ligação é só de asset. Roda em `RuntimeInitializeOnLoadMethod(AfterSceneLoad)`, então funciona em qualquer cena. **Cenas não devem conter esses managers, nem `Canvas` de jogo, nem `EventSystem`.** O prefab fica numa pasta `Resources` dentro de `Prefabs` porque `Resources.Load` só encontra arquivos em pastas com esse nome.
- **`Persistence/`**: `GameState` (POCO serializado) e `GameSaveManager` (`Save()`/`Load()` em JSON em `Application.persistentDataPath/savegame.json`). Salva itens, objetos de mundo consumidos e o **estado da história** (`StoryState`, do módulo `Core`: booleanos, números e textos). Ainda não há menu de save; na cena de teste, use o painel de debug.
  - O save tem **um formato único**: `GameState` guarda o estado da história em três listas de pares nome e valor (`storyBools`, `storyNumbers`, `storyTexts`, tipos em `StoryEntries.cs`), porque o `JsonUtility` não serializa dicionários. Não há campo nem conversão de formato anterior (D-20).
  - `StoryStatePersistence` (C# puro, estático) faz a cópia entre o `StoryState` e o `GameState`: `Capture` no `Save()` e `Restore` no `Load()`. Fica fora do `GameSaveManager`, que é `MonoBehaviour` e lê arquivo, para a cópia ser testável em EditMode. Acesso a arquivo continua só no `GameSaveManager`.
  - O `JsonUtility` ignora em silêncio uma chave que não tem campo: um campo de `GameState` renomeado por engano deixa de ser lido sem aviso. Os testes de ida e volta (`GameStateTests`, `StoryStatePersistenceTests`) são a proteção.
- **`LockedActionBehaviour`**: Portão de cena. Libera uma ação quando os requisitos são atendidos (um `ItemDataSO`, que é consumido, e/ou uma variável de história booleana, que não é) e **lembra** que liberou, gravando `unlockedFlagId` no `StoryState` — que já entra no save, então a porta continua aberta depois de recarregar a cena ou carregar um save. Os campos de variável (`requiredFlagId`, `unlockedFlagId`) levam o nome com o `$`, no formato `$` + minúsculas sem acento, dígitos e `_` (ex.: `$porta_mecanicas_destrancada`); fora do formato, o `OnValidate` avisa. Os dois campos levam `[StoryFlag]` (módulo `Core`): no Inspector eles são listas das variáveis booleanas declaradas em `Assets/Roteiro/variaveis.yarn`, desenhadas pelo módulo [Editor](../Editor/README.md), com "(nenhuma)", a descrição da escolhida e um aviso para um valor não declarado. Mora aqui, e não no `Inventory`, porque combina item (Inventory) com flag (Core): é progressão de história, não regra de inventário. Ligado ao objeto pelo `OnInteract` do `InteractableItem`, igual ao `InteractableDialogueTrigger`.
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
- **`ItemScriptActions`**: os comandos e a função de roteiro que ligam o diálogo ao inventário. Mora aqui porque o `Dialogue` não referencia o `Inventory` (D-01), e o `GameFlow` enxerga os dois. É por ele que o asmdef do `GameFlow` referencia o pacote Yarn Spinner (`YarnSpinner.Unity`).

    | No roteiro | Método | O que faz |
    |---|---|---|
    | `<<dar_item id>>` | `GiveItem` | `InventoryManager.Collect`. Já ter o item não é erro, e não entra uma segunda cópia |
    | `<<remover_item id>>` | `RemoveItem` | `InventoryManager.TryUse`. Não ter o item não é erro |
    | `tem_item("id")` | `HasItem` | `InventoryManager.HasItem`; `false` se o id não existe |

  - Os métodos são **estáticos** porque é assim que o gerador de código do Yarn Spinner os acha sem registro nosso e sem exigir o nome de um GameObject no roteiro. O componente existe para guardar o `ItemRegistry` (campo `itemRegistry`, no `Managers.prefab`), que traduz o id em `ItemDataSO`; o `Instance` estático é zerado em `SubsystemRegistration` (D-29).
  - São comando e consulta: chamada direta ao `InventoryManager` (D-05). Não há mensagem nova; o painel reage a `ItemCollectedMessage` e `ItemUsedMessage`, que o inventário já publica.
  - Um id que não está no `ItemRegistry` loga `[ItemScriptActions] <<dar_item x>>: o item 'x' não existe no ItemRegistry (nó '...'). Ignorado.` e a conversa segue. O nome do nó vem de `DialogueManager.CurrentNodeName`.
  - O teste `ProjectScripts_CiteOnlyItemIdsThatExistInTheItemRegistry` confere todo id citado nos roteiros contra o `ItemRegistry`, e reprova id não literal.
  - *Limitação conhecida:* um comando com o número errado de parâmetros trava a conversa (comportamento do pacote, #37). O teste acima reprova esse roteiro antes do Play.
- **`DevTools/PlaceholderTint`**: Marcador visual provisório. Hoje está ligado ao **`OnOpened`** da porta de `[Teste] Mecanicas`, pintando-a de verde — ou seja, verde quer dizer "esta porta está aberta", e não "você clicou nela". Existe como componente porque um `UnityEvent` do Inspector não aceita argumento do tipo `Color`, então não dá para ligar `SpriteRenderer.color` direto. Troque pela arte de porta aberta quando existir.
- **`StoryVariableNameCheck`**: o aviso de autoria que o `OnValidate` do portão mostra quando um campo de variável está fora do formato (`StoryVariableName.FollowsConvention`, no `Core`).
- **`DevTools/`**: `SaveLoadDebugPanel`, ligado aos botões Save / Load / Reset Session / Reload Scene do `Canvas_Debug` da cena `[Teste] Mecanicas`. Ferramenta de teste, não UI de jogo: o `Canvas_Debug` fica **na cena**, fora do `GameUI.prefab`, e os botões dele dependem do `EventSystem` persistente (a cena não tem um).

---

## Fluxo de Interações e Arquitetura de Mensagens

O `GameFlow` **reage** a notificações dos outros módulos (via `MessageBroker` do `Core`) e, a partir delas, decide o estado do jogo. Ele não é quem inicia diálogos: cada módulo é dono do seu próprio ciclo de vida e apenas **avisa** o que aconteceu.

### 1. Entrando em Diálogo
O jogador clica em um objeto narrativo. O `InteractableDialogueTrigger` (via `UnityEvent` do `InteractableItem`) chama **diretamente** `DialogueManager.Instance.StartDialogue(nodeName)`, com o nome de um nó do roteiro `.yarn` — iniciar um diálogo é um **comando** com um único dono, e comando não passa pelo barramento. O campo `nodeName` é texto livre, com tooltip; o `OnValidate` avisa se o nome preenchido foge do formato (`ScriptNodeName`, do módulo `Dialogue`), e um nome vazio só avisa ao clicar.

**Fluxo:**
1. O `DialogueManager` valida o pedido. Se o nome for vazio, o nó não existir, o roteiro tiver erro de compilação, já houver conversa ou não houver interface registrada, ele loga um aviso (ou erro), retorna `false` e **nada mais acontece**: o jogo segue em `GameplayState` com o input liberado.
2. Sendo válido, o `DialogueRunner` do Yarn Spinner começa a conversa e o `DialogueManager` publica **`DialogueStartedMessage`** *antes* da primeira fala.
3. O `GameStateController` ouve a `DialogueStartedMessage`, altera a máquina de estados para **`DialogueState`** e chama `PlayerInputGate.SetEnabled(false)`.
   - *Consequência:* O módulo `PointNClick` lê esse gate no `Update` e bloqueia cliques e *panning* de tela, impedindo o jogador de interagir com o cenário enquanto a conversa acontece.

Como a troca de estado acontece **antes** da primeira fala aparecer, nunca existe um frame em que o texto já está na tela mas o jogo ainda se considera em `GameplayState`.

### 2. Retornando ao Gameplay
Ao término do nó (e dos nós para onde ele salta ou desvia), o `DialogueRunner` encerra a conversa.

**Fluxo:**
1. O `DialogueManager` publica a mensagem **`DialogueEndedMessage`** (também no fim anormal, quando o roteiro para por um erro).
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

---

## Mudanças planejadas

| Issue | O que muda neste módulo |
|---|---|
| #12, #13 | Troca de sala com estado de transição; saídas e pontos de entrada |
| #14 | Decide onde mora o registro de objetos de mundo consumidos (hoje no `InventoryManager`) |
| #16 | Save automático ao entrar na sala; carregar recarrega a cena |
| #18 | Estado de pausa, sobreposto com `Push`/`Pop` |
