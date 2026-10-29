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

### Salvar e carregar por `eval` (#16)

O arquivo do save é `Application.persistentDataPath/savegame.json` (no Windows, `%USERPROFILE%/AppData/LocalLow/NoxStudios/ProjetoVN`). **Toda troca de sala salva**, então a suíte PlayMode e qualquer verificação em Play **sobrescrevem o save do Editor**: copie o arquivo antes, se quiser guardá-lo, e devolva depois.

- **O `Load()` é uma troca de sala:** devolve `true` na hora e leva vários frames. Dispare num `eval` e observe em outros (como em "Trocar de sala por `eval`"); no mesmo `eval` do `Load()` só o `PlayerInputGate.IsEnabled == false` já vale. O fim é o gate voltar a `true`.
- **Ler o arquivo:** `System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "savegame.json"))`. A verificação escreve o arquivo por `eval` ou `eval_file`, o que é ferramenta de teste, não código do jogo.
- **Save corrompido:** grave `{garbage` (ou um arquivo vazio) no caminho acima e chame `Load()`: `false`, o aviso "O save está corrompido" e nenhuma exceção. Sem o `GameStateJson`, o `JsonUtility` lançava `ArgumentException` (`JSON parse error: Missing a name for object member` para `{garbage`; `JSON must represent an object type` para `null` e `[1,2]`; `""` devolvia `null`).
- **Sala inexistente:** grave um JSON válido com `"currentScene": "Sala Que Nao Existe"` e um item em `ownedItemIds`. `Load()` devolve `false`, com o erro do `RoomNavigator` e o aviso do `GameSaveManager`, e o inventário **não** muda (prova que o `Load()` é tudo ou nada).
- **Save recusado em conversa:** `DialogueManager.Instance.StartDialogue("peguei_chave")`, espere 1 s e chame `Save()` (`false`, "Save recusado: há uma conversa aberta") e `Load()` (`false`, com o aviso do `RoomNavigator`). Encerre a conversa com `AdvanceDialogue()`, um `eval` por vez.
- **Um `eval` com nome de variável de história (`$…`)** vai em `eval_file` ou entre aspas simples.

### Testar o menu por `eval` (#17)

Copie o `savegame.json` para fora do repositório antes (a verificação o sobrescreve) e devolva no fim. Espere ~10 s depois de cada `editor_play` antes do primeiro `eval`; confira que o jogo avança (`Time.frameCount` duas vezes).

- **Os botões do título** são `Button` + `MenuButtonUI`, achados por caminho: `Canvas/Title Window/Left Panel/Midle/Button Icon` = NOVO JOGO, `Button Icon (1)` = CONTINUAR, `(2)` = CONFIGURAÇÕES, `(3)` = SAIR. A `Confirm New Game Window` repete os nomes (`Button Icon` = NOVO JOGO, `Button Icon (1)` = VOLTAR), então **use o caminho inteiro**; `GameObject.Find` só acha objeto ativo (a confirmação, inativa, só aparece depois do clique em NOVO JOGO com save).
- **Clicar:** `GameObject.Find("<caminho>").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();` (o CLI não simula mouse). **Enter:** `UnityEngine.EventSystems.ExecuteEvents.Execute(go, new UnityEngine.EventSystems.BaseEventData(UnityEngine.EventSystems.EventSystem.current), UnityEngine.EventSystems.ExecuteEvents.submitHandler)`; um clique só, sem o aviso "já há uma troca de sala em curso" no console, prova que o botão tem um caminho de clique único.
- **A interface de jogo escondida:** `GameObject.Find("Canvas_Game").GetComponent<UnityEngine.Canvas>().enabled` é `false` no menu e `true` na sala. Para provar que ela não recebe clique, um `RaycastAll` do `EventSystem` no centro do `InventoryPanel` (`GetWorldCorners` no canvas Overlay já dá pixels de tela): no menu não acha o `InventoryPanel`, na sala acha.
- **O Continuar:** `Button.interactable` e a cor do `Button Label` (`disabledTextColor` quando desligado). **Save corrompido:** grave `{garbage` no `savegame.json` e dê Play: Continuar desligado, Novo Jogo direto. **Sair:** `EditorApplication.isPlaying` fica `false`.
- **O Novo Jogo e o Continuar são trocas de sala** (vários frames): no mesmo `eval` só o `PlayerInputGate.IsEnabled == false` vale; observe em outro `eval`.

### Ligar um `UnityEvent` por script de Editor (#17)

