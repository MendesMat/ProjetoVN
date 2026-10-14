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
| `ExpressionTag` | C# puro: lê de `line.Metadata` a etiqueta de expressão, ignorando as do Yarn (`lastline`, `line:…`) | `Logic/` |
| `ConversationStage` | C# puro, criado com `new` pelo apresentador: quem está em cada um dos dois lugares da tela de personagens, quem está em destaque e a última expressão de cada um. Estado só da conversa em curso, fora do save. Ver [Personagens na tela](#personagens-na-tela) | `Logic/` |
| `CharacterStageView` | `MonoBehaviour` fino: dois `Image` (esquerda e direita) e a cor do escurecido. Só desenha o que o `ConversationStage` decidiu | `GameUI.prefab` (`Canvas_Game/DialogueUI/CharacterStage`) |
| `ConversationLog` | C# puro, criado com `new` pelo apresentador: o histórico da conversa em curso, uma lista de entradas já formatadas em texto rico do TMP (falas, opção escolhida, itens do roteiro). Fora do save. Ver [O histórico de falas](#o-histórico-de-falas) | `Logic/` |
| `DialogueUIController` | O **apresentador** do Yarn Spinner (`DialoguePresenterBase`): mostra a caixa, a placa de nome, a tela de personagens, as escolhas e o indicador de continuar, revela o texto da fala aos poucos (com o `LetterTypewriter` do pacote) e é dono do `ConversationLog` | `GameUI.prefab` (`Canvas_Game/DialogueUI`), aninhado no `Managers.prefab` |
| `DialogueHistoryView` | `MonoBehaviour` fino: o painel do histórico. Só mostra as entradas que o apresentador entrega e rola até a mais recente | `GameUI.prefab` (`Canvas_Game/DialogueUI/DialogueHistory`) |
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

