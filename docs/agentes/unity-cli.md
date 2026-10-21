# Unity CLI: receitas e armadilhas

O Unity CLI (`unity`) controla o Editor aberto pelo pacote `com.unity.pipeline`, já instalado no projeto. Antes de usar, carregue a skill `unity:unity-cli`. Esta página guarda só o que é específico deste projeto: as receitas do dia a dia e as armadilhas que já custaram tempo.

Regra geral: **com o Editor aberto, altere cenas, prefabs e assets pelo CLI**, e não editando o YAML à mão. As exceções estão em [Armadilhas de serialização](#armadilhas-de-serialização).

## Receitas

Todos os comandos rodam a partir da raiz do repositório. Em scripts, acrescente `--no-banner`; para ler a saída por programa, `--format json`.

### O Editor está conectado?

```bash
unity status
```

O estado esperado é `ready`. Se nenhum Editor aparecer, verifique se há erro de compilação: com erro, o Editor entra em Safe Mode e o CLI não conecta.

Se o Editor estiver fechado (não existe `Temp/UnityLockfile` e `unity editors running` devolve zero instâncias), abra o projeto e repita o `unity status` até ele ficar `ready`:

```bash
unity open .
```

Não encadeie esse comando em um pipe (`unity open . | tail`): o Editor herda a saída e o pipe só fecha quando o Editor fecha.

### Listar os comandos que o Editor expõe

```bash
unity command --query scene
```

Sem `--query`, lista todos. Cada linha mostra os parâmetros do comando.

### Recompilar depois de alterar scripts

```bash
unity command recompile
```

```bash
unity command recompile_status
```

Repita o segundo até o resultado ser `completed` ou `up_to_date`. Erros de compilação aparecem no console:

```bash
unity command console --level error --tail 20
```

### Rodar os testes

```bash
unity command run_tests --mode EditMode --timeout 180
```

O resultado esperado tem a forma `48/48 aprovados (EditMode, 3.87s)`. O número cresce a cada issue; o que importa é não haver falha. Para um subconjunto:

```bash
unity command run_tests --mode EditMode --filter StoryStateTests
```

Alguns testes provocam um erro de propósito (o do `MessageBroker` com assinante que lança exceção). A entrada `InvalidOperationException: falha proposital` no console é esperada.

### Rodar a suíte PlayMode

```bash
unity command run_tests --mode playmode --async_tests true --timeout 300
```

```bash
unity command test_status
```

A PlayMode **não roda de forma síncrona** pelo CLI: `run_tests --mode playmode` sem `--async_tests` é recusado, porque entrar no Play recarrega o domínio e derruba a requisição. O comando volta na hora, sem esperar a execução: na saída em tabela (o padrão) ele mostra `0/0 aprovados (PlayMode, 0s)`, e isso é o esperado, não uma suíte vazia; o `running` só aparece com `--format json`. O resultado vem do `test_status`: repita-o a cada uns 5 s até ele dizer `completed: 3/3 aprovados` (o número cresce com as issues). Durante a recarga de domínio da entrada no Play o `test_status` pode falhar com `Network error`: repita. Para ver a mensagem de uma falha, use `unity command test_status --format json`: o campo `result` traz, por teste, `Status`, `Message` e `StackTrace`. `--mode all` roda a EditMode na hora e devolve a PlayMode como `playmode_running`.

Para conferir quais testes cada suíte enxerga, `unity command list_tests --mode playmode` e `--mode editor` (um asmdef só de Editor faria um teste PlayMode aparecer na EditMode).

O Test Framework liga o `runInBackground` durante a execução e devolve o valor no fim (`PreparePlayModeRunTask.cs`, `RestoreProjectSettingsTask.cs`). Com a D-31 o valor já é `true`, então isso não muda nada: depois da suíte, `git diff ProjectSettings/ProjectSettings.asset` continua vazio.

**Salve a cena antes de rodar os testes.** Com a cena aberta suja, o Test Runner abre o diálogo "Scene(s) Have Been Modified" e bloqueia a thread principal do Editor: `run_tests` e `eval` expiram sem erro claro, e só um clique no diálogo destrava (medido na execução da #3). Rode `save_scene` depois de qualquer alteração de cena e antes do `run_tests`, **só se a cena estiver suja** (`SceneManager.GetActiveScene().isDirty`) e confira o `git status` depois: na #15, uma sequência de execuções com `save_scene` antes de cada uma deixou o `[Teste] Mecanicas.unity` modificado, com `RectTransform` de filhos de um layout group do canvas gravados com valores calculados (a causa não foi isolada). Reverta a cena com `git checkout` se isso acontecer; ele não entra no commit.

### Executar C# no Editor

```bash
unity command eval 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;'
```

O código é um corpo de método: instruções terminadas em `;` e um `return` para devolver valor. Use nomes de tipo completos, com namespace.

### Atualizar o banco de assets depois de criar arquivos por fora

```bash
unity command eval 'UnityEditor.AssetDatabase.Refresh(); return "ok";'
```

É isso que faz o Editor gerar o `.meta` de um arquivo criado direto no disco, como um README novo dentro de `Assets/`.

### Abrir e salvar cena

```bash
unity command open_scene --path "Assets/Scenes/[Teste] Mecanicas.unity"
```

```bash
unity command save_scene
```

### Entrar e sair do Play Mode

```bash
unity command editor_play
```

```bash
unity command editor_stop
```

### Capturar a tela do jogo

```bash
unity command capture_game_view --source screen --save_path Temp/captura.png
```

`--source screen` só funciona em Play Mode e é o único que inclui a interface (o canvas é Overlay). Sem ele, a captura mostra só o que a câmera renderiza.

**A captura sempre cai dentro de `Assets/`.** O exemplo acima grava em `Assets/Temp/captura.png`, com `.meta`; um caminho absoluto dentro do projeto dá no mesmo, e um caminho fora do projeto é recusado (`400 Bad Request: Path … is outside the project root`). Para a captura não entrar no commit, copie o arquivo para fora do repositório e apague a pasta antes de commitar: `unity command delete_asset --asset "Assets/Temp" --confirm true`.

### Mover, renomear e apagar assets

```bash
unity command move_asset --asset "Assets/Scripts/A/Coisa.cs" --destination "Assets/Scripts/B/Coisa.cs"
```

```bash
unity command delete_asset --asset "Assets/Scenes/Antiga.unity" --confirm true
```

Sempre pelo CLI ou por `AssetDatabase`, nunca por `mv` ou `rm`: é o que leva o `.meta` junto e preserva o GUID.

## Verificação em Play Mode

O que os testes EditMode não cobrem (MonoBehaviours, cenas, `UnityEvent`) é verificado rodando o jogo e conferindo o estado por `eval`. Cada issue traz o seu roteiro de verificação no comentário de levantamento.

### O Play Mode e o Editor sem foco

Um Editor dirigido pelo CLI está sem foco. O projeto roda sem foco (`runInBackground` ligado no Player Settings, D-31), então **o Play Mode avança e ninguém precisa atribuir nada** antes de uma verificação que dependa de frames.

**Não atribua `Application.runInBackground`.** No Editor essa propriedade é a própria opção do Player Settings (`SetPlayerSettingsRunInBackground`): uma atribuição muda o `ProjectSettings/ProjectSettings.asset` na próxima gravação (medido na #79: `runInBackground: 1 → 0` no arquivo depois de um `AssetDatabase.SaveAssets()`). Se o `git status` mostrar `runInBackground` alterado, alguém atribuiu; o arquivo não entra no commit e o valor correto é `1`.

**Diagnóstico.** Se uma verificação der resultado estranho em Play Mode, confirme que o jogo avança: leia `Time.frameCount` duas vezes, com alguns segundos de intervalo.

```bash
unity command eval 'return UnityEngine.Time.frameCount;'
```

Se o número não mudou, o jogo está parado e qualquer conclusão sobre `Start()`, `Update()` ou corrotinas é falsa. O sintoma engana, porque `SceneManager.LoadScene` ainda conclui e os objetos são recriados, e parece que o código de restauração é que está quebrado. Esse congelamento foi visto com `runInBackground` desligado nas verificações da M3; na #79 ele **não se reproduziu** (com a opção desligada e o Editor sem foco, o `frameCount` avançou), então a condição exata que o provoca não está isolada. Por isso o diagnóstico fica, mesmo com a opção ligada.

Asserções feitas só por chamada direta de método (`porta.Interact()`, `GameSaveManager.Instance.Save()`) não dependem de frames e valem mesmo com o jogo parado.

### Comandos que falham com "Network error" logo depois de criar ou apagar scripts

Depois de mexer em `.cs`, o Editor recarrega o domínio, e um `eval` ou `recompile` enviado nesse intervalo falha com `Network error: An error occurred while sending the request`. Não é erro do código: espere e rode `unity command recompile_status` até `completed`, depois repita o comando.

### O `$` e o shell

Os nomes de variável de história começam com `$`. Dentro de aspas duplas o shell o expande para vazio e o `eval` roda com o nome errado, sem erro. Todo `eval` com um nome como `$falou_com_luna` vai entre aspas simples.

### O `eval` roda tudo em um frame só

Um `eval` com um laço que chama `AdvanceDialogue()` várias vezes não avança nada além da primeira fala: a apresentação do Yarn é assíncrona e só troca a fala no frame seguinte. Para percorrer uma conversa, faça uma chamada de `eval` por passo, com um `sleep` de um ou dois segundos entre elas, e leia o `DialogueText` a cada passo. O mesmo vale para ler a interface logo depois de `StartDialogue` ou `MakeChoice`: espere um instante.

**O texto é revelado aos poucos (#11).** Cada fala pede dois `AdvanceDialogue()`, um para completar e outro para avançar, ou uma espera até a revelação acabar. Dois `AdvanceDialogue()` no mesmo `eval` só completam a fala (é o caso "dois cliques no mesmo frame"). O `text` do `DialogueText` tem a fala inteira desde o começo da revelação: o que o jogador vê é `maxVisibleCharacters`, e o total é `textInfo.characterCount`. Cada `eval` leva perto de 1,5 s de ida e volta, e uma fala curta termina de aparecer sozinha entre dois comandos, o que faz um clique que devia completar avançar. Para medir, baixe a velocidade só em runtime: em Play Mode, `SerializedObject` no `DialogueUIController`, campo `charactersPerSecond` (1 deixa cada passo inequívoco), e `ApplyModifiedProperties()`. O valor volta ao do prefab no Play seguinte, e o `GameUI.prefab` não fica alterado.

### O `eval` é o corpo de um método

Não aceita `using` no topo nem método de extensão por sintaxe de ponto de um namespace que você não importou. Para consultar UI Toolkit, chame a extensão como método estático: `UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.DropdownField>(raiz).ToList()`. Código com mais de uma ou duas linhas vai em um arquivo `.cs` e roda com `unity command eval_file --file <caminho>`, o que também escapa do problema do `$` no shell.

### O erro de console que é do CLI, não do jogo

Um `eval` enviado enquanto o Editor ainda está entrando em Play Mode pode estourar o tempo de resposta. O comando falha e o pacote Pipeline deixa um erro no console:

```
Failed to handle /api/exec request: Main thread operation timed out after 5000ms
```

Esse erro não vem do jogo. Espere uns dez segundos depois do `editor_play` antes do primeiro `eval`. Se o erro aparecer mesmo assim, ele não conta como falha de "console sem erro"; para uma leitura limpa, saia do Play Mode, rode `clear_console` e repita.

### Trocar de branch com o Editor aberto quando os pacotes mudam

Se as duas branches têm `Packages/manifest.json` diferentes (uma instala um pacote que a outra não tem), **feche o Unity antes do `git checkout`** e reabra depois. Com o Editor aberto, ele pode ficar preso no diálogo "Package Manager (busy for …) Resolving packages…": a thread principal para, todo `eval` falha com `Main thread operation timed out`, o `run_tests` expira e a janela do Package Manager não lista nada. Aconteceu duas vezes na issue #5, entre `main` e a branch que instalava o Yarn Spinner.

- **Como reconhecer:** o arquivo `upm.log`, em `%LOCALAPPDATA%/Unity/Editor/`, mostra `project:resolve-packages --> 200` (a resolução terminou em segundos), mas o diálogo continua aberto. O Package Manager não está trabalhando; quem travou foi o Editor. O mecanismo exato não foi confirmado.
- **Como sair:** finalize o `Unity.exe` pelo Gerenciador de Tarefas e reabra o projeto já na branch certa. Não é preciso reiniciar o computador. Ao reabrir, responda **No** ao diálogo "Recovering Scene Backups" (a cena válida é a do git) e apague `Assets/_Recovery/` se ela existir.
- **Não chame `UnityEditor.PackageManager.Client.Resolve()` por `eval`** para forçar a resolução com o Editor aberto: foi logo depois disso, seguido de um `AssetDatabase.Refresh()`, que o Editor travou na segunda vez.
- Enquanto o pacote não é resolvido, o console mostra erros de compilação que não são do código (`The type or namespace name 'Yarn' could not be found`), e um `run_tests` roda os assemblies antigos e devolve um resultado que não vale para a branch.

### O que não dá para simular

O CLI não simula clique de mouse. Um comportamento que dependa do clique real (como o clique que encerra um diálogo não atingir o mundo) é verificado chamando os métodos na mesma ordem e conferindo o estado, e a issue registra que o teste com mouse de verdade fica para o programador.

### Trocar de sala por `eval` (#12)

A troca leva vários frames, e o `eval` roda em um só: dispare num `eval` e observe em outros, com espera entre eles. Só o fechamento do `PlayerInputGate` acontece no mesmo frame da chamada. Se a troca ficar parada no preto, confira antes se o jogo avança (`Time.frameCount` duas vezes, ver [O Play Mode e o Editor sem foco](#o-play-mode-e-o-editor-sem-foco)).

```bash
unity command eval 'return ProjetoVN.GameFlow.Rooms.RoomNavigator.Instance.ChangeRoom("[Teste] Sala B");'
```

Para a troca ser observável, suba a duração só em runtime: em Play Mode, `SerializedObject` no `RoomNavigator`, campo `fadeSeconds` (3 deixa cada metade inequívoca), e `ApplyModifiedProperties()`. O valor volta ao do prefab no Play seguinte e o `Managers.prefab` não fica alterado. Durante a troca, `ScreenFader`/`CanvasGroup` mostram `alpha` subindo e `blocksRaycasts` verdadeiro, `PlayerInputGate.IsEnabled` falso, e um `RaycastAll` do `EventSystem` no centro da tela acha primeiro a `Image` `Black` do `Canvas_Fade`. Uma sala só carrega por nome se estiver na lista de cenas do build; o `Application.CanStreamedLevelBeLoaded` é o que decide.

**Com ponto de entrada (#13).** `ChangeRoom` tem a sobrecarga `ChangeRoom(sala, idDoPonto)`. Depois da troca, leia a posição do cenário: `GameObject.Find("ScrollingBackground").transform.position.x`. Os cliques das saídas se simulam com `GameObject.Find("Exit_Janela").GetComponent<ProjetoVN.PointNClick.InteractableItem>().OnClick()` (o CLI não simula mouse). Um `eval` de `OnClick` numa sala onde o objeto não existe só dá `Object reference not set`, sem trocar nada: confira a cena ativa antes. Com o mouse real sobre a borda da janela do Editor o pan anda sozinho (medido `x = 0,2` em vez de `0` antes de qualquer chegada); leia a posição logo depois da chegada, quando o gate ainda está fechado ou acabou de abrir.

**Provocar uma falha inesperada na troca.** Destruir o `Canvas_Fade` no meio da troca faz o `ScreenFader` lançar `MissingReferenceException` (é o único jeito de fora, sem gancho de teste). Suba `fadeSeconds` para 8 e: (a) antes do carregamento, destrua o fade 1 ou 2 s depois de disparar a troca; (b) depois do carregamento, dispare, leia a cena ativa em laço (um `eval` por volta) até ela mudar, e só então destrua o fade (com 3 s a troca inteira termina entre dois `eval` e a falha não acontece). Cada tentativa gasta o `Canvas_Fade` da sessão de Play: **saia e entre no Play de novo** antes de repetir.

**Gravar uma cena alterada por `eval_file`.** `EditorSceneManager.SaveOpenScenes()` dentro de um `eval` **não gravou** a cena (`SerializedObject.ApplyModifiedProperties` deixa a cena suja, mas o arquivo ficou como estava), e o `run_tests` seguinte abriu o diálogo "Scene(s) Have Been Modified": o Editor travou e todo comando do CLI (inclusive `cancel_tests`) expirou até reiniciar o Unity. Grave sempre com `unity command save_scene` e **confira no arquivo** (`grep` no `.unity`) antes de rodar a suíte. Numa fase vermelha de teste de conteúdo (sabotar a cena de propósito) isso importa em dobro: ao reiniciar o Editor, a cena sabotada é a que está no disco.

### Os testes PlayMode dividem uma sessão de Play (#15)

O `Managers.prefab` é criado uma vez por sessão de Play (`ManagersBootstrap`, `AfterSceneLoad`) e é `DontDestroyOnLoad`: os testes da `Tests.PlayMode` compartilham os mesmos managers, e o que um deixa no inventário o outro vê. O `SetUp` zera o estado **pelos donos** (`InventoryManager.ReplaceAll`, `ReplaceConsumedWorldObjectIds`); **`MessageBroker.Clear()` não serve aqui**, porque apagaria as assinaturas do `GameStateController` e o jogo deixaria de entrar e sair do modo de diálogo pelo resto da sessão. Mais quatro pontos:

- **`Start` só roda no frame seguinte.** Depois de `LoadSceneAsync`, um `yield return null` antes de chamar `ChangeRoom`: `IsExploring` só fica verdadeiro no `Start` do `GameStateController`.
- **O fim da troca é o `PlayerInputGate.IsEnabled` voltar a `true`** (é o `GameplayState.Enter`, a última coisa que ela faz); `GameStateController.IsExploring` é `internal` e não deve ser aberto só para o teste.
- **Erro no console reprova o teste PlayMode** (`LogAssert`); aviso não. Uma recusa de troca loga aviso, então `LogAssert.Expect` a documenta.
- **Chamar `Collect()` direto.** O `OnClick` do `Item_Chave` também abre a conversa `peguei_chave`, e uma conversa aberta recusa a troca de sala (D-19).

### FPS, VSync e quadro longo no Play Mode (#80)

A suíte PlayMode roda em dois modos (10 FPS e sem limite, ver [Tests/README.md](../../Assets/Scripts/Tests/README.md)). Fatos medidos no Editor 6000.3.9f1 sem foco:

- **O Editor roda no nível de qualidade `Very Low`** (`vSyncCount: 0`); a build de desktop usa `Ultra` (`vSyncCount: 1`), então **numa build o jogo roda na frequência do monitor** (60, 144, 240 Hz). Sem limite, o Editor sem foco faz ~380 a 470 FPS.
- **`Application.targetFrameRate` vale no Play Mode sem foco** (`= 10` deu 10 FPS). **No Editor o `vSyncCount` não manda:** com `vSyncCount = 1` o alvo 10 venceu, e com `-1` o jogo seguiu a ~440 FPS. A armadilha de que o `targetFrameRate` é ignorado com VSync ligado vale na **build**; por isso o teste zera o `vSyncCount` mesmo assim.
- **Sair do Play devolve o `vSyncCount` e o `targetFrameRate`** (medido: `1 10` em Play, `0 -1` depois do `editor_stop`), e o `git status` fica limpo. Por isso ler `0 -1` **fora** do Play não prova que o `TearDown` devolveu os valores; essa devolução só se observa dentro do Play. O código a garante: `RestoreFrameRate()` é a primeira instrução do `[UnityTearDown]`. Mesmo assim, **não chame `AssetDatabase.SaveAssets()`** com o `vSyncCount` alterado: `QualitySettings` é um asset de `ProjectSettings/`, como o `runInBackground` (D-31).
- **`Time.deltaTime` é limitado pelo `Time.maximumDeltaTime` (0,333 s); `Time.unscaledDeltaTime` não é.** Um travamento de 1,5 s deixou no quadro seguinte `dt = 0,333` e `udt = 1,566`. Código que anima por `unscaledDeltaTime` (o `ScreenFader`) recebe o passo inteiro; o que anima por `deltaTime` (o pan) recebe no máximo 0,333 s.
- **Provocar um quadro longo:** um laço com `System.Diagnostics.Stopwatch` na thread principal (no `eval` ou no teste); nada de thread nem `Thread.Sleep` (regra 9).
- **Os quadros de um `eval` para outro:** registre por `Application.onBeforeRender` e guarde a lista com `AppDomain.CurrentDomain.SetData`, para ler em outro `eval`.

### Contar o que está visível

O painel de inventário reaproveita slots: um slot devolvido é desativado, não destruído. `transform.childCount` não diz quantos itens estão visíveis. Conte os filhos ativos.

## Armadilhas de serialização

Lições que custaram horas. Todas valem para cenas, prefabs e assets.

### Um `UnityEvent` guarda o tipo por nome

Uma ligação feita no Inspector grava o alvo como `"Namespace.Tipo, Assembly"`. **Renomear a classe, mudar o namespace ou mover a classe de asmdef quebra a ligação sem erro de compilação.** Depois de qualquer uma dessas mudanças:

1. Procure o nome antigo em todos os `.unity` e `.prefab` (`m_TargetAssemblyTypeName` e `m_ObjectArgumentAssemblyTypeName`).
2. Corrija as ocorrências.
3. Nunca substitua em bloco uma string de assembly: tipos diferentes compartilham a mesma string.

### `m_EditorClassIdentifier` nunca é atualizado pelo Editor

Essa linha do YAML é só uma dica de leitura. Salvar a cena, reserializar o componente e `ForceReserializeAssets` deixam o valor antigo. A ligação real é o GUID em `m_Script`, então nada quebra, mas a string errada engana quem lê o arquivo. Para corrigir, feche a cena no Editor e edite o YAML. O formato é `<Assembly>::<Namespace>.<Tipo>`.

### Trocar ou renomear um campo não reescreve os assets

O Unity descarta a chave desconhecida ao carregar, mas só regrava o arquivo quando algo o modifica. Depois de renomear ou trocar o tipo de um campo de um tipo autorado, force a regravação:

```bash
unity command eval 'UnityEditor.AssetDatabase.ForceReserializeAssets(new[] { "Assets/caminho/do/arquivo.asset" }); return "ok";'
```

### Apagar um script

Antes de apagar um `.cs` de MonoBehaviour ou ScriptableObject:

1. Pegue o GUID no `.meta` do script.
2. Procure esse GUID em `Assets/` (cenas, prefabs, assets).
3. Remova os componentes e assets que o usam, **pelo Editor**, antes de apagar o script. A ordem inversa deixa "Missing Script".

### Mover um script

Mova com `move_asset` para que o `.meta` vá junto. Se o `.meta` se perder, o GUID muda e todo componente daquele tipo vira "Missing Script", com as ligações de `UnityEvent` destruídas.

### Um arquivo novo dentro de `Assets/` precisa do `.meta`

Vale também para `.md`, `.yarn` e pastas. Atualize o banco de assets e commite o `.meta` junto.

### Managers e interface de jogo não entram em cena

`DialogueManager`, o `DialogueRunner` do Yarn Spinner, `StoryStateVariableStorage`, `InventoryManager`, `GameStateController`, `RoomNavigator`, `DialogueInputHandler`, `GameSaveManager` e `ItemScriptActions` existem só no `Managers.prefab`. Um deles colocado em uma cena sobrescreve o `Instance` do persistente.

O mesmo vale para a interface de jogo, que vem aninhada nesse prefab (`Assets/Prefabs/UI/GameUI.prefab`: `Canvas_Game`, `Canvas_Fade` e `EventSystem`):

- **Um `EventSystem` em uma cena** soma-se ao persistente e o uGUI loga `There are 2 event systems in the scene. Please ensure there is always exactly one event system in the scene` **a cada frame** (milhares de avisos em segundos). Na verificação, conte os `EventSystem` em Play Mode (deve ser 1) e confira que esse aviso não aparece.
- **Um `Canvas_Game` em uma cena** duplica o apresentador: o da cena se registra por último no `DialogueManager` e, ao ser destruído com a cena, deixa o diálogo sem interface.
- Em Play Mode, `GameObject.Find("Canvas_Game")` devolve o objeto persistente (cena `DontDestroyOnLoad`, pai `GameUI`, avô `Managers`). Para editar a interface fora do Play, abra o `GameUI.prefab`, não a cena.

## Armadilhas do Yarn Spinner

Descobertas na issue #5. A prova de conceito (#3) e o código do pacote estão em `Library/PackageCache/dev.yarnspinner.unity@*/`.

- **Comando desconhecido trava o diálogo.** Um `<<comando>>` sem handler faz o runner logar erro e parar sem avançar. O `DialogueManager.SkipUnknownCommand`, ligado ao `onUnhandledCommand` por **ligação persistente** no `Managers.prefab` (um `AddListener` em runtime não conta), ignora o comando com um erro e a conversa segue. Mesmo assim: **não escreva um comando no `.yarn` antes de ele existir** (o compilador não valida o nome).
- **Comando conhecido com número errado de parâmetros trava o diálogo.** `<<dar_item>>` sem id, ou `<<dar_item a b>>`, faz o runner logar `Can't call command <<dar_item>>: dar_item requires 1 parameter, but 0 were provided.` e chamar `Continue()` sem sinalizar o fim do comando: a conversa fica presa na fala anterior, com `IsDialogueRunning` verdadeiro, então nem a rede de segurança do `DialogueManager` nem o `SkipUnknownCommand` atuam (medido na #6; registrado na #37). Para os comandos de item, o `ScriptContentTests` reprova o roteiro antes do Play. Para sair de uma conversa presa durante uma verificação, saia do Play Mode.
- **Função desconhecida compila sem erro nem aviso.** Um `tem_itm("x")` digitado errado é declarado de forma implícita pelo compilador e só falha em runtime. `ProjectScripts_CompileWithoutErrorsOrWarnings` não pega esse erro.
- **Comando e função novos: C# primeiro, `.yarn` depois.** O gerador de código do pacote acha métodos **estáticos** com `[YarnCommand]` e `[YarnFunction]` sem registro nosso; o asmdef do módulo precisa referenciar `YarnSpinner.Unity`. Para executar um roteiro fora do runner (o `ScriptRun` dos testes), a função precisa estar registrada na `Library` do `Yarn.Dialogue`, senão a VM lança `Function tem_item is not present in the library`. Em um roteiro em memória no Play Mode, passe `runner.Dialogue.Library` ao `CompilationJob`.
- **`<<jump>>` para nó inexistente compila com aviso.** O compilador dá só o aviso `YS0012`, e em runtime o Yarn lança e para sem chamar o fim da conversa. A rede de segurança no `Update` do `DialogueManager` encerra a conversa com um erro. O `ScriptContentTests` reprova qualquer aviso nos roteiros, o que pega o erro antes do Play.
- **Um `.yarn` só vira conteúdo dentro de um `.yarnproject`.** Os roteiros ficam em `Assets/Roteiro/`, cobertos por `Assets/Roteiro/Roteiro.yarnproject` (`**/*.yarn`). Um `.yarn` fora dessa pasta não é importado.
- **Arquivos criados pelo pacote em `ProjectSettings/Packages/dev.yarnspinner/`.** O `YarnSpinnerProjectSettings.json` é commitado (a #5 foi autorizada a isso). O `ProjetoVN.GameFlow-generated.ysls.json` também é commitado desde a #8: o Unity o **regenera a cada compilação** (lista os comandos e funções de C#: `dar_item`, `remover_item`, `tem_item`, com parâmetros, arquivo e linha) e é ele que faz a extensão do VS Code conhecer os comandos do jogo. **Quem criar ou alterar um `[YarnCommand]` ou `[YarnFunction]` (#10, #20) commita o arquivo regenerado no mesmo PR** (qualquer mudança em `ItemScriptActions.cs` já o altera, por causa da linha). A extensão só usa um `.ysls.json` listado no campo `definitions` do `Roteiro.yarnproject`; um arquivo solto na pasta é "não rastreado" e `dar_item` aparece como `Unknown command`. O campo aponta para o arquivo gerado (`../../ProjectSettings/Packages/dev.yarnspinner/ProjetoVN.GameFlow-generated.ysls.json`) e o Unity reimporta o projeto sem reclamar dele. Se um novo asmdef do projeto ganhar comandos, o Unity gera um `<Assembly>-generated.ysls.json` novo: acrescente-o ao `definitions`. O pacote também abre a janela "About Yarn Spinner" na primeira carga.
- **O choque de nome `Dialogue`.** Dentro de `ProjetoVN.*`, `Dialogue` resolve para o namespace `ProjetoVN.Dialogue`, não para a classe do Yarn. Escreva `Yarn.Dialogue` por extenso.
- **`lastline`.** O compilador etiqueta com `lastline` a fala que é o comando **imediatamente anterior** a um bloco de opções, e o apresentador usa isso para mostrar fala e opções juntas. Um `<<set>>` entre a fala e as opções tira a etiqueta, e o jogador passa a precisar de um clique a mais.
- **Narração com dois-pontos vira personagem.** `Atenção: a porta fechou.` é lida como a personagem "Atenção", e `Eram 10:30 da manhã.` como a personagem "Eram 10": **qualquer** dois-pontos numa linha de narração faz o que vem antes virar o nome. Evite-os ou escape com `\:` (`Eram 10\:30 da manhã.`).
- **`variableStorage` nulo no runner** faz o Yarn criar um `InMemoryVariableStorage` em silêncio, e o roteiro passa a ter um estado paralelo (contra a D-18). Na verificação, conte os `InMemoryVariableStorage` em Play Mode: deve ser zero.
- **Não toque em `DialogueRunner.Dialogue` nem em `YarnProject.Program` antes de conferir `compiledYarnProgram`:** com erro de compilação eles lançam.
- **`#nullable`.** O pacote declara `YarnTask<DialogueOption?>`; no nosso código, sem contexto anulável, escreva `YarnTask<DialogueOption>`.
- **Remover vários scripts e assets de uma vez:** apague os assets que os usam antes e faça tudo entre `AssetDatabase.StartAssetEditing()` e `StopAssetEditing()`, com um único `Refresh`. Um erro de compilação no meio deixa o Editor em Safe Mode e o CLI para de conectar (`unity pipeline list` confirma).

### Testar um roteiro quebrado sem criar arquivo

Criar um `.yarn` quebrado em `Assets/Roteiro/` suja o projeto e obriga a apagá-lo depois. Em Play Mode dá para testar os quatro casos de roteiro inválido trocando o `yarnProject` do runner por um criado em memória (`eval_file`):

- Compile um texto com `Yarn.Compiler.Compiler.Compile(CompilationJob.CreateFromString(...))`.
- Crie `ScriptableObject.CreateInstance<Yarn.Unity.YarnProject>()`, grave `compiledYarnProgram = Google.Protobuf.MessageExtensions.ToByteArray(resultado.Program)` e uma `Yarn.Unity.Localization` com `AddLocalizedStrings` sobre a tabela de textos em `baseLocalization`.
- Troque com `SerializedObject(runner).FindProperty("yarnProject").objectReferenceValue = projeto`. Para o caso do roteiro com erro de compilação, deixe `compiledYarnProgram = null`.
- Volte ao projeto real com `AssetDatabase.LoadAssetAtPath<Yarn.Unity.YarnProject>("Assets/Roteiro/Roteiro.yarnproject")`.
- Grave também `project.lineMetadata = new Yarn.Unity.LineMetadata()` e, para cada entrada da tabela de textos com etiquetas, `AddMetadata(id, etiquetas)`; sem isso a etiqueta `lastline` some e a fala não aparece junto das opções.
- **Pare a conversa em curso antes de trocar o projeto** (`FindFirstObjectByType<Yarn.Unity.DialogueRunner>().Stop()`). Com uma conversa aberta, `StartDialogue` devolve `false` e a interface continua mostrando a fala antiga.
- Para medir como o texto fica na interface (medições da #8), leia os componentes TMP do `DialogueUIController` por `SerializedObject` (`dialogueText`, `speakerNameText`, `choiceTexts`): `preferredHeight` e `preferredWidth` valem, mas **`textInfo.lineCount` devolve 1** em texto que quebra, então não use.

### Receber um roteiro (PR de roteirista)

Um PR de roteirista não tem issue nem as três sessões (ver [fluxo-de-trabalho.md](fluxo-de-trabalho.md#pr-de-roteiro)). O que o programador (ou o agente, a pedido dele) faz antes do merge:

1. Faça checkout da branch `roteiro/<nome>-<assunto>`. Se o `Packages/manifest.json` da branch for igual ao da `main` (o normal num PR de roteiro), trocar de branch com o Editor aberto é seguro; se diferir, veja a armadilha dos pacotes acima.
2. Atualize o banco de assets para o Unity gerar o `.meta` de cada `.yarn` novo, e commite os `.meta` **na branch do roteirista**:

   ```bash
   unity command eval 'UnityEditor.AssetDatabase.Refresh(); return "ok";'
   ```

3. Reimporte o projeto de roteiro (um `.yarn` novo pode não entrar sem isso, #34):

   ```bash
   unity command eval 'UnityEditor.AssetDatabase.ImportAsset("Assets/Roteiro/Roteiro.yarnproject", UnityEditor.ImportAssetOptions.ForceUpdate); return "ok";'
   ```

4. Rode a suíte. O `ScriptContentTests` reprova aviso de compilação, id de item inexistente, nó fora da convenção e variável fora do `variaveis.yarn`:

   ```bash
   unity command run_tests --mode EditMode --timeout 180
   ```

5. Abra a conversa em Play Mode (receita acima) e confira falas, opções e tamanho do texto.
6. Confira o `git status`: só `Assets/Roteiro/` e os `.meta`. O merge, por *squash*, é do programador.

## Outras armadilhas do projeto

- **Arte da interface em SVG.** Os SVGs são importados como Textured Sprite e desenhados com `Image` comum. Cada textura tem exatamente o tamanho do elemento em 1080p, porque o importador não gera mipmaps e uma textura maior volta a serrilhar. Detalhes no [README do Dialogue](../../Assets/Scripts/Dialogue/README.md).
- **PNG novo vem como `Sprite Mode: Multiple`.** O importador padrão do projeto 2D importa um PNG como `Sprite (2D and UI)` em modo `Multiple`, e sem fatias o Unity não gera sprite nenhum (`LoadAssetAtPath<Sprite>` devolve `null`). Para uma imagem inteira, ajuste `TextureImporter.spriteImportMode = SpriteImportMode.Single` e chame `SaveAndReimport()` antes de referenciá-la (aconteceu nos retratos da #10).
- **O tamanho de um sprite na tela vem do tamanho em unidades, não do `sprite.rect`.** O `Max Size` do importador reduz a textura: `sprite.rect` passa a ser em pixels da textura reduzida (duas artes mais altas que 2048, de alturas diferentes, ficam as duas com 2048), mas o tamanho em unidades (`sprite.bounds`) não muda. Dimensione um `Image` com `SetNativeSize()` e mantenha **Pixels Per Unit 100** em toda arte de personagem; calcular o tamanho por `sprite.rect` iguala personagens de alturas diferentes (aconteceu na #48).
- **`Image` sem sprite desenha um quadrado branco.** Um lugar vazio se desliga pelo `gameObject`, nunca limpando o `sprite`.
- **Esconder um painel com muito texto: desligue o `Canvas`, não o objeto.** Religar um objeto faz cada `TMP_Text` filho refazer o texto, e o custo cresce com as entradas (~100 ms para 200 falas no histórico, #25). Um `Canvas` aninhado (com `GraphicRaycaster`) com o objeto sempre ativo esconde, deixa de desenhar e de receber clique, e reabre em poucos ms. Ao medir, lembre que o `eval` só enxerga membros públicos (um `internal` dá erro de compilação no `eval`) e que `Time.unscaledDeltaTime` lido em outro `eval` não é o quadro da abertura: instale um registrador em `Application.onBeforeRender` e leia os quadros depois.
- **Balão com largura máxima em layout group (#55).** Para um balão do tamanho do texto, limitado por uma largura máxima, não use `ContentSizeFitter` dentro de layout group (o Unity desaconselha, e ele não tem máximo). A linha é um `HorizontalLayoutGroup` (`childControlWidth/Height` ligados, `childForceExpand*` desligados) com o balão dentro: `Image` fatiada, `VerticalLayoutGroup` com `childForceExpandWidth` **desligado** e um `LayoutElement` com `flexibleWidth = 0`. O código mede o texto com `GetPreferredValues(texto, máximo - padding, 0)` e grava o resultado no `preferredWidth`. Com `childForceExpandWidth` ligado, o balão relata `flexibleWidth = 1`, o `HorizontalLayoutGroup` o estica até a linha toda e o `preferredWidth` é ignorado (medido: 1420 px em vez de 936).
- **Sprite fatiado em 9 partes precisa ser branco puro (#55).** A `Image` multiplica o sprite pela cor: um sprite que não é branco escurece toda cor aplicada (o `Background.psd` embutido do Unity escureceu a Luna de `#D9A6FF` para uns `#B98ADB`). Para um SVG fatiado, importe como Textured Sprite e grave a borda em `m_SpriteData.SpriteBorder` do `SVGImporter` por `SerializedObject` (os nomes têm o prefixo `m_`: `m_SvgType`, `m_TextureSize`, `m_TextureWidth`, `m_TextureHeight`, `m_SampleCount`), depois `SaveAndReimport()`, e confira `sprite.border`.
- **O quadro de uma medição pelo CLI traz ~20 ms de ferramenta (#55).** Um `eval` que só liga um `Canvas` com uma linha ativa mede ~20,5 ms no quadro em que roda, e uma rolagem sem custo próprio mede ~21 ms. Ao medir "o pior quadro" de uma reabertura, subtraia esse piso (o histórico de 200 balões mede ~24 a 25 ms, ou seja, ~4 ms próprios) e compare com o piso medido, não com zero. No `onBeforeRender`, `Time.unscaledDeltaTime` é a duração do quadro **anterior**: o quadro em que a chamada rodou aparece no registro seguinte. `TMPro_EventManager.TEXT_CHANGED_EVENT` é um `FastAction<Object>`.
- **Contraste com alfa se mede nos pixels, em espaço linear (#61).** O projeto está em `ColorSpace.Linear` e o Unity mistura o alfa nele: a conta de contraste de um balão translúcido feita com `Color.Lerp` (em gama) deu 6,36:1 e a captura media 3,94:1. Calcule em espaço linear e confira em uma captura (`capture_game_view --source screen`), lendo os pixels com `System.Drawing.Bitmap` no PowerShell. Para o pior caso (cena branca atrás de um painel translúcido), ponha uma `Image` branca de tela cheia logo abaixo do painel em Play Mode, por `eval_file`.
- **Nitidez se julga em Full HD.** No Game view, use 1920×1080, não "16:9 Aspect".
- **Ids de save.** `ItemDataSO.Id` e o `persistentId` dos coletáveis são chaves do save. Um prefab duplicado copia o `persistentId`; use o menu de contexto **Regenerate Persistent Id** no componente. O `CollectableIdsTests` reprova id vazio ou repetido em qualquer cena.
- **Duplicar um objeto como o Ctrl+D, por CLI (#14).** `UnityEditor.Selection.activeGameObject = go; UnityEditor.Unsupported.DuplicateGameObjectsUsingPasteboard();` cria a cópia (`Item_Chave (1)`) e **o `OnValidate` roda**: o aviso de id repetido aparece na hora. Para descartar sem salvar, `DestroyImmediate` na cópia e `save_scene`; o `.unity` volta idêntico ao do git (`EditorSceneManager.ClearSceneDirtiness` não existe nesta versão, e reabrir a cena suja abre um diálogo).
- **O Test Runner restaura as cenas abertas.** Uma cena que o `ProjectComponents` fechou por engano volta depois de `run_tests`, então a suíte sozinha não mostra esse defeito. Para verificar o auxiliar, chame-o por `eval` (`ProjetoVN.Tests.EditMode.ProjectComponents.ReadAll<UnityEngine.Transform, int>(t => 1)`) com uma cena descarregada na Hierarchy (`OpenScene(..., Additive)` seguido de `CloseScene(cena, false)`) e confira `SceneManager.sceneCount`.
- **Rastreamento.** Para ver o fluxo de mensagens e as trocas de estado, acrescente `VN_TRACE_MESSAGES` em Project Settings → Player → Scripting Define Symbols. Isso altera `ProjectSettings`, então não vai para o commit.
