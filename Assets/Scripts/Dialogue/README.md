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
| `DialogueUIController` | O **apresentador** do Yarn Spinner (`DialoguePresenterBase`): mostra a caixa, a placa de nome, a tela de personagens, as escolhas e o indicador de continuar | `GameUI.prefab` (`Canvas_Game/DialogueUI`), aninhado no `Managers.prefab` |
| `DialogueChoiceButton` | Um por botão de escolha: `OnClick` → `DialogueManager.MakeChoice(índice)` | `GameUI.prefab` |
| `DialogueInputHandler` | Lê a ação `UI/AdvanceDialogue` (botão esquerdo, espaço, enter, botão sul) e chama `AdvanceDialogue` (D-08) | `Managers.prefab` |

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

- `AdvanceDialogue()`: pede a próxima fala (`RequestNextLine`). Com escolhas na tela, não faz nada: o clique não pula a escolha.
- `MakeChoice(int)`: repassa o botão clicado ao apresentador.
- `IsDialogueActive()`: há uma conversa em curso.
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
- **A fala antes de opções** (regra do `lastline`): o compilador etiqueta com `lastline` a fala que é o comando imediatamente anterior a um bloco de opções. O apresentador não espera clique nela: mostra fala e opções juntas, sem o indicador de continuar. Um `<<set>>` entre a fala e as opções tira a etiqueta, e o jogador passa a precisar de um clique a mais.
- **Escolhas:** mostra, em ordem, **todas** as opções do bloco. Uma opção indisponível (a condição `<<if>>` dela é falsa) aparece com o `Button` desabilitado (`interactable = false`, esmaecido pelo `disabledColor` do botão) e ocupa um botão, para o jogador ver que existe um caminho fechado (D-11). `Choose` ignora o índice de uma opção indisponível, também numa chamada direta a `MakeChoice`. O `Button` de cada `choiceButtonObjects[i]` é lido uma vez no `Start`; sem `Button`, o `OnValidate` avisa e a opção indisponível não fica desabilitada. Há quatro botões; mais opções do que botões (disponíveis ou não) é erro de conteúdo: mostra as primeiras e loga um erro com a contagem. **Nenhuma opção disponível:** `RunOptionsAsync` devolve `null` sem mostrar nada e a conversa segue pela fala depois do bloco; isso depende de `allowOptionFallthrough` ligado no `DialogueRunner` do `Managers.prefab` (sem ele o runner loga erro e a conversa fica presa). **Quando as opções abrem, o protagonista entra na tela de personagens e fica em destaque** (é a vez do jogador), mesmo com a fala anterior ainda na caixa; ver [Personagens na tela](#personagens-na-tela).
- **Indicador de continuar** (`>>`): aparece a cada fala que espera clique e some quando as escolhas abrem. É só um indicador, **não é clicável**: o clique esquerdo já avança em qualquer lugar, e um `>>` clicável avançaria duas falas por clique.
- **Fim da conversa:** `DialogueEndedMessage` esconde a caixa e as escolhas e limpa a tela de personagens, também no fim anormal.
- **Assíncrono (D-06):** o runner e o apresentador usam `YarnTask`, que no Unity 6 é `Awaitable` na thread principal. Depois de cada `await` o apresentador reconfere se ainda existe.

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

**O `CharacterStageView`** liga e desliga o `gameObject` do `Image` de cada lado, **não** o `sprite`: um `Image` sem sprite desenha um quadrado branco. Para cada lado ocupado, atribui o sprite, chama `SetNativeSize()`, pinta de branco (destaque) ou de `dimmedColor` (escurecido) e liga. A cor do escurecido é um campo do Inspector, com 55% de brilho de partida. Entrar, escurecer e trocar de lugar são instantâneos. Os dois `Image` têm `Raycast Target` desligado (o sprite não bloqueia o clique que avança a fala nem os botões), e o `OnValidate` avisa se algum for ligado.

**Posições e escala** (o `CharacterStage` é filho do `DialogueUI`, no índice 0, portanto desenhado **antes**, ou seja, atrás da `DialogueBox`, e na frente do cenário e do inventário):

| O quê | Valor |
|---|---|
| `CharacterStage` | âncora e pivô `(0,5, 0)`, `anchoredPosition (0, -319)`, **`localScale (0,39, 0,39, 1)`**. É o **único** lugar com a escala e a linha de chão: todos os personagens usam os mesmos, sem ajuste por personagem |
| `LeftSlot` e `RightSlot` | âncora e pivô `(0,5, 0)`, `anchoredPosition (-1692, 0)` e `(1692, 0)`: na tela, centros em x 300 e x 1620. Começam desligados |
| `DialogueBox` | `anchoredPosition (0, 24)`: centrada. Em 1080p ocupa x 210–1710, com o topo em y 392 |

De onde vêm os números (medidos nos PNG de origem): o cinto do protagonista está na linha 1717 da imagem e o topo do cabelo na 45. Com escala 0,39 e a base da imagem em y −319, o cinto cai em y 392 (a borda de cima da caixa) e o topo da cabeça em y 1044, com 36 px de folga. A Luna tem 82% da altura dele e aparece até o meio do peito. Na faixa das opções (y 420–942) os corpos ocupam x 136–509 à esquerda e 1456–1829 à direita, e os botões ficam em x 680–1240, então nada fica sob um botão. Os valores são de partida; a composição se julga por captura em 1920×1080.

**O tamanho vem do tamanho em unidades, não dos pixels da textura.** `sprite.rect` muda com o `Max Size` do importador (as duas artes ficariam com 2048 de altura e do mesmo tamanho); o tamanho em unidades não muda (medido: 17,70×28,80 e 18,20×35,40, a arte de origem dividida por 100). `SetNativeSize()` usa o segundo, desde que **toda arte de personagem tenha Pixels Per Unit 100**.

O que isso exige de quem monta o prefab e o conteúdo:

- **O nome mostrado sai de um único método privado** (`NameToDisplay`), que hoje devolve o `DisplayName`. É onde a #26 troca `Protagonista` pelo nome que o jogador escolheu, sem reescrever roteiro (D-21).
- **`characterRegistry` vazio:** todo nome aparece em texto puro, sem cor e sem sprite, as opções não trazem ninguém, com **um** erro no `Start` e um aviso no `OnValidate`. **`characterStage` vazio:** os personagens não aparecem na tela.
- **`CharacterRegistry.Protagonist`** é o campo que diz quem é o protagonista. O `OnValidate` do registro avisa quando está vazio ou fora da lista, e o `ScriptContentTests` confere (`CharacterRegistry_NamesAProtagonistFromItsList`).
- A cor padrão da placa é a que o `SpeakerName` tem no prefab, guardada no `Start`.
- O `ScriptContentTests` reprova nome fora do registro e expressão inexistente antes do Play; os avisos acima são a rede de segurança para o que chegar ao jogo.

**Limitações conhecidas:** o sprite do lado esquerdo é desenhado por cima do painel de inventário (que só ficará visível em puzzle; será tratado depois); os pés das duas artes não estão à mesma distância da base da imagem (a Luna fica uns 16 px mais baixa em relação ao protagonista, sem compensação); o PNG do protagonista tem riscos soltos que a arte vai limpar; a `raiva` da Luna é uma cópia provisória da neutra com um quadrado vermelho; e não há comando para o roteiro tirar alguém da tela, então um `<<jump>>` que emenda dois encontros deixa o primeiro personagem na tela, escurecido, até alguém tomar o lugar dele.

### Arte

A arte fica em `Assets/UI/` (SVGs importados como **Textured Sprite** e desenhados com `Image` comum do uGUI). `Exemplo.svg` é o mockup de layout, não é usado em cena.

A arte de **personagem** não é SVG: é PNG de corpo inteiro em `Assets/Sprites/Personagens/<personagem>/` (importada como `Single`, Pixels Per Unit 100, sem mipmaps, `Max Size` 2048; ver [salas.md](../../../docs/autoria/salas.md#personagens)). Em 1080p o sprite é desenhado a 39% do tamanho original, então a textura reduzida a 2048 continua nítida.

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

---

## O que ainda não existe

- Texto revelado aos poucos: issue #11.
- O nome do protagonista escolhido pelo jogador na placa de nome: issue #26 (o ponto de troca é o `NameToDisplay`). Histórico de falas: issue #25.
- Seletor de nó no Inspector: issue #32.

## Escrevendo roteiro

O guia dos roteiristas é [docs/autoria/roteiro.md](../../../docs/autoria/roteiro.md); o roteiro de exemplo comentado ([`Assets/Roteiro/exemplo_comentado.yarn`](../../../Assets/Roteiro/exemplo_comentado.yarn)) mostra cada recurso funcionando (narração, fala, o protagonista, condição, opção, opção bloqueada, `<<detour>>`, `<<jump>>`, `<<stop>>`, `visited()`, item), e os roteiros de teste (`Assets/Roteiro/Testes/`) são as fixtures da suíte: não os use de modelo nem os edite. Em uma linha de narração, **qualquer** dois-pontos (`Atenção: ...`, `10:30`) faz o que vem antes virar o nome de um personagem; o guia ensina o escape `\:`. Também medidos e registrados no guia: `//` e `#etiqueta` somem do texto (a etiqueta no fim de uma fala com nome é a expressão do personagem), `[b]` é removido, `[` solto derruba a compilação do projeto, e a caixa, o botão e a placa têm limite de texto.