`UnityEventTools.AddVoidPersistentListener(button.onClick, componente.Metodo)` grava a ligação no `Button.onClick`, e `AddObjectPersistentListener<UIWindow>(evento, manager.OpenWindow, janela)` a do `OpenWindow` com argumento de objeto. Para um evento `[SerializeField] private UnityEvent<bool>` de um componente, pegue a instância por reflexão **do Editor** (`GetField(..., NonPublic | Instance).GetValue(componente)`), chame `UnityEventTools.AddPersistentListener(evento, alvo.MetodoBool)` (ligação **dinâmica**, `m_Mode: 0`: o `bool` vem de quem invoca) e termine com `EditorUtility.SetDirty` e `unity command save_scene`. Confira no `.unity` por `grep` (`m_MethodName`, `m_Mode`). Um `eval_file` não aceita `using` no topo: use os nomes completos (`UnityEditor.Events.UnityEventTools`).

### Testar a pausa por `eval` (#18)

Copie o `savegame.json` para fora do repositório antes (a verificação e a suíte o sobrescrevem) e devolva no fim. Espere ~10 s depois do `editor_play` e confira que o jogo avança. **O `pm`** das receitas abaixo é `UnityEngine.Object.FindFirstObjectByType<ProjetoVN.GameFlow.Menu.PauseMenu>(UnityEngine.FindObjectsInactive.Include)`; um `eval_file` (arquivo `.cs`) é mais cômodo que o `eval` para o que passa de uma linha.

