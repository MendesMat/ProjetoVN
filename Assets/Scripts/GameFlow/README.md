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
  - `RoomTransitionState`: troca de sala em curso (#12). Seu `Enter()` bloqueia o `PlayerInputGate`. Quem entra e sai dele é o `RoomNavigator`, pelos três membros `internal` do `GameStateController`: `IsExploring` (o modo atual é o `GameplayState`), `EnterRoomTransition()` e `LeaveRoomTransition()`.
- **`Rooms/`** (#12, #13): a troca de sala, as saídas e os pontos de entrada.
  - `RoomNavigator`: o **único dono** da troca de sala e o único lugar do jogo que chama `SceneManager.LoadScene*`. `public bool ChangeRoom(string roomName, string entryPointId)` recusa o pedido ou o aceita e dispara a troca (fluxo em "Troca de sala", abaixo); a sobrecarga de um parâmetro (`ChangeRoom(roomName)`, a do Reload Scene) chega sem ponto de entrada. O nome da sala é o **nome da cena**, e a cena precisa estar na lista de cenas do build. Vive no `Managers.prefab`, na raiz, ao lado do `GameStateController` (pega-o com `GetComponent` no `Awake`). Dois campos: `screenFader` (o `Canvas_Fade`; vazio = nenhuma troca é aceita, erro no console, aviso no `OnValidate`) e `fadeSeconds` (duração de **cada metade**, escurecer e clarear; 0,4 de partida; 0 = sem fade). `Instance` zerado em `SubsystemRegistration` (D-29). `public string CurrentEntryPointId { get; private set; }` guarda o ponto de entrada da última chegada (#16): começa `null` (o jogo começou numa sala sem passar pela troca) e fica `null` depois de uma troca sem ponto (Reload Scene); o `GameSaveManager` o grava.
  - `RoomExit` (#13): uma saída da sala. Dois campos com tooltip, `destinationRoom` (o nome da cena de destino) e `entryPointId` (o id de um `RoomEntryPoint` da sala de destino). `public void Leave()` chama `RoomNavigator.Instance.ChangeRoom(destinationRoom, entryPointId)` e ignora o `bool` (as recusas já logam); sem `RoomNavigator.Instance`, `LogError` (D-04). É `void` porque um `UnityEvent` do Inspector não lista métodos que devolvem `bool`: liga-se no **On Interact** de um objeto ou no **On Already Unlocked** de uma porta. O `OnValidate` avisa destino vazio e ponto de entrada vazio.
  - `RoomEntryPoint` (#13): um marcador na cena da sala, sem lógica. O campo `id` (público como `Id`) é o nome que as saídas citam; o ponto marca o lugar do cenário que fica **no centro da tela** na chegada, e só o X importa. O `OnValidate` avisa id vazio e o `OnDrawGizmos` desenha uma linha vertical na Scene view. Não tem `Instance` (D-29): o `RoomNavigator` o procura na cena nova.
  - `ScreenFader`: a apresentação do fade. Anima o `alpha` de um `CanvasGroup` com `Time.unscaledDeltaTime` (um `timeScale` zerado nunca prende a tela no preto) e liga o `blocksRaycasts` **antes** de escurecer e desliga **depois** de clarear. `HideImmediately()` zera o `alpha` e o bloqueio sem animação: só a recuperação de falha o usa. Não decide nada. O `Time.unscaledDeltaTime` não é limitado, então um quadro longo (um travamento) termina a metade do fade de uma vez, com o `alpha` no valor exato; o `blocksRaycasts` só desliga depois do clarear e o input só reabre depois disso (teste `RoomChange_SurvivesALongFrameWhileFadingOutAndIn`, #80).
  - O `Canvas_Fade` é filho do `GameUI.prefab`, irmão do `Canvas_Game`: `Canvas` Overlay com **Sort Order 100** (acima do `Canvas_Game`, 0, e do `Canvas_Debug` da cena de teste, 10), `GraphicRaycaster`, `CanvasGroup` (alpha 0, `blocksRaycasts` e `interactable` desligados), `ScreenFader` e uma `Image` preta `Black` que ocupa a tela toda. Em repouso nada é desenhado nem captura clique; durante a troca o `GraphicRaycaster` dele engole os cliques na interface (inventário, histórico, botões de debug), e os cliques no mundo, o hover e o pan param pelo `PlayerInputGate`.
- **`Menu/`** (#17): o menu principal. `MainMenu` (na cena `Menu.unity`) e `GameUIVisibility` (no `Canvas_Game` do `GameUI.prefab`). Detalhes em "Menu principal", abaixo.
- **`FrameRate/`** (#81): o limite de FPS com a janela sem foco (D-31).
  - `UnfocusedFrameRateLimiter`: o MonoBehaviour fino, no `Managers.prefab` ao lado do `RoomNavigator`. Em `OnApplicationFocus(false)` lê `QualitySettings.vSyncCount` e `Application.targetFrameRate`, desliga o VSync e limita o jogo a `unfocusedFrameRate` FPS (campo com tooltip e `[Min(1)]`, 30 de partida; é lido na hora da perda, então dá para ajustá-lo em Play Mode). Em `OnApplicationFocus(true)` devolve os dois valores de antes. Sem `Instance` e sem estado estático: ninguém o chama (D-29), e nenhuma mensagem é publicada (D-05). **Caso de borda conhecido:** uma janela que abre sem nunca receber o foco (atrás de outra, sem ter sido ativada) não gera `OnApplicationFocus(false)`, e o `Application.isFocused` já vale `True` nela (medido na #81 numa Development Build), então o limite só entra na primeira perda de foco depois que a janela teve foco. Consertar isso pede uma consulta nativa do sistema (código por plataforma, contra a D-15 e a D-27), e a #81 aceitou a limitação.
  - **Por que desliga o VSync:** no desktop, com `vSyncCount != 0` o Unity ignora o `targetFrameRate`. A build usa o nível de qualidade `Ultra` (`vSyncCount: 1`), então só baixar o FPS não limitaria nada. O VSync é escrito antes do FPS.
  - `UnfocusedFrameRate` (C# puro, TDD): guarda o par de antes do limite **uma vez só** (duas perdas seguidas não guardam o próprio limite como original; o Editor pode mandar o evento duas vezes no mesmo quadro) e o devolve **uma vez só** (um ganho de foco sem perda antes, como na partida, não muda nada; nesse caso o `TryRestore` devolve `false` e `default` no `out`). `maxFrameRate < 1` lança `ArgumentOutOfRangeException` (erro de programação; o campo já tem `[Min(1)]`). `FrameRateSettings` é o par `VSyncCount` e `TargetFrameRate`, para testar a regra sem tocar em `QualitySettings`, que no Editor é um asset de `ProjectSettings/`.
  - **No Editor, "foco" é o da Game view,** não o da janela. Um Editor dirigido pelo CLI mantém a Game view com o foco (medido: `Application.isFocused = True`), então o limite não age; ele age quando alguém clica em outra aba (Inspector, Console, Scene) durante o Play. **Não clique no Editor durante a suíte PlayMode:** a perda de foco no meio de uma fixture a 10 FPS aplicaria 30 FPS e reprovaria a guarda de FPS da #80.
  - Um valor só: nenhuma API do Unity diferencia a janela minimizada da janela apenas sem foco, e separar os dois casos exigiria chamada nativa por sistema (D-27).
- **`ManagersBootstrap`**: cria, uma única vez por sessão, o prefab `Assets/Prefabs/Resources/Managers.prefab` (`GameStateController`, `RoomNavigator`, `UnfocusedFrameRateLimiter`, `DialogueManager`, `DialogueRunner`, `StoryStateVariableStorage`, `DialogueInputHandler`, `InventoryManager`, `GameSaveManager`, `ItemScriptActions`) e o marca `DontDestroyOnLoad`. O prefab traz aninhada a interface de jogo (`Assets/Prefabs/UI/GameUI.prefab`: `Canvas_Game`, com o `GameUIVisibility`, `Canvas_Fade` e `EventSystem`), que nasce e persiste junto, sem código no bootstrap (D-19); o `GameFlow` não referencia o módulo `UI`, a ligação é só de asset. Roda em `RuntimeInitializeOnLoadMethod(AfterSceneLoad)`, então funciona em qualquer cena. **Cenas não devem conter esses managers, nem `Canvas` de jogo, nem `EventSystem`.** O prefab fica numa pasta `Resources` dentro de `Prefabs` porque `Resources.Load` só encontra arquivos em pastas com esse nome.
- **`Persistence/`**: `GameState` (POCO serializado), `GameStateJson` e `GameSaveManager` (`Save()`/`Load()` em JSON em `Application.persistentDataPath/savegame.json`). Salva itens, objetos de mundo consumidos, o **estado da história** (`StoryState`, do módulo `Core`: booleanos, números e textos), a cena atual (`currentScene`) e o ponto de entrada por onde o jogador chegou nela (`currentEntryPointId`, #16). Salvar automático e carregar: ver "Salvar e carregar", abaixo. O Novo Jogo e o Continuar do menu principal (#17) usam `StartNewGame` e `Load()`; o save manual pela pausa é a #18, e na cena de teste o painel de debug também salva e carrega.
  - O save tem **um formato único**: `GameState` guarda o estado da história em três listas de pares nome e valor (`storyBools`, `storyNumbers`, `storyTexts`, tipos em `StoryEntries.cs`), porque o `JsonUtility` não serializa dicionários. Não há campo nem conversão de formato anterior (D-20).
  - `StoryStatePersistence` (C# puro, estático) faz a cópia entre o `StoryState` e o `GameState`: `Capture` no `Save()` e `Restore` no `Load()`. Fica fora do `GameSaveManager`, que é `MonoBehaviour` e lê arquivo, para a cópia ser testável em EditMode. Acesso a arquivo continua só no `GameSaveManager`.
  - `GameStateJson` (C# puro, estático, TDD; #16): `TryRead(string json, out GameState state)` converte o texto do save. Texto nulo ou em branco, JSON inválido (`ArgumentException` do `JsonUtility`, que não devolve `null` nesse caso) e JSON que não é um objeto (`null`, `[1,2]`) devolvem `false` e `state = null`. `{}` é um save válido, com listas vazias e `currentScene` vazio: quem recusa a sala vazia é a troca de sala, não o parser. Não lê arquivo (D-23).
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
  - Como o `Load()` recarrega a cena (#16), o `Start()` do portão já roda com a flag restaurada, e a aparência acerta sozinha, o mesmo que o `CollectableItemBehaviour`.
- **`ItemScriptActions`**: os comandos e a função de roteiro que ligam o diálogo ao inventário. Mora aqui porque o `Dialogue` não referencia o `Inventory` (D-01), e o `GameFlow` enxerga os dois. É por ele que o asmdef do `GameFlow` referencia o pacote Yarn Spinner (`YarnSpinner.Unity`).

    | No roteiro | Método | O que faz |
    |---|---|---|
    | `<<dar_item id>>` | `GiveItem` | `InventoryManager.Collect`. Já ter o item não é erro, e não entra uma segunda cópia. Se o item entrou, registra "Você recebeu: <nome>" no histórico da conversa |
    | `<<remover_item id>>` | `RemoveItem` | `InventoryManager.TryUse`. Não ter o item não é erro. Se o item saiu, registra "Você entregou: <nome>" no histórico da conversa |
    | `tem_item("id")` | `HasItem` | `InventoryManager.HasItem`; `false` se o id não existe |

  - Os métodos são **estáticos** porque é assim que o gerador de código do Yarn Spinner os acha sem registro nosso e sem exigir o nome de um GameObject no roteiro. O componente existe para guardar o `ItemRegistry` (campo `itemRegistry`, no `Managers.prefab`), que traduz o id em `ItemDataSO`; o `Instance` estático é zerado em `SubsystemRegistration` (D-29).
  - O histórico (#25) recebe o evento por chamada direta a `DialogueManager.RecordItemReceived` e `RecordItemHandedOver`, com o `ItemName` do item já em `string`, **só quando o inventário mudou**. O `Dialogue` não enxerga o `Inventory` (D-01) e não há mensagem nova. Sem conversa aberta, o `DialogueManager` ignora o registro. Sem `DialogueManager` (o `Managers.prefab` não foi criado), o comando loga um erro com o nome do comando e do item; o inventário já mudou.
  - São comando e consulta: chamada direta ao `InventoryManager` (D-05). Não há mensagem nova; o painel reage a `ItemCollectedMessage` e `ItemUsedMessage`, que o inventário já publica.
  - Um id que não está no `ItemRegistry` loga `[ItemScriptActions] <<dar_item x>>: o item 'x' não existe no ItemRegistry (nó '...'). Ignorado.` e a conversa segue. O nome do nó vem de `DialogueManager.CurrentNodeName`.
  - O teste `ProjectScripts_CiteOnlyItemIdsThatExistInTheItemRegistry` confere todo id citado nos roteiros contra o `ItemRegistry`, e reprova id não literal.
  - *Limitação conhecida:* um comando com o número errado de parâmetros trava a conversa (comportamento do pacote, #37). O teste acima reprova esse roteiro antes do Play.
- **`DevTools/PlaceholderTint`**: Marcador visual provisório. Hoje está ligado ao **`OnOpened`** da porta de `[Teste] Mecanicas`, pintando-a de verde — ou seja, verde quer dizer "esta porta está aberta", e não "você clicou nela". Existe como componente porque um `UnityEvent` do Inspector não aceita argumento do tipo `Color`, então não dá para ligar `SpriteRenderer.color` direto. Troque pela arte de porta aberta quando existir.
- **`StoryVariableNameCheck`**: o aviso de autoria que o `OnValidate` do portão mostra quando um campo de variável está fora do formato (`StoryVariableName.FollowsConvention`, no `Core`).
- **`DevTools/`**: `SaveLoadDebugPanel`, ligado aos botões Save / Load / Reset Session / Reload Scene do `Canvas_Debug` da cena `[Teste] Mecanicas`. O **Reload Scene** pede `RoomNavigator.Instance.ChangeRoom(<cena ativa>)`: a recarga ganha o fade e segue as mesmas recusas da troca (sem `RoomNavigator`, erro no console). Como toda troca de sala, ele **salva** (sem ponto de entrada). Os métodos do painel continuam `void` e ignoram o `bool` de `Save()`/`Load()` (as recusas já logam): um `UnityEvent` do Inspector não lista método que devolve `bool`, e a ligação gravada na cena quebraria em silêncio. Ferramenta de teste, não UI de jogo: o `Canvas_Debug` fica **na cena**, fora do `GameUI.prefab`, e os botões dele dependem do `EventSystem` persistente (a cena não tem um).

---

## Fluxo de Interações e Arquitetura de Mensagens

O `GameFlow` **reage** a notificações dos outros módulos (via `MessageBroker` do `Core`) e, a partir delas, decide o estado do jogo. Ele não é quem inicia diálogos: cada módulo é dono do seu próprio ciclo de vida e apenas **avisa** o que aconteceu.

### 1. Entrando em Diálogo
O jogador clica em um objeto narrativo. O `InteractableDialogueTrigger` (via `UnityEvent` do `InteractableItem`) chama **diretamente** `DialogueManager.Instance.StartDialogue(nodeName)`, com o nome de um nó do roteiro `.yarn` — iniciar um diálogo é um **comando** com um único dono, e comando não passa pelo barramento. O campo `nodeName` é marcado com `[ScriptNode]` (do `Core`) e, no Inspector, é uma lista dos nós do roteiro desenhada pelo módulo `Editor` (um nó que deixou de existir continua no campo, com aviso); o valor gravado segue sendo um `string`. O `OnValidate` avisa se o nome preenchido foge do formato (`ScriptNodeName`, do módulo `Dialogue`), o que só acontece quando o campo cai para texto comum, e um nome vazio só avisa ao clicar. O teste `DialogueTriggerNodesTests` reprova, em qualquer cena ou prefab de `Assets/`, um gatilho cujo nó não existe mais no roteiro.

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

### 4. Troca de sala (#12)
`RoomNavigator.ChangeRoom(roomName, entryPointId)` devolve `bool`, como `StartDialogue`. Uma troca recusada loga e **não muda nada**: sem fade, sem mexer no gate. As recusas, nesta ordem:

| Pedido | Resposta |
|---|---|
| Nome vazio ou em branco | aviso |
| Sem `GameStateController` | erro que cita só ele |
| Sem `ScreenFader` | erro que cita só ele |
| Fora da exploração: **troca já em curso** ou **conversa aberta** (o aviso diz qual) | aviso |
| Sala inexistente (`Application.CanStreamedLevelBeLoaded` falso) | **erro** com o nome pedido; o jogo permanece na sala atual. Vale também para uma cena que existe no projeto mas não está na lista de cenas do build |

**Uma conversa aberta recusa a troca** (decisão do programador na #12: "não devem existir trocas de sala durante uma conversa"). Isso vale também para o **Reload Scene** do debug. A alternativa de deixar a conversa seguir por cima da sala nova (o comportamento anterior à #12) foi descartada.

Aceita a troca, na ordem:
1. `EnterRoomTransition()` **na hora**, no mesmo frame da chamada: o `RoomTransitionState` fecha o `PlayerInputGate`.
2. `ScreenFader.FadeOutAsync`: liga o bloqueio do `Canvas_Fade` e escurece em `fadeSeconds`.
3. `SceneManager.LoadSceneAsync(roomName)`.
4. **Chegada** (`ArriveAt`), com a cena nova já carregada e a tela ainda escura. Com `entryPointId` vazio ou nulo (Reload Scene, primeira cena do Play), não faz nada. Senão procura o primeiro `RoomEntryPoint` com esse `Id` na cena; se não acha, **erro** no console com a sala e o id, e o jogador chega na posição em que a cena foi salva. Achando, pede ao `ScreenPanController` da sala `CenterOn(x do ponto)`; uma sala sem pan (a largura da tela) não faz nada. O `CenterOn` põe o X no centro da tela e respeita `minX`/`maxX`.
5. **Salva** (#16), ainda com a tela escura: `CurrentEntryPointId` recebe o ponto pedido (o ponto é gravado mesmo que a sala não o tenha; o erro da chegada já avisou) e `GameSaveManager.Save()` grava o jogo. Sem `GameSaveManager`, `LogError` e a troca segue. Gravar com a tela escura esconde qualquer soluço de disco, e o save já existe se o jogador fechar o jogo no clarear.
6. `ScreenFader.FadeInAsync`: clareia em `fadeSeconds` e desliga o bloqueio.
7. `LeaveRoomTransition()`: volta ao `GameplayState`, que libera o gate. O gate ignora o clique do frame da liberação.

É apresentação e carregamento assíncronos (D-06, `Awaitable`): depois do carregamento o `destroyCancellationToken` é conferido, e sair do Play Mode no meio da troca a cancela sem erro no console.

**Falha inesperada na troca** (decisão do programador no levantamento da #13, opção (b)): um `Awaitable` descartado com `_ =` engole a exceção sem log, e o jogo ficaria preso em `RoomTransitionState` com a tela escura. O `ChangeRoomAsync` tem um `catch (Exception)` depois do de cancelamento: registra a exceção com `Debug.LogException(exception, this)`, clareia a tela na hora (`ScreenFader.HideImmediately`, se o fader ainda existir) e segue para o `LeaveRoomTransition()`. O jogador volta à exploração na sala que tiver ficado carregada (a antiga se a falha veio antes do carregamento, a nova se veio depois). O caminho de falha **não** salva. Uma `IOException` na gravação do save automático (disco cheio, arquivo travado) cai nesse mesmo `catch`: a tela clareia na hora e o jogo volta à exploração na sala nova, sem travar (comportamento conhecido, não tratado à parte, D-27). Ponto de entrada inexistente e sala sem pan **não** são falha: são erro de conteúdo e têm mensagem própria, sem exceção. Nenhuma mensagem nova: a troca é um comando com um dono (D-05), e a música por sala (#19) criará a notificação dela quando houver quem a escute.

*Limitação conhecida:* uma conversa que começasse **durante** a troca poria o jogo em `DialogueState`, e o `LeaveRoomTransition()` o devolveria à exploração com a conversa aberta. Hoje nada faz isso (os cliques estão bloqueados e nenhum objeto de cena inicia diálogo no `Start`), e não foi tratado (D-27).

### 5. Salvar e carregar (#16)
`GameSaveManager.Save()` e `Load()` devolvem `bool`, como `ChangeRoom`. Recusa loga e **não muda nada**.

**`Save()`**, nesta ordem de recusa:

| Pedido | Resposta |
|---|---|
| Sem `InventoryManager` | erro |
| **Conversa aberta** (`DialogueManager.IsDialogueActive()`) | aviso "Save recusado: há uma conversa aberta (D-20)" |
| Sem `RoomNavigator` | erro |

Aceito, grava o `GameState` com `currentScene` (a cena ativa) e `currentEntryPointId` (`RoomNavigator.CurrentEntryPointId`, vazio se `null`) e devolve `true`. Quem salva sozinho é o `RoomNavigator`, na chegada de toda troca de sala (passo 5 da troca), por chamada direta (D-05); o `bool` é ignorado, porque as recusas já logam. Dar Play direto numa sala no Editor **não** salva até a primeira troca; o Novo Jogo abre a sala inicial por `ChangeRoom` (`StartNewGame`, abaixo), então já nasce com save.

**`HasSave`** (#17) é `true` quando o arquivo existe **e** o `GameStateJson.TryRead` aceita o texto. Um save corrompido conta como "sem save": o Continuar fica desabilitado e o Novo Jogo não pede confirmação.

**`StartNewGame(roomName, entryPointId)`** (#17), nesta ordem de recusa (a ordem do `Load()`):

| Pedido | Resposta |
|---|---|
| Sem `InventoryManager` | erro |
| Sem `RoomNavigator` | erro |
| `RoomNavigator.ChangeRoom(roomName, entryPointId)` recusada (sala vazia, fora do build, conversa aberta, troca em curso) | aviso que cita a sala; o motivo já foi logado; **nada foi mexido** |

Aceita a troca, faz o mesmo `Restore` do `Load()` com um `GameState` vazio (`new GameState()`): zera itens, objetos consumidos e `StoryState`, e tudo o que entrar no save no futuro é zerado junto, sem uma lista própria do que é estado. Como no `Load()`, a cena nova ainda não está ativa, então os objetos dela já leem o estado novo no `Start()`. **O arquivo não é apagado** (D-20): a chegada na sala inicial o sobrescreve, porque toda troca salva. Devolve `bool` e as recusas já logam.

**`Load()`**, nesta ordem:

| Pedido | Resposta |
|---|---|
| Arquivo inexistente | aviso "Nenhum save encontrado" |
| Sem `InventoryManager`, sem `itemRegistry` ou sem `RoomNavigator` | erro |
| Save corrompido (`GameStateJson.TryRead` falso) | aviso "O save está corrompido", **sem exceção** |
| `RoomNavigator.ChangeRoom(currentScene, currentEntryPointId)` recusada (sala vazia, sala fora do build, conversa aberta, troca em curso) | aviso que cita a sala; a recusa do `RoomNavigator` já logou o motivo; **nada foi restaurado** |

Com a troca aceita, restaura itens, objetos consumidos e o `StoryState` e devolve `true`. **A ordem é o que faz o `Load()` ser tudo ou nada:** a troca é pedida antes, e o estado só é restaurado se ela foi aceita. O estado é restaurado antes de a cena nova carregar (o `ChangeRoomAsync` ainda está escurecendo, e o `LoadSceneAsync` não ativa a cena dentro da chamada), então o `Start()` dos objetos da cena nova já lê o estado certo. Restaurar antes de pedir a troca quebraria o tudo ou nada (conversa aberta ou sala inexistente deixariam o estado mudado e a sala não). O tudo ou nada vale para as **recusas**, não para uma falha inesperada depois de a troca ser aceita: como o estado já foi restaurado, uma exceção antes do carregamento (por exemplo, o `ScreenFader` destruído no escurecer) deixa o jogador, pela regra de "Falha inesperada na troca", na sala antiga com o estado do save.

Consequências, não bugs:
- O `Load()` leva vários frames (é uma troca de sala): dispare num `eval` e observe em outros. O fim é o `PlayerInputGate.IsEnabled` voltar a `true`.
- O `Load()` de uma sala que já está aberta a recarrega (é o que o Reload Scene faz).
- Ao chegar pela troca do `Load()`, o save automático grava de novo o estado recém-restaurado. Um id de item desconhecido, que o `Load()` já reporta, some do arquivo nessa regravação.
- Uma conversa aberta recusa o `Load()` (D-19): carregar no meio de uma conversa não faz sentido.
- A suíte PlayMode e qualquer verificação em Play **sobrescrevem o `savegame.json` do Editor**, porque toda troca de sala salva (aceito pelo programador na #16; para guardar um save de teste, copie o arquivo antes).

### 6. Menu principal (#17)
`Assets/Scenes/Menu.unity` é a **cena 0** do build (`EditorBuildSettings`): o jogo abre no menu. A interface de jogo (diálogo e inventário) continua persistente e fora das cenas (D-19), então o menu só a **esconde**.

- **`GameUIVisibility`**: no `Canvas_Game` do `GameUI.prefab` (`[RequireComponent(typeof(Canvas))]`, sem campo no Inspector). `Instance` zerado em `SubsystemRegistration` (D-29); `Show()` e `Hide()` ligam e desligam o **`Canvas`**, nunca o objeto: com o objeto ativo, o `DialogueUIController` e o `InventoryPresenter` seguem registrados e assinando (um `Canvas` desligado não desenha nem recebe clique).
- **`MainMenu`**: MonoBehaviour no objeto `Main Menu` da cena `Menu.unity`; os botões o chamam por `UnityEvent` do Inspector (D-07), e todos os métodos ligados são `void`. Só lê um manager a partir do `Start` (na primeira cena os managers ainda não existem no `Awake`). Sem estado estático.

| Campo (todos com tooltip) | O que é |
|---|---|
| `startRoom` | O nome da cena do Novo Jogo (`[Teste] Mecanicas` de partida). **O único lugar onde a sala inicial é configurada.** Precisa estar na lista de cenas do build |
| `startEntryPointId` | O id do `RoomEntryPoint` da chegada. Vazio = a posição em que a cena foi salva |
| `onSaveChecked` (`UnityEvent<bool>`) | Chamado no `Start` com `HasSave`. Ligado, **como método dinâmico**, ao `MenuButtonUI.SetInteractable` do CONTINUAR |
| `onNewGameNeedsConfirmation` | Chamado pelo Novo Jogo quando há save. Ligado ao `UIWindowManager.OpenWindow` da `Confirm New Game Window` |

| Método | O que faz |
|---|---|
| `Start` | `GameUIVisibility.Instance.Hide()` e `onSaveChecked.Invoke(HasSave)` |
| `OnDestroy` | `GameUIVisibility.Instance.Show()`, com o `null` conferido (ao sair do Play o prefab pode ter sido destruído antes). O menu é destruído quando a cena da sala carrega, com a tela já escura |
| `RequestNewGame()` | NOVO JOGO: com save, `onNewGameNeedsConfirmation`; sem save, `StartNewGame()` |
| `StartNewGame()` | `GameSaveManager.StartNewGame(startRoom, startEntryPointId)`; ignora o `bool` (as recusas já logam). É o NOVO JOGO da janela de confirmação |
| `ContinueGame()` | `GameSaveManager.Load()`; ignora o `bool` |
| `QuitGame()` | `Application.Quit()`; no Editor também `EditorApplication.ExitPlaymode()` (o `Quit` não faz nada no Editor) |

O `OnValidate` avisa `startRoom` vazio e cada `UnityEvent` sem ligação (sem a confirmação ligada, o Novo Jogo com save não faria nada). Sem `GameSaveManager` ou sem `GameUIVisibility` (o prefab não foi criado), erro no console com contexto (D-04).

**Dar Play numa sala continua igual:** sem `MainMenu` na cena ninguém chama `Hide()` e o `Canvas_Game` nasce ligado. O bootstrap não depende da primeira cena. **Não ponha `EventSystem` nem `Canvas_Game` em `Menu.unity`**; o menu usa o `EventSystem` persistente. O teste `MainMenuTests` confere que a primeira cena do build tem um `MainMenu`, que há um só, e que a sala inicial é uma sala do build (não o próprio menu) com o ponto de entrada citado existindo.

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
| #18 | Estado de pausa, sobreposto com `Push`/`Pop` |
