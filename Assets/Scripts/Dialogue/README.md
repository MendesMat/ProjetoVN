# Dialogue

O módulo `Dialogue` é um **adaptador fino** entre o [Yarn Spinner](https://docs.yarnspinner.dev/) (decisão D-17) e o resto do projeto. Quem escreve a conversa é o roteirista, em arquivos `.yarn` de `Assets/Roteiro/`; este módulo só começa a conversa, entrega cada fala e cada escolha à interface, liga as variáveis do roteiro ao estado da história e avisa o jogo quando a conversa começa e termina.

> **Antes de mudar qualquer coisa aqui:** [regras de comunicação](../../../docs/arquitetura/visao-geral.md#regras-de-comunicação) · [decisões](../../../docs/arquitetura/decisoes.md) (D-05, D-06, D-11, D-12, D-17, D-18, D-21, D-22) · [fluxo de trabalho](../../../docs/agentes/fluxo-de-trabalho.md) · [armadilhas do Yarn Spinner](../../../docs/agentes/unity-cli.md#armadilhas-do-yarn-spinner)

O módulo enxerga só o `Core` e o pacote Yarn Spinner (`ProjetoVN.Dialogue.asmdef` referencia `YarnSpinner.Unity` e duas DLLs do pacote). Ele **não** referencia `Inventory`: os comandos de roteiro que mexem em itens (`<<dar_item>>`, `<<remover_item>>`, `tem_item()`) moram no `GameFlow` (`ItemScriptActions`).

---

## As peças

| Peça | O que é | Onde vive |
|---|---|---|
| `DialogueRunner` | Do Yarn Spinner: executa o roteiro | `Managers.prefab` (nenhuma cena o contém) |
| `StoryStateVariableStorage` | `MonoBehaviour` fino: o armazenamento de variáveis que o runner usa. Só delega ao `StoryStateVariables` | `Managers.prefab` |
| `StoryStateVariables` | C# puro, `Yarn.IVariableStorage` sobre o `StoryState`. **Não guarda valor nenhum**: cada leitura e gravação vai ao `StoryState` (D-18) | `Logic/` |
| `DialogueManager` | `MonoBehaviour` com `Instance`, o dono do diálogo: a API pública e o adaptador do runner | `Managers.prefab` |
| `ConversationNotifier` | C# puro: publica `DialogueStartedMessage` e `DialogueEndedMessage`, cada uma uma vez por conversa | `Logic/` |
| `ScriptNodeName` | C# puro: o formato dos nomes de nó (`FollowsConvention`) | `Logic/` |
| `CharacterSO` | ScriptableObject somente leitura (D-02): `Id`, `DisplayName`, `NameColor` e a lista de sprites por expressão (`CharacterExpression`: nome da expressão e `Sprite`). A primeira é a expressão padrão (`DefaultExpression`); `TryGetSprite` acha o sprite de uma expressão | `Characters/`; os assets em `Assets/Scripts/ScriptableObjects/Characters/` |
| `CharacterRegistry` | ScriptableObject com a lista de `CharacterSO`. `TryGetByScriptName` acha o personagem pelo nome exibido (comparação exata). O campo `Protagonist` aponta quem o jogo trata como protagonista. Sem `Instance` (D-04): é referenciado pelo apresentador | `Characters/` |
| `ExpressionTag` | C# puro: lê de `line.Metadata` a etiqueta de expressão, ignorando as do Yarn (`lastline`, `line:…`) e a `acao`, que é de opção e não é expressão | `Logic/` |
| `ConversationStage` | C# puro, criado com `new` pelo apresentador: quem está em cada um dos dois lugares da tela de personagens, quem está em destaque e a última expressão de cada um. Estado só da conversa em curso, fora do save. Ver [Personagens na tela](#personagens-na-tela) | `Logic/` |
| `CharacterStageView` | `MonoBehaviour` fino: dois `Image` (esquerda e direita) e a cor do escurecido. Só desenha o que o `ConversationStage` decidiu | `GameUI.prefab` (`Canvas_Game/DialogueUI/CharacterStage`) |
| `ThoughtLine` | C# puro, `static`: o nome reservado `Pensamento` (`ReservedName`) e `Is(nome)`, a comparação exata (ordinal) que diz se uma linha do roteiro é o pensamento do protagonista. Ver [O pensamento do protagonista](#o-pensamento-do-protagonista) | `Logic/` |
| `ProtagonistName` | C# puro, `static`: as regras do nome do protagonista (#26). `VariableName` (`$nome_protagonista`), `Unknown` (`???`), `ToDisplay(guardado)` (`???` para nada gravado ou só espaços), `Normalize` (tira os espaços das pontas e junta os repetidos), `TryNormalize` (aceita só se sobrar texto com ao menos uma letra e só caracteres permitidos) e `IsAllowedCharacter` (letra, espaço, hífen e apóstrofo). Ver [O nome do protagonista](#o-nome-do-protagonista) | `Logic/` |
| `ConversationEntry` | `readonly struct`: uma entrada do histórico (`Kind` fala, narração, pensamento ou item; `SpeakerName`; `SpeakerColor`, opaca; `Text`; `IsProtagonist`; `ShowsName`). O `enum ConversationEntryKind` mora no mesmo arquivo | `Logic/` |
| `ActionOption` | C# puro, `static`: a etiqueta `acao` (`Tag`) e `IsMarkedIn(metadata)`, a comparação exata (ordinal) que diz se uma opção do roteiro é uma opção de ação, um gesto do protagonista (#65, D-21). Ver o tópico **Escolhas** | `Logic/` |
| `ConversationLog` | C# puro, criado com `new` pelo apresentador: o histórico da conversa em curso, uma lista de `ConversationEntry` (falas, opção escolhida, itens do roteiro). Decide quando o nome aparece. Fora do save. Ver [O histórico de falas](#o-histórico-de-falas) | `Logic/` |
| `BubbleTextColor` | C# puro, `static`: a cor do texto de um balão, a escura (`Dark`) ou a clara (`Light`) de maior contraste WCAG com o balão; `IsReadable` diz se alguma chega a 4,5:1 | `Logic/` |
| `DialogueUIController` | O **apresentador** do Yarn Spinner (`DialoguePresenterBase`): mostra a caixa, a placa de nome, a tela de personagens, as escolhas e o indicador de continuar, revela o texto da fala aos poucos (com o `LetterTypewriter` do pacote) e é dono do `ConversationLog` | `GameUI.prefab` (`Canvas_Game/DialogueUI`), aninhado no `Managers.prefab` |
| `ProtagonistNameSheet` | `MonoBehaviour` fino: a folha da lista de passageiros (#26), com o campo do nome, o botão ASSINAR e o aviso. Inativa no prefab; `AskAsync(nomeAtual)` a abre e devolve o nome (`null` se cancelada), `Cancel()` a fecha. Ver [O nome do protagonista](#o-nome-do-protagonista) | `GameUI.prefab` (`Canvas_Game/DialogueUI/ProtagonistNameSheet`) |
| `DialogueHistoryView` | `MonoBehaviour` fino: o painel do histórico, um balão por entrada. Só mostra as entradas que o apresentador entrega, acrescenta só as novas a cada abertura e rola até a mais recente | `GameUI.prefab` (`Canvas_Game/DialogueUI/DialogueHistory`) |
| `DialogueChoiceButton` | Um por botão de escolha: `OnClick` → `DialogueManager.MakeChoice(índice)` | `GameUI.prefab` |
| `DialogueInputHandler` | Lê a ação `UI/AdvanceDialogue` (botão esquerdo, espaço, enter, botão sul) e chama `AdvanceDialogue`, que completa ou avança a fala (D-08). Um clique de ponteiro sobre um botão da interface não avança | `Managers.prefab` |

O módulo publica duas notificações, `DialogueStartedMessage` e `DialogueEndedMessage` (`Messaging/`), de que o `GameFlow` depende. **Fala e escolha não passam pelo barramento**: o runner as entrega ao apresentador por chamada direta (D-12), porque têm um destinatário só.

---

## Como uma cena inicia um diálogo

Iniciar é um **comando** com um dono, então vai por chamada direta (D-05):

```csharp
// Devolve false, com o motivo no console, se a conversa não pôde começar.
DialogueManager.Instance.StartDialogue("porta_trancada");
```

Em objetos de cena, o caminho pronto é o `InteractableDialogueTrigger` (módulo `GameFlow`): o campo **Node Name** é uma lista dos nós do roteiro, e o método `TriggerDialogue()` é ligado ao `UnityEvent` `OnInteract` do `InteractableItem`. O seletor é do módulo `Editor` (marcado por `[ScriptNode]`, do `Core`), e um teste de EditMode reprova gatilho que aponte para nó inexistente (#32).

`StartDialogue` confere, nesta ordem, e devolve `false` sem publicar nada em cada caso:

1. nome vazio (aviso);
2. já há conversa em curso (aviso, com o nome do nó que está rodando);
3. nenhuma interface de diálogo registrada (erro);
4. o `DialogueRunner` sem `YarnProject` (erro), ou o projeto sem programa compilado, ou seja, com erro de compilação (aviso);
5. o nó não existe no roteiro (aviso, com o nome).

Só então o `DialogueRunner` começa, e a conversa continua com:

- `AdvanceDialogue()`: com a fala ainda sendo revelada, **completa** a fala (`RequestHurryUpLine`); com a fala completa, **avança** para a próxima (`RequestNextLine`). Com escolhas na tela, com o histórico aberto ou com o diálogo pausado (`IsPaused`), não faz nada: o clique não pula a escolha. Quem decide entre completar e avançar é o `DialogueManager`, consultando `IsRevealingLine` do apresentador (D-06: completar passa pelo dono do diálogo, não pela view). Dois cliques no mesmo frame só completam: o segundo ainda vê a fala sendo revelada.
- `MakeChoice(int)`: repassa o botão clicado ao apresentador; com o diálogo pausado, não faz nada.
- `IsDialogueActive()`: há uma conversa em curso.
- `IsPaused` e `SetPaused(bool)` (#18): o diálogo pausado. **Quem escreve é só o `PauseState`**, do `GameFlow` (D-28); o `Dialogue` apenas lê. Pausado, `AdvanceDialogue` e `MakeChoice` não fazem nada, e o `DialogueUIController` recusa `Choose` e `OpenHistory` (os dois leem `DialogueManager.Instance.IsPaused`). É estado consultável no dono do diálogo, como o do histórico aberto (D-30), e não passa pelo `PlayerInputGate`, que o `DialogueInputHandler` não consulta. O texto parado e o `<<wait>>` esperando vêm do tempo congelado (`Time.timeScale = 0`), não deste bloqueio. Ao retomar, a conversa segue do mesmo ponto: a fala continua sendo revelada de onde parou.
- `StopDialogue()` (#18): encerra a conversa em curso (`_runner.Stop().Forget()`) e **não faz nada sem conversa**. O fim é **síncrono**: o `Stop()` do Yarn chama `Dialogue.Stop()`, que dispara `OnDialogueCompleted` na hora, e como o `DialogueUIController.OnDialogueCompleteAsync` devolve `CompletedTask` a `DialogueEndedMessage` sai no mesmo frame (medido em Play Mode: `IsDialogueActive()` é `false` logo depois). Quem o usa é o Voltar ao menu da pausa; o que o roteiro já gravou no `StoryState` e no inventário fica na sessão e some no Continuar ou no Novo Jogo, que restauram o estado.
- `AskProtagonistName()` (#26, `public async YarnTask`): abre a folha do nome, espera o jogador assinar e grava o nome no `StoryState` (`$nome_protagonista`). É o que o comando `<<assinar_lista>>` (`GameFlow`) chama. Sem conversa em curso ou sem interface registrada, loga um erro e volta. Ver [O nome do protagonista](#o-nome-do-protagonista).
- `RecordItemReceived(string)` e `RecordItemHandedOver(string)`: acrescentam ao histórico "Você recebeu: <nome>" e "Você entregou: <nome>". São chamadas pelo `ItemScriptActions` (`GameFlow`), que é quem enxerga o inventário; sem conversa aberta ou sem apresentador, não fazem nada.
- `CurrentNodeName`: o nome do nó em que o roteiro está, ou `null` fora de uma conversa. É `string` de propósito, para quem está fora do módulo dizer o nó em uma mensagem de erro sem depender de tipo do Yarn.

---

## Roteiro inválido nunca trava o jogo

Os quatro casos avisam no console e deixam o jogo em exploração (o `GameFlow` só entra em `DialogueState` ao ouvir `DialogueStartedMessage`, e só sai ao ouvir `DialogueEndedMessage`):

| Caso | O que acontece |
|---|---|
| Roteiro com **erro de compilação** | O `StartDialogue` avisa ("o roteiro tem erro de compilação") e devolve `false`, sem tocar no programa quebrado. A importação do `Roteiro.yarnproject` também loga o erro |
| **Nó inexistente** pedido por uma cena | O `StartDialogue` avisa com o nome e devolve `false` |
| **`<<jump>>` para nó inexistente** no meio da conversa | O compilador só dá aviso, e em runtime o Yarn lança e para sem terminar a conversa. A **rede de segurança** no `Update` do `DialogueManager` percebe que a conversa está aberta e o runner parado, loga um erro e a encerra (publica `DialogueEndedMessage`) |
| **Comando desconhecido** (`<<comando_que_nao_existe>>`) | O runner pararia sem avançar. O `SkipUnknownCommand`, ligado ao `onUnhandledCommand` do runner por ligação persistente no `Managers.prefab`, loga um erro com o comando e o nó e deixa a conversa seguir |

A rede de segurança só é correta porque o início e o fim da conversa no apresentador são síncronos (`OnDialogueStartedAsync` e `OnDialogueCompleteAsync` terminam na hora). Se algum dia eles passarem a esperar, uma conversa recém-aberta pareceria parada.

O `ScriptContentTests` reprova qualquer erro **ou aviso** de compilação nos roteiros reais, o que pega o `<<jump>>` quebrado, a variável não declarada e o código inalcançável antes do Play.

---

## Variáveis do roteiro e o estado da história

O `DialogueRunner` usa o `StoryStateVariableStorage`, que delega ao `StoryStateVariables`. O nome vai **com o `$`** (`$falou_com_luna`), igual ao resto do projeto (D-18). Uma variável do roteiro é uma entrada do `StoryState`: o roteiro, os portões e o save leem e gravam a mesma coisa, e `Load()`, `ResetSession()` e os portões continuam valendo sem código novo.

- **Leitura:** devolve o valor gravado só se ele **é** do tipo pedido (sem conversão silenciosa). Se a variável nunca foi gravada, cai no **valor inicial declarado** no roteiro (`<<declare $x = false>>`), que não é gravado no `StoryState`. A queda é obrigatória: o contador de visitas dos nós (`$Yarn.Internal.Visiting.<nó>`, que sustenta `visited()`) é lido direto do armazenamento.
- **Escrita:** bool, número (`float`) e texto vão para o tipo certo do `StoryState`. Nome sem `$` não grava e o `StoryState` avisa.
- **Não deixe o `variableStorage` do runner vazio:** o Yarn criaria um armazenamento próprio em silêncio, e o roteiro teria um estado paralelo.

---

## O apresentador

O `DialogueUIController` herda de `DialoguePresenterBase`. Ele se **registra** no `DialogueManager` no `Start` (`RegisterPresenter`) e sai no `OnDestroy` (`UnregisterPresenter`); os dois são `internal` de propósito, porque um membro público com tipo derivado do Yarn obrigaria o `GameFlow` a referenciar o pacote. Se a interface for destruída com uma conversa aberta, o `DialogueManager` para o runner e a conversa **encerra**: `DialogueEndedMessage` é publicada e o input volta.

**A interface é persistente (#9), então recarregar ou trocar a cena não a destrói e não encerra a conversa:** a fala continua na tela por cima da cena nova, com o `PlayerInputGate` fechado, até o jogador avançar até o fim (medido na #9: `Reload Scene` do painel de debug durante uma fala deixa `IsDialogueActive()` verdadeiro). A #12 fechou essa porta: **uma conversa aberta recusa a troca de sala** (`RoomNavigator.ChangeRoom` loga um aviso, devolve `false` e nada muda), e o **Reload Scene** do painel de debug, que agora passa pelo `RoomNavigator`, segue a mesma regra. A conversa só termina pelo roteiro. Ver [GameFlow](../GameFlow/README.md).

- **Fala:** o nome é o do personagem da fala (`Luna: ...`); sem personagem (**narração**), a placa de nome fica escondida e ninguém entra na tela de personagens. Com o nome reservado `Pensamento`, é o **pensamento** do protagonista (abaixo). O texto é a fala sem o nome. O personagem é resolvido como descrito em [Personagens na tela](#personagens-na-tela).
- **Revelação:** o texto da fala aparece aos poucos, pelo `LetterTypewriter` do Yarn Spinner (D-17: o pacote já conta os caracteres, ignora a marcação de TMP e mostra tudo quando apressado). A velocidade é o campo **Characters Per Second** do `DialogueUIController` no `GameUI.prefab` (40 por padrão, mínimo 1), o **único** lugar onde ela se ajusta; vale a partir da fala seguinte, também em Play Mode. A contagem usa tempo escalado (`Time.timeAsDouble`): com `Time.timeScale = 0` a revelação para. Ela conta tempo, não quadros, então o FPS não muda a velocidade: a FPS baixo vários caracteres aparecem no mesmo quadro, a FPS alto um caractere leva vários quadros, e um quadro longo conta no máximo 0,333 s (`Time.maximumDeltaTime`), cerca de 13 caracteres a 40 por segundo. O nome, a placa e a tela de personagens aparecem na hora, no começo da fala; só o texto é revelado. O texto é preparado com zero caracteres visíveis **antes** de a caixa ligar, então uma fala nova nunca aparece inteira por um frame. Para saber o que o jogador vê, leia `maxVisibleCharacters` do `DialogueText`: o `text` já tem a fala inteira desde o começo.
- **A fala antes de opções** (regra do `lastline`): o compilador etiqueta com `lastline` a fala que é o comando imediatamente anterior a um bloco de opções. O apresentador não espera clique nela: revela a fala e, **com ela completa**, as opções abrem logo em seguida, sem o indicador de continuar. Clicar durante a revelação completa a fala e as opções abrem; o clique não as pula. Um `<<set>>` entre a fala e as opções tira a etiqueta, e o jogador passa a precisar de um clique a mais.
- **Escolhas:** mostra, em ordem, **todas** as opções do bloco. Uma opção indisponível (a condição `<<if>>` dela é falsa) aparece com o `Button` desabilitado (`interactable = false`, esmaecido pelo `disabledColor` do botão) e ocupa um botão, para o jogador ver que existe um caminho fechado (D-11). `Choose` ignora o índice de uma opção indisponível, também numa chamada direta a `MakeChoice`. O `Button` de cada `choiceButtonObjects[i]` é lido uma vez no `Start`; sem `Button`, o `OnValidate` avisa e a opção indisponível não fica desabilitada. Há quatro botões; mais opções do que botões (disponíveis ou não) é erro de conteúdo: mostra as primeiras e loga um erro com a contagem. **Nenhuma opção disponível:** `RunOptionsAsync` devolve `null` sem mostrar nada e a conversa segue pela fala depois do bloco; isso depende de `allowOptionFallthrough` ligado no `DialogueRunner` do `Managers.prefab` (sem ele o runner loga erro e a conversa fica presa). **Quando as opções abrem, o protagonista entra na tela de personagens e fica em destaque** (é a vez do jogador), mesmo com a fala anterior ainda na caixa; ver [Personagens na tela](#personagens-na-tela). **Opção de ação (#65):** uma opção com a etiqueta `#acao` (`ActionOption.IsMarkedIn(option.Line.Metadata)`) ganha `FontStyles.Italic` no `choiceTexts[i]`; o Yarn entrega a etiqueta sem o `#`, e o `lastline` nunca vem em opção, então não atrapalha. O estilo original de cada `choiceTexts[i]` é guardado no `Start` (`_defaultChoiceStyles`) e **reatribuído em todo `ShowOptions`**, porque os botões são reaproveitados (D-11): um bloco com ação no botão 2 seguido de um bloco só de falas não deixa o itálico. O itálico vem da etiqueta e o `interactable` vem de `IsAvailable`, então uma opção de ação bloqueada sai em itálico e desabilitada, sem código extra. A etiqueta não aparece no texto do botão.
- **Indicador de continuar** (`>>`): aparece só quando a fala terminou de ser revelada e espera clique; some no começo da fala seguinte e quando as escolhas abrem. É só um indicador, **não é clicável**: o clique esquerdo já avança em qualquer lugar, e um `>>` clicável avançaria duas falas por clique.
- **Fim da conversa:** `DialogueEndedMessage` esconde a caixa e as escolhas, fecha o painel do histórico e limpa a tela de personagens, também no fim anormal. Também limpa a seleção do `EventSystem`, mas **só se o objeto selecionado é da interface de diálogo** (filho do `DialogueUI`, inativo ou não): a opção clicada continua selecionada, escondida, até o fim da conversa, e o Enter a reacionaria. Uma seleção de fora, como o botão do menu, fica. O mesmo `HideAll` roda no `Start` do apresentador, e lá não mexe na seleção da cena: na primeira cena o `Start` da interface roda depois do `Start` da cena, e antes (#88) ele apagava a seleção inicial do menu.
- **Assíncrono (D-06):** o runner e o apresentador usam `YarnTask`, que no Unity 6 é `Awaitable` na thread principal. Depois de cada `await` o apresentador reconfere se ainda existe. O estado "revelando" (`IsRevealingLine`) mora no apresentador. O typewriter é **aguardado** dentro do `RunLineAsync`, e o runner só entrega a fala seguinte quando ele termina: duas revelações nunca escrevem no mesmo texto. O token do typewriter liga o `HurryUpToken` da fala ao `destroyCancellationToken` do apresentador; o `Stop()` do runner cancela os dois tokens da fala e a revelação termina sem exceção. Recarregar ou trocar a cena **não** interrompe a revelação (a interface é persistente); sair do Play Mode no meio dela não deixa erro no console (medido na #11).

Todo campo exposto no Inspector do `DialogueUIController` tem tooltip, e o `OnValidate` avisa quando um campo obrigatório está vazio ou quando `Choice Texts` não tem o tamanho de `Choice Button Objects`.

### Personagens na tela

A cada fala, o `ShowLine` procura o nome de quem fala (`line.CharacterName`) no `CharacterRegistry` do controller, decide o que mostrar na placa de nome e **diz ao `ConversationStage` o que aconteceu**. A tela de personagens (`CharacterStageView`) só desenha o resultado.

| Momento | Placa de nome | Tela de personagens |
|---|---|---|
| **Narração** (sem nome) | desligada | ninguém entra; quem está na tela escurece (`DimEveryone`) |
| **Pensamento** (`Pensamento:`, #61) | desligada | `DimEveryone`, **sem aviso** de personagem desconhecido: o nome reservado é reconhecido antes da busca no registro |
| **Personagem conhecido**, sem etiqueta | o `DisplayName`, na `NameColor` do asset | ele fala (`Speak`) com a expressão padrão (a primeira da lista): em destaque, e os demais escurecem |
| Personagem conhecido, **com etiqueta** (`#raiva`) | idem | idem, com a expressão da etiqueta, **para esta fala** |
| Personagem conhecido, **expressão que ele não tem** | idem | idem, com a expressão padrão e `LogWarning` (personagem, expressão e nó) |
| Personagem conhecido **sem nenhuma expressão** | idem | `DimEveryone`, sem aviso |
| **Personagem desconhecido** | o nome do roteiro, na cor padrão da placa | `DimEveryone`, com `LogWarning` (nome e nó) |
| **As opções de resposta abrem** | continua a da fala anterior (quem perguntou) | `GiveTurnToPlayer` com o `Protagonist` do registro: entra se não estava e fica em destaque, e quem estava escurece. Sem registro, sem `Protagonist` ou sem expressão: `DimEveryone` |
| **A conversa termina** | some com a caixa | `Clear`: os dois lugares ficam vazios |

Nada trava: o aviso sai e a conversa segue. A chamada da vez do protagonista fica **depois** de `HasAvailableOption()`: um bloco sem nenhuma opção disponível devolve `null` antes de qualquer coisa (D-11) e não traz ninguém.

**O `ConversationStage`** (C# puro, `Logic/`, testado em `ConversationStageTests`) é quem decide:

- **O lado:** quem entra ocupa a esquerda se ela está livre, senão a direita. Com os dois ocupados, toma o lugar de **quem teve a vez há mais tempo**. Quem já está na tela fala de novo no mesmo lugar. O protagonista não tem lado fixo.
- **O que conta como vez:** cada `Speak` e cada `GiveTurnToPlayer`. `DimEveryone` não conta (narração não faz ninguém sair).
- **O destaque:** só quem falou por último. Quem escurece **mantém a última expressão** até a própria fala seguinte. Nas opções, o protagonista que já estava na tela mantém a última expressão dele (opção não tem etiqueta); se entra agora, usa a padrão.
- **Estado de apresentação (D-02, D-06):** é criado com `new` pelo controller, vale só para a conversa em curso e não entra no save. Usa o `CharacterSO` apenas como chave (compara referência) e a expressão como `string`. O fim da conversa o limpa, então a conversa seguinte começa vazia. `<<jump>>` e `<<detour>>` não o tocam: o apresentador nem recebe o evento de troca de nó.

**O `CharacterStageView`** liga e desliga o `gameObject` do `Image` de cada lado, **não** o `sprite`: um `Image` sem sprite desenha um quadrado branco. Para cada lado ocupado, atribui o sprite, chama `SetNativeSize()`, pinta de branco (destaque) ou de `dimmedColor` (escurecido) e liga. A cor do escurecido é um campo do Inspector: o padrão do componente é 55% de brilho, e o jogo usa hoje 30%, gravado como override da instância do `GameUI` no `Managers.prefab`. Entrar, escurecer e trocar de lugar são instantâneos. Os dois `Image` têm `Raycast Target` desligado (o sprite não bloqueia o clique que avança a fala nem os botões), e o `OnValidate` avisa se algum for ligado.

**Posições e escala** (o `CharacterStage` é filho do `DialogueUI`, no índice 0, portanto desenhado **antes**, ou seja, atrás da `DialogueBox`, e na frente do cenário e do inventário):

| O quê | Valor |
|---|---|
| `CharacterStage` | âncora e pivô `(0,5, 0)`, `anchoredPosition (0, 0)`, **`localScale (0,5, 0,5, 1)`**. É o **único** lugar com a escala e a linha de chão: todos os personagens usam os mesmos, sem ajuste por personagem |
| `LeftSlot` e `RightSlot` | âncora e pivô `(0,5, 0)`, `anchoredPosition (-812, 0)` e `(812, 0)`: na tela, centros em x 554 e x 1366. Começam desligados |
| `DialogueBox` | `anchoredPosition (0, 24)`: centrada. Em 1080p ocupa x 210–1710, com o topo em y 392 |

De onde vêm os números: da **cena de referência** que a arte compôs em 3840×2160, guardada em [`docs/arte/referencia_cena_de_conversa.png`](../../../docs/arte/referencia_cena_de_conversa.png). A arte de personagem é exportada em 100% dessa cena e cortada na borda de baixo da tela, então a interface de 1920×1080 a desenha na metade (`0,5`) com a base da imagem na borda de baixo da tela (`y = 0`). Na referência, o centro de cada personagem fica a 406 px do meio da tela. Os personagens têm alturas diferentes de propósito, e o alinhamento pela base preserva a diferença: o topo do cabelo do protagonista fica a 15 px do topo da tela e o da Luna a 128 px.

**O tamanho vem do tamanho em unidades, não dos pixels da textura.** `sprite.rect` muda com o `Max Size` do importador (uma arte mais alta que 2048 é reduzida a 2048, e duas artes de alturas diferentes poderiam ficar do mesmo tamanho); o tamanho em unidades não muda (medido: 8,60×19,04 na Luna e 8,85×21,39 no protagonista, a arte de origem dividida por 100). `SetNativeSize()` usa o segundo, desde que **toda arte de personagem tenha Pixels Per Unit 100**.

O que isso exige de quem monta o prefab e o conteúdo:

- **O nome mostrado sai de um único método privado** (`NameToDisplay`): o `DisplayName` do personagem e, para o `Protagonist` do `CharacterRegistry`, o nome que o jogador escolheu (`ProtagonistName.ToDisplay` sobre `$nome_protagonista` do `StoryState`), ou `???` enquanto ele não assinou (#26, D-21). O roteiro continua escrevendo `Protagonista:`. A placa e o histórico passam por ele; o pensamento, não.
- **`characterRegistry` vazio:** todo nome aparece em texto puro, sem cor e sem sprite, as opções não trazem ninguém, com **um** erro no `Start` e um aviso no `OnValidate`. **`characterStage` vazio:** os personagens não aparecem na tela.
- **`CharacterRegistry.Protagonist`** é o campo que diz quem é o protagonista. O `OnValidate` do registro avisa quando está vazio ou fora da lista, e o `ScriptContentTests` confere (`CharacterRegistry_NamesAProtagonistFromItsList`).
- A cor padrão da placa é a que o `SpeakerName` tem no prefab, guardada no `Start`.
- O `ScriptContentTests` reprova nome fora do registro e expressão inexistente antes do Play; os avisos acima são a rede de segurança para o que chegar ao jogo.

**Limitações conhecidas:** **os personagens passam por baixo das bordas dos botões de opção** (os botões ocupam x 680–1240; quem está à esquerda chega a x 775 e quem está à direita começa em x 1145), porque a cena de referência não mostra as opções; isso se resolve quando a arte entregar a interface nova, com uma referência de opções abertas; o sprite do lado esquerdo é desenhado por cima do painel de inventário (que só ficará visível em puzzle; será tratado depois); a `raiva` da Luna é uma cópia provisória da neutra com um quadrado vermelho; e não há comando para o roteiro tirar alguém da tela, então um `<<jump>>` que emenda dois encontros deixa o primeiro personagem na tela, escurecido, até alguém tomar o lugar dele.

### O pensamento do protagonista

`Pensamento: texto` no roteiro (#61) é o pensamento do protagonista. O Yarn Spinner o entrega como uma fala de um personagem chamado `Pensamento`, e o `DialogueUIController.ShowSpeaker` o reconhece com `ThoughtLine.Is(speaker)` **antes** de procurar no `CharacterRegistry` (D-12, D-21): `Pensamento` não é um `CharacterSO`, e procurá-lo geraria o aviso de personagem desconhecido a cada pensamento. A comparação é exata, então `pensamento:` continua sendo um personagem desconhecido.

| Onde | O que muda |
|---|---|
| **Caixa** | placa de nome desligada, `SpeakerName` vazio, texto em **itálico** e na cor `thoughtColor` (campo do `DialogueUIController`, sob o cabeçalho "Pensamento", `#C9DCFF` de partida). Ninguém entra na tela e quem está nela escurece (`DimEveryone`) |
| **Histórico** | uma entrada `Thought`, à **direita**; ver a tabela de [O que entra](#o-histórico-de-falas) |
| **`NameToDisplay`** | **não** é chamado: a troca do nome do protagonista (#26) não afeta o pensamento, e nenhum nome aparece nele |

**A caixa é a mesma entre as falas**, então o estilo é reatribuído em **toda** fala (`ApplyTextStyle`): itálico e `thoughtColor` no pensamento, e no resto o `fontStyle` e a `color` que o `DialogueText` tinha no `Start` (guardados, e não fixados no código). O `LetterTypewriter` só mexe em `text` e `maxVisibleCharacters`, nunca na cor nem no estilo.

Uma etiqueta de expressão em um pensamento é ignorada em runtime (não há sprite); a verificação dos roteiros a reprova antes do Play. `CharacterRegistry.OnValidate` loga erro se algum personagem tem `DisplayName` igual a `ThoughtLine.ReservedName`, e `CharacterRegistry_HasNoCharacterNamedLikeTheThought` o reprova no teste, porque o roteiro nunca chegaria a ele. O `OnValidate` do `DialogueUIController` avisa se `thoughtColor` tem alfa menor que 1: a caixa usaria o alfa e o histórico o descartaria (`ConversationLog` guarda a cor opaca).

**Contraste medido (#61, Game view de 1920×1080, cena de teste, fala completa).** Medido nos pixels da captura, a caixa de diálogo valia `#956A81` e o `#C9DCFF` tinha só **3,26:1** nela (o texto normal, `#F7EBF5`, 3,90:1). O que deixava a caixa clara **não era o cenário aparecendo, e sim a camada rosa da arte** (`DialogueBox.svg`, 42%): o Unity mistura o alfa em espaço linear, e o rosa fica bem mais claro do que o valor do SVG sugere. Por isso a arte mudou na #61 (pedido do programador, a caixa estava difícil de ler): a camada escura (`cls-2`) passou de 82% a **opaca** e o rosa (`cls-7`) de 42% a **25%**. A caixa mede agora `#775367`, igual em qualquer cenário, e o `#C9DCFF` tem **4,71:1** (o texto normal, 5,64:1). Se alguém reclarear a caixa, refaça essa conta: o pensamento precisa de ~4,5:1. A cor é um campo do Inspector e se ajusta sem código. No histórico, o balão é o `neutralBubbleColor` (`#24111F`, alfa 0,82) misturado 20% ao `#C9DCFF` (`#453A4C`, mesmo alfa): medido nos pixels de uma captura de 1280×720 com uma cena branca atrás do painel (o pior caso), o texto tem **6,40:1** (`#4E4752` de balão), e **8,12:1** sobre a cena de teste, acima dos 4,5:1 do critério. **A conta de contraste de um balão com alfa tem de ser feita em espaço linear**: a primeira versão deixava o balão só a 20% de opacidade na cor de pensamento, a conta em gama dava 6,36:1 e a captura media 3,94:1 (2,43:1 no pior caso), porque o projeto está em `ColorSpace.Linear` e o Unity mistura o alfa nele. Em espaço linear, o tingimento 0,05 dá 8,49:1 sobre a cena branca; 0,1, 7,87; 0,2, 6,53; 0,3, 5,23; 0,4, 4,14.

### O histórico de falas

O jogador relê a conversa em curso (D-30, #25), com a aparência de um app de mensagens (#55). O `DialogueUIController` é dono de um `ConversationLog` (D-12: quem precisa das falas as recebe do apresentador, dentro do módulo) e o `DialogueHistoryView` só mostra o que ele entrega.

**O que entra, e como aparece** (o registro guarda entradas estruturadas; quem decide lado, cor e estilo é a view):

| Momento | Entrada | Lado e estilo |
|---|---|---|
| Fala do `Protagonist` do registro (`AddProtagonistLine`) | `Speech`, com o nome já resolvido por `NameToDisplay` (D-21: o nome escolhido pelo jogador troca em um lugar só e o histórico acompanha; as falas já gravadas ficam com o nome que tinham na hora, então a primeira continua `???` depois da assinatura) e a `NameColor` do `CharacterSO`, guardada com alfa 1 | **direita**; balão na `NameColor` |
| Fala de outro personagem (`AddSpokenLine`) | `Speech`, idem. Personagem desconhecido: o nome do roteiro e a cor padrão da placa (branca) | **esquerda**; balão na `NameColor` |
| Narração | `Narration`, só o texto, sem nome | **centro**; balão neutro, texto em itálico |
| Pensamento do protagonista (`AddThought`, #61) | `Thought`, só o texto, sem nome, com a cor de pensamento guardada com alfa 1 em `SpeakerColor` (a cor da voz) | **direita**; balão neutro (escuro e translúcido) tingido pela cor de pensamento, na força `thoughtBubbleTint`; texto em itálico na cor de pensamento (não passa por `BubbleTextColor`). Não usa a `NameColor` do protagonista |
| Opção escolhida (`Choose`, depois das guardas) | como fala do `Protagonist` do registro (`AddProtagonistLine`). Sem registro ou sem `Protagonist`, entra como narração. **Exceção, a opção de ação (#65):** entra sempre como narração (`AddNarration`), no centro e em itálico, e não como fala do protagonista. O `runSelectedOptionAsLine` do runner está **desligado** no `Managers.prefab`: ligá-lo duplicaria a opção no histórico | **direita** |
| Item dado ou tirado pelo roteiro | `Item`, `Você recebeu: <nome>` / `Você entregou: <nome>`, com o `ItemName` do item (os prefixos moram no `ConversationLog`). Só quando o inventário muda (`ItemScriptActions`) | **centro**; balão neutro, fonte menor (`itemFontSize`, 28) |

**O nome só aparece na primeira fala de uma sequência.** `ShowsName` é calculado no `Add`: é verdadeiro em uma fala cuja entrada anterior não é fala do mesmo `SpeakerName` (comparação ordinal) **e do mesmo lado** (`IsProtagonist`: o jogador pode ter escolhido para o protagonista o nome de outro personagem, #26), ou que é a primeira do registro. Narração, pensamento e item quebram a sequência (o pensamento não é `Speech`, então `ContinuesSequenceOf` não mudou: a fala do protagonista depois de um pensamento mostra o nome de novo); a opção escolhida depois de uma fala do protagonista a continua; depois do `Clear`, a primeira fala mostra o nome de novo. O nome vai dentro do balão, em negrito e **sem cor própria**.

**A cor do texto vem do contraste.** O balão de um personagem é a `NameColor` dele, e o texto é `BubbleTextColor.For(cor do balão)`: o escuro (`#24111F`, o fundo da caixa) ou o claro (`#F7EBF5`, a cor do `DialogueText`), o de maior contraste WCAG com o RGB do balão (alfa ignorado). O balão neutro (`neutralBubbleColor`, `#24111F` com alfa 0,82) sai com texto claro. As `NameColor` atuais são pastéis claros, então o texto delas é escuro (Luna `#D9A6FF`: 9,23:1; Protagonista `#FFF3FB`: 16,57:1). Um cinza médio (luminância de ~0,15 a ~0,21) não alcança 4,5:1 com nenhum dos dois: o `OnValidate` do `CharacterSO` avisa e o `CharacterRegistry_NameColorsAllowReadableBubbleText` reprova. A correção é ajustar a cor do personagem.
**Quando zera:** no `OnDialogueStartedAsync`, que o runner chama uma vez por `StartDialogue`; `<<jump>>` e `<<detour>>` não o chamam, então os saltos entre nós não apagam nada. Junto do `_history.Clear()`, o apresentador chama `historyView.ForgetShownEntries()`, que só zera a contagem de entradas já mostradas: sem esse aviso, uma conversa nova com mais entradas que a anterior pareceria ter "só algumas novas" e misturaria balões velhos com novos. O método continua devolvendo `YarnTask.CompletedTask` na hora, porque a rede de segurança do `DialogueManager` depende de o início ser síncrono. O histórico continua legível no objeto depois do fim da conversa, mas o painel só abre durante uma conversa.

**Abrir e fechar:**
- O botão **Histórico** (`openHistoryButton`) é filho da `DialogueBox` e aparece e some com ela. Fica desabilitado enquanto a fala é revelada (`SetRevealing` liga o `interactable` junto de `IsRevealingLine`); com a fala completa e com as opções na tela, funciona. `OpenHistory()` confere isso de novo: sem conversa, com a fala sendo revelada ou sem a view, não faz nada.
- O controller liga o clique em código, no `Start`, e a view liga o próprio botão **Fechar** em `OnEnable`. Não há atalho de teclado: abrir e fechar limpam a seleção do `EventSystem` (o `Open()` sempre; o `Close()` só faz algo, e só limpa, se o histórico estava aberto, para não apagar a seleção da cena quando o apresentador esconde tudo no `Start`, #88), e os botões têm `Navigation = None`, senão o Enter (`UI/Submit`) reacionaria o botão clicado.
- **Bloqueio com o histórico aberto** (`IsHistoryOpen`): `DialogueManager.AdvanceDialogue` retorna logo no início (cobre clique, espaço, enter e botão sul) e `Choose` ignora tudo (cobre também `MakeChoice` direto). O fundo do painel ocupa a tela inteira com `Raycast Target` ligado e barra o clique nos botões de opção. As opções continuam esperando: o `RunOptionsAsync` segue no `await`. A pausa (#18) abre **por cima** do histórico (o `Canvas_Pause` tem ordem 50) sem fechá-lo, e ao retomar ele continua aberto; o histórico não abre pela pausa (`OpenHistory` recusa com `IsPaused`).
- O clique de ponteiro sobre um botão da interface não avança a fala: veja `DialogueInputHandler` abaixo.

**O painel** (último filho de `Canvas_Game/DialogueUI`, portanto por cima da caixa e das opções; o objeto fica sempre ativo e o painel se esconde com o `Canvas` aninhado desligado):

| O quê | Valor |
|---|---|
| `HistoryButton` (em `DialogueBox`) | âncora e pivô `(1, 1)`, `anchoredPosition (-35, 28)`, 230×71, sprite `Nameplate` (arte provisória), rótulo "Histórico" 32. Espelha a placa de nome e, medido na captura em 1280×720, não cobre o `>>` nem o texto |
| `DialogueHistory` | âncoras esticadas na tela, `Image` preta sem sprite com alfa 0,85 e `Raycast Target` ligado; leva o `DialogueHistoryView`, um `Canvas` (sem `Override Sorting`) e um `GraphicRaycaster`. O objeto fica **ativo**; o `Canvas` fica desligado enquanto o histórico está fechado |
| `Scroll` | `anchoredPosition (0, -20)`, **1240×860**, `ScrollRect` só vertical (`Clamped`, `scrollSensitivity` 20, **não calibrado com mouse de verdade**), barra vertical sempre visível. Em 1920×1080 ocupa x 340–1580 e y 90–950: o topo fica abaixo do `Fechar` e a borda direita 16 px antes do `DebugPanel` da cena de teste (x 1596–1896) |
| `Viewport` | `RectMask2D`, com 24 de folga à direita para a barra |
| `Scrollbar` | 8 px de largura, rosa translúcido da paleta da caixa: fundo `rgba(216,154,189,0.15)`, `Handle` `rgba(222,126,184,0.7)`; `Permanent` |
| `Content` | `VerticalLayoutGroup` (espaçamento 16, margem 20, controla largura e altura dos filhos) e `ContentSizeFitter` vertical |
| `BubbleTemplate` | desligado. Uma **linha**: `HorizontalLayoutGroup` (controla largura e altura, sem expandir). Dentro, `Bubble`: `Image` com `Assets/UI/ChatBubble.svg` (fatiada em 9 partes, borda 24, `Raycast Target` desligado), `VerticalLayoutGroup` (margem 28/28/16/16, sem expandir a largura) e `LayoutElement` (`flexibleWidth` 0; o `preferredWidth` é gravado por entrada). Dentro, `Text`: `TMP_Text` LiberationSans SDF 34, `richText` ligado, `Raycast Target` desligado |
| `CloseButton` | topo, ao centro (`(0,5, 1)`, `anchoredPosition (0, -50)`), 230×71, mesmo sprite; longe do canto superior direito, onde o `Canvas_Debug` da cena de teste fica por cima |

**O `DialogueHistoryView`** reaproveita as linhas (D-11): cria uma cópia do `BubbleTemplate` quando falta e desliga as que sobram, nunca as destrói. Cada linha guarda as suas partes (`HorizontalLayoutGroup`, `Image`, `LayoutElement`, `TMP_Text`) em uma classe privada aninhada, achadas uma vez na criação. Campos próprios, todos com tooltip: `bubbleTemplate` (`RectTransform`), `neutralBubbleColor`, `thoughtBubbleTint` (0,05 a 0,3; padrão 0,2: quanto a cor de pensamento tinge o balão neutro do pensamento; acima de 0,3 o texto cai abaixo de ~5:1 sobre uma cena branca), `maxBubbleWidth` (0,3 a 1; padrão 0,65, fração da largura do `Content`) e `itemFontSize` (28); sem template completo o histórico não abre e o console avisa.

- **Uma entrada vira uma linha:** o alinhamento da linha (`UpperLeft`, `UpperRight` ou `UpperCenter`), a cor do balão, a cor, o tamanho e o estilo do texto (itálico só na narração e no pensamento) e o texto (`<b>Nome</b>` e uma quebra de linha antes, quando `ShowsName`) são **reatribuídos sempre**, porque a linha é reaproveitada. A largura do balão é `Min(GetPreferredValues(texto, máximo - margem, 0).x + margem, máximo)`, com `máximo = content.rect.width × maxBubbleWidth`, gravada no `preferredWidth` do `LayoutElement`.
- **Abertura incremental:** a view guarda quantas entradas do registro atual já estão nas linhas. `Open` configura só as novas e reconstrói o layout (`LayoutRebuilder.ForceRebuildLayoutImmediate`; sem isso a rolagem para no tamanho antigo e não chega à entrada mais recente) **só se algo mudou**. **Reabrir sem entrada nova não reatribui texto nem refaz o layout** (medido: zero eventos `TEXT_CHANGED` do TMP e os mesmos objetos). Sempre põe `verticalNormalizedPosition = 0` e liga o `Canvas`.

**Abrir e fechar** seguem iguais à #25. **Abrir e fechar ligam e desligam o `Canvas` do painel, não o objeto:** desligar o objeto desliga cada `TMP_Text`, e religá-lo faz o TMP refazer o texto de todos (medido: ~100 ms por abertura com 200 falas, 34 ms no layout e 62 ms no primeiro acesso à rolagem). Com o objeto ativo e só o `Canvas` desligado, o painel não desenha nem recebe clique, e a reabertura custa poucos milissegundos. O objeto precisa ficar ativo no prefab (o `OnValidate` avisa; `Open` loga erro). Um objeto por entrada deixa o `RectMask2D` descartar o que está fora da área; virtualizar a lista foi descartado (D-27). Para contar o que está visível, conte os filhos **ativos** do `Content`, menos o template.

**Medido na #25** (Editor, Game view de 1280×720, 201 entradas de uns 130 caracteres, de uma vez): a primeira abertura leva uns 80 ms na chamada e o quadro seguinte leva uns 170 ms (o TMP gera as malhas das entradas novas), ou seja, **~0,25 s**. As reaberturas levam **3,5 a 3,8 ms** na chamada, com o pior quadro em 21 a 22 ms (o `Canvas` religado é reagrupado); antes de desligar o `Canvas` em vez do objeto, eram ~100 ms. Durante a rolagem (posições 1; 0,75; 0,5; 0,25 e 0) o quadro leva 3 a 4 ms. Reabrir não cria objetos novos (202 filhos no `Content`). A primeira abertura só é cara quando muitas entradas aparecem de uma vez. Ninguém sabe o tamanho real das conversas.

**Medido na #55** (Editor, Game view de 1579×888, 16:9, 200 balões de ~130 caracteres alternando Luna e Protagonista, sem criar nada antes da abertura; limites: primeira abertura ≤ 400 ms, reabertura ≤ 25 ms no pior quadro):

| O quê | Resultado |
|---|---|
| **Primeira abertura** (200 balões criados na hora), em três sessões de Play | a chamada leva 172, 151 e 151 ms; o histórico fica pronto (quadro abaixo de 20 ms) **375, 302 e 298 ms** depois do início da chamada |
| **Reabertura** (linhas já criadas e sem entrada nova), 16 amostras | pior quadro entre **22,7 e 24,9 ms** em 10 delas e entre **25,9 e 37,1 ms** em 6 (mediana 24,7). A chamada leva 2,9 a 3,0 ms |
| **Piso da medição** | um `eval` que só liga o `Canvas` com uma linha ativa mede **20,5 a 20,8 ms** no mesmo quadro: a ferramenta soma ~20 ms a qualquer quadro medido. O custo próprio de 200 balões é de ~4 ms (com os `Image` desligados, o quadro cai ~2 ms) |
| **Rolagem** (1; 0,75; 0,5; 0,25 e 0) | primeiro quadro de 20,8 a 22,0 ms (o piso) e 3 a 4,5 ms nos seguintes |

**A reabertura passa de 25 ms em 6 das 16 amostras**: o quadro de uma reabertura típica fica em ~24,5 ms, a 0,5 ms do limite, e o ruído do Editor, somado ao piso de ~20 ms da ferramenta, o empurra para cima de vez em quando; sem o piso ela leva ~4 ms. O plano B (entregar cada entrada à view assim que ela entra no registro) **não foi aplicado**: a primeira abertura cabe nos 400 ms.

**Limitações conhecidas:** uma conversa que **começa** por opções não tem caixa na tela (ela só liga com uma fala), então não mostra o botão; o histórico estaria vazio de qualquer jeito. O botão, o painel e o balão usam arte provisória (`ChatBubble.svg`, um retângulo arredondado branco, trocável: precisa continuar **branco puro**, porque a `Image` multiplica o sprite pela cor do balão). O painel de 1240 px foi calculado para a cena de teste em **16:9**: em proporções mais estreitas (16:10, 4:3) o `DebugPanel`, ancorado no canto superior direito, anda para a esquerda e volta a cruzar o painel. O fundo do painel (preto com alfa 0,85) deixa a cena aparecer, como a revisão da #25 apontou.

### O nome do protagonista

O jogador escolhe o nome (#26, D-21) ao assinar a lista de passageiros. O caminho é de chamadas diretas (D-05), sem mensagem no barramento:

`<<assinar_lista>>` (roteiro) → `ProtagonistNameScriptActions.SignPassengerList` (`GameFlow`) → `DialogueManager.AskProtagonistName` → `DialogueUIController.AskProtagonistNameAsync` → `ProtagonistNameSheet.AskAsync`.

- **Quem grava** é o `DialogueManager`: a folha só devolve o texto. O nome vai para a variável de texto `$nome_protagonista` do `StoryState` (D-18) **depois** da assinatura; até lá nada é gravado, e `???` é só a exibição de "nada gravado" (`ProtagonistName.ToDisplay`) e o valor inicial declarado no roteiro, para a placa e `{$nome_protagonista}` dizerem a mesma coisa. Se a conversa terminar com a folha aberta, nada é gravado.
- **O apresentador** esconde a caixa e o indicador de continuar ao abrir a folha (a fala seguinte religa a caixa no `ShowLine`); os personagens ficam na tela, escurecidos pelo fundo da folha. É ele quem lê o nome guardado: para a placa (`NameToDisplay`) e para o campo (`StoredProtagonistName`). O campo `Protagonist Name Sheet` do `DialogueUIController` tem tooltip e aviso no `OnValidate`; vazio, `<<assinar_lista>>` loga um erro e o nome continua `???`.
- **O filtro** é o `onValidateInput` do campo: ele devolve `'\0'` (o caractere não entra) quando (a) `ProtagonistName.IsAllowedCharacter` recusa, (b) a fonte do **próprio campo** não tem o caractere (`font.HasCharacter(c, true, true)`, com as fontes de reserva), ou (c) o nome deixaria de caber na placa de nome (`GetPreferredValues` da placa, sobre o texto normalizado, contra a largura dela). O limite é a largura da placa, e não um número de caracteres: seis `W` já passam de 182 px. **Sem aviso e sem `characterLimit`**: o caractere simplesmente não entra. O filtro decide caractere por caractere sobre o texto normalizado, então, ao colar um texto comprido, entram só os caracteres que ainda cabem: depois de um recusado, um mais estreito ainda pode entrar, e os espaços sempre entram, porque o texto medido os junta. Na assinatura, o nome é normalizado. O ASSINAR só fica desabilitado com o nome vazio ou sem letra (`Escreva um nome.`).
- **O texto que o TMP passa ao `onValidateInput`** já vem sem a parte selecionada, e a posição é a de inserção: digitar sobre o nome padrão selecionado mede só o caractere novo. Não "corrija" isso.
- **`.text = ...` não passa pelo filtro**, só o que o jogador digita ou cola. O código nunca escreve no campo um nome que não caiba: o nome atual vem da placa (cabia quando foi assinado) e o `Lucas` cabe (102 px em 182).
- **O foco:** ao abrir, o campo é selecionado com o texto todo selecionado (`onFocusSelectAll`), então digitar o substitui. A pausa leva a seleção do `EventSystem` para os botões dela e a deixa vazia ao fechar: enquanto a folha está aberta e o jogo não está pausado, o `Update` devolve o foco ao campo se a seleção estiver vazia. O `restoreOriginalTextOnEscape` do campo é **desligado**: Esc abre a pausa, e com a opção ligada o campo voltaria ao texto de quando ganhou o foco.
- **Cancelamento:** `DialogueRunner.Stop()` encerra a conversa sem esperar o comando pendente, e o runner recria o `dialogueCancellationSource` a cada `StartDialogue`. Por isso o `HideAll` (que roda na `DialogueEndedMessage`, dentro do `Stop`) chama `protagonistNameSheet.Cancel()`, que completa a tarefa com `null` na hora: se ela terminasse depois de outra conversa começar, o runner chamaria `Continue()` na conversa errada.
- **A folha** é filha de `DialogueUI`, entre `DialogueChoices` e `DialogueHistory`, inativa no prefab. Fundo preto com alfa 0,6 e `Raycast Target` ligado (barra o clique no botão Histórico e nas opções embaixo); o `Paper` (760×820, creme `#F2EAD8`) com o título, o cabeçalho `Nº` / `NOME`, cinco linhas pautadas (3 px), três passageiros já assinados (`Marta S.`, `José Carlos`, `Aparecida`, texto fixo no prefab), o campo na quarta linha, o aviso e o ASSINAR (sprite `Nameplate`, `Navigation = None`). Tudo na `LiberationSans SDF`, com tinta azul `#1F3A93`. Os campos da folha (`nameField`, `signButton`, `messageText`, `nameplateText` e `defaultName`) têm tooltip; `defaultName` (`Lucas`) é validado no `OnValidate`: com a mesma regra do campo e, com `nameplateText` atribuído, contra a largura da placa (o aviso aparece se o nome padrão não couber).
- **Quando a arte definir a fonte de caneta**, trocá-la é trocar o font asset do texto do campo e dos passageiros no prefab, sem código: o filtro (b) lê a fonte do próprio campo. O limite (c) continua sendo a placa.

**Limitações conhecidas:** a arte da folha é provisória; a assinatura não é registrada no histórico (D-27); salvar fica bloqueado durante a conversa (D-20), então o nome vai para o arquivo só no próximo save (chegada a uma sala ou SALVAR da pausa); o teclado e o mouse de verdade (tecla morta, colar, clicar fora, Esc físico com a folha aberta) não foram exercitados pelo CLI.

### Arte

**A caixa (`DialogueBox.svg`) é opaca (#61):** o preenchimento escuro é `rgb(36,17,31)` sem transparência e a camada rosa tem 25%, para o texto ter contraste igual em qualquer cenário. Os valores medidos estão em [O pensamento do protagonista](#o-pensamento-do-protagonista).

A arte fica em `Assets/UI/` (SVGs importados como **Textured Sprite** e desenhados com `Image` comum do uGUI). `Exemplo.svg` é o mockup de layout, não é usado em cena.

A arte de **personagem** não é SVG: é PNG em `Assets/Sprites/Personagens/<personagem>/`, exportado em 100% de uma cena composta em 3840×2160 e cortado na borda de baixo da tela (importada como `Single`, Pixels Per Unit 100, sem mipmaps, `Max Size` 2048; ver [salas.md](../../../docs/autoria/salas.md#personagens)). Em 1080p o sprite é desenhado a 50% do tamanho original, então a textura continua nítida mesmo quando o importador a reduz a 2048.

Não use o tipo "UI SVGImage": ele desenha a arte como malha de triângulos sem anti-aliasing, e as bordas ficam serrilhadas (nem o canvas Overlay nem o URP deste projeto suavizam). Como Textured Sprite, o importador rasteriza o SVG com **8 amostras por pixel** (com 4, aparecia uma emenda diagonal fina no preenchimento semitransparente da caixa).

**Cada textura tem exatamente o tamanho que o elemento ocupa na tela em 1080p** (caixa 1500×368, nameplate 230×72, escolha 560×120, `>>` 168×150). O importador de SVG não gera mipmaps, então uma textura maior que o elemento é *reduzida* na tela sem filtragem adequada e as bordas finas voltam a serrilhar — maior não é melhor. Por isso as escolhas usam `ChoicePill.svg`, uma cópia de `Nameplate.svg` rasterizada em 560×120: a mesma textura não fica nítida em 230 px e em 560 px ao mesmo tempo. **Se mudar o tamanho de um elemento na tela, mude o `Texture Size` do SVG para o mesmo valor.**

**Exceção, o balão do histórico:** `ChatBubble.svg` é um retângulo arredondado **branco opaco** de 64×64, importado como Textured Sprite (8 amostras) com a borda de 9 partes em 24 px (`SpriteBorder`), e a `Image` o estica (`Sliced`) até o tamanho do texto: a regra do tamanho exato não vale para sprite fatiado. Ele precisa continuar branco puro, e a `Image` o pinta com a cor do personagem.

O pacote `com.unity.vectorgraphics` continua instalado mesmo sem usar `SVGImage`: é ele que fornece o Inspector de configurações de importação dos SVGs. Sem ele, os arquivos ainda importam, mas o Inspector mostra só um aviso e não dá para editar nada.

Para julgar nitidez, use o Game view em **Full HD (1920×1080)**, não "16:9 Aspect": este último renderiza no tamanho da janela, abaixo da resolução-alvo, e reduz toda a UI. A montagem de referência está em `Assets/Prefabs/UI/GameUI.prefab`, em `Canvas_Game/DialogueUI`.

---

## Integração com o GameFlow

O Diálogo **é dono do seu próprio ciclo de vida** e **não dita regras de input**: ele só anuncia o que aconteceu, e quem decide o modo do jogo é o `GameFlow`.

- `DialogueStartedMessage`: publicada pelo `ConversationNotifier` no `onDialogueStart` do runner, **antes** da primeira fala. O `GameFlow` entra em `DialogueState` e trava o `PlayerInputGate`.
- `DialogueEndedMessage`: publicada no fim da conversa (inclusive o fim por erro). O `GameFlow` volta a `GameplayState` e libera o input; o clique que encerrou o diálogo não atinge o mundo, porque o gate ignora o frame da liberação.
- Uma conversa, por mais que salte e desvie entre nós, publica **um** Started e **um** Ended, e nunca um Ended sem Started.

O módulo não conhece o `GameFlow` nem o `PointNClick`: a direção das dependências continua `Core ← Dialogue ← GameFlow`.

### O clique em um botão não avança a fala

O `DialogueInputHandler` ignora um clique de **ponteiro** (`context.control.device is Pointer`) quando o primeiro resultado de `EventSystem.current.RaycastAll`, na posição do ponteiro, tem um `Selectable` no próprio objeto ou no pai (D-08). Teclado e controle não mudam. É necessário porque a ação `UI/AdvanceDialogue` (botão esquerdo, tipo `Button`) dispara ao **apertar**, durante o update do Input System, antes do `Update` do `EventSystem`, enquanto o `Button` só age ao **soltar**: sem a regra, apertar o botão Histórico com a fala completa avançaria a fala antes de o histórico abrir.

- Use `RaycastAll` com um `PointerEventData` novo, e **não** `IsPointerOverGameObject()`: dentro de um callback de `InputAction` ele lê o estado do frame anterior, e o Input System avisa no console.
- `DialogueBox`, `DialogueText`, `Nameplate` e `ContinueIndicator` têm `Raycast Target` desligado (medido), então clicar na caixa continua avançando. Um clique no botão Histórico desabilitado (durante a revelação) também não completa a fala. Os botões de opção já não avançavam nada (`RequestNextLine` não tem efeito durante opções).
- O efeito colateral é que os botões do `Canvas_Debug` da cena de teste deixam de avançar a fala quando clicados.

---

## Escrevendo roteiro

O guia dos roteiristas é [docs/autoria/roteiro.md](../../../docs/autoria/roteiro.md); o roteiro de exemplo comentado ([`Assets/Roteiro/exemplo_comentado.yarn`](../../../Assets/Roteiro/exemplo_comentado.yarn)) mostra cada recurso funcionando (narração, fala, o protagonista, o pensamento, condição, opção, opção bloqueada, `<<detour>>`, `<<jump>>`, `<<stop>>`, `visited()`, item), e os roteiros de teste (`Assets/Roteiro/Testes/`) são as fixtures da suíte: não os use de modelo nem os edite. Em uma linha de narração, **qualquer** dois-pontos (`Atenção: ...`, `10:30`) faz o que vem antes virar o nome de um personagem; o guia ensina o escape `\:`. Também medidos e registrados no guia: `//` e `#etiqueta` somem do texto (a etiqueta no fim de uma fala com nome é a expressão do personagem), `[b]` é removido, `[` solto derruba a compilação do projeto, e a caixa, o botão e a placa têm limite de texto.