- **Pausar e retomar:** `pm.TogglePause()` (o Esc) e `pm.Resume()`. A abertura é **síncrona**: no mesmo `eval` já valem a janela ativa (`pm.transform.Find("Pause Window").gameObject.activeSelf`), `PlayerInputGate.IsEnabled == false`, `Time.timeScale == 0`, `DialogueManager.Instance.IsPaused` e a seleção (`EventSystem.current.currentSelectedGameObject`, o `Button Continue`). Os botões se acham por caminho a partir do `pm`: `Pause Window/Panel/Button Continue|Button Save|Button Return` e `Confirm Return Window/Panel/Button Return|Button Back`; **clicar** é `.GetComponent<UnityEngine.UI.Button>().onClick.Invoke()` (o CLI não simula mouse).
- **O Esc de verdade chega mesmo sem foco** (medido na #18, com o Editor dirigido pelo CLI): `UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));` em um `eval` e, **em outro**, `new KeyboardState()` para soltar. Uma aperta e solta abre; outra, fecha. Deixe um `sleep` de um segundo antes de ler o estado.
- **Medir o texto parado:** a velocidade padrão (40 caracteres por segundo) termina `peguei_chave` em pouco mais de 1 s, antes de dar tempo de pausar. Em Play Mode, `SerializedObject` no `DialogueUIController`, campo `charactersPerSecond`, valor 5, `ApplyModifiedProperties()` (volta ao do prefab no Play seguinte) e leia `maxVisibleCharacters` do `dialogueText` (também por `SerializedObject`): com a pausa aberta o valor **não muda** em 2 s, e ao retomar segue de onde parou (medido: 16 de 44 pausado, 22 de 44 logo depois de retomar).
- **O fim da conversa por `StopDialogue()` é síncrono:** com uma conversa aberta, `DialogueManager.Instance.StopDialogue(); return DialogueManager.Instance.IsDialogueActive();` **no mesmo `eval`** devolve `false`. O `GoToMainMenu` depende disso (recusa fora da exploração).
- **Voltar ao menu:** `pm.TogglePause()`, o botão `Button Return`, o `Button Return` da confirmação (ou `pm.ReturnToMainMenu()` direto). É uma troca de sala: observe em outro `eval` (cena ativa `Menu`, `Canvas_Game` com `Canvas.enabled == false`, `timeScale == 1`, janelas inativas). **Prove que não salvou** lendo o `currentScene` do `savegame.json` (`System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "savegame.json"))`) antes e depois: continua a sala. No menu, `pm.TogglePause()` não abre nada e não loga.
- **O clique no fundo da pausa:** um `RaycastAll` do `EventSystem` em um ponto **dentro da tela** (`Screen.width` × `Screen.height`; no Editor sem a Game view maximizada é uma janela pequena, medida 766×431 contra os 1280×720 da captura) acha primeiro a `Pause Window` e depois o `InventoryPanel`. Um ponto fora da tela não acha a pausa: o centro do `InventoryPanel`, por exemplo, pode estar fora dela.
- **Sair do Play com a pausa aberta** não deixa o `timeScale` zerado: depois do `editor_stop` o valor é 1 e o Play seguinte também começa em 1, e `git status ProjectSettings` fica vazio (medido na #18). Mesmo assim, **não chame `AssetDatabase.SaveAssets()` com o jogo pausado**.
- **A tela de verificação:** `capture_game_view --source screen` mostra a pausa por cima de tudo (o `Canvas_Pause` tem ordem 50, acima do `Canvas_Debug`). Compare um pixel com e sem pausa para provar o escurecimento (medido: ~64% do brilho).

### Montar um canvas novo em um prefab por `eval_file` (#18)

Foi assim que o `Canvas_Pause` entrou no `GameUI.prefab`: `PrefabUtility.LoadPrefabContents`, criar os objetos (`new GameObject(nome, typeof(RectTransform))`, `AddComponent`), ligar os `UnityEvent` (ver a receita acima) e `PrefabUtility.SaveAsPrefabAsset` + `UnloadPrefabContents`, tudo em `try`/`finally`. Pontos que custaram tempo:

- **O `SaveAsPrefabAsset` reordena o YAML inteiro** e o `git diff` mostra milhares de linhas, mas todos os objetos antigos continuam com o mesmo `fileID` e o conteúdo igual, exceto a lista de filhos da raiz. Confira comparando os blocos `--- !u!<tipo> &<fileID>` do arquivo antigo (`git show HEAD:<arquivo>`) com os do novo; confira também que o `Managers.prefab`, que aninha o `GameUI.prefab`, **não** mudou (`git status`).
- **Texto criado por script nasce sem quebra de linha** (`textWrappingMode` = NoWrap): uma frase longa passa da borda do painel. Ponha `TMPro.TextWrappingModes.Normal`.
- **`OnValidate` roda no `AddComponent`, com os `UnityEvent` ainda nulos:** um aviso de autoria que chama `GetPersistentEventCount()` precisa tratar `null` (o `PauseMenu` trata). Sem isso o `eval_file` termina `ok` e deixa uma `NullReferenceException` no console.
- **A `InputActionReference` de uma ação nova** vem de `AssetDatabase.LoadAllAssetsAtPath("Assets/InputSystem_Actions.inputactions")`, filtrando por nome (`UI/Pause`), depois de um `Refresh`. A lista traz **dois conjuntos** de referências com os mesmos nomes (medido: 12 + 12, com `fileID` diferentes); o primeiro conjunto é o que os prefabs já usam (o `advanceDialogueAction` do `Managers.prefab` aponta para o primeiro `UI/AdvanceDialogue`), então pegue a primeira ocorrência. Confira o `fileID` gravado no `.prefab`.
- **O `Menu.unity` pode ser a única cena aberta no Editor** (o Play a partir dele). Um script que a abre de forma aditiva e a fecha para ler sprites não pode fechá-la: confira `SceneManager.GetSceneByPath(...).isLoaded` antes (fechar a última cena só gera o aviso "Unloading the last loaded scene is not supported").
- **O `Time.timeScale` no `eval` em Edit Mode vale 1**; quem o zera é o `PauseState`, só em Play.

### A lista de cenas do build só vai para o disco com `SaveAssets` (#17)

`EditorBuildSettings.scenes = ...` por `eval` muda a lista em memória, mas o `ProjectSettings/EditorBuildSettings.asset` só é regravado depois de `AssetDatabase.SaveAssets()`. Sem o `QualitySettings` alterado, essa chamada não toca o `ProjectSettings.asset` (D-31, #80); confira o `git diff ProjectSettings/`: só a lista de cenas.

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

#### O limite de FPS sem foco (#81)

O `UnfocusedFrameRateLimiter` (`Managers.prefab`) limita o jogo a 30 FPS e desliga o VSync quando a janela perde o foco (D-31). No Editor, **"foco" é o da Game view, não o da janela** (documentação do `OnApplicationFocus`).

- **Sob o CLI o limite não age.** Medido com o Chrome em primeiro plano e o Play Mode rodando: `Application.isFocused = True`, e um `Application.focusChanged` assinado por `eval` não recebeu evento algum. Por isso uma verificação dirigida pelo CLI continua nos FPS que ela mesma pediu (a suíte PlayMode da #80 inclusive). O limite entra quando alguém clica em outra aba do Editor (Inspector, Console, Scene) durante o Play.
- **Não clique no Editor durante a suíte PlayMode.** A perda de foco no meio de uma fixture a 10 FPS guarda `(0, 10)` e aplica `(0, 30)`, e o `FrameRate_IsTheOneThisFixtureAsked` reprova. Espere o `test_status` dizer `completed`.
- **Simular a perda e o ganho de foco:** `UnityEngine.GameObject.Find("Managers").SendMessage("OnApplicationFocus", false);` (e `true`). Hoje só o `UnfocusedFrameRateLimiter` tem `OnApplicationFocus` (`grep` em `Assets/Scripts`); se outro componente do `Managers` ganhar um, mire só o limiter. Em seguida leia `QualitySettings.vSyncCount` e `Application.targetFrameRate` (esperado `0 30` sem foco e os valores de antes com o foco).
- **Medir o FPS:** um `eval` guarda `Time.frameCount` e `Time.realtimeSinceStartup` com `AppDomain.CurrentDomain.SetData`; ~5 s depois, um segundo `eval` calcula quadros por segundo. Nunca dentro de um `eval` só (ele roda em um quadro). Sem o limite, o Editor sem foco faz ~360 a 470 FPS; limitado, 30,0. Uma janela de medição que pega a transição entre dois estados dá um valor intermediário (visto 169 uma vez, não reproduzido): repita a medição antes de concluir.
- **Ajustar o campo em Play:** `SerializedObject` no `UnfocusedFrameRateLimiter`, campo `unfocusedFrameRate`, e `ApplyModifiedProperties()`. É lido na hora da perda de foco, e volta ao do prefab no Play seguinte.
- **Não chame `AssetDatabase.SaveAssets()` com o `vSyncCount` alterado** (como na #80): `QualitySettings` é um asset de `ProjectSettings/`. No Editor, o `vSyncCount` não manda (só o `targetFrameRate` limita a Game view), então a verificação confere a devolução do par, não o efeito do VSync.
- **O que o CLI não faz:** trocar o foco de janela. O efeito real (alt-tab, minimizar, segundo monitor, uso de GPU no Gerenciador de Tarefas) fica para o programador, numa build de Windows.

### Testar um campo de texto (`TMP_InputField`) por `eval` (#26)

O CLI não digita nem clica, mas dá para exercitar o campo da folha do nome (`ProtagonistNameSheet`) de quatro jeitos. Ache o campo por `SerializedObject` no `DialogueUIController` (`protagonistNameSheet`) e no componente (`nameField`, `signButton`); com `$` no nome da variável, use `eval_file`.

- **`field.text = "..."` não passa pelo `onValidateInput`.** Só o que o jogador digita ou cola é filtrado. Para testar o filtro, chame `field.onValidateInput(textoAtual, posição, caractere)` direto: devolve o caractere ou `'\0'` (recusado).
- **`field.ProcessEvent(new UnityEngine.Event { type = UnityEngine.EventType.KeyDown, character = 'W' })` percorre o caminho real do `Append`** (filtro, posição, seleção) e **funciona**: com o campo focado e o cursor no fim (`caretPosition`, `selectionAnchorPosition` e `selectionFocusPosition`), um caractere permitido entra e um recusado não. Para o Escape, use `keyCode = UnityEngine.KeyCode.Escape` (e confira que o texto não voltou ao de antes).
- **Enter e botão:** `field.onSubmit.Invoke(field.text)` e `button.onClick.Invoke()`.
- **O Esc de verdade (`QueueStateEvent`, receita da pausa, #18) não dispara a ação `UI/Pause` se a Game view não tem o foco.** O estado da tecla chega (`Keyboard.current.escapeKey.isPressed` fica verdadeiro), mas o Input System deste projeto está em `editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus` e `Application.isFocused` é falso sob o CLI. Foi o que se viu na #26, com e sem a folha aberta; a receita da #18 registra que o Esc chegava, então a causa provável é o foco da Game view naquele momento (não isolada). Para pausar nesses casos, chame `PauseMenu.TogglePause()`; o `editor_focus` traz a janela para a frente, o que rouba o foco de quem está usando o computador, então só o use com o programador de acordo.

### Testar a música por `eval` (#19)

Copie o `savegame.json` para fora do repositório antes (a verificação e a suíte o sobrescrevem) e devolva no fim. Espere ~10 s depois do `editor_play` e confira que o jogo avança. O CLI não ouve: a verificação lê `isPlaying`, `volume` e `time` das `AudioSource`; a suavidade da troca e o loop sem estalo ficam para o programador, com fone.

- **O estado de uma vez:** `AudioManager.Instance.CurrentMusic` e `AudioManager.Instance.transform.Find("Music").GetComponentsInChildren<AudioSource>(true)` (as duas fontes do filho `Music` do `Managers.prefab`; um `GetComponentsInChildren` direto no manager devolve as cinco, também as do ambiente e dos efeitos, desde a #20). Leia `clip`, `isPlaying`, `volume`, `time` e `outputAudioMixerGroup.name` (`Music`). Em um `eval_file` (arquivo `.cs`) cabe tudo numa linha de saída.
- **Não leia `AudioSource.time` de uma fonte sem clipe:** o Unity loga `Attempting to get time on an audio source that has a resource that is not a clip` e polui a conferência de "console sem aviso". Confira `clip != null` antes.
- **Ver a troca em câmera lenta:** em Play Mode, `SerializedObject` no `AudioManager`, campo `crossfadeSeconds` (4 dá 3 ou 4 leituras no meio; 12 dá tempo de abrir a pausa), e `ApplyModifiedPropertiesWithoutUndo()`. O valor volta ao do prefab no Play seguinte e o `Managers.prefab` não fica alterado. Cada leitura pelo CLI leva ~1,5 a 2 s; no meio da troca os dois volumes andam em sentidos opostos com `entrada² + saída² ≈ 1` (medido: 0,68 e 0,74).
- **A música reiniciou ou continuou?** `AudioSource.time` dá a volta no loop (8 s nas faixas de teste), então um "maior que antes" mente. Leia `Time.realtimeSinceStartup` junto com o `time` nas duas leituras e confira `(time_antes + tempo_real_decorrido) mod duração ≈ time_depois` (medido ao recarregar a sala: diferença de 0,01 s sobre 8,7 s).
- **`AudioSource.time` anda em degraus do tamanho do bloco de áudio** (medido na revisão da #19: 0,0213 s, 1024 amostras a 48 000 Hz). Com o Editor a centenas de quadros por segundo, um ou dois quadros leem o mesmo valor, então "esperei um quadro e o `time` cresceu" é teste instável. Os testes do `MusicTests` leem o `time` de partida só longe das pontas do loop (`WaitUntilTimeIsAwayFromTheLoopEnds`: nem perto do começo, onde um reinício passaria por avanço, nem perto do fim, onde a volta do loop faria o `time` diminuir) e esperam em tempo real mais que um bloco (`WaitForSecondsRealtime`) antes de comparar.
- **Trocar de sala, voltar ao menu e carregar** são as receitas de "Trocar de sala por `eval`", "Testar a pausa por `eval`" e "Testar o menu por `eval`". Na volta ao menu, `CurrentMusic` fica nulo na chegada, a fonte que tocava cai a zero e para; no menu não há `RoomMusic`.
- **O `sceneLoaded` e o `AudioListener`** (medido no levantamento, troca `[Teste] Mecanicas` → `[Teste] Sala B`): o `sceneUnloaded` da sala antiga e o `sceneLoaded` da nova saem no mesmo quadro, o `sceneCount` é 1 no `sceneLoaded`, a cena recebida já tem o `AudioListener` dela e todo quadro desenhado teve exatamente um. Por isso o `Managers.prefab` não leva `AudioListener`.
- **Importar uma faixa por `eval_file`:** `AudioImporter.preloadAudioData` é obsoleto; o campo está em `defaultSampleSettings` (`SampleSettings.preloadAudioData`). Atribua o struct inteiro de volta (`importer.defaultSampleSettings = settings`) e chame `SaveAndReimport()`.
- **Compilar foi lento (medido na #19, nesta máquina):** um `recompile` depois de mexer nos scripts do `Audio` e dos testes levou ~4 min até o `recompile_status` dizer `completed`, e a `run_tests` seguinte, ~5 s. Não adianta repetir o comando; escreva tudo o que precisa compilar antes de pedir (a TDD em ciclos pequenos custa caro aqui), e rode a compilação em segundo plano.

### Testar os sons por `eval` (#20)

Copie o `savegame.json` para fora do repositório antes (a verificação e a suíte o sobrescrevem) e devolva no fim. Espere ~10 s depois do `editor_play` e confira que o jogo avança. O CLI não ouve: a verificação lê o estado do `AudioManager` e das `AudioSource`; o timbre, a suavidade e o loop sem estalo ficam para o programador, com fone.

- **`isPlaying` não prova que um `PlayOneShot` tocou (medido na #20, Play Mode no `Menu`):** um clipe de 2 s com `PlayOneShot` dá `isPlaying = True` no mesmo quadro e `False` cerca de 1 s depois (`time = 0`), com o clipe ainda no meio. Por isso o `AudioManager` expõe **`LastEffect`** e **`EffectCount`**: leia `EffectCount` antes e depois da ação, e `LastEffect.name`. Vale para hover, clique, avanço de fala, coleta, porta e `<<tocar_efeito>>`.
- **O ambiente:** `CurrentAmbience` e as duas fontes do filho `Ambience` (`am.transform.Find("Ambience").GetComponentsInChildren<AudioSource>(true)`): `clip`, `isPlaying` e `volume`. Entrando, o volume sobe a 1; `StopAmbience` leva a 0 e para a fonte em ~1 s (cada leitura pelo CLI leva ~1,5 s, então a saída costuma já ter terminado na primeira). **Não leia `AudioSource.time` de uma fonte sem clipe** (loga e suja o console).
- **Hover e clique:** `ExecuteEvents.Execute(botao, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler)` e `button.onClick.Invoke()`; valem com `Time.timeScale == 0`. Um botão desabilitado (`interactable = false`) não soa ao passar o mouse.
- **Os comandos por chamada direta:** `ProjetoVN.GameFlow.SoundScriptActions.PlayAmbience("onibus_estrada")`, `StopAmbience()`, `PlayMusic("teste_musica_b")`, `PlayEffect("freada_batida")`. Um id inexistente deixa um erro no console (o esperado da verificação) com "nó '?'" fora de conversa.
- **Percorrer o prólogo:** `DialogueManager.Instance.StartDialogue("onibus_viagem")` (o Novo Jogo leva ao `[Fatia] Onibus`, onde ele abre por um clique na poltrona, `onibus_poltrona`; a abertura automática é a #71) e, um `eval` por fala, `AdvanceDialogue()`, lendo `CurrentAmbience` e `LastEffect`: o ambiente entra na primeira fala, some na lembrança (primeira fala do Pai), volta em "Espero ter feito a escolha certa, mãe..." e some em "Que porra é essa?!", com `LastEffect.name == "freada_batida"` lido **antes** do último avanço (o avanço soa depois e sobrescreve o `LastEffect`).
- **Sair do Play** deixa `git status` limpo (sem `ProjectSettings/`) se ninguém atribuiu `runInBackground`.

### Montar cenas por `eval_file` e percorrer a fatia vertical (#21)

As salas `[Fatia] Onibus` e `[Fatia] Posto` foram montadas por scripts de Editor (`eval_file`), com o mesmo resultado serializado que o Inspector. Pontos que custaram tempo:

- **`EditorSceneManager.NewScene` (modo `Single`) descarrega os assets que nenhuma cena usa.** Um `ScriptableObject` ou sprite carregado **antes** dele vira "nulo falso": `SerializedProperty.objectReferenceValue = asset` grava `{fileID: 0}` e nenhum erro aparece (aconteceu com `Moeda.asset` nas duas cenas). Carregue o asset **depois** de criar a cena, e depois de salvar confira por `grep` (`itemData`, `requiredItem`, `clip`, `music`).
- **Instancie o prefab com `PrefabUtility.InstantiatePrefab`**, nunca com `Instantiate`, para o objeto continuar sendo uma instância do `Interactable`. Os campos privados se escrevem por `SerializedObject` e as ligações de `UnityEvent` pela receita de [Ligar um `UnityEvent` por script de Editor](#ligar-um-unityevent-por-script-de-editor-17); aqui os eventos da `InteractableItem` (`OnInteract`) e do `LockedActionBehaviour` (`OnLocked`, `OnUnlocked`, `OnOpened`, `OnAlreadyUnlocked`) são campos **públicos** e se acessam direto no `eval_file`; a reflexão do Editor fica para o campo `[SerializeField] private`, como na receita da #17.
- **Vários componentes do mesmo tipo no mesmo objeto** (os três `InteractableDialogueTrigger` da `Maquina_Cafe`): ligue cada evento à instância certa (a variável que guarda o `AddComponent`) e confira no `.unity` que os `m_Target` dos eventos são diferentes.
- **Salvar:** `EditorSceneManager.SaveScene(cena, caminho)` gravou as cenas novas e `SaveScene(cena)` as alterações do `Menu.unity`; confira o retorno `True` e o `git status`.
- **Os nomes de tipo no `eval`:** `PlayerInputGate` está em `ProjetoVN.PointNClick`, `DialogueManager` em `ProjetoVN.Dialogue.Logic`, `DialogueUIController` e `DialogueHistoryView` em `ProjetoVN.Dialogue.UI`, `StoryState` em `ProjetoVN.Core.State` (`Bools`, `Numbers` e `Texts` são só de leitura), `InventoryManager` em `ProjetoVN.Inventory`.

**Percorrer a fatia em Play Mode** (a receita da verificação da #21; copie o `savegame.json` para fora do repositório antes e devolva no fim):

- **Clicar num objeto:** `GameObject.Find("Porta_Onibus").GetComponent<ProjetoVN.PointNClick.InteractableItem>().OnClick()`. Um objeto desativado (a moeda depois de coletada) não é achado por `Find`: isso é o que se confere.
- **Texto e quem fala:** leia `dialogueText` e `speakerNameText` do `DialogueUIController` por `SerializedObject` (`TMP_Text`). Com `charactersPerSecond` em 1000 (só em Play) cada fala pede **um** `AdvanceDialogue()` por `eval`, e a conversa terminou quando `IsDialogueActive()` é falso.
- **O prólogo inteiro:** de `Poltrona` até as opções do motorista são 32 `AdvanceDialogue()` (um `eval` cada, ~1 s de espera); aí `MakeChoice(0)` vai direto à folha do nome (`<<assinar_lista>>`), que se assina com a receita de [Testar um campo de texto](#testar-um-campo-de-texto-tmp_inputfield-por-eval-26).
- **As opções:** os botões estão em `choiceButtonObjects` e os textos em `choiceTexts` (`SerializedObject` do `DialogueUIController`); uma opção condicional que não vale aparece com `Button.interactable == false`.
- **O histórico:** `DialogueUIController.OpenHistory()` e leia os `TMP_Text` ativos do `DialogueHistoryView` (cada balão traz o nome em negrito e a fala; a posição X distingue esquerda e direita). Feche pelo `Button` do próprio `DialogueHistoryView`.
- **A tela de personagens:** os `Image` ativos do `CharacterStage` mostram quem está em cada lado (`LeftSlot:luna_neutra`, `RightSlot:protagonista_neutra`).
- **Voltar ao menu e começar de novo:** `PauseMenu.ReturnToMainMenu()` (vale mesmo com a pausa fechada) e `MainMenu.StartNewGame()` / `ContinueGame()`; cada um é uma troca de sala (~6 s). O save só muda quando o jogador **chega** a uma sala: o Continuar volta ao estado da última chegada, sem o que aconteceu depois (a conversa com a Luna, por exemplo).
- **Console:** `clear_console` depois de entrar no Play e leia os erros no fim: o esperado é zero erros e zero avisos.
- **`EventSystem` e `InMemoryVariableStorage`:** um e zero, em cada sala.

### A captura de 1280×720 some com linha fina

A Game view do Editor costuma ter 1280×720, abaixo dos 1920×1080 da interface, e a captura (`capture_game_view --source screen`) reduz tudo a 0,67×. Uma linha de 2 px vira uns 1,3 px e **desaparece em algumas posições** (na folha do nome, duas das cinco pautas sumiram). Use 3 px ou mais para linha fina, ou confira em Full HD.

### Contar o que está visível

O painel de inventário reaproveita slots: um slot devolvido é desativado, não destruído. `transform.childCount` não diz quantos itens estão visíveis. Conte os filhos ativos.

### Gerar a build de Windows (#22)

O roteiro de [ponta-a-ponta.md](ponta-a-ponta.md) roda numa build de Windows 64. Gere-a pelo **Editor aberto** (o `unity build` de linha de comando abre um segundo Editor em batch e não roda com o projeto aberto), fora do Play Mode e com a cena salva (a build usa as cenas do disco):

```bash
unity command build --target StandaloneWindows64 --outputPath "Builds/Windows/ProjetoVN.exe" --confirm true
```

```bash
unity command build_status
```

Repita o segundo até `completed` e confira `result: Succeeded` e `totalErrors: 0` (medido: 13 s com o cache quente, 73 s do zero; ~165 MB). O único aviso do relatório é do pacote Pipeline: `No RuntimePipelineConfig asset found… Pipeline will be disabled in Player builds`. Por isso **o CLI não alcança a build**: nada de `eval` dentro do jogo.

- **É uma build Release**, mesmo com `EditorUserBuildSettings.development = True` neste Editor: sem `--options`, o comando não liga `Development` (o `boot.config` fica sem `player-connection` e sem `profiler-enable`). O que se verifica é o que o jogador recebe.
- **Sem `--scenes`** a build usa a lista do `EditorBuildSettings` (o menu, as salas `[Fatia]` e as cenas `[Teste]`). Não passe uma lista no comando: ela viraria uma segunda lista de salas, fora dos testes (`MainMenuTests` e `RoomLinksTests` guardam só o `EditorBuildSettings`).
- **A build regrava quatro arquivos.** Depois de **cada** build, reverta e confira que o `git status` está limpo:

  ```bash
  git checkout -- ProjectSettings/ProjectSettings.asset ProjectSettings/UnityConnectSettings.asset Assets/Settings/UniversalRP.asset Assets/UniversalRenderPipelineGlobalSettings.asset
  ```

  O que muda neles: o `InputSystem_Actions` entra em `preloadedAssets` e nasce um `m_BuildTargetBatching` de Standalone (`ProjectSettings.asset`), `m_Enabled: 0 → 1` (`UnityConnectSettings.asset`), e os dois assets do URP. Nenhum entra no commit. Se o `companyName` ou outra mudança *desejada* estiver no `ProjectSettings.asset`, commite-a **antes** da build, para a reversão devolver o arquivo já com ela.
- **`Builds/` está no `.gitignore`.** A `Builds/Development/` é uma build antiga e fica onde está.
- **A pasta de dados é `%USERPROFILE%\AppData\LocalLow\NoxStudios\ProjetoVN`** (nome da empresa do Player Settings), a mesma do Editor: o `savegame.json` é um só para o Editor, a build e a suíte PlayMode. O `Player.log` fica ali também e **gira a cada abertura**: a sessão anterior vira `Player-prev.log` e a de antes dela se perde. Copie o log antes de abrir a build de novo.
- **Ler os erros do `Player.log`.** Não há prefixo de nível; um `Debug.LogError` aparece como a mensagem seguida da pilha `UnityEngine.Debug:LogError (object)`. O filtro está no passo 16 do roteiro de ponta a ponta. A linha `d3d12: failed to query info queue interface (0x80004002).` aparece em toda execução (é do driver), e a `D3D12PipelineLibrary must be recreated due to invalid disk cache`, só na primeira abertura de uma build nova (por isso o aquecimento).
- **A build abre em tela cheia sem borda.** Para abri-la por terminal sem tomar a tela:

  ```powershell
  Start-Process -FilePath Builds\Windows\ProjetoVN.exe -ArgumentList '-screen-fullscreen','0','-screen-width','960','-screen-height','540' -WindowStyle Minimized
  ```

  **Esses argumentos ficam gravados** em `HKCU\Software\NoxStudios\ProjetoVN` (valores `Screenmanager …`) e valem para as aberturas seguintes, inclusive as sem argumento: a build passa a abrir em janela de 960×540. O passo 0 do roteiro apaga essas preferências (os valores `… Default_h…` ficam).
- **O Esc da pausa funciona na build com foco de verdade.** A ressalva de "foco da Game view" da receita do campo de texto é só do Editor.

#### Jogar a build por script (#22)

Sem o programador e sem a ferramenta de controle do computador, dá para percorrer o roteiro com um script PowerShell que traz a janela do jogo para a frente, tira uma captura dela (`Graphics.CopyFromScreen` sobre a área cliente, depois `Read` no PNG) e envia cliques e teclas pelo `user32.dll`. Abra a build em janela (`-screen-fullscreen 0 -screen-width 1280 -screen-height 720`, ver acima) e chame `SetProcessDPIAware()` antes de medir coordenadas. O script e o resultado ficam fora do repositório. O que custou tempo:

- **O pan por borda só reage a movimento relativo do mouse** (`mouse_event` com `MOUSEEVENTF_MOVE`). `SetCursorPos` sozinho leva o cursor, mas o `ScreenPanController` não desloca o cenário: um laço que alterna `-6` e `+3` px perto da borda (≈ 48 px de cenário por volta de 240 ms) faz o pan andar. Clique funciona com `SetCursorPos` seguido de `mouse_event` de botão.
- **Esc:** `keybd_event` com o código virtual `0x1B` **e** o scancode `0x01`, segurando ~200 ms. Só o código virtual, ou `SendKeys` com `{ESC}`, não abriu a pausa. Para digitar na folha do nome, `SendKeys.SendWait("Nome")` funciona.
- **Os objetos provisórios são quadrados pretos** (72×72 px a 1280×720); achá-los na captura (varrer a linha `y = 360` atrás de pixels `0,0,0`) é mais rápido que adivinhar o pan. Cada quadrado não diz o que é: clique e leia a fala.
- **Uma fala a cada clique no botão de avançar** (`≈ (1024, 566)` a 1280×720), com ~2 s de espera entre cliques; o prólogo inteiro leva ~30 cliques.
- **Fim do pan.** O pan chega ao vazio preto nas duas bordas (#110, #108): ao voltar, desloque o cenário para o lado oposto até a varredura achar um quadrado.
- **O que não se verifica assim:** som e música, o alt-tab real e a sensação do pan. O `Player.log` e o `savegame.json` continuam sendo as provas de estado.

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

`DialogueManager`, o `DialogueRunner` do Yarn Spinner, `StoryStateVariableStorage`, `InventoryManager`, `GameStateController`, `RoomNavigator`, `UnfocusedFrameRateLimiter`, `DialogueInputHandler`, `GameSaveManager`, `ItemScriptActions`, `SoundScriptActions`, `ItemCollectedSound` e `AudioManager` (com as cinco `AudioSource`, nos filhos `Music`, `Ambience` e `Effects`) existem só no `Managers.prefab`. Um deles colocado em uma cena sobrescreve o `Instance` do persistente.

O `UnfocusedFrameRateLimiter` é a exceção ao motivo: ele não tem `Instance`, mas um segundo exemplar (por exemplo, um posto em uma cena) faz o limite **não ser desfeito**. A perda de foco chega aos dois, e o segundo guarda o `(0, 30)` do primeiro como original; se ele também for o último a receber o ganho de foco, o jogo **fica a 30 FPS com o foco de volta**.

O mesmo vale para a interface de jogo, que vem aninhada nesse prefab (`Assets/Prefabs/UI/GameUI.prefab`: `Canvas_Game`, `Canvas_Pause`, `Canvas_Fade` e `EventSystem`):

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
