# Tests

Este módulo guarda os testes automatizados do projeto (Unity Test Framework), em duas suítes: **EditMode** (`EditMode/`, assembly `Tests`) e **PlayMode** (`PlayMode/`, assembly `Tests.PlayMode`).

> **Antes de escrever testes:** [regras de comunicação](../../../docs/arquitetura/visao-geral.md#regras-de-comunicação) · [decisão D-24, sobre testes](../../../docs/arquitetura/decisoes.md#d-24--testes) · [como a TDD se aplica aqui](../../../docs/agentes/skills.md#como-a-tdd-se-aplica-aqui)

Os testes EditMode vivem em `EditMode/` e rodam pela janela **Window → General → Test Runner**, aba *EditMode*, ou pelo Unity CLI:

```bash
unity command run_tests --mode EditMode --timeout 180
```

Os testes PlayMode vivem em `PlayMode/`, aba *PlayMode*, e pelo CLI só rodam de forma assíncrona ([receita](../../../docs/agentes/unity-cli.md#rodar-a-suíte-playmode)):

```bash
unity command run_tests --mode playmode --async_tests true --timeout 300
```

```bash
unity command test_status
```

### Suíte EditMode

- **`MessageBrokerTests`**: entrega, cancelamento de assinatura, assinatura duplicada, isolamento de exceções, `Clear`, e o comportamento de snapshot quando alguém assina durante um despacho.
- **`StoryStateVariablesTests`**: o armazenamento de variáveis do Yarn sobre o `StoryState`: gravar e ler cada tipo, ler o que foi gravado por fora, sem conversão silenciosa de tipo, nome sem `$` com aviso, `Clear`.
- **`ScriptVariablesTests`**: roteiros curtos de verdade sobre o `StoryState`: `<<set>>`, `<<if>>`, valor inicial declarado, incremento, `visited()` (com o contador no `StoryState`), texto interpolado, a etiqueta `lastline` que o apresentador usa, opção condicional (indisponível enquanto a condição é falsa, disponível quando o `StoryState` a satisfaz, bloco todo indisponível) e a descrição `///` de uma declaração.
- **`ScriptContentTests`**: os arquivos reais de `Assets/Roteiro/` compilam sem erro **nem aviso** (pega `<<jump>>` para nó inexistente, variável não declarada e código inalcançável antes do Play), contêm os nós de teste (`ProjectScripts_ContainTheTestNodes`, sem fixar o tamanho do projeto: um roteiro novo não exige mexer no teste), todo nó do projeto segue a convenção de nome (`ProjectScripts_AllNodesFollowTheNamingConvention`) e dizem o que o jogo mostra. Todo id de item citado existe no único `ItemRegistry` do projeto e é literal (`ProjectScripts_CiteOnlyItemIdsThatExistInTheItemRegistry`). O registro de variáveis é cobrado aqui: toda variável é declarada em `variaveis.yarn`, tem descrição e segue a convenção de nome, e as três variáveis da cena de teste existem com o tipo certo. A conversa da Luna tem os testes da afinidade (sobe no "Sim", não sobe no "Não") e da opção condicional (bloqueada na primeira conversa, disponível na segunda). Os personagens são cobrados contra o único `CharacterRegistry` do projeto: todo nome antes dos dois-pontos é o nome exibido de um personagem do registro, exceto o nome reservado `Pensamento` (`ProjectScripts_NameOnlyCharactersInTheCharacterRegistry`, a mensagem traz nó, linha e nome); toda etiqueta de expressão está em fala com nome, nunca em narração, pensamento ou opção, é a única da fala e existe nas expressões de quem fala (`ProjectScripts_UseOnlyExpressionsTheSpeakerHas`); o registro tem ids únicos no formato, nomes exibidos únicos, e expressões com nome no formato, sem repetição e com sprite (`CharacterRegistry_HasUniqueIdsAndNamesInTheConvention`), aponta um protagonista que está na lista (`CharacterRegistry_NamesAProtagonistFromItsList`), não tem personagem com o nome exibido `Pensamento`, que o roteiro nunca alcançaria (`CharacterRegistry_HasNoCharacterNamedLikeTheThought`), e toda `NameColor` permite texto legível, com contraste de 4,5:1, no balão do histórico (`CharacterRegistry_NameColorsAllowReadableBubbleText`; a mensagem traz o id e o contraste de quem reprovar); `#acao` só aparece em opção (`ProjectScripts_MarkOnlyOptionsAsActions`, a mensagem traz nó e linha); e a Luna pede `raiva` em "Ele falou não." (`LunaRespostaNao_AsksForTheAngryExpression`).
- **`ConversationStageTests`**: o `ConversationStage`, a regra pura da tela de personagens. Tela nova vazia; o primeiro a falar entra à esquerda e o segundo à direita; quem já está na tela mantém o lado; o protagonista que abre a conversa fica à esquerda; conversa só de opções (o protagonista entra sozinho); o terceiro toma o lugar de quem falou há mais tempo (inclusive depois de alguém falar de novo) e quem saiu volta do outro lado; narração (`DimEveryone`) escurece sem mudar lado nem expressão e não conta como vez; a etiqueta vale para a fala em que está e quem escurece mantém a última expressão; opções com o protagonista fora (lado livre ou tela cheia) e já na tela; a vez do jogador conta para a regra do terceiro; `Clear` esvazia e a conversa seguinte começa na esquerda. Os personagens são `ScriptableObject.CreateInstance<CharacterSO>()` criados no `SetUp` e destruídos no `TearDown`; a classe só compara referência. O que decide o controller (protagonista sem sprite quando as opções abrem, personagem sem expressão) se confere em Play Mode.
- **`ExpressionTagTests`**: o `ExpressionTag`, que separa a etiqueta de expressão das que o Yarn põe sozinho (`lastline`, `line:…`) e da `acao`, que é de opção.
- **`ActionOptionTests`**: o `ActionOption`: a etiqueta é `acao` (sem `#`), `IsMarkedIn` é verdadeiro com a etiqueta, sozinha ou entre outras, e falso sem etiquetas, com `null`, com uma expressão e com outras grafias (`Acao`, `ação`, `acoes`).
- **`ScriptSpeakersTests`**: o `ScriptSpeakers`, que lista, por linha do roteiro compilado, o nó, o número da linha, quem fala e as etiquetas de expressão. Lê o personagem com o `LineParser` do próprio Yarn, então o escape `\:` e o nome vindo de expressão (`{0}`) se comportam como no jogo. Narração e opção não têm personagem; `TaggedWithoutSpeaker` lista as etiquetas de expressão nelas e no pensamento. Cada linha diz se é opção (`IsOption`, lida das instruções `AddOption` do `Program`, que o `In` recebe junto com a tabela de textos) e se é opção de ação (`IsAction`); `ActionOutsideOptions` lista o `#acao` em fala, narração e pensamento. A opção de ação não tem expressão, e `-> Assinar #acao #raiva` continua em `TaggedWithoutSpeaker`. `Pensamento:` é lido como pensamento (`IsThought`), não é contado em `Spoken` (então a verificação do registro o aceita), e `pensamento:` em minúsculas continua sendo um personagem desconhecido.
- **`ScriptItemReferencesTests`**: o `ScriptItemReferences`, que lê do roteiro compilado cada `dar_item`, `remover_item` e `tem_item` com o nó e o id, e separa as referências sem id literal (variável, expressão, id ausente, parâmetro a mais). Lê o programa, não o texto, então um comando em comentário não conta.
- **`ScriptItemCommandsTests`**: `tem_item` em uma condição, com e sem o item, e o texto que `dar_item` e `remover_item` entregam ao jogo.
- **`ThoughtLineTests`**: o `ThoughtLine`: o nome reservado é `Pensamento` e `Is` compara de forma exata (minúsculas, espaço no fim, `null`, vazio e `Protagonista` são falsos).
- **`ConversationLogTests`**: o histórico da conversa em entradas estruturadas: tipo, nome, cor (guardada opaca), texto e se é do protagonista; "Você recebeu" e "Você entregou"; a regra do nome (só na primeira fala de uma sequência do mesmo personagem; narração, pensamento e item interrompem; a opção escolhida continua a sequência do protagonista); o pensamento (`AddThought`: sem nome, cor guardada opaca, nunca mostra o nome, e a fala do protagonista depois dele mostra o nome de novo); a ordem, a limpeza entre conversas (a primeira fala depois mostra o nome) e o volume de 200 entradas.
- **`BubbleTextColorTests`**: o `BubbleTextColor`: contraste WCAG (preto e branco dão 21:1, uma cor com ela mesma dá 1:1), texto escuro em balão claro e pastel, texto claro em balão escuro, o alfa do balão ignorado, e um cinza médio reprovado (`IsReadable` falso).
- **`DialogueTriggerNodesTests`**: todo `InteractableDialogueTrigger` de cena ou prefab de `Assets/` com **Node Name** preenchido aponta para um nó que existe no roteiro (`ScenesAndPrefabs_TriggerOnlyNodesThatExistInTheScript`). Compara com `ScriptRun.FromProjectFiles().NodeNames`, que compila os `.yarn` direto, sem importador nem cache. Lê o campo `nodeName` por `SerializedObject` (o campo é privado), então a leitura já vem com os overrides de prefab resolvidos, e **Node Name** vazio não reprova. A mensagem traz `<asset>: <caminho do objeto>: nó '<nome>'`, e é o nome do nó que distingue os dois gatilhos do `Door_PortaTrancada`. Um gatilho apontando para o nó `variaveis` não reprova (ele existe no roteiro).
- **`ProjectComponents`** (auxiliar, `EditMode/ProjectComponents.cs`): `ReadAll<TComponent, TResult>(read)` percorre os componentes de um tipo em todas as cenas e prefabs de `Assets/` (os pacotes ficam de fora) e devolve, de cada um, o que `read` leu e o local (`<asset>: <Pai/Filho/Objeto>`). `read` roda dentro da janela em que a cena está aberta, porque o componente deixa de existir ao fechá-la. Abre de forma aditiva só as cenas que não estão carregadas e as fecha em seguida; uma cena já aberta é lida como está e nunca é fechada, e uma cena que está na Hierarchy mas descarregada é carregada para a leitura e volta descarregada, na Hierarchy (a #14 corrigiu o defeito que a tirava de lá).
- **`ConversationNotifierTests`**: um `DialogueStartedMessage` e um `DialogueEndedMessage` por conversa, e nunca um Ended sem Started.
- **`ScriptNodeNameTests`**: o formato dos nomes de nó.
- **`StoryVariableNameTests`**: as duas regras de nome: o que o armazenamento aceita (inclusive os nomes internos do Yarn) e o que quem monta cena pode digitar.
- **`StoryStateTests`**: booleano, número e texto: leitura e escrita, falso gravado × nunca gravado, nomes inválidos (vazio em silêncio, sem `$` com aviso), um nome em um só tipo, substituição total e limpeza.
- **`StoryStatePersistenceTests`**: a cópia entre `StoryState` e `GameState` (`Capture` e `Restore`), incluindo a ida e volta pelo JSON com afinidade, falso gravado e acentos.
- **`GameStateTests`**: ida e volta do JSON, com os três tipos de estado da história.
- **`RoomLinksTests`** (#13): três testes de conteúdo sobre `RoomExit` e `RoomEntryPoint` em cenas e prefabs de `Assets/`, no padrão do `DialogueTriggerNodesTests` (campos privados lidos por `SerializedObject`, com `Assert.Fail` se um campo for renomeado). As salas válidas são as cenas **habilitadas** em `EditorBuildSettings.scenes`, pelo nome do arquivo. `ScenesAndPrefabs_ExitsNameARoomInTheBuildList`: todo destino está preenchido e é uma sala do build (`<asset>: <objeto>: destino '<nome>'`). `ScenesAndPrefabs_ExitsNameAnEntryPointOfTheDestination`: todo ponto de entrada citado está preenchido e existe na cena de destino (`…: ponto '<id>' em '<sala>'`); saída com destino inválido não é repetida aqui. `Scenes_EntryPointIdsAreFilledAndUniquePerRoom`: nenhum ponto sem id nem com id repetido na mesma cena (a troca usa o primeiro que achar); ponto em prefab é ignorado.
- **`CollectableIdsTests`** (#14): `Scenes_CollectablePersistentIdsAreFilledAndUniqueAcrossRooms` lê o `persistentId` de todo `CollectableItemBehaviour` de cena (por `SerializedObject`, com `Assert.Fail` se o campo for renomeado) e reprova o id vazio (`<asset>: <objeto>: id vazio`) e o id repetido **em qualquer cena**, não só na mesma, porque o registro de consumidos é um só para o jogo inteiro (`'<id>' repetido em: <local 1>; <local 2>`). Coletável em prefab é ignorado, como o ponto de entrada no `RoomLinksTests`; as instâncias de um prefab aparecem nas cenas e reprovam se repetirem o id.

### Suíte PlayMode

`RoomChangeTests` (#15) roda o jogo de verdade: o `Managers.prefab` criado pelo `ManagersBootstrap` ao entrar no Play e as cenas `[Teste] Mecanicas` e `[Teste] Sala B`, trocadas pelo ponto de entrada `janela`. São testes de **caracterização**: o comportamento já existia, verificado à mão nas #12 a #14.

- **`RoomChange_KeepsASingleInstanceOfEachManager`**: depois de duas idas e voltas, há exatamente um `GameStateController`, `RoomNavigator`, `InventoryManager`, `GameSaveManager`, `DialogueManager`, `DialogueUIController`, `ScreenFader` e `EventSystem`, todos dentro do mesmo `Managers.prefab` do começo (não foram recriados nem vieram de uma cena). Um `Canvas_Game` ou `EventSystem` esquecido numa cena aparece como contagem 2.
- **`CollectedItem_StaysCollectedAfterLeavingAndComingBack`**: o coletável recolhido (por `Collect()` direto) está desativado na volta à sala, e o inventário continua com aquele item.
- **`RoomChange_BlocksInputUntilTheNewRoomIsShown`**: no frame da chamada e em todo frame até a sala nova estar na tela, o `PlayerInputGate` está fechado, `CanClickThisFrame` é falso e o fade barra a interface (`blocksRaycasts`); uma segunda troca no meio é recusada (aviso esperado); no fim a sala ativa é a nova, o fade está transparente e sem barrar. O teste exige ao menos um frame de transição, para não passar vazio.

Regras próprias (D-24): o `SetUp` zera o inventário e os consumidos **pelos donos**, nunca por `MessageBroker.Clear()` (apagaria as assinaturas dos managers vivos); o teste só lê o `PlayerInputGate` (D-28); a espera de uma troca tem limite de 10 s e falha dizendo em que sala parou; o `TearDown` espera uma troca pendente acabar. A fase vermelha de cada teste é uma sabotagem temporária do código de jogo (descrita no PR), nunca commitada.

> **Lacuna conhecida e deliberada:** `LockedActionBehaviour`, o `DialogueManager`, o apresentador
> `DialogueUIController`, o `DialogueHistoryView`, o `DialogueInputHandler` (inclusive a regra de que um
> clique em botão da interface não avança a fala), o `InteractableDialogueTrigger`, o `RoomNavigator`, o `RoomExit`, o `RoomEntryPoint` e o `ScreenFader` não têm teste de comportamento em EditMode (os nós que os gatilhos citam são conferidos pelo `DialogueTriggerNodesTests`). Todos exigiriam um `GameObject` (o `InventoryManager`, o `DialogueRunner`), o que quebraria
> a regra "C# puro, sem GameObjects" desta suíte; o `LockedActionBehaviour` ainda por cima é um
> `MonoBehaviour` cujas asserções interessantes são sobre `UnityEvent` disparando. O `ItemScriptActions`
> é cola entre três singletons e fica na mesma lacuna. A `Tests.asmdef` referencia `ProjetoVN.Inventory`
> só para o teste de ids usar o `ItemRegistry`. São verificados em Play Mode na cena `[Teste] Mecanicas`
> (o roteiro de verificação de cada issue). O que o `DialogueManager` decide sem Unity mora em classes
> puras testadas aqui: `ConversationNotifier` e `ScriptNodeName`. A revelação do texto também fica
> nessa lacuna: a contagem é o `LetterTypewriter` do pacote, e a escolha entre completar e avançar
> é um `if` do `DialogueManager`; as duas são verificadas em Play Mode (roteiro da #11). A troca de sala
> (recusas, ordem dos passos) também: é `MonoBehaviour`, estado, cena e prefab, e continua no roteiro
> de Play Mode da #12. Managers únicos depois de várias trocas, persistência do coletável e gate fechado
> durante a transição estão na suíte PlayMode (#15).
>
> A parte do `LockedActionBehaviour` que **mais** mereceria um teste automatizado é a ordem dos
> guardas: a flag é checada antes do item porque `IsTrue` não consome nada e `TryUse` consome.
> Extrair isso para uma classe pura só para testar criaria mais superfície do que as quatro linhas
> que ela embrulharia, então a proteção é o comentário no código mais a asserção em Play Mode
> ("portão de flag não come a chave").

---

## Estrutura e Práticas

- **Teste a lógica em C# puro, direto.** `StoryStateVariables`, `ConversationNotifier`, `InventoryModel` e `InventoryService` não são `MonoBehaviour`: instancie com `new` e chame os métodos. É por isso que o projeto mantém essa separação, e é por isso que ele **não** precisa de interfaces nem de um container de injeção de dependência para ser testável.
- **Comandos e consultas se testam chamando o método**, não publicando mensagens. Publicar uma mensagem para provocar um comando testa o barramento, não o sistema.
- **Use o `MessageBroker` para verificar as notificações**: assine a mensagem que o sistema deveria publicar, execute a ação e confira o que chegou. Chame `MessageBroker.Clear()` no `SetUp` para que um teste não herde assinaturas de outro.
- **Todo estado estático precisa ser limpo no `SetUp` *e* no `TearDown`.** Vale para `MessageBroker.Clear()` e para `StoryState.ClearAll()`: sem isso, a ordem dos testes passa a importar e a suíte fica intermitente.
- **Um teste que espera um aviso usa `LogAssert.Expect`; um que prova silêncio termina com `LogAssert.NoUnexpectedReceived()`.** Um aviso inesperado (`LogType.Warning`) não reprova um teste sozinho.
- **Fase vermelha no Unity:** um teste que cita uma API que ainda não existe é erro de compilação, e a suíte inteira deixa de rodar. Crie a assinatura vazia primeiro, para o teste falhar por asserção.
- **Para testar um roteiro, use o `ScriptRun`** (`EditMode/ScriptRun.cs`): `ScriptRun.FromText("title: no
---
...")` compila um texto, `ScriptRun.FromProjectFiles()` compila os `.yarn` de `Assets/Roteiro/`, e `Start`, `Choose`, `Lines`, `OptionTexts`, `Completed` e `ErrorsAndWarnings` dizem o que o jogador veria; `Declarations` lista as variáveis explícitas do roteiro (nome, tipo, descrição, arquivo), `UnavailableOptionTexts` guarda as opções do bloco em espera cuja condição é falsa (elas também estão em `OptionTexts`; `Choose(i)` indexa todas, inclusive as indisponíveis), `Commands` guarda o texto de cada comando, `OwnedItems` é o inventário que `tem_item` consulta, `Program` é o roteiro compilado e `StringTable` é a tabela de textos da compilação (texto, nó, linha e etiquetas de cada linha), que o `ScriptSpeakers` lê junto com o `Program`. Ele roda sobre o `StoryState` de verdade, então o `SetUp` e o `TearDown` chamam `StoryState.ClearAll()`. Escreva `Yarn.Dialogue` por extenso: dentro de `ProjetoVN.*`, `Dialogue` é o namespace.
- O `.asmdef` deste módulo é restrito ao ambiente de testes, então ele não entra na build final do jogo.

---

## Verificação em Play Mode (armadilha que já custou tempo)

O que não dá para cobrir em EditMode é verificado rodando o jogo pelo Editor conectado; as receitas
estão em [docs/agentes/unity-cli.md](../../../docs/agentes/unity-cli.md). Uma pegadinha importante:

> **Se o Editor estiver sem foco — o caso normal quando ele é dirigido pela CLI — o Play Mode
> congela**, porque o Player Settings tem `runInBackground: 0`. Nenhum frame avança, então a Unity
> **nunca chama `Start()`, `Update()` nem continua corrotinas**. O sintoma engana: `SceneManager.LoadScene`
> ainda conclui e os objetos são recriados de verdade, então parece que o código de restauração é que
> está quebrado.
>
> Comece todo teste que dependa de frames com:
> ```csharp
> UnityEngine.Application.runInBackground = true;
> ```
> É uma mudança só de runtime: não suja o `ProjetoSettings.asset`. Para confirmar que está rodando,
> leia `UnityEngine.Time.frameCount` duas vezes com alguns segundos de intervalo — se o número não
> mudar, o jogo está parado e qualquer asserção sobre `Start()` é falso negativo.

Asserções feitas só com chamadas diretas de método (`door.Interact()`, `manager.Save()`) não dependem
de frames e funcionam mesmo com o jogo parado.