Em objetos de cena, o caminho pronto é o `InteractableDialogueTrigger` (módulo `GameFlow`): o campo **Node Name** recebe o nome do nó, e o método `TriggerDialogue()` é ligado ao `UnityEvent` `OnInteract` do `InteractableItem`. Não há seletor de nó no Inspector (issue #32): o nome é texto livre, e um nome errado só aparece ao clicar, com um aviso no console.

`StartDialogue` confere, nesta ordem, e devolve `false` sem publicar nada em cada caso:

1. nome vazio (aviso);
2. já há conversa em curso (aviso, com o nome do nó que está rodando);
3. nenhuma interface de diálogo registrada (erro);
4. o `DialogueRunner` sem `YarnProject` (erro), ou o projeto sem programa compilado, ou seja, com erro de compilação (aviso);
5. o nó não existe no roteiro (aviso, com o nome).

Só então o `DialogueRunner` começa, e a conversa continua com:

- `AdvanceDialogue()`: com a fala ainda sendo revelada, **completa** a fala (`RequestHurryUpLine`); com a fala completa, **avança** para a próxima (`RequestNextLine`). Com escolhas na tela ou com o histórico aberto, não faz nada: o clique não pula a escolha. Quem decide entre completar e avançar é o `DialogueManager`, consultando `IsRevealingLine` do apresentador (D-06: completar passa pelo dono do diálogo, não pela view). Dois cliques no mesmo frame só completam: o segundo ainda vê a fala sendo revelada.
- `MakeChoice(int)`: repassa o botão clicado ao apresentador.
- `IsDialogueActive()`: há uma conversa em curso.
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

**A interface é persistente (#9), então recarregar ou trocar a cena não a destrói e não encerra a conversa:** a fala continua na tela por cima da cena nova, com o `PlayerInputGate` fechado, até o jogador avançar até o fim (medido na #9: `Reload Scene` do painel de debug durante uma fala deixa `IsDialogueActive()` verdadeiro). Hoje só esse botão chega aí. O que acontece com uma conversa aberta numa troca de sala é decisão da #12.

- **Fala:** o nome é o do personagem da fala (`Luna: ...`); sem personagem (**narração**), a placa de nome fica escondida e ninguém entra na tela de personagens. O texto é a fala sem o nome. O personagem é resolvido como descrito em [Personagens na tela](#personagens-na-tela).
- **Revelação:** o texto da fala aparece aos poucos, pelo `LetterTypewriter` do Yarn Spinner (D-17: o pacote já conta os caracteres, ignora a marcação de TMP e mostra tudo quando apressado). A velocidade é o campo **Characters Per Second** do `DialogueUIController` no `GameUI.prefab` (40 por padrão, mínimo 1), o **único** lugar onde ela se ajusta; vale a partir da fala seguinte, também em Play Mode. A contagem usa tempo escalado (`Time.timeAsDouble`): com `Time.timeScale = 0` a revelação para. O nome, a placa e a tela de personagens aparecem na hora, no começo da fala; só o texto é revelado. O texto é preparado com zero caracteres visíveis **antes** de a caixa ligar, então uma fala nova nunca aparece inteira por um frame. Para saber o que o jogador vê, leia `maxVisibleCharacters` do `DialogueText`: o `text` já tem a fala inteira desde o começo.
- **A fala antes de opções** (regra do `lastline`): o compilador etiqueta com `lastline` a fala que é o comando imediatamente anterior a um bloco de opções. O apresentador não espera clique nela: revela a fala e, **com ela completa**, as opções abrem logo em seguida, sem o indicador de continuar. Clicar durante a revelação completa a fala e as opções abrem; o clique não as pula. Um `<<set>>` entre a fala e as opções tira a etiqueta, e o jogador passa a precisar de um clique a mais.
- **Escolhas:** mostra, em ordem, **todas** as opções do bloco. Uma opção indisponível (a condição `<<if>>` dela é falsa) aparece com o `Button` desabilitado (`interactable = false`, esmaecido pelo `disabledColor` do botão) e ocupa um botão, para o jogador ver que existe um caminho fechado (D-11). `Choose` ignora o índice de uma opção indisponível, também numa chamada direta a `MakeChoice`. O `Button` de cada `choiceButtonObjects[i]` é lido uma vez no `Start`; sem `Button`, o `OnValidate` avisa e a opção indisponível não fica desabilitada. Há quatro botões; mais opções do que botões (disponíveis ou não) é erro de conteúdo: mostra as primeiras e loga um erro com a contagem. **Nenhuma opção disponível:** `RunOptionsAsync` devolve `null` sem mostrar nada e a conversa segue pela fala depois do bloco; isso depende de `allowOptionFallthrough` ligado no `DialogueRunner` do `Managers.prefab` (sem ele o runner loga erro e a conversa fica presa). **Quando as opções abrem, o protagonista entra na tela de personagens e fica em destaque** (é a vez do jogador), mesmo com a fala anterior ainda na caixa; ver [Personagens na tela](#personagens-na-tela).
- **Indicador de continuar** (`>>`): aparece só quando a fala terminou de ser revelada e espera clique; some no começo da fala seguinte e quando as escolhas abrem. É só um indicador, **não é clicável**: o clique esquerdo já avança em qualquer lugar, e um `>>` clicável avançaria duas falas por clique.
- **Fim da conversa:** `DialogueEndedMessage` esconde a caixa e as escolhas, fecha o painel do histórico e limpa a tela de personagens, também no fim anormal.
- **Assíncrono (D-06):** o runner e o apresentador usam `YarnTask`, que no Unity 6 é `Awaitable` na thread principal. Depois de cada `await` o apresentador reconfere se ainda existe. O estado "revelando" (`IsRevealingLine`) mora no apresentador. O typewriter é **aguardado** dentro do `RunLineAsync`, e o runner só entrega a fala seguinte quando ele termina: duas revelações nunca escrevem no mesmo texto. O token do typewriter liga o `HurryUpToken` da fala ao `destroyCancellationToken` do apresentador; o `Stop()` do runner cancela os dois tokens da fala e a revelação termina sem exceção. Recarregar ou trocar a cena **não** interrompe a revelação (a interface é persistente); sair do Play Mode no meio dela não deixa erro no console (medido na #11).

Todo campo exposto no Inspector do `DialogueUIController` tem tooltip, e o `OnValidate` avisa quando um campo obrigatório está vazio ou quando `Choice Texts` não tem o tamanho de `Choice Button Objects`.

### Personagens na tela

A cada fala, o `ShowLine` procura o nome de quem fala (`line.CharacterName`) no `CharacterRegistry` do controller, decide o que mostrar na placa de nome e **diz ao `ConversationStage` o que aconteceu**. A tela de personagens (`CharacterStageView`) só desenha o resultado.

| Momento | Placa de nome | Tela de personagens |
|---|---|---|
| **Narração** (sem nome), incluindo o pensamento | desligada | ninguém entra; quem está na tela escurece (`DimEveryone`) |
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

- **O nome mostrado sai de um único método privado** (`NameToDisplay`), que hoje devolve o `DisplayName`. É onde a #26 troca `Protagonista` pelo nome que o jogador escolheu, sem reescrever roteiro (D-21).
- **`characterRegistry` vazio:** todo nome aparece em texto puro, sem cor e sem sprite, as opções não trazem ninguém, com **um** erro no `Start` e um aviso no `OnValidate`. **`characterStage` vazio:** os personagens não aparecem na tela.
- **`CharacterRegistry.Protagonist`** é o campo que diz quem é o protagonista. O `OnValidate` do registro avisa quando está vazio ou fora da lista, e o `ScriptContentTests` confere (`CharacterRegistry_NamesAProtagonistFromItsList`).
- A cor padrão da placa é a que o `SpeakerName` tem no prefab, guardada no `Start`.
- O `ScriptContentTests` reprova nome fora do registro e expressão inexistente antes do Play; os avisos acima são a rede de segurança para o que chegar ao jogo.

**Limitações conhecidas:** **os personagens passam por baixo das bordas dos botões de opção** (os botões ocupam x 680–1240; quem está à esquerda chega a x 775 e quem está à direita começa em x 1145), porque a cena de referência não mostra as opções; isso se resolve quando a arte entregar a interface nova, com uma referência de opções abertas; o sprite do lado esquerdo é desenhado por cima do painel de inventário (que só ficará visível em puzzle; será tratado depois); a `raiva` da Luna é uma cópia provisória da neutra com um quadrado vermelho; e não há comando para o roteiro tirar alguém da tela, então um `<<jump>>` que emenda dois encontros deixa o primeiro personagem na tela, escurecido, até alguém tomar o lugar dele.

### O histórico de falas

O jogador relê a conversa em curso (D-30, #25). O `DialogueUIController` é dono de um `ConversationLog` (D-12: quem precisa das falas as recebe do apresentador, dentro do módulo) e o `DialogueHistoryView` só mostra o que ele entrega.

**O que entra, e em que formato** (o registro guarda o texto pronto para o TMP):

| Momento | Entrada |
|---|---|
| Fala de personagem | `<color=#RRGGBB>Nome</color>: texto`, com a `NameColor` do `CharacterSO` (sem alfa) e o nome já resolvido por `NameToDisplay` (D-21: a #26 troca o nome em um lugar só e o histórico acompanha). Personagem desconhecido: o nome do roteiro e a cor padrão da placa |
| Narração | só o texto, sem nome |
| Opção escolhida (`Choose`, depois das guardas) | como fala do `Protagonist` do registro, com o nome e a cor do `CharacterSO` dele. Sem registro ou sem `Protagonist`, entra como narração. O `runSelectedOptionAsLine` do runner está **desligado** no `Managers.prefab`: ligá-lo duplicaria a opção no histórico |
| Item dado ou tirado pelo roteiro | `Você recebeu: <nome>` / `Você entregou: <nome>`, com o `ItemName` do item. Só quando o inventário muda (`ItemScriptActions`) |

**Quando zera:** no `OnDialogueStartedAsync`, que o runner chama uma vez por `StartDialogue`; `<<jump>>` e `<<detour>>` não o chamam, então os saltos entre nós não apagam nada. O método continua devolvendo `YarnTask.CompletedTask` na hora, porque a rede de segurança do `DialogueManager` depende de o início ser síncrono. O histórico continua legível no objeto depois do fim da conversa, mas o painel só abre durante uma conversa.

**Abrir e fechar:**
- O botão **Histórico** (`openHistoryButton`) é filho da `DialogueBox` e aparece e some com ela. Fica desabilitado enquanto a fala é revelada (`SetRevealing` liga o `interactable` junto de `IsRevealingLine`); com a fala completa e com as opções na tela, funciona. `OpenHistory()` confere isso de novo: sem conversa, com a fala sendo revelada ou sem a view, não faz nada.
- O controller liga o clique em código, no `Start`, e a view liga o próprio botão **Fechar** em `OnEnable`. Não há atalho de teclado: abrir e fechar limpam a seleção do `EventSystem`, e os botões têm `Navigation = None`, senão o Enter (`UI/Submit`) reacionaria o botão clicado.
- **Bloqueio com o histórico aberto** (`IsHistoryOpen`): `DialogueManager.AdvanceDialogue` retorna logo no início (cobre clique, espaço, enter e botão sul) e `Choose` ignora tudo (cobre também `MakeChoice` direto). O fundo do painel ocupa a tela inteira com `Raycast Target` ligado e barra o clique nos botões de opção. As opções continuam esperando: o `RunOptionsAsync` segue no `await`.
- O clique de ponteiro sobre um botão da interface não avança a fala: veja `DialogueInputHandler` abaixo.

**O painel** (último filho de `Canvas_Game/DialogueUI`, portanto por cima da caixa e das opções; o objeto fica sempre ativo e o painel se esconde com o `Canvas` aninhado desligado):

| O quê | Valor |
|---|---|
| `HistoryButton` (em `DialogueBox`) | âncora e pivô `(1, 1)`, `anchoredPosition (-35, 28)`, 230×71, sprite `Nameplate` (arte provisória), rótulo "Histórico" 32. Espelha a placa de nome e, medido na captura em 1280×720, não cobre o `>>` nem o texto |
| `DialogueHistory` | âncoras esticadas na tela, `Image` preta sem sprite com alfa 0,85 e `Raycast Target` ligado; leva o `DialogueHistoryView`, um `Canvas` (sem `Override Sorting`) e um `GraphicRaycaster`. O objeto fica **ativo**; o `Canvas` fica desligado enquanto o histórico está fechado |
| `Scroll` | centrado, 1500×900, `ScrollRect` só vertical (`Clamped`, `scrollSensitivity` 20, **não calibrado com mouse de verdade**), barra vertical de 24 à direita sempre visível |
| `Viewport` | `RectMask2D`, com 40 de folga à direita para a barra |
| `Content` | `VerticalLayoutGroup` (espaçamento 28, margem 20, controla largura e altura dos filhos) e `ContentSizeFitter` vertical |
| `EntryTemplate` | `TMP_Text` desligada, LiberationSans SDF 34, cor `F7EBF5` (a do `DialogueText`), `richText` ligado, `Raycast Target` desligado |
| `CloseButton` | topo, ao centro (`(0,5, 1)`, `anchoredPosition (0, -50)`), 230×71, mesmo sprite; longe do canto superior direito, onde o `Canvas_Debug` da cena de teste fica por cima |

**O `DialogueHistoryView`** reaproveita as `TMP_Text` (D-11): cria uma cópia do template quando falta e desliga as que sobram, nunca as destrói. Ao abrir, atribui os textos, reconstrói o layout na hora (`LayoutRebuilder.ForceRebuildLayoutImmediate`; sem isso a rolagem para no tamanho antigo e não chega à entrada mais recente), põe `verticalNormalizedPosition = 0` e liga o `Canvas`. **Abrir e fechar ligam e desligam o `Canvas` do painel, não o objeto:** desligar o objeto desliga cada `TMP_Text`, e religá-lo faz o TMP refazer o texto de todos (medido: ~100 ms por abertura com 200 falas, 34 ms no layout e 62 ms no primeiro acesso à rolagem). Com o objeto ativo e só o `Canvas` desligado, o painel não desenha nem recebe clique, e a reabertura custa poucos milissegundos. O objeto precisa ficar ativo no prefab (o `OnValidate` avisa; `Open` loga erro). Um objeto por entrada deixa o `RectMask2D` descartar o que está fora da área; virtualizar a lista foi descartado (D-27). Para contar o que está visível, conte os filhos **ativos** do `Content`, menos o template.

**Medido na #25** (Editor, Game view de 1280×720, 201 entradas de uns 130 caracteres, de uma vez): a primeira abertura leva uns 80 ms na chamada e o quadro seguinte leva uns 170 ms (o TMP gera as malhas das entradas novas), ou seja, **~0,25 s**. As reaberturas levam **3,5 a 3,8 ms** na chamada, com o pior quadro em 21 a 22 ms (o `Canvas` religado é reagrupado); antes de desligar o `Canvas` em vez do objeto, eram ~100 ms. Durante a rolagem (posições 1; 0,75; 0,5; 0,25 e 0) o quadro leva 3 a 4 ms. Reabrir não cria objetos novos (202 filhos no `Content`). A primeira abertura só é cara quando muitas entradas aparecem de uma vez; ela cai se as entradas forem criadas conforme as falas chegam (não feito, D-27). Ninguém sabe o tamanho real das conversas.

**Limitações conhecidas:** uma conversa que **começa** por opções não tem caixa na tela (ela só liga com uma fala), então não mostra o botão; o histórico estaria vazio de qualquer jeito. O botão e o painel usam arte provisória.

### Arte

A arte fica em `Assets/UI/` (SVGs importados como **Textured Sprite** e desenhados com `Image` comum do uGUI). `Exemplo.svg` é o mockup de layout, não é usado em cena.

A arte de **personagem** não é SVG: é PNG em `Assets/Sprites/Personagens/<personagem>/`, exportado em 100% de uma cena composta em 3840×2160 e cortado na borda de baixo da tela (importada como `Single`, Pixels Per Unit 100, sem mipmaps, `Max Size` 2048; ver [salas.md](../../../docs/autoria/salas.md#personagens)). Em 1080p o sprite é desenhado a 50% do tamanho original, então a textura continua nítida mesmo quando o importador a reduz a 2048.

Não use o tipo "UI SVGImage": ele desenha a arte como malha de triângulos sem anti-aliasing, e as bordas ficam serrilhadas (nem o canvas Overlay nem o URP deste projeto suavizam). Como Textured Sprite, o importador rasteriza o SVG com **8 amostras por pixel** (com 4, aparecia uma emenda diagonal fina no preenchimento semitransparente da caixa).

**Cada textura tem exatamente o tamanho que o elemento ocupa na tela em 1080p** (caixa 1500×368, nameplate 230×72, escolha 560×120, `>>` 168×150). O importador de SVG não gera mipmaps, então uma textura maior que o elemento é *reduzida* na tela sem filtragem adequada e as bordas finas voltam a serrilhar — maior não é melhor. Por isso as escolhas usam `ChoicePill.svg`, uma cópia de `Nameplate.svg` rasterizada em 560×120: a mesma textura não fica nítida em 230 px e em 560 px ao mesmo tempo. **Se mudar o tamanho de um elemento na tela, mude o `Texture Size` do SVG para o mesmo valor.**

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

## O que ainda não existe

- O nome do protagonista escolhido pelo jogador na placa de nome e no histórico: issue #26 (o ponto de troca é o `NameToDisplay`).
- Seletor de nó no Inspector: issue #32.

## Escrevendo roteiro

O guia dos roteiristas é [docs/autoria/roteiro.md](../../../docs/autoria/roteiro.md); o roteiro de exemplo comentado ([`Assets/Roteiro/exemplo_comentado.yarn`](../../../Assets/Roteiro/exemplo_comentado.yarn)) mostra cada recurso funcionando (narração, fala, o protagonista, condição, opção, opção bloqueada, `<<detour>>`, `<<jump>>`, `<<stop>>`, `visited()`, item), e os roteiros de teste (`Assets/Roteiro/Testes/`) são as fixtures da suíte: não os use de modelo nem os edite. Em uma linha de narração, **qualquer** dois-pontos (`Atenção: ...`, `10:30`) faz o que vem antes virar o nome de um personagem; o guia ensina o escape `\:`. Também medidos e registrados no guia: `//` e `#etiqueta` somem do texto (a etiqueta no fim de uma fala com nome é a expressão do personagem), `[b]` é removido, `[` solto derruba a compilação do projeto, e a caixa, o botão e a placa têm limite de texto.
