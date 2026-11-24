# Unity CLI: receitas e armadilhas

O Unity CLI (`unity`) controla o Editor aberto pelo pacote `com.unity.pipeline`, já instalado no projeto na versão **1.0.0** (#189; era a 0.7.0-exp.1). Antes de usar, carregue a skill `unity:unity-cli`. Esta página guarda só o que é específico deste projeto: as receitas do dia a dia e as armadilhas que já custaram tempo.

Regra geral: **com o Editor aberto, altere cenas, prefabs e assets pelo CLI**, e não editando o YAML à mão. As exceções estão em [Armadilhas de serialização](#armadilhas-de-serialização).

**Os comandos têm grupo** (`unity command <grupo> <nome>`): `script eval`, `test run`, `scene list_open`, `editor.playmode start`. Os nomes antigos (`eval`, `run_tests`, `list_open_scenes`, `editor_play`…) respondem `No command named '…' is available`, e os parâmetros são em kebab-case (`--async-tests`, `--output-path`). Quando uma receita antiga, de uma issue ou de um PR, citar um nome sem grupo, procure o novo com `unity command --query <palavra>` e confira os parâmetros com `unity command <grupo> <nome> --help`. Nesta página e nas outras, "por `eval`" quer dizer `script eval`.

**Nem todo comando do manual existe neste Editor.** `unity command --obsolete-command-names` (o mapa oficial dos nomes antigos) recusa neste projeto ("só o Unity Editor lista, com o pacote Unity Pipeline 0.9.0-exp.1 ou posterior"), apesar de o pacote ser o 1.0.0; a lista vale a de `unity command --detail full --format json`. Os comandos de diálogo modal (`editor dialog`, `editor.dialog list`/`answer`/`dismiss`, saída `18`) **não existem** aqui: ver [O agente não vê diálogo modal](#o-agente-não-vê-diálogo-modal-189).

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

Sem `--query`, lista os grupos (`unity command` mostra 38, de `animation` a `test`). `unity command <grupo>` lista os comandos de um grupo, `unity command <grupo> <nome> --help` mostra os parâmetros, e `unity command --detail full --format json` traz tudo (descrição, parâmetros, tipos, obrigatórios) para filtrar por programa.

### Recompilar depois de alterar scripts

```bash
unity command script recompile
```

```bash
unity command script recompile_status
```

Repita o segundo até o resultado ser `completed` ou `up_to_date` (o campo `errors` traz os erros de compilação; medido na #189: sem nada para compilar, os dois dizem `up_to_date`). Erros de compilação aparecem no console:

```bash
unity command console get --level error --tail 20
```

### Rodar os testes

```bash
unity command test run --mode editor --timeout 180
```

Antes de rodar qualquer suíte, confira que nenhuma cena aberta, ativa ou aditiva, está suja: o Test Runner abre um diálogo que trava o Editor e o CLI (ver [Salve a cena antes de rodar os testes](#rodar-a-suíte-playmode), no fim da seção seguinte).

O comando é síncrono e o resultado é **uma linha por teste** (`ProjetoVN.Tests.EditMode.StoryStateTests.… Passed`), sem linha de total: conte as aprovadas com `| grep -c " Passed "` (493 na #189) e procure o resto com `grep -v " Passed "` (só sobram o cabeçalho e uma linha em branco quando tudo passa). Com `--format json` cada linha é um objeto com `fullName`, `status`, `duration`, `message` e `stackTrace`. O exit é `0` quando todos passam; o código de uma falha não foi medido (o CLI documenta `8`, "os testes rodaram e alguns falharam"): confira também as linhas. O número cresce a cada issue; o que importa é não haver falha. Para um subconjunto:

```bash
unity command test run --mode editor --filter StoryStateTests
```

Alguns testes provocam um erro de propósito (o do `MessageBroker` com assinante que lança exceção). A entrada `InvalidOperationException: falha proposital` no console é esperada.

### Rodar a suíte PlayMode

```bash
unity command test run --mode playmode --async-tests true --timeout 300
```

```bash
unity command test status
```

A PlayMode **não roda de forma síncrona** pelo CLI: `test run --mode playmode` sem `--async-tests` é recusado, com exit `2` (`PlayMode tests cannot run synchronously over HTTP: entering play mode triggers a domain reload that drops the request`; medido na #189). A recusa é do próprio CLI e **continua com o domínio sem recarga** (#140), então a forma assíncrona segue obrigatória. O comando volta na hora, sem esperar a execução, e **não imprime nada** (saída vazia, exit `0`): isso é o esperado, não uma suíte vazia. O resultado vem do `test status`: repita-o a cada uns 10 s até a primeira linha dizer `status: completed`, e leia a linha `summary:` (`{"total":58,"passed":58,"failed":0,…}` na #189; o número cresce com as issues). Sem corrida em andamento o `test status` diz `status: no_tests`. Para ver só as falhas, `unity command test status --detail failures` (com tudo aprovado o `results` vem vazio); sem `--detail`, a linha `results:` traz todos os testes, com `status`, `message` e `stackTrace` por teste. `--mode all` roda a EditMode na hora e devolve a PlayMode como `playmode_running`. Medido na #189 (domínio sem recarga, **duas suítes PlayMode seguidas sem recompilar**, o caso da correção UUM-150763 da 0.10.0): 58/58 nas duas, 88 s cada, e 58 nomes distintos no resultado da segunda (nenhum resultado repetido).

Para conferir quais testes cada suíte enxerga, `unity command test list --mode playmode` e `--mode editor` (um asmdef só de Editor faria um teste PlayMode aparecer na EditMode).

O Test Framework liga o `runInBackground` durante a execução e devolve o valor no fim (`PreparePlayModeRunTask.cs`, `RestoreProjectSettingsTask.cs`). Com a D-31 o valor já é `true`, então isso não muda nada: depois da suíte, `git diff ProjectSettings/ProjectSettings.asset` continua vazio.

**Salve a cena antes de rodar os testes.** Com **qualquer cena aberta suja, a ativa ou uma aditiva**, o Test Runner abre o diálogo "Scene(s) Have Been Modified" e bloqueia a thread principal do Editor: `test run` e `eval` expiram sem erro claro (`EDITOR_TIMEOUT`, exit 14). Vale para **as duas suítes**: as duas chamam `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()`, que pergunta sobre todas as cenas modificadas da Hierarchy, não só a ativa (`SaveModifiedSceneTask.cs`, na `TaskList.cs` do `com.unity.test-framework` 1.6.0). Medido na execução da #3, com a cena ativa suja, e na #171, com a cena ativa limpa e só o `[Fatia] Onibus`, aditivo, sujo (suíte EditMode): o diálogo abriu do mesmo jeito.

**Clicar em Cancel não destrava o CLI.** Na #171, depois do Cancel o Editor voltou ao normal (sem diálogo, sem barra de progresso, respondendo a cliques) e `unity status` dizia `ready`, mas todo comando (`script eval --file`, `test status`, `console`, `eval`, `scene list_open`) seguiu expirando até o Unity ser fechado e reaberto com `unity open .`. É o mesmo sintoma de "Gravar uma cena alterada por `script eval --file`", em [Trocar de sala por `eval` (#12)](#trocar-de-sala-por-eval-12). Na #3 não se registrou qual botão foi clicado. **Don't Save** recarrega as cenas ainda sujas e descarta a alteração (`ReloadModifiedScenesTask.cs`; lido no código, não medido).

**Antes de cada `test run`**, rode `unity command scene list_open` e confira o `isDirty` de **cada** cena, seguindo os três casos de [Abrir e salvar cena](#abrir-e-salvar-cena): todas limpas, rode a suíte; suja por alteração sua, grave e confira o `.unity` por `grep`; suja e você não sabe de quem é, **pare** e avise o programador. Uma cena sem título e suja (`path` vazio) cai no último caso, a não ser que a própria sessão a tenha criado e vá gravá-la com um caminho, como em [salas.md](../autoria/salas.md); `editor save_all` e `scene save` **recusam** uma cena suja sem título, sem abrir o "Salvar como" (medido na #189: exit `6`, `Scene '' has never been saved (no path)` e `Cannot save all: 1 dirty scene(s) have never been saved`, e nada é gravado); o código do pacote 1.0.0 tem a recusa (`Commands/Scenes/SceneCommands.cs:126-134` e `:154-163`; a 0.7.0 já a tinha). Quem grava uma cena nova é o `scene create --path <caminho>`.

Depois de gravar, confira o `git status`: na #15, uma sequência de execuções com a gravação da cena antes de cada uma deixou o `[Teste] Mecanicas.unity` modificado, com `RectTransform` de filhos de um layout group do canvas gravados com valores calculados (a causa não foi isolada). Reverta a cena com `git checkout` se isso acontecer; ele não entra no commit.

### Executar C# no Editor

```bash
unity command script eval 'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;'
```

O código é um trecho com instruções terminadas em `;` e um `return` para devolver valor; aceita `using` no topo e função local. O resultado é **o valor puro**, em texto (`4` para `return 2 + 2;`, `"bool=True"` com aspas sob `--format json`), sem o objeto `{output, diagnostics, success, result}` da 0.7.0. A falha se lê pelo exit `6`: um erro de compilação imprime `error: COMPILATION_ERROR: …` com linha e coluna, e uma exceção em tempo de execução `error: RUNTIME_EXCEPTION: …` com a pilha (medido na #189). Um `Debug.Log` não volta na resposta; ele vai ao console (`console get`). Os `using` de `System.Linq`, `System.Threading.Tasks`, `UnityEngine` e `UnityEditor` já vêm importados (`Mathf.Max(…)` e `Enumerable.Range(…)` compilam sem namespace); os tipos do jogo entram pelo nome completo (`ProjetoVN.Core.State.StoryState`) ou por um `using` (ver [O `eval` aceita `using`](#o-eval-aceita-using-e-importa-o-básico-189)).

### Atualizar o banco de assets depois de criar arquivos por fora

```bash
unity command script eval 'UnityEditor.AssetDatabase.Refresh(); return "ok";'
```

É isso que faz o Editor gerar o `.meta` de um arquivo criado direto no disco, como um README novo dentro de `Assets/`.

Depois de criar um **`.yarn`**, o `Refresh` não basta: com o Editor sem foco o arquivo não entra no `Roteiro.yarnproject`. Reimporte também o projeto de roteiro (passo 2 de [Integrar um roteiro novo](#integrar-um-roteiro-novo); causa na [armadilha do Yarn Spinner](#armadilhas-do-yarn-spinner), #34).

### Abrir e salvar cena

```bash
unity command scene open --path "Assets/Scenes/[Teste] Mecanicas.unity"
```

```bash
unity command scene save
```

**Trocar de cena descarta o que não foi salvo (#166).** `unity command scene open --path` e `unity command scene create --path` sem `--additive`, e `EditorSceneManager.NewScene` e `OpenScene` em modo `Single` por `eval`, **fecham todas as cenas abertas**, as aditivas inclusive, e descartam a alteração não salva **sem diálogo e sem erro**: o comando responde com sucesso e o `.unity` fica como estava. Medido no Editor 6000.3.9f1, fora do Play, marcando a cena suja com um objeto novo (`MarkSceneDirty`) e trocando de cena em seguida (as linhas de `open_scene` e `create_scene` são da 0.7.0; na #189, com a 1.0.0, o `scene open` do `Menu` por cima de uma cena sem título e suja repetiu o resultado: exit 0, sem diálogo, só o `Menu` aberto e a cena suja sumiu; o `scene create` não foi remedido):

| Situação antes | Chamada | Resultado |
|---|---|---|
| Cena sem título, suja | `NewScene(EmptyScene, Single)` por `script eval --file` | Voltou em 122 ms, sem diálogo; a cena antiga deixou de existir |
| `[Fatia] Posto` (ativa) e `[Fatia] Onibus` (aditiva), salvas e sujas | `NewScene(EmptyScene, Single)` por `script eval --file` | Sem diálogo; `sceneCount` foi de 2 para 1 e as duas foram fechadas; os dois `.unity` não ganharam a marca, nem o backup de `Temp/__Backupscenes` (o `0.backup` era de meia hora antes) |
| `[Fatia] Posto` salva e suja | `scene open` de outra cena | `success: true`, exit 0, sem diálogo; a alteração se perdeu |
| `[Fatia] Onibus` salva e suja | `scene create` sem `--additive` | `success: true`, exit 0, sem diálogo; a alteração se perdeu |

**Antes de trocar de cena**, rode `unity command scene list_open` e confira o `isDirty` de **cada** cena (a lista traz também as aditivas):

```bash
unity command scene list_open
```

- Todas com `isDirty: false`: pode trocar.
- Alguma com `isDirty: true` e a alteração é **sua**, desta sessão: grave com `unity command scene save --path <cena>` (ou `editor save_all`) e confira o `.unity` por `grep`, como no parágrafo "Gravar uma cena alterada por `script eval --file`", em [Trocar de sala por `eval` (#12)](#trocar-de-sala-por-eval-12) (`SaveOpenScenes()` por `eval` não grava).
- Alguma com `isDirty: true` e você **não sabe de quem é** (uma sala em ajuste pelo programador, o resto de outra sessão): **pare** e avise o programador. Gravar um trabalho que não é seu é tão ruim quanto perdê-lo.

A ausência de diálogo não quer dizer que nada se perdeu: o diálogo "Scene(s) Have Been Modified" é do Test Runner (ver [Rodar a suíte PlayMode](#rodar-a-suíte-playmode)), um caso diferente. `OpenScene` em modo `Additive` não fecha nada; é por isso que o despejo da #107 (`if (!wasLoaded)`) não perde trabalho. O diálogo é o da `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()`, que o Test Runner chama antes de toda suíte (#171): não a chame para se proteger, porque ela abre o mesmo diálogo.

### Entrar e sair do Play Mode

```bash
unity command editor.playmode start
```

```bash
unity command editor.playmode stop
```

### Capturar a tela do jogo

```bash
unity command capture game_view --source screen --output-path Temp/captura.png
```

`--source screen` só funciona em Play Mode e é o único que inclui a interface (o canvas é Overlay). Sem ele, a captura mostra só o que a câmera renderiza. A resposta traz `width`, `height`, `bytes` e `savedPath`.

**A captura grava onde você mandar, fora de `Assets/` também** (#189; na 0.7.0 ela sempre caía em `Assets/Temp/`, com `.meta`). `--output-path` aceita um caminho **relativo à raiz do projeto** (`Temp/captura.png`, pasta ignorada pelo git; medido: gravou em `<projeto>\Temp\captura.png`) ou **absoluto, fora do repositório** (o scratchpad da sessão; medido: PNG de 1280×720 gravado e `git status` limpo). Nenhum dos dois cria `.meta`, então não há `Assets/Temp` para apagar. Leia o PNG gravado para conferir a tela (a ferramenta de leitura de arquivo mostra a imagem).

**O aviso "The Game view was not redrawn for this capture"** aparece quando o Play não desenhou um quadro novo desde o último: a imagem é então o último quadro renderizado, e não o estado de agora. Na #189 ele apareceu **de forma intermitente com o Play avançando** (`Time.frameCount` crescia e o `editor status` dizia `runInBackground: true` e `playerLoopTicking: true`), e o texto do aviso fala em "Run In Background off". Trate-o como um alerta: confira o `frameCount` e, se a imagem importar para a conclusão (um efeito no meio, um texto novo), capture de novo e compare. `--source camera` renderiza uma câmera na hora e, segundo a descrição do comando, desenha a interface Overlay por cima em URP (`--overlay auto|true|false`); não foi medido aqui.

### Ver o jogo em outro formato de tela (#157)

No Play Mode, mude o tamanho da Game view por `eval` e capture com o **mesmo** tamanho:

```bash
unity command script eval 'UnityEditor.PlayModeWindow.SetCustomRenderingResolution(2560, 1080, "21x9"); return "ok";'
```

```bash
unity command capture game_view --source screen --width 2560 --height 1080 --output-path <scratchpad>/captura_21x9.png
```

- **`--width` e `--height` da captura têm de ser iguais aos da Game view.** O comando reescala a imagem para o tamanho pedido: com um tamanho diferente a imagem sai deformada e uma medida de pixels não vale.
- **A resolução escolhida continua valendo depois do Play** (medido no levantamento da #157: 2560×1080 com o Play parado). Ao fim, devolva com `SetCustomRenderingResolution(1920, 1080, "FullHD")`.
- Formatos de conferência: 2560×1080 (21:9), 1440×1080 (4:3), 1728×1080 (16:10) e 3024×1964 (MacBook).
- **Na build, em janela:** abra com `-screen-fullscreen 0 -screen-width 1920 -screen-height 822` (≈21:9) e com `-screen-width 1200 -screen-height 900` (4:3). Exporte antes as preferências de tela (`reg export 'HKCU\Software\NoxStudios\Noturna' <arquivo>.reg`) e devolva no fim (`reg delete` da chave e `reg import`), porque os argumentos `-screen-*` ficam gravados. **Capture com `PrintWindow` (flag `2`, `PW_RENDERFULLCONTENT`) sobre a janela do jogo, e não com `Graphics.CopyFromScreen`:** o `SetForegroundWindow` de um processo sem foco não garante o primeiro plano, e na #157 as duas primeiras capturas por `CopyFromScreen` saíram com a janela de outro aplicativo do programador por cima do jogo (conteúdo pessoal; os PNG foram apagados). O `PrintWindow` devolveu só os pixels do jogo, e a área cliente sai recortando a janela pela diferença entre `ClientToScreen` e `GetWindowRect`. Apague qualquer captura que mostre outra janela.
- Para medir o limite do pan, desligue o `ScreenPanController` só em runtime antes de mover o cenário à mão (o mouse real de quem usa o computador move o pan entre duas leituras) e chame `CenterOn(1000)` e `CenterOn(-1000)`: o `ScrollingBackground.x` fica em ±22,46 em 1920×1080 e em ±19,50 em 2560×1080.

### Mover, renomear e apagar assets

```bash
unity command asset move --asset "Assets/Scripts/A/Coisa.cs" --destination "Assets/Scripts/B/Coisa.cs"
```

```bash
unity command asset delete --asset "Assets/Scenes/Antiga.unity" --confirm true
```

Sempre pelo CLI ou por `AssetDatabase`, nunca por `mv` ou `rm`: é o que leva o `.meta` junto e preserva o GUID.

## Verificação em Play Mode

O que os testes EditMode não cobrem (MonoBehaviours, cenas, `UnityEvent`) é verificado rodando o jogo e conferindo o estado por `eval`. Cada issue traz o seu roteiro de verificação no comentário de levantamento.

### O Play Mode e o Editor sem foco

Um Editor dirigido pelo CLI está sem foco. O projeto roda sem foco (`runInBackground` ligado no Player Settings, D-31), então **o Play Mode avança e ninguém precisa atribuir nada** antes de uma verificação que dependa de frames.

**Não atribua `Application.runInBackground`.** No Editor essa propriedade é a própria opção do Player Settings (`SetPlayerSettingsRunInBackground`): uma atribuição muda o `ProjectSettings/ProjectSettings.asset` na próxima gravação (medido na #79: `runInBackground: 1 → 0` no arquivo depois de um `AssetDatabase.SaveAssets()`). Se o `git status` mostrar `runInBackground` alterado, alguém atribuiu; o arquivo não entra no commit e o valor correto é `1`.

**Diagnóstico.** Se uma verificação der resultado estranho em Play Mode, confirme que o jogo avança: leia `Time.frameCount` duas vezes, com alguns segundos de intervalo.

```bash
unity command script eval 'return UnityEngine.Time.frameCount;'
```

Desde a 1.0.0 o `unity command editor status` responde isso sem `eval`: os campos `playMode`, `frameCount`, `playerLoopTicking`, `runInBackground`, `codeLoadCount` (quantas vezes o código foi carregado), `domainReloadInProgress` e `autoRefresh` (medido na #189: `playing`, `frameCount` crescendo, `playerLoopTicking: true`, `runInBackground: true`). `playerLoopTicking: false` com `playMode: playing` é o jogo parado.

**O `editor.playmode start` liga um override do Run In Background até o Play acabar** (descrição do comando, 1.0.0; a 0.10.0 corrigiu a gravação do override no `ProjectSettings.asset`). Como o projeto já está em `1` (D-31), nada muda no arquivo: medido na #189, depois de um Play inteiro (`start`, Novo Jogo, capturas, `stop`) o `git diff ProjectSettings/` ficou vazio e o `editor status` seguiu em `runInBackground: true`. Confira esse `git diff` ao fim de toda verificação em Play, e não use `editor.playmode set_run_in_background` (é o mesmo override, por outro caminho).

Se o número não mudou, o jogo está parado e qualquer conclusão sobre `Start()`, `Update()` ou corrotinas é falsa. O sintoma engana, porque `SceneManager.LoadScene` ainda conclui e os objetos são recriados, e parece que o código de restauração é que está quebrado. Esse congelamento foi visto com `runInBackground` desligado nas verificações da M3; na #79 ele **não se reproduziu** (com a opção desligada e o Editor sem foco, o `frameCount` avançou), então a condição exata que o provoca não está isolada. Por isso o diagnóstico fica, mesmo com a opção ligada.

Asserções feitas só por chamada direta de método (`porta.Interact()`, `GameSaveManager.Instance.Save()`) não dependem de frames e valem mesmo com o jogo parado.

### O Play Mode não recarrega o domínio (#140)

O Editor deste projeto entra no Play em **Reload Scene only** (`EnterPlayModeOptions.DisableDomainReload`): a cena é recarregada, o domínio, não. "Recarregar o domínio" é o Unity descartar o código C# da memória e carregá-lo de novo, o que devolvia todo campo estático ao valor inicial. Sem isso, **o que um Play deixa na memória está lá no Play seguinte**, e os métodos `SubsystemRegistration` da D-29 são o que zera o estado do jogo.

- **Como ler a opção:** `UnityEditor.EditorSettings.enterPlayModeOptions` devolve `DisableDomainReload`. O `m_EnterPlayModeOptionsEnabled: 1` do arquivo **não diz o que vale**: manda o `m_EnterPlayModeOptions` (`1` agora; era `0`). A propriedade `enterPlayModeOptionsEnabled` acompanha a opção (`False` com `None`, `True` com `DisableDomainReload`, medido na #140), mas não diz qual recarga foi desligada: leia a `enterPlayModeOptions`. Para mudar, `eval` fora do Play com `enterPlayModeOptions = ...` e `AssetDatabase.SaveAssets()`, e confira que o `git diff ProjectSettings/` traz só essa linha.
- **O que a medição da #140 viu** (`[Teste] Mecanicas`, cinco Plays de cada, o tempo entre `EnterPlaymode()` e o primeiro quadro, pela receita de `SessionState` e `Time.unscaledTimeAsDouble`):

  | | Mínimo | Mediana | Máximo | `Domain Reload Profiling` no `Editor.log` |
  |---|---|---|---|---|
  | Antes (`None`) | 1656 ms | 1731 ms | 1821 ms | 5 em 5 Plays |
  | Depois (`DisableDomainReload`) | 70 ms | 76 ms | 176 ms | 0 em 5 Plays |

  A suíte PlayMode (48 testes) foi de 77,44 s para 74,75 s e 74,67 s: a entrada no Play é uma parte pequena dela. A EditMode (384 testes) não depende da opção.
- **A recompilação ainda recarrega o domínio** (medido na revisão da #140: em Edit Mode, uma marca gravada com `AppDomain.CurrentDomain.SetData` voltou `null` depois de `UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation()`, e o `AppDomain.GetHashCode()` mudou; é o que a seção seguinte descreve para `.cs` novo ou alterado). A prova de que "o segundo Play não recarrega" só vale se nenhum `.cs` for salvo entre os dois Plays.
- **O que sobrevive entre Plays agora:** estático sem reset, `AppDomain.CurrentDomain.SetData`, `SessionState` e assinatura de evento estático. As receitas de FPS e de quadros desta página guardam dados com `AppDomain.CurrentDomain.SetData` (e um `Application.onBeforeRender` assinado por `eval`): **grave antes de ler**, ou o valor lido pode ser o de um Play anterior, e remova a assinatura ao terminar.
- **Sintoma de um estático sem reset:** "funciona no primeiro Play e falha no segundo". Com a build não acontece, porque ela sempre começa do zero. Procure o campo estático que não tem método em `SubsystemRegistration` (D-29).
- **Receita de prova (dois Plays seguidos, sem recompilar).** No primeiro Play, num `script eval --file`, grave uma marca no domínio (`AppDomain.CurrentDomain.SetData("marca", "play1")`) e outra na história (`StoryState.SetBool("$marca", true)`); leia a contagem de assinantes do `MessageBroker` por reflexão (ferramenta de verificação, não código do jogo): `typeof(MessageBroker).GetNestedType("Handlers`1", BindingFlags.NonPublic).MakeGenericType(tipoDaMensagem).GetField("Snapshot").GetValue(null)` como `System.Array`, e o `Length`; e o `GetInstanceID()` de cada `Instance` de manager. Pare o Play no meio de uma troca de sala (com `fadeSeconds` em 3, `ChangeRoom` e `editor.playmode stop` com o `PlayerInputGate.IsEnabled` em `false`). No segundo Play, espere ~10 s, confirme o `frameCount` e leia o mesmo: a marca do domínio **continua** (o domínio não foi recarregado), a da história **sumiu**, o gate é `true`, as contagens são as do primeiro Play, os `GetInstanceID()` são outros, e há um único `GameStateController`. O `$` e o shell: use `script eval --file`.

### Comandos que falham com "Network error" logo depois de criar ou apagar scripts

Depois de mexer em `.cs`, o Editor recarrega o domínio, e um `eval` ou `script recompile` enviado nesse intervalo falha com `Network error: An error occurred while sending the request`. Não é erro do código: espere e rode `unity command script recompile_status` até `completed`, depois repita o comando.

### O `$` e o shell

Os nomes de variável de história começam com `$`. Dentro de aspas duplas o shell o expande para vazio e o `eval` roda com o nome errado, sem erro. Todo `eval` com um nome como `$falou_com_luna` vai entre aspas simples.

### Acento e escape num `script eval --file` (#101)

O `script eval --file` lê o `.cs` como UTF-8 (`System.IO.File.ReadAllText`): um `'ç'` literal num arquivo em UTF-8 compila e vale `E7`, e `'\\'` vale `5C`. Quem estraga acento e escape é **a ferramenta que grava o arquivo**, e sem erro:

- O `Write` do agente e o `printf` do Git Bash gravam a sequência `\uXXXX` (por exemplo, a do cedilha, `\u00E7`) já como o caractere (`ç`, bytes `C3 A7`); o `\\` e um substituto sozinho (`\uD83D`) ficam como estão no `Write`. O `printf` e o heredoc (`<< 'EOF'`) do Git Bash **reduzem `\\` a `\`**: na #189 um `'\\'` gravado por heredoc chegou ao compilador como `'\'` (`CS1010: Newline in constant`). Para escape, grave com o `Write`.
- O `Set-Content` do PowerShell 5.1 sem `-Encoding utf8` grava em ANSI (`E7` num byte só), e o `'ç'` chega ao compilador como `U+FFFD`, sem erro (medido: o resultado foi `FFFD`).

Monte o caractere por código, `(char)0xE7`, e a string por concatenação: `"Concei" + (char)0xE7 + (char)0xE3 + "o"` devolve `Conceição` em qualquer gravação. Para saber o que foi gravado, `od -c <arquivo>` no Git Bash ou `Format-Hex` no PowerShell. Os erros `Too many characters in character literal` e `Unexpected character` vistos na #101 não se reproduziram; não os atribua ao `script eval --file`. Remedido na #189 com a 1.0.0, um arquivo gravado pelo `Write` com `(int)'ç'`, `(int)'\\'` e `"Conceição"` devolveu `a231|92|Conceição` (`E7`, `5C`, texto inteiro), e um arquivo em ANSI devolveu `x65533` (`U+FFFD`): o pacote novo continua lendo o arquivo como UTF-8.

### O `eval` roda tudo em um frame só

Um `eval` com um laço que chama `AdvanceDialogue()` várias vezes não avança nada além da primeira fala: a apresentação do Yarn é assíncrona e só troca a fala no frame seguinte. Para percorrer uma conversa, faça uma chamada de `eval` por passo, com um `sleep` de um ou dois segundos entre elas, e leia o `DialogueText` a cada passo. O mesmo vale para ler a interface logo depois de `StartDialogue` ou `MakeChoice`: espere um instante.

**O texto é revelado aos poucos (#11).** Cada fala pede dois `AdvanceDialogue()`, um para completar e outro para avançar, ou uma espera até a revelação acabar. Dois `AdvanceDialogue()` no mesmo `eval` só completam a fala (é o caso "dois cliques no mesmo frame"). O `text` do `DialogueText` tem a fala inteira desde o começo da revelação: o que o jogador vê é `maxVisibleCharacters`, e o total é `textInfo.characterCount`. Cada `eval` leva perto de 2,5 s de ida e volta com a 1.0.0 (medido na #189: 2,5 s para `return 1;`; eram ~1,5 s na 0.7.0), e uma fala curta termina de aparecer sozinha entre dois comandos, o que faz um clique que devia completar avançar. Para medir, baixe a velocidade só em runtime: em Play Mode, `SerializedObject` no `DialogueUIController`, campo `charactersPerSecond` (1 deixa cada passo inequívoco), e `ApplyModifiedProperties()`. O valor volta ao do prefab no Play seguinte, e o `GameUI.prefab` não fica alterado.

### O `eval` aceita `using` e importa o básico (#189)

Na 0.7.0 o `eval` era só o corpo de um método: sem `using` no topo e sem método de extensão por sintaxe de ponto. **Na 1.0.0 isso mudou** (medido na #189): `using ProjetoVN.Core.State; return StoryState.IsTrue("$nada");` compila, inclusive em `script eval '<código>'` na linha de comando; uma função local (`string F(int x) { return "f" + x; } return F(3);`) devolve `f3`; e a extensão por ponto funciona com o `using` (`using UnityEngine.UIElements; … l.Query<Label>().ToList().Count`). O pacote importa sozinho `System.Linq`, `System.Threading.Tasks`, `UnityEngine` e `UnityEditor`. Já os tipos do jogo, sem `using`, dão `CS0103: The name 'StoryState' does not exist in the current context`. Código com mais de uma ou duas linhas vai em um arquivo `.cs` e roda com `unity command script eval --file <caminho>` (o caminho pode ser absoluto, fora do repositório: o scratchpad da sessão funcionou), o que também escapa do problema do `$` no shell.

**Cada `script eval` carrega um assembly que o Unity não descarrega** até a próxima recarga de domínio (apontado no levantamento da #189; não medido). Com o Play sem recarga (#140), uma verificação longa acumula assemblies; a recompilação de scripts os descarrega.

### O erro de console que é do CLI, não do jogo

Um `eval` enviado enquanto o Editor ainda está entrando em Play Mode pode estourar o tempo de resposta. O comando falha e o pacote Pipeline deixa um erro no console:

```
Failed to handle /api/exec request: Main thread operation timed out after 5000ms
```

Esse erro não vem do jogo. Espere uns dez segundos depois do `editor.playmode start` antes do primeiro `eval`. Se o erro aparecer mesmo assim, ele não conta como falha de "console sem erro"; para uma leitura limpa, saia do Play Mode, rode `console clear` e repita.

### O agente não vê diálogo modal (#189)

As três travas conhecidas do CLI têm a mesma causa: um **diálogo modal** abre no Editor, a thread principal para, e o agente não o vê. São o "Scene(s) Have Been Modified" do Test Runner ([Rodar a suíte PlayMode](#rodar-a-suíte-playmode), #171), o "Resolving packages" do Package Manager ([Quando os pacotes mudam](#quando-os-pacotes-mudam), #5) e o "modified externally … reload" da troca de branch ([Quando uma cena aberta muda](#quando-uma-cena-aberta-muda-187), #187). O sintoma é igual nos três: todo comando expira (`Main thread operation timed out`, `EDITOR_TIMEOUT`, exit `14`) e o `unity status` segue dizendo `ready`.

O manual do CLI 1.0 e a skill `unity:unity-cli` descrevem comandos que leem e respondem um diálogo (`unity command editor dialog`, `editor.dialog list`/`answer`/`dismiss`), o exit `18` (`AWAITING_INPUT`) e o estado `STATUS_BLOCKED_BY_DIALOG`. **Nada disso existe neste Editor** (medido e lido na #189, com o `com.unity.pipeline` 1.0.0 no Editor 6000.3.9f1):

- A detecção do pacote depende de uma API do próprio Unity, a `UnityEditor.EditorDialogEvents` (evento `dialogWillShow`), que só existe a partir do **Unity 6000.7**, hoje só em beta (`6000.7.0b9`; a estável mais nova é a `6000.6.9f1`). O arquivo `Editor/EditorDialogStateMirror.cs` do pacote começa com `#if UNITY_6000_7_OR_NEWER`; no 6000.3.9f1 ele nem compila. O changelog do pacote já avisa, na 0.6.0: o endpoint de diálogos "requires a recent enough trunk build".
- `unity command --query dialog` não acha nenhum comando do Editor (só `terms accept`, `projects open` e `open`, da CLI). Nenhuma versão do `com.unity.pipeline` dá isso no 6000.3: atualizar o pacote não muda, e mudar o Editor fixado em `CLAUDE.md` está fora do que o projeto faz.
- **Na prática:** não procure `editor dialog` nem espere o exit `18`. O reconhecimento continua sendo o do sintoma acima, o do console (`Main thread operation timed out`) e a conferência **antes** do comando que abre o diálogo (`scene list_open` com `isDirty: false` antes de todo `test run`; `Packages/` e cenas abertas antes de toda troca de branch). Se o diálogo já abriu, só uma pessoa o fecha: pare e avise o programador.
- Se o projeto passar a usar o 6000.7 ou mais novo, revise esta página: os comandos de diálogo passam a existir, e a receita de cada trava pode ganhar um passo "liste e responda o diálogo".

### Trocar de branch com o Editor aberto

Duas coisas que mudam no disco numa troca de branch travam o Editor aberto ou o enganam: os **pacotes** e uma **cena aberta** (#187). A receita [Antes de trocar de branch](#antes-de-trocar-de-branch) cobre as duas, e os comandos de trabalho (`/executar-issue`, `/revisar-issue`) a seguem.

#### Quando os pacotes mudam

Se as duas branches têm `Packages/manifest.json` diferentes (uma instala um pacote que a outra não tem), **feche o Unity antes de trocar de branch** (`git checkout`, `gh pr checkout`) **ou de atualizar a atual** (`git pull`, quando o que chega muda os pacotes) e reabra depois. O que trava é o `manifest.json` mudar no disco com o Editor aberto, seja qual for o comando git. Com o Editor aberto, ele pode ficar preso no diálogo "Package Manager (busy for …) Resolving packages…": a thread principal para, todo `eval` falha com `Main thread operation timed out`, o `test run` expira e a janela do Package Manager não lista nada. Aconteceu duas vezes na issue #5, entre `main` e a branch que instalava o Yarn Spinner.

- **Como reconhecer:** o arquivo `upm.log`, em `%LOCALAPPDATA%/Unity/Editor/`, mostra `project:resolve-packages --> 200` (a resolução terminou em segundos), mas o diálogo continua aberto. O Package Manager não está trabalhando; quem travou foi o Editor. O mecanismo exato não foi confirmado.
- **Como sair:** finalize o `Unity.exe` pelo Gerenciador de Tarefas e reabra o projeto já na branch certa. Não é preciso reiniciar o computador. Ao reabrir, responda **No** ao diálogo "Recovering Scene Backups" (a cena válida é a do git) e apague `Assets/_Recovery/` se ela existir.
- **Não chame `UnityEditor.PackageManager.Client.Resolve()` por `eval`** para forçar a resolução com o Editor aberto: foi logo depois disso, seguido de um `AssetDatabase.Refresh()`, que o Editor travou na segunda vez.
- Enquanto o pacote não é resolvido, o console mostra erros de compilação que não são do código (`The type or namespace name 'Yarn' could not be found`), e um `test run` roda os assemblies antigos e devolve um resultado que não vale para a branch.

**A #189 mudou o `com.unity.pipeline` de `0.7.0-exp.1` para `1.0.0`** (`Packages/manifest.json` e `packages-lock.json`). Enquanto uma branch ou um PR for anterior a ela, toda troca entre essa branch e a `main` nova dá `1` na conferência `git diff --quiet HEAD <destino> -- Packages/`, e a receita pede ao programador que feche o Unity. Para instalar ou trocar a versão de um pacote do projeto, com o Unity **fechado**: `unity pipeline install --version <versão> --project-path .` (valida no registro e grava só a linha do `manifest.json`; `unity pipeline list-versions` lista as versões), abra com `unity open .` e confira `unity pipeline editors --format json` (`pipelineVersion`, `updateAvailable`) e o `git diff Packages/packages-lock.json` (só o bloco do pacote; na #189 foi uma linha). Ao reabrir, o Editor pode perguntar "Recovering Scene Backups": responda **No**.

**A #214 apagou o pacote embutido `com.merry-yellow.code-assist`** (a pasta dele em `Packages/` e o bloco dele no `packages-lock.json`, sem mexer no `manifest.json`, onde um pacote embutido não aparece). Enquanto uma branch for anterior a ela, a troca entre essa branch e a `main` dá `1` na conferência de `Packages/` mesmo com o `manifest.json` igual, e a receita pede ao programador que feche o Unity. É por isso que a conferência compara a pasta inteira, e não só o manifesto. Para tirar um pacote embutido, com o Unity **fechado**: `git rm -r` da pasta dele em `Packages/`, depois `unity open .`; o Package Manager reescreve o `packages-lock.json` sozinho, e o `git diff` do lock deve mostrar só a saída do bloco do pacote.

#### Quando uma cena aberta muda (#187)

Se a troca de branch altera, cria ou apaga o `.unity` de uma cena que **está aberta** no Editor, o Editor fica com a versão da outra branch na memória e o disco com a desta. O que acontece depende de quem chama o `AssetDatabase.Refresh()` primeiro:

- **Com um `Refresh`, o diálogo trava o CLI.** O Editor abre "The open scene(s) have been modified externally … Do you want to reload the scene(s)?", com **Reload** e **Ignore**, e a thread principal para. Todo comando do CLI expira com `Main thread operation timed out`, o `unity status` segue dizendo `ready` e o console enche de erros do CLI (73 na revisão da #130), nenhum do jogo. O agente não vê o diálogo, e o sintoma é igual ao do Test Runner e do Package Manager. Visto na revisão da #130, com a `[Fatia] Onibus` aberta e limpa, na volta da branch do PR para a `main`; só se fechou quando o programador clicou em **Reload**.
- **Sem `Refresh`, o estrago é silencioso.** Nada abre, os comandos respondem, a cena segue `isDirty: false` e o Editor continua com a versão antiga na memória (medido: o `Item_Moeda`, que a branch nova não tem, continuou na cena). A próxima gravação da cena a escreve no disco por cima da versão da branch.

**Quem chama o `Refresh` sem ninguém pedir** (relido na #189 no código do `com.unity.pipeline` **1.0.0**, `Library/PackageCache/com.unity.pipeline@178b93997022/Editor/`; lido no código, não medido; a hash da pasta muda com a versão):

- `script recompile` (`Commands/RecompileCommand.cs:99`): é o **primeiro comando** da `/revisar-issue` depois do `gh pr checkout`, e é ali que o diálogo abre.
- `scene create`, `scene save` e `editor save_all` (`Commands/Scenes/SceneCommands.cs:75`, `:143`, `:176`). O `scene save` **grava a cena da memória e só depois** chama o `Refresh` (linhas 137 a 143): com a cena antiga na memória, ele sobrescreve no disco a versão da branch **sem diálogo nenhum**. É o mesmo estrago do **Ignore**, por outro caminho. O `scene open` não chama.
- Também chamam: `asset create_folder` (`Commands/Assets/FolderCommands.cs:32`) e o auxiliar que cria a pasta que falta antes de gravar um prefab ou uma cena (`Commands/Prefabs/PrefabCommands.cs:456`). Os comandos de cena (`scene open`, `scene save`, `scene create`, `editor save_all`) **recusam rodar em Play Mode** (`RefuseInPlayMode`).
- O `editor.playmode` também chama, ao criar o asset de cenário (`Commands/PlayModeScenarioCommands.cs:612` e `:633`); o projeto não usa esse comando.
- `script eval` **não** chama: o código só cita o `Refresh` num comentário que diz que ele não ocorre (`Commands/Scripts/RunScriptCommand.cs:109`; na 0.7.0 eram `RecompileCommand.cs:95`, `SceneCommands.cs:65/:115/:141` e `RunScriptCommand.cs:119`, e o comportamento é o mesmo).
- O `Auto Refresh` do Editor (`EditorPrefs kAutoRefreshMode` = 1) age quando a janela ganha foco: se o programador clicar no Unity depois de uma troca, o diálogo pode abrir sem comando nenhum (não medido).

**O que a #187 mediu** (2026-11-17, Editor 6000.3.9f1, `com.unity.pipeline` 0.7.0-exp.1, Editor sem foco). Para imitar no disco o que um `git checkout` faz, o `[Fatia] Onibus.unity` foi sobrescrito pela versão de `1001a75` (a de antes da #130, com o `Item_Moeda`) e devolvido com `git checkout --` no fim:

| # | Situação | Chamada | Resultado |
|---|---|---|---|
| 1 | `[Fatia] Onibus` aberta e limpa; arquivo trocado no disco | `eval` e `scene list_open`, cinco vezes em ~80 s, **sem `Refresh`** | Todos responderam, **sem diálogo**. `GameObject.Find("Item_Moeda")` deu `False` e `isDirty: false`: o Editor segue com a versão antiga na memória enquanto o disco tem a outra |
| 2 | Continuação do 1 | `scene open` do `Menu`, `git checkout --` do arquivo, `eval` com `AssetDatabase.Refresh()` | Sem diálogo; `scene list_open` respondeu |
| 3 | Só o `Menu` aberto; arquivo trocado no disco | `Refresh`, depois `scene open` do `[Fatia] Onibus` | Sem diálogo; `Item_Moeda` deu `True` (o Editor importou a versão do disco) |
| 4 | Continuação do 3 | `scene open` do `Menu`, `git checkout --`, `Refresh` | Sem diálogo; o `git status` voltou ao de antes |
| 5 | `Menu` aberto | `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)` por `eval` | Cena sem título, `path` vazio, `isDirty: false`, `sceneCount` 1; o `scene open` do `Menu` em seguida voltou normalmente |

O caso "cena aberta + `Refresh`" não foi repetido de propósito: o diálogo foi o da revisão da #130, e provocá-lo trava o CLI até alguém clicar. A conclusão é que o diálogo **não depende do `Refresh` explícito**: abre no primeiro `Refresh`, de quem for. Por isso a conferência tem de acontecer **antes** da troca, e a defesa é não ter aberta nenhuma cena que a troca altere. Uma cena sem título não tem arquivo, então nenhuma troca de branch a altera.

#### Antes de trocar de branch

Os comandos de trabalho (`/executar-issue`, `/revisar-issue`) fazem esta conferência antes de todo `git checkout`, `gh pr checkout` e `git pull`. O `git checkout -b` não precisa dela: ele cria a branch no commit em que se está, e nenhum arquivo muda no disco.

0. **Os arquivos só desta máquina (#217).** `.claude/` (os comandos de trabalho), `.vscode/`, `.vsconfig` e `Directory.Build.props` ficam fora do git, só no disco, mas os commits anteriores à #217 os rastreiam. Numa troca entre um commit que os rastreia e um que não, o git mexe neles mesmo com o `.gitignore`: indo para onde não são rastreados, **apaga** os arquivos do disco; indo para onde são, **sobrescreve** a cópia do disco com a versão commitada, sem aviso; e, se a cópia do disco difere da commitada, recusa a troca (`Your local changes to the following files would be overwritten`). Medido num repositório descartável na #217, com `git checkout` e `git pull`.
   1. **Quando vale:** depois do `git fetch origin` do passo 1, se um dos dois comandos imprime `.claude`, com o mesmo destino do passo 2:

      ```bash
      git ls-tree --name-only HEAD .claude
      ```

      ```bash
      git ls-tree --name-only <destino> .claude
      ```

      Nenhum dos dois imprime nada: os dois lados são posteriores à #217, e este passo não se aplica.
   2. **Guardar, antes da troca.** Copia os arquivos para `../ProjetoVN-local/`, ao lado do repositório, e devolve à versão commitada os que o `HEAD` rastreia, para que o git não recuse a troca:

      ```bash
      test -d .claude/skills && rm -rf ../ProjetoVN-local && mkdir ../ProjetoVN-local && cp -a .claude .vscode .vsconfig Directory.Build.props ../ProjetoVN-local/ && git ls-files -z .claude .vscode .vsconfig Directory.Build.props | xargs -0 -r git checkout --
      ```

      O `test -d` para a linha quando a `.claude/skills/` já sumiu do disco: assim uma cópia boa, de antes, não é apagada. Nesse caso (saída `1`), não troque de branch antes de restaurar a cópia (o comando do item 3) e repetir o guardar.
   3. **Restaurar, depois da troca:**

      ```bash
      cp -a ../ProjetoVN-local/. .
      ```

      Num commit posterior à #217, o `git status` fica limpo. Num anterior, os arquivos aparecem como modificados (` M`) sempre que a cópia difere da commitada: é esperado, e o guardar da próxima troca os devolve antes de trocar.

   O `ProjetoVN.GameFlow-generated.ysls.json` não entra na cópia: o Unity o recria na próxima compilação. Para ler um arquivo de um commit antigo, use `git show <commit>:<arquivo>` ou um `git worktree add` fora do repositório, e não um `checkout` na cópia de trabalho. Uma sessão em worktree ou na nuvem não recebe a `.claude/`: o programador aceitou isso na #217.

1. Atualize as referências remotas, para que `origin/...` exista e esteja em dia:

   ```bash
   git fetch origin
   ```

2. Compare `Packages/` da branch atual (`HEAD`) com o destino, que é o commit que o comando vai pôr no disco:
   - `main`, para um `git checkout main`;
   - `origin/<branch>`, para um `gh pr checkout` (ele atualiza a branch local pela remota, mesmo quando ela já existe e está atrasada) ou para uma branch que ainda não existe localmente;
   - `origin/<branch atual>`, para um `git pull`.

   ```bash
   git diff --quiet HEAD <destino> -- Packages/
   ```

   A comparação cobre a pasta `Packages/` inteira, não só o `manifest.json`: o `manifest.json` e o `packages-lock.json` mudam juntos (a #5 mexeu nos dois). Uma diferença só no lock é rara e custa, no máximo, um fechar e reabrir à toa.

3. Leia a saída (`echo $?` logo depois, no Git Bash):
   - **`0`:** `Packages/` é igual nas duas pontas. Siga para o passo 4 (cenas abertas) e troque de branch com o Editor aberto.
   - **`1`:** os pacotes mudam. Se o `unity status` lista um Editor com este projeto, **pare**: diga ao programador qual troca vai fazer e por quê (citando esta seção) e peça para ele fechar o Unity. Continue quando ele confirmar e o `unity status` não listar mais o projeto. Troque de branch e reabra com `unity open .` (sem pipe, como em [O Editor está conectado?](#o-editor-está-conectado)), repetindo `unity status` até `ready`. Se o Editor já estava fechado, troque direto. Nos dois casos o passo 4 não se aplica: sem Editor aberto não há cena na memória nem diálogo.
   - **Qualquer outra saída (`128` é a mais comum, de uma referência que não existe):** não é seguro. Faça o `git fetch origin`, confira o nome do destino e repita; não troque de branch antes de ter `0` ou `1`.

4. **Cenas abertas (#187).** Só vale se a troca segue com o Editor aberto (com o Editor fechado, ou depois de o programador fechá-lo no passo 3, não há diálogo nem cena na memória). Use o **mesmo destino** do passo 2; na primeira execução, `main` **e** `origin/main`, por causa do `git pull`.
   1. Liste as cenas que mudam até o destino:

      ```bash
      git diff --name-only HEAD <destino> -- '*.unity'
      ```

      A lista cobre cena alterada, criada e apagada (de `1001a75` até a `main` ela devolve o `[Fatia] Loja`, que nasce, e o `[Fatia] Onibus` e o `[Fatia] Posto`, que mudam); os `.meta` ficam de fora. Lista vazia: nenhuma cena muda, e a troca não precisa de mais nada.
   2. Rode `unity command scene list_open`. Nenhuma cena aberta está na lista: troque de branch. Alguma está: confira o `isDirty` de **todas** as cenas abertas, pelos três casos de [Abrir e salvar cena](#abrir-e-salvar-cena) (limpa, sua, de outra pessoa), porque o passo seguinte descarta sem diálogo o que não foi salvo (#166).
   3. Com todas limpas, troque a cena aberta por uma vazia e sem título:

      ```bash
      unity command script eval 'UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single); return "ok";'
      ```

      Confira com `unity command scene list_open`: uma cena, `path` vazio, `isDirty: false`. O `NewScene` em modo `Single` fecha **todas** as cenas abertas, as aditivas inclusive.
   4. Troque de branch. Depois, `unity command script recompile` (ou o `Refresh` por `eval`) e abra a cena que a sessão vai usar com `unity command scene open` (o `Menu`, se nenhuma em particular). **Não deixe a cena sem título aberta para a suíte:** o caso dela com o Test Runner não foi medido.

   Por que a cena vazia e não o `Menu`: abrir o `Menu` também funciona (casos 2 a 4 da medição), mas só quando o próprio `Menu` não está na lista, e ele muda de vez em quando (a #21, a #96 e a #116 o alteraram). A cena sem título funciona sempre, sem ramo a mais na receita.

**Se o diálogo já abriu** (todo comando expira, `unity status` diz `ready` e o console tem `Main thread operation timed out`): **pare** e peça ao programador que clique em **Reload**. A cena estava limpa, então nada se perde. **Ignore**, não: deixa a versão da outra branch na memória, e a próxima gravação desfaz no disco o que a branch atual tem. Depois do Reload, confirme com `unity command scene list_open`; se os comandos seguirem expirando, é o caso do Cancel da #171 ([Rodar a suíte PlayMode](#rodar-a-suíte-playmode)): feche e reabra o Unity (`unity open .`).

**Troca já feita sem a receita e sem travar:** não grave a cena aberta. O `scene save` escreve a versão da memória no disco, que pode ser a da outra branch (caso 1). Abra a cena de novo com `scene open` só depois de um `Refresh`, ou peça ao programador.

**A volta para a `main` no fim de uma revisão** é o caso que já travou uma sessão (a primeira revisão da #5). Se a conferência de pacotes der `1` com o Editor aberto, a sessão **não troca**: deixa a branch do PR em checkout e avisa o programador, no encerramento, que a volta para a `main` ficou para ele, com o Unity fechado. A sessão já terminou o trabalho, então não vale parar esperando a resposta. A conferência das cenas, ao contrário, a sessão resolve sozinha (passo 4), mesmo na volta.

### O que não dá para simular

O CLI não simula clique de mouse. Um comportamento que dependa do clique real (como o clique que encerra um diálogo não atingir o mundo) é verificado chamando os métodos na mesma ordem e conferindo o estado, e a issue registra que o teste com mouse de verdade fica para o programador.

**O `runtime simulate_pointer` mudou com a 1.0.0 (#189), mas o clique no mundo não foi medido.** Na 0.7.0 (#76 e #124) o comando respondia `Success` e **não movia o `Mouse.current`**: na revisão da #13, com a `Exit_Janela` no centro, a cena não mudou e `Mouse.current.position` ficou `(0,00, 0,00)`; um `QueueStateEvent` de `MouseState` dava `(300,00, 200,00)` no mesmo `eval` e `(0,00, 0,00)` no seguinte, porque a posição enfileirada não durava até o próximo quadro. Na #189, em Play Mode com `Application.isFocused = False`, `unity command runtime simulate_pointer --x 300 --y 200 --action move` respondeu `pos=(300,200) action=move button=left` e, **num `eval` seguinte**, `Mouse.current.position` deu `(300,00, 200,00)`: a posição agora fica. O que o comando aciona em objetos do mundo: o `--action move` **chega** ao `PointNClickSelector` **com o Editor em foco** (medido no levantamento da #208: em Play Mode no `[Fatia] Posto`, com `Application.isFocused = True`, o `--action move` sobre o `Motorista` fez o seletor chamar `OnHoverEnter()` e, ao sair, `OnHoverExit()`; o contorno apareceu e sumiu). Com `Application.isFocused = False` **não chegou** (execução da #208, mesma sala): o `Mouse.current` lido por `eval` foi para o centro, o `Physics2D.OverlapPoint` desse ponto acertou o `Motorista`, o seletor estava ligado e o gate aberto, e o contorno não apareceu. Não conte com o `simulate_pointer` para acionar o seletor sem foco. O clique que vira `OnClick()` **não foi medido**: o teste da #189 foi interrompido porque a conversa de chegada do ônibus estava aberta. Até alguém medir, **o caminho do clique continua sendo `InteractableItem.OnClick()` por `eval`**; ao medir, registre aqui o resultado do `--action click` sobre um objeto com a conversa fechada. Armadilha: no levantamento da #208 (com foco) o `Mouse.current.position` lido por `eval` seguiu no valor do mouse real depois de cada `simulate_pointer`, embora o seletor tenha reagido; na execução (sem foco) a posição simulada foi lida certa, como na #189, e o seletor não reagiu. Nenhuma das duas leituras diz se o seletor reagiu: confira pelo objeto (contorno, escala), não pelo `Mouse.current` lido por `eval`.

**O `simulate_pointer` não chega à interface (medido no levantamento da #197, na 1.0.0).** Com as opções da Luna abertas e o ponteiro simulado sobre o `ChoiceButton0`, o `--action move` moveu o `Mouse.current` (`(870, 780)` lido depois), mas a ação `Point` do `InputSystemUIInputModule` ficou em `(0,00, 0,00)`, fase `Waiting`, e o `GetLastRaycastResult(0)` veio vazio: sem hover e sem raycast. Foi assim com `editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus`, `backgroundBehavior = ResetAndDisableNonBackgroundDevices`, `Application.isFocused = False`, antes e depois de focar a Game view por `EditorWindow.FocusWindowIfItsOpen`. Hover em interface continua sendo `ExecuteEvents` (receita em [Testar os sons por `eval`](#testar-os-sons-por-eval-20)).

### Trocar de sala por `eval` (#12)

A troca leva vários frames, e o `eval` roda em um só: dispare num `eval` e observe em outros, com espera entre eles. Só o fechamento do `PlayerInputGate` acontece no mesmo frame da chamada. Se a troca ficar parada no preto, confira antes se o jogo avança (`Time.frameCount` duas vezes, ver [O Play Mode e o Editor sem foco](#o-play-mode-e-o-editor-sem-foco)).

```bash
unity command script eval 'return ProjetoVN.GameFlow.Rooms.RoomNavigator.Instance.ChangeRoom("[Teste] Sala B");'
```

Para a troca ser observável, suba a duração só em runtime: em Play Mode, `SerializedObject` no `RoomNavigator`, campo `fadeSeconds` (3 deixa cada metade inequívoca), e `ApplyModifiedProperties()`. O valor volta ao do prefab no Play seguinte e o `Managers.prefab` não fica alterado. Durante a troca, `ScreenFader`/`CanvasGroup` mostram `alpha` subindo e `blocksRaycasts` verdadeiro, `PlayerInputGate.IsEnabled` falso, e um `RaycastAll` do `EventSystem` no centro da tela acha primeiro a `Image` `Black` do `Canvas_Fade`. Uma sala só carrega por nome se estiver na lista de cenas do build; o `Application.CanStreamedLevelBeLoaded` é o que decide.

**Com ponto de entrada (#13).** `ChangeRoom` tem a sobrecarga `ChangeRoom(sala, idDoPonto)`. Depois da troca, leia a posição do cenário: `GameObject.Find("ScrollingBackground").transform.position.x`. Os cliques das saídas se simulam com `GameObject.Find("Exit_Janela").GetComponent<ProjetoVN.PointNClick.InteractableItem>().OnClick()` (o CLI não simula mouse). Um `eval` de `OnClick` numa sala onde o objeto não existe só dá `Object reference not set`, sem trocar nada: confira a cena ativa antes. Com o mouse real sobre a borda da janela do Editor o pan anda sozinho (medido `x = 0,2` em vez de `0` antes de qualquer chegada); leia a posição logo depois da chegada, quando o gate ainda está fechado ou acabou de abrir.

**Provocar uma falha inesperada na troca.** Destruir o `Canvas_Fade` no meio da troca faz o `ScreenFader` lançar `MissingReferenceException` (é o único jeito de fora, sem gancho de teste). Suba `fadeSeconds` para 8 e: (a) antes do carregamento, destrua o fade 1 ou 2 s depois de disparar a troca; (b) depois do carregamento, dispare, leia a cena ativa em laço (um `eval` por volta) até ela mudar, e só então destrua o fade (com 3 s a troca inteira termina entre dois `eval` e a falha não acontece). Cada tentativa gasta o `Canvas_Fade` da sessão de Play: **saia e entre no Play de novo** antes de repetir.

**Gravar uma cena alterada por `script eval --file`.** `EditorSceneManager.SaveOpenScenes()` dentro de um `eval` **não gravou** a cena (`SerializedObject.ApplyModifiedProperties` deixa a cena suja, mas o arquivo ficou como estava), e o `test run` seguinte abriu o diálogo "Scene(s) Have Been Modified": o Editor travou e todo comando do CLI (inclusive `test cancel`) expirou até reiniciar o Unity. Grave sempre com `unity command scene save` e **confira no arquivo** (`grep` no `.unity`) antes de rodar a suíte. Numa fase vermelha de teste de conteúdo (sabotar a cena de propósito) isso importa em dobro: ao reiniciar o Editor, a cena sabotada é a que está no disco.

### Salvar e carregar por `eval` (#16)

O arquivo do save é `Application.persistentDataPath/savegame.json` (no Windows, `%USERPROFILE%/AppData/LocalLow/NoxStudios/Noturna`). **Toda troca de sala salva**, então a suíte PlayMode e qualquer verificação em Play **sobrescrevem o save do Editor**: copie o arquivo antes, se quiser guardá-lo, e devolva depois.

- **O `Load()` é uma troca de sala:** devolve `true` na hora e leva vários frames. Dispare num `eval` e observe em outros (como em "Trocar de sala por `eval`"); no mesmo `eval` do `Load()` só o `PlayerInputGate.IsEnabled == false` já vale. O fim é o gate voltar a `true`.
- **Ler o arquivo:** `System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "savegame.json"))`. A verificação escreve o arquivo por `eval` ou `script eval --file`, o que é ferramenta de teste, não código do jogo.
- **Save corrompido:** grave `{garbage` (ou um arquivo vazio) no caminho acima e chame `Load()`: `false`, o aviso "O save está corrompido" e nenhuma exceção. Sem o `GameStateJson`, o `JsonUtility` lançava `ArgumentException` (`JSON parse error: Missing a name for object member` para `{garbage`; `JSON must represent an object type` para `null` e `[1,2]`; `""` devolvia `null`).
- **Sala inexistente:** grave um JSON válido com `"currentScene": "Sala Que Nao Existe"` e um item em `ownedItemIds`. `Load()` devolve `false`, com o erro do `RoomNavigator` e o aviso do `GameSaveManager`, e o inventário **não** muda (prova que o `Load()` é tudo ou nada).
- **Save recusado em conversa:** `DialogueManager.Instance.StartDialogue("peguei_chave")`, espere 1 s e chame `Save()` (`false`, "Save recusado: há uma conversa aberta") e `Load()` (`false`, com o aviso do `RoomNavigator`). Encerre a conversa com `AdvanceDialogue()`, um `eval` por vez.
- **Um `eval` com nome de variável de história (`$…`)** vai em `script eval --file` ou entre aspas simples.
- **Um `savegame.json.tmp` na pasta** é o resto de uma gravação que falhou ou foi interrompida (#89): o save seguinte o sobrescreve e nada o lê. Depois de um save bem-sucedido a pasta tem só o `savegame.json`.
- **Provar que o save anterior sobrevive a uma falha na gravação (#89).** Num `script eval --file`, abra o save com `new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.Read)` e guarde o stream em `AppDomain.CurrentDomain.SetData("save89", stream)` (continua aberto depois do `eval`). Copie o arquivo antes. Dispare `ChangeRoom` para outra sala e espere o gate: o console registra `IOException: The process cannot access the file because it is being used by another process`, o jogo volta à exploração, o `savegame.json` fica **byte a byte igual** (`cmp`) e o `savegame.json.tmp` tem o estado novo. Depois, `((Stream)AppDomain.CurrentDomain.GetData("save89")).Dispose();` e `SetData("save89", null)`: o próximo `ChangeRoom` apaga o `.tmp` e grava o save. O primeiro save (sem arquivo anterior, caminho do `File.Move`) se prova apagando `savegame.json` e `savegame.json.tmp` pelo shell antes do `ChangeRoom`.
- **No `eval`, a resposta de um `ChangeRoom` vem na hora, mas o fim é o `PlayerInputGate.IsEnabled` voltar a `true`:** espere uns 4 s antes de ler o arquivo.

### Testar o menu por `eval` (#17)

Copie o `savegame.json` para fora do repositório antes (a verificação o sobrescreve) e devolva no fim. Espere ~10 s depois de cada `editor.playmode start` antes do primeiro `eval`; confira que o jogo avança (`Time.frameCount` duas vezes).

- **Os botões do título** são `Button` + `MenuButtonUI`, achados por caminho: `Canvas/Title Window/Left Panel/Midle/Button Icon` = NOVO JOGO, `Button Icon (1)` = CONTINUAR, `(2)` = OPÇÕES, `(3)` = SAIR. A `Confirm New Game Window` repete os nomes (`Button Icon` = NOVO JOGO, `Button Icon (1)` = VOLTAR), então **use o caminho inteiro**; `GameObject.Find` só acha objeto ativo (a confirmação, inativa, só aparece depois do clique em NOVO JOGO com save).
- **Clicar:** `GameObject.Find("<caminho>").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();` (o CLI não simula mouse). **Enter:** `UnityEngine.EventSystems.ExecuteEvents.Execute(go, new UnityEngine.EventSystems.BaseEventData(UnityEngine.EventSystems.EventSystem.current), UnityEngine.EventSystems.ExecuteEvents.submitHandler)`; um clique só, sem o aviso "já há uma troca de sala em curso" no console, prova que o botão tem um caminho de clique único.
- **A interface de jogo escondida:** `GameObject.Find("Canvas_Game").GetComponent<UnityEngine.Canvas>().enabled` é `false` no menu e `true` na sala. Para provar que ela não recebe clique, um `RaycastAll` do `EventSystem` no centro do `InventoryPanel` (`GetWorldCorners` no canvas Overlay já dá pixels de tela): no menu não acha o `InventoryPanel`, na sala acha.
- **O Continuar:** `Button.interactable` e a cor do `Button Label` (`disabledTextColor` quando desligado). **Save corrompido:** grave `{garbage` no `savegame.json` e dê Play: Continuar desligado, Novo Jogo direto. **Sair:** `EditorApplication.isPlaying` fica `false`.
- **O Novo Jogo e o Continuar são trocas de sala** (vários frames): no mesmo `eval` só o `PlayerInputGate.IsEnabled == false` vale; observe em outro `eval`.

### Ligar um `UnityEvent` por script de Editor (#17)

`UnityEventTools.AddVoidPersistentListener(button.onClick, componente.Metodo)` grava a ligação no `Button.onClick`, e `AddObjectPersistentListener<UIWindow>(evento, manager.OpenWindow, janela)` a do `OpenWindow` com argumento de objeto. Para um evento `[SerializeField] private UnityEvent<bool>` de um componente, pegue a instância por reflexão **do Editor** (`GetField(..., NonPublic | Instance).GetValue(componente)`), chame `UnityEventTools.AddPersistentListener(evento, alvo.MetodoBool)` (ligação **dinâmica**, `m_Mode: 0`: o `bool` vem de quem invoca) e termine com `EditorUtility.SetDirty` e `unity command scene save`. Confira no `.unity` por `grep` (`m_MethodName`, `m_Mode`). O nome completo (`UnityEditor.Events.UnityEventTools`) ou um `using` no topo do `.cs` funcionam (ver [O `eval` aceita `using`](#o-eval-aceita-using-e-importa-o-básico-189)).

### Testar a pausa por `eval` (#18)

Copie o `savegame.json` para fora do repositório antes (a verificação e a suíte o sobrescrevem) e devolva no fim. Espere ~10 s depois do `editor.playmode start` e confira que o jogo avança. **O `pm`** das receitas abaixo é `UnityEngine.Object.FindFirstObjectByType<ProjetoVN.GameFlow.Menu.PauseMenu>(UnityEngine.FindObjectsInactive.Include)`; um `script eval --file` (arquivo `.cs`) é mais cômodo que o `eval` para o que passa de uma linha.

- **Pausar e retomar:** `pm.TogglePause()` (o Esc) e `pm.Resume()`. A abertura é **síncrona**: no mesmo `eval` já valem a janela ativa (`pm.transform.Find("Pause Window").gameObject.activeSelf`), `PlayerInputGate.IsEnabled == false`, `Time.timeScale == 0`, `DialogueManager.Instance.IsPaused` e a seleção (`EventSystem.current.currentSelectedGameObject`, o `Button Continue`). Os botões se acham por caminho a partir do `pm`: `Pause Window/Panel/Button Continue|Button Save|Button Return` e `Confirm Return Window/Panel/Button Return|Button Back`; **clicar** é `.GetComponent<UnityEngine.UI.Button>().onClick.Invoke()` (o CLI não simula mouse).
- **O Esc de verdade só abre a pausa com a Game view em foco** (medido na revisão da #18; o modo do Input System e a falta de asset de `InputSettings` foram conferidos no levantamento da #124). O Esc enfileirado é `UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));` em um `eval` e, **em outro**, `new KeyboardState()` para soltar; uma aperta e solta abre, outra fecha, com um `sleep` de um segundo antes de ler o estado. Mas o Input System deste projeto está em `editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus` (o padrão do pacote: o projeto não tem asset de `InputSettings`), e **sem o foco da Game view a tecla chega** (`Keyboard.current.escapeKey.isPressed` fica verdadeiro) **mas o `UI/Pause` não dispara**.
  - **Confira o foco antes do primeiro Esc:** `UnityEngine.Application.isFocused`. Com `False`, a pausa não abre.
  - **Um Esc que funcionou não prova o seguinte.** O foco da Game view muda sozinho entre sessões e até no meio de uma (na revisão da #18 o Esc funcionou no começo e falhou depois; no levantamento da #124, sob o CLI, `isFocused` era `True` sem ninguém ter pedido).
  - **Quando o foco não está garantido, chame `pm.TogglePause()`** (é o que o Esc faz).
  - **Dar o foco** é possível por `unity command editor focus` ou `UnityEditor.EditorWindow.FocusWindowIfItsOpen(System.Type.GetType("UnityEditor.GameView,UnityEditor"))` (com o segundo, na revisão da #18, `isFocused` passou a `True` e o Esc abriu e fechou a pausa), mas os dois **roubam o foco de quem está usando o computador**: só com o programador de acordo. Não atribua `Application.runInBackground` como saída (D-31).
- **Mover a seleção pelo teclado (#96):** `ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject, new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Down }, ExecuteEvents.moveHandler)` é síncrono: a seleção nova já vale no mesmo `eval`. Com a conversa aberta o SALVAR está desabilitado, e "para baixo" do CONTINUAR tem de ir ao VOLTAR AO MENU (`VerticalButtonNavigation`). Leia o destaque pelo `decorateImage` do `MenuButtonUI` (`SerializedObject`, campo `decorateImage`) e pela cor do `label`. **O `OnEnable` do pai roda antes do dos filhos:** um componente de coluna que consulte `Selectable.IsActive()` dos botões no próprio `OnEnable` lê `false` em todos e grava vizinho `null`; use `gameObject.activeInHierarchy`.
- **Medir o texto parado:** a velocidade padrão (40 caracteres por segundo) termina `peguei_chave` em pouco mais de 1 s, antes de dar tempo de pausar. Em Play Mode, `SerializedObject` no `DialogueUIController`, campo `charactersPerSecond`, valor 5, `ApplyModifiedProperties()` (volta ao do prefab no Play seguinte) e leia `maxVisibleCharacters` do `dialogueText` (também por `SerializedObject`): com a pausa aberta o valor **não muda** em 2 s, e ao retomar segue de onde parou (medido: 16 de 44 pausado, 22 de 44 logo depois de retomar).
- **O fim da conversa por `StopDialogue()` é síncrono:** com uma conversa aberta, `DialogueManager.Instance.StopDialogue(); return DialogueManager.Instance.IsDialogueActive();` **no mesmo `eval`** devolve `false`. O `GoToMainMenu` depende disso (recusa fora da exploração).
- **Voltar ao menu:** `pm.TogglePause()`, o botão `Button Return`, o `Button Return` da confirmação (ou `pm.ReturnToMainMenu()` direto). É uma troca de sala: observe em outro `eval` (cena ativa `Menu`, `Canvas_Game` com `Canvas.enabled == false`, `timeScale == 1`, janelas inativas). **Prove que não salvou** lendo o `currentScene` do `savegame.json` (`System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "savegame.json"))`) antes e depois: continua a sala. No menu, `pm.TogglePause()` não abre nada e não loga.
- **O clique no fundo da pausa:** um `RaycastAll` do `EventSystem` em um ponto **dentro da tela** (`Screen.width` × `Screen.height`; no Editor sem a Game view maximizada é uma janela pequena, medida 766×431 contra os 1280×720 da captura) acha primeiro a `Pause Window` e depois o `InventoryPanel`. Um ponto fora da tela não acha a pausa: o centro do `InventoryPanel`, por exemplo, pode estar fora dela.
- **Sair do Play com a pausa aberta** não deixa o `timeScale` zerado: depois do `editor.playmode stop` o valor é 1 e o Play seguinte também começa em 1, e `git status ProjectSettings` fica vazio (medido na #18). Mesmo assim, **não chame `AssetDatabase.SaveAssets()` com o jogo pausado**.
- **A tela de verificação:** `capture game_view --source screen` mostra a pausa por cima de tudo (o `Canvas_Pause` tem ordem 50, acima do `Canvas_Debug`). Compare um pixel com e sem pausa para provar o escurecimento (medido: ~64% do brilho).

### Montar um canvas novo em um prefab por `script eval --file` (#18)

Foi assim que o `Canvas_Pause` entrou no `GameUI.prefab`: `PrefabUtility.LoadPrefabContents`, criar os objetos (`new GameObject(nome, typeof(RectTransform))`, `AddComponent`), ligar os `UnityEvent` (ver a receita acima) e `PrefabUtility.SaveAsPrefabAsset` + `UnloadPrefabContents`, tudo em `try`/`finally`. Pontos que custaram tempo:

- **O `SaveAsPrefabAsset` reordena o YAML inteiro** e o `git diff` mostra milhares de linhas, mas todos os objetos antigos continuam com o mesmo `fileID` e o conteúdo igual, exceto a lista de filhos da raiz. Confira comparando os blocos `--- !u!<tipo> &<fileID>` do arquivo antigo (`git show HEAD:<arquivo>`) com os do novo; confira também que o `Managers.prefab`, que aninha o `GameUI.prefab`, **não** mudou (`git status`).
- **Texto criado por script nasce sem quebra de linha** (`textWrappingMode` = NoWrap): uma frase longa passa da borda do painel. Ponha `TMPro.TextWrappingModes.Normal`.
- **`OnValidate` roda no `AddComponent`, com os `UnityEvent` ainda nulos:** um aviso de autoria que chama `GetPersistentEventCount()` precisa tratar `null` (o `PauseMenu` trata). Sem isso o `script eval --file` termina `ok` e deixa uma `NullReferenceException` no console.
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
- **Sair do Play devolve o `vSyncCount` e o `targetFrameRate`** (medido: `1 10` em Play, `0 -1` depois do `editor.playmode stop`), e o `git status` fica limpo. Por isso ler `0 -1` **fora** do Play não prova que o `TearDown` devolveu os valores; essa devolução só se observa dentro do Play. O código a garante: `RestoreFrameRate()` é a primeira instrução do `[UnityTearDown]`. Mesmo assim, **não chame `AssetDatabase.SaveAssets()`** com o `vSyncCount` alterado: `QualitySettings` é um asset de `ProjectSettings/`, como o `runInBackground` (D-31).
- **`Time.deltaTime` é limitado pelo `Time.maximumDeltaTime` (0,333 s); `Time.unscaledDeltaTime` não é.** Um travamento de 1,5 s deixou no quadro seguinte `dt = 0,333` e `udt = 1,566`. Código que anima por `unscaledDeltaTime` (o `ScreenFader`) recebe o passo inteiro; o que anima por `deltaTime` (o pan) recebe no máximo 0,333 s.
- **Provocar um quadro longo:** um laço com `System.Diagnostics.Stopwatch` na thread principal (no `eval` ou no teste); nada de thread nem `Thread.Sleep` (regra 9).
- **Os quadros de um `eval` para outro:** registre por `Application.onBeforeRender` e guarde a lista com `AppDomain.CurrentDomain.SetData`, para ler em outro `eval`. Sem a recarga de domínio, a lista e a assinatura sobrevivem ao Play: grave a lista no Play em que vai lê-la e remova a assinatura ao terminar ([#140](#o-play-mode-não-recarrega-o-domínio-140)).

#### O limite de FPS sem foco (#81)

O `UnfocusedFrameRateLimiter` (`Managers.prefab`) limita o jogo a 30 FPS e desliga o VSync quando a janela perde o foco (D-31). No Editor, **"foco" é o da Game view, não o da janela** (documentação do `OnApplicationFocus`).

- **Sob o CLI o limite não age.** Medido com o Chrome em primeiro plano e o Play Mode rodando: `Application.isFocused = True`, e um `Application.focusChanged` assinado por `eval` não recebeu evento algum. Por isso uma verificação dirigida pelo CLI continua nos FPS que ela mesma pediu (a suíte PlayMode da #80 inclusive). O limite entra quando alguém clica em outra aba do Editor (Inspector, Console, Scene) durante o Play.
- **Não clique no Editor durante a suíte PlayMode.** A perda de foco no meio de uma fixture a 10 FPS guarda `(0, 10)` e aplica `(0, 30)`, e o `FrameRate_IsTheOneThisFixtureAsked` reprova. Espere o `test status` dizer `completed`.
- **Simular a perda e o ganho de foco:** `UnityEngine.GameObject.Find("Managers").SendMessage("OnApplicationFocus", false);` (e `true`). Hoje só o `UnfocusedFrameRateLimiter` tem `OnApplicationFocus` (`grep` em `Assets/Scripts`); se outro componente do `Managers` ganhar um, mire só o limiter. Em seguida leia `QualitySettings.vSyncCount` e `Application.targetFrameRate` (esperado `0 30` sem foco e os valores de antes com o foco).
- **Medir o FPS:** um `eval` guarda `Time.frameCount` e `Time.realtimeSinceStartup` com `AppDomain.CurrentDomain.SetData`; ~5 s depois, um segundo `eval` calcula quadros por segundo. Grave os dois valores no mesmo Play em que vai lê-los: sem a recarga de domínio, o valor de um Play anterior continua lá ([#140](#o-play-mode-não-recarrega-o-domínio-140)). Nunca dentro de um `eval` só (ele roda em um quadro). Sem o limite, o Editor sem foco faz ~360 a 470 FPS; limitado, 30,0. Uma janela de medição que pega a transição entre dois estados dá um valor intermediário (visto 169 uma vez, não reproduzido): repita a medição antes de concluir.
- **Ajustar o campo em Play:** `SerializedObject` no `UnfocusedFrameRateLimiter`, campo `unfocusedFrameRate`, e `ApplyModifiedProperties()`. É lido na hora da perda de foco, e volta ao do prefab no Play seguinte.
- **Não chame `AssetDatabase.SaveAssets()` com o `vSyncCount` alterado** (como na #80): `QualitySettings` é um asset de `ProjectSettings/`. No Editor, o `vSyncCount` não manda (só o `targetFrameRate` limita a Game view), então a verificação confere a devolução do par, não o efeito do VSync.
- **O que o CLI não faz:** trocar o foco de janela. O efeito real (alt-tab, minimizar, segundo monitor, uso de GPU no Gerenciador de Tarefas) fica para o programador, numa build de Windows.

### Testar um campo de texto (`TMP_InputField`) por `eval` (#26)

O CLI não digita nem clica, mas dá para exercitar o campo da folha do nome (`ProtagonistNameSheet`) de vários jeitos. Ache o campo por `SerializedObject` no `DialogueUIController` (`protagonistNameSheet`) e no componente (`nameField`, `signButton`); com `$` no nome da variável, use `script eval --file`.

- **`field.text = "..."` não passa pelo `onValidateInput`.** Só o que o jogador digita ou cola é filtrado. Para testar o filtro, chame `field.onValidateInput(textoAtual, posição, caractere)` direto: devolve o caractere ou `'\0'` (recusado).
- **`field.ProcessEvent(new UnityEngine.Event { type = UnityEngine.EventType.KeyDown, character = 'W' })` percorre o caminho real do `Append`** (filtro, posição, seleção) e **funciona**: com o campo focado e o cursor no fim (`caretPosition`, `selectionAnchorPosition` e `selectionFocusPosition`), um caractere permitido entra e um recusado não. Para o Escape, use `keyCode = UnityEngine.KeyCode.Escape` (e confira que o texto não voltou ao de antes).
- **Enter e botão:** `field.onSubmit.Invoke(field.text)` e `button.onClick.Invoke()`.
- **Colar (#213):** `field.ProcessEvent(new UnityEngine.Event { type = UnityEngine.EventType.KeyDown, keyCode = UnityEngine.KeyCode.V, modifiers = UnityEngine.EventModifiers.Control })` lê o `GUIUtility.systemCopyBuffer` e chama o `Append(char)` caractere por caractere, então o filtro vale para cada um. O clipboard é o do computador de quem usa o Windows: guarde-o e devolva-o **no mesmo `eval`**, em `try`/`finally`, e confira `GUIUtility.systemCopyBuffer == guardado` depois.
- **Selecionar tudo (#213):** atribuir `selectionAnchorPosition` e `selectionFocusPosition` **não** reproduz uma seleção de verdade. O TMP decide o que remover do texto do filtro pelos índices de caractere internos (`m_CaretPosition`), que só se atualizam com a seleção feita pelo próprio campo; atribuídas as duas propriedades, o `Append` trata o campo como sem seleção (medido: `Lucas` selecionado por atribuição + espaço virou `'Lucas '`, e o filtro direto recusava). `SelectAll()` é `protected`. Para o caso "tudo selecionado", mande o Ctrl+A por `ProcessEvent` (`keyCode = KeyCode.A`, `modifiers = EventModifiers.Control`): aí a próxima tecla substitui o texto, e o filtro recebe `("", 0)`.
- **O `GetPreferredValues` do TMP não conta o espaço do fim (#213).** Medido na placa de nome (193 px): `'WWWWWW'`, `'WWWWWW '` e `'WWWWWW     '` dão os mesmos 180,74 px; o espaço do começo e o do meio contam. Medir o texto do campo sem normalizar não limita os espaços do fim.
- **O Esc de verdade (`QueueStateEvent`, receita da pausa, #18) não dispara a ação `UI/Pause` se a Game view não tem o foco.** O estado da tecla chega (`Keyboard.current.escapeKey.isPressed` fica verdadeiro), mas o Input System deste projeto está em `editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus` e `Application.isFocused` era falso sob o CLI na #26. Foi o que se viu ali, com e sem a folha aberta, e a causa está confirmada: na revisão da #18, com `FocusWindowIfItsOpen` na Game view, `isFocused` passou a `True` e o Esc abriu a pausa (ver a receita da pausa). Para pausar nesses casos, chame `PauseMenu.TogglePause()`; o `editor focus` e o `FocusWindowIfItsOpen` trazem a janela para a frente, o que rouba o foco de quem está usando o computador, então só os use com o programador de acordo.

### Testar a música por `eval` (#19)

Copie o `savegame.json` para fora do repositório antes (a verificação e a suíte o sobrescrevem) e devolva no fim. Espere ~10 s depois do `editor.playmode start` e confira que o jogo avança. O CLI não ouve: a verificação lê `isPlaying`, `volume` e `time` das `AudioSource`; a suavidade da troca e o loop sem estalo ficam para o programador, com fone.

- **O estado de uma vez:** `AudioManager.Instance.CurrentMusic` e `AudioManager.Instance.transform.Find("Music").GetComponentsInChildren<AudioSource>(true)` (as duas fontes do filho `Music` do `Managers.prefab`; um `GetComponentsInChildren` direto no manager devolve as cinco, também as do ambiente e dos efeitos, desde a #20). Leia `clip`, `isPlaying`, `volume`, `time` e `outputAudioMixerGroup.name` (`Music`). Em um `script eval --file` (arquivo `.cs`) cabe tudo numa linha de saída.
- **Não leia `AudioSource.time` de uma fonte sem clipe:** o Unity loga `Attempting to get time on an audio source that has a resource that is not a clip` e polui a conferência de "console sem aviso". Confira `clip != null` antes.
- **Ver a troca em câmera lenta:** em Play Mode, `SerializedObject` no `AudioManager`, campo `crossfadeSeconds` (4 dá 3 ou 4 leituras no meio; 12 dá tempo de abrir a pausa), e `ApplyModifiedPropertiesWithoutUndo()`. O valor volta ao do prefab no Play seguinte e o `Managers.prefab` não fica alterado. Cada leitura pelo CLI leva ~1,5 a 2 s; no meio da troca os dois volumes andam em sentidos opostos com `entrada² + saída² ≈ 1` (medido: 0,68 e 0,74).
- **A música reiniciou ou continuou?** `AudioSource.time` dá a volta no loop (8 s nas faixas de teste), então um "maior que antes" mente. Leia `Time.realtimeSinceStartup` junto com o `time` nas duas leituras e confira `(time_antes + tempo_real_decorrido) mod duração ≈ time_depois` (medido ao recarregar a sala: diferença de 0,01 s sobre 8,7 s).
- **`AudioSource.time` anda em degraus do tamanho do bloco de áudio** (medido na revisão da #19: 0,0213 s, 1024 amostras a 48 000 Hz). Com o Editor a centenas de quadros por segundo, um ou dois quadros leem o mesmo valor, então "esperei um quadro e o `time` cresceu" é teste instável. Os testes do `MusicTests` leem o `time` de partida só longe das pontas do loop (`WaitUntilTimeIsAwayFromTheLoopEnds`: nem perto do começo, onde um reinício passaria por avanço, nem perto do fim, onde a volta do loop faria o `time` diminuir) e esperam em tempo real mais que um bloco (`WaitForSecondsRealtime`) antes de comparar.
- **Trocar de sala, voltar ao menu e carregar** são as receitas de "Trocar de sala por `eval`", "Testar a pausa por `eval`" e "Testar o menu por `eval`". Na volta ao menu, `CurrentMusic` fica nulo na chegada, a fonte que tocava cai a zero e para; no menu não há `RoomMusic`.
- **O `sceneLoaded` e o `AudioListener`** (medido no levantamento, troca `[Teste] Mecanicas` → `[Teste] Sala B`): o `sceneUnloaded` da sala antiga e o `sceneLoaded` da nova saem no mesmo quadro, o `sceneCount` é 1 no `sceneLoaded`, a cena recebida já tem o `AudioListener` dela e todo quadro desenhado teve exatamente um. Por isso o `Managers.prefab` não leva `AudioListener`.
- **Importar uma faixa por `script eval --file`:** `AudioImporter.preloadAudioData` é obsoleto; o campo está em `defaultSampleSettings` (`SampleSettings.preloadAudioData`). Atribua o struct inteiro de volta (`importer.defaultSampleSettings = settings`) e chame `SaveAndReimport()`.
- **Compilar foi lento (medido na #19, nesta máquina):** um `script recompile` depois de mexer nos scripts do `Audio` e dos testes levou ~4 min até o `script recompile_status` dizer `completed`, e a `test run` seguinte, ~5 s. Não adianta repetir o comando; escreva tudo o que precisa compilar antes de pedir (a TDD em ciclos pequenos custa caro aqui), e rode a compilação em segundo plano.

### Testar os sons por `eval` (#20)

Copie o `savegame.json` para fora do repositório antes (a verificação e a suíte o sobrescrevem) e devolva no fim. Espere ~10 s depois do `editor.playmode start` e confira que o jogo avança. O CLI não ouve: a verificação lê o estado do `AudioManager` e das `AudioSource`; o timbre, a suavidade e o loop sem estalo ficam para o programador, com fone.

- **`isPlaying` não prova que um `PlayOneShot` tocou (medido na #20, Play Mode no `Menu`):** um clipe de 2 s com `PlayOneShot` dá `isPlaying = True` no mesmo quadro e `False` cerca de 1 s depois (`time = 0`), com o clipe ainda no meio. Por isso o `AudioManager` expõe **`LastEffect`** e **`EffectCount`**: leia `EffectCount` antes e depois da ação, e `LastEffect.name`. Vale para hover, clique, avanço de fala, coleta, porta e `<<tocar_efeito>>`.
- **O `EffectCount` é global (#197):** ele soma o pedido de qualquer sistema, inclusive o hover de um botão que apareceu sob o ponteiro (ver "Botão que abre em destaque sem ninguém ter tocado", em [Outras armadilhas](#outras-armadilhas-do-projeto)), e esse hover cai num quadro qualquer, também com o Editor sem foco. Para provar que uma ação síncrona **não** soa, leia a contagem, chame a ação e compare **no mesmo quadro** (o mesmo `eval`, ou o mesmo trecho de teste sem `yield`); quando a espera for inevitável, confira também o `LastEffect.name`, que diz se foi `fala_avancar` ou um `interface_hover`.
- **O ambiente:** `CurrentAmbience` e as duas fontes do filho `Ambience` (`am.transform.Find("Ambience").GetComponentsInChildren<AudioSource>(true)`): `clip`, `isPlaying` e `volume`. Entrando, o volume sobe a 1; `StopAmbience` leva a 0 e para a fonte em ~1 s (cada leitura pelo CLI levava ~1,5 s na 0.7.0 e leva ~2,5 s na 1.0.0, então a saída costuma já ter terminado na primeira). **Não leia `AudioSource.time` de uma fonte sem clipe** (loga e suja o console).
- **Hover e clique:** `ExecuteEvents.Execute(botao, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler)` e `button.onClick.Invoke()`; valem com `Time.timeScale == 0`. Um botão desabilitado (`interactable = false`) não soa ao passar o mouse.
- **Os comandos por chamada direta:** `ProjetoVN.GameFlow.SoundScriptActions.PlayAmbience("onibus_estrada")`, `StopAmbience()`, `PlayMusic("teste_musica_b")`, `PlayEffect("freada_batida")`. Um id inexistente deixa um erro no console (o esperado da verificação) com "nó '?'" fora de conversa.
- **Percorrer o prólogo:** `DialogueManager.Instance.StartDialogue("onibus_viagem")` (o Novo Jogo leva ao `[Fatia] Onibus`, onde ele abre sozinho na chegada, pelo nó `onibus_chegada` do `Room Arrival`, #127; a poltrona só mostra um pensamento) e, um `eval` por fala, `AdvanceDialogue()`, lendo `CurrentAmbience` e `LastEffect`: o ambiente entra na primeira fala, some na lembrança (primeira fala do Pai), volta em "Espero ter feito a escolha certa, mãe..." e some em "Que porra é essa?!", com `LastEffect.name == "freada_batida"` lido **antes** do último avanço (o avanço soa depois e sobrescreve o `LastEffect`).
- **Sair do Play** deixa `git status` limpo (sem `ProjectSettings/`) se ninguém atribuiu `runInBackground`.

### Testar os efeitos de tela por `eval` (#71)

Copie o `savegame.json` para fora do repositório antes (a verificação e a suíte o sobrescrevem) e devolva no fim. Abra o `Menu.unity` antes do Play (todas as cenas limpas, `scene list_open`), espere ~10 s e confira que o jogo avança. Tudo que passa de uma linha, ou tem `$`, vai em `script eval --file`.

- **O estado de uma vez:** `Object.FindFirstObjectByType<ProjetoVN.GameFlow.ScreenEffects.ScriptScreen>(FindObjectsInactive.Include)` e, nos filhos, `transform.Find("Darkness")` e `transform.Find("Picture")` (a `Picture` fica **inativa** sem imagem: leia o `activeSelf` antes do alfa). O alfa é o do `CanvasGroup` de cada um; o do fade da troca é o do `ScreenFader`.
- **Um `eval` não vê um efeito de 3 s (~2,5 s de ida e volta com a 1.0.0, eram ~1,5 s):** para "o que aconteceu em cada quadro" instale um registrador em `Application.onBeforeRender` num `script eval --file`: uma `List<float[]>` guardada por `AppDomain.CurrentDomain.SetData("rec.log", ...)` (e o `UnityAction` em `"rec.handler"`, para a instalação seguinte remover o anterior), com `Time.frameCount`, o alfa do fade, o do escuro, o da `Picture` (ou `-1` se inativa), `IsDialogueActive()`, `Time.realtimeSinceStartup` e se a cena ativa é a esperada. Grave antes de ler e **remova a assinatura ao terminar** ([#140](#o-play-mode-não-recarrega-o-domínio-140)): `Application.onBeforeRender -= (UnityAction)AppDomain.CurrentDomain.GetData("rec.handler")`. **Esquecer a remoção reprova a suíte PlayMode inteira** (medido na #71: 55 de 55 com `Unhandled log message: MissingReferenceException ... CanvasGroup`, com a pilha em `PipelineEvaluation.PipelineEval_...`), porque o registrador sobrevive ao Play e, no seguinte, lê os `CanvasGroup` do Play anterior, já destruídos. Medido na #71: do quadro em que o ônibus fica ativo até a conversa abrir, `max(fade, escuro)` foi **1,000** em todos; no primeiro quadro com conversa, fade 0 e escuro 1,000; o escuro levou 3,00 a 3,04 s (90 quadros a 30 FPS sem foco) para chegar a 0.
- **Para clicar, pausar ou capturar no meio de um efeito, desacelere só em runtime:** `Time.timeScale = 0.15f` por `eval` antes do `StartNewGame` (o efeito usa tempo escalado; o fade da troca não) e `1f` no fim. Com ele, um clarear de 3 s dura ~20 s e dá para `AdvanceDialogue()` três vezes (o `AudioManager.EffectCount` não sobe e o alfa do escuro segue descendo), `PauseMenu.TogglePause()` (o alfa fica igual entre duas leituras com 2 s de intervalo; `Resume()` devolve o `timeScale` de antes, aqui 0,15) e `capture game_view --source screen --width 1920 --height 1080` (o `Canvas_Pause` aparece por cima do escuro). Termine **sempre** devolvendo `timeScale = 1`.
- **`StartNewGame` e `ChangeRoom` recusam com uma conversa aberta** (devolvem `false`, e o motivo vai ao console só como aviso): numa verificação que repete o Novo Jogo, `DialogueManager.Instance.StopDialogue()` antes. A resposta `false` enganou um passo da #71 (a leitura seguinte mostrou o estado da conversa anterior).
- **Avançar o prólogo depressa:** `SerializedObject` no `DialogueUIController`, campo `charactersPerSecond` = 1000, para uma fala terminar de aparecer entre dois `eval`; um `AdvanceDialogue()` por `eval`. A lembrança começa depois do **quarto** `AdvanceDialogue()` (escurece em 1,5 s, a `Picture` entra a alfa 1 por baixo do preto, o escuro cai em 1,5 s) e o retorno ao ônibus depois de mais **quinze** (a `Picture` cai de 1 a 0 em ~1,8 s e desativa).
- **Encerrar a conversa no meio de um efeito:** `PauseMenu.ReturnToMainMenu()` (receita da pausa) com a imagem na tela. No `Menu`, o escuro é 0, a `Picture` está inativa, o `Canvas_Game` está desligado e o console não ganhou erro nenhum.
- **A volta ao ônibus com o prólogo feito:** `StoryState.SetBool("$prologo_concluido", true)` num `script eval --file`, `ChangeRoom("[Fatia] Posto")`, e depois `ChangeRoom("[Fatia] Onibus", "porta")` com o registrador: o fade vai de 1 a 0 em ~4 quadros com o escuro em 1, a conversa abre e fecha no mesmo quadro (o `IsDialogueActive` nunca aparece no registro) e o escuro cai a 0 em ~16 quadros (0,4 s a 30 FPS).
- **Um roteiro com erro de conteúdo** ([receita](#testar-um-roteiro-quebrado-sem-criar-arquivo)): `<<mostrar_imagem nao_existe>>` loga `[ScreenScriptActions] <<mostrar_imagem nao_existe>>: a imagem 'nao_existe' não existe no PictureRegistry (nó 't_img'; as que existem: lembranca_pais). Ignorado.` e a fala seguinte aparece; `<<escurecer -1>>` loga a duração negativa com o nó e vale 0 (o escuro entra na hora e some quando a conversa termina). Troque o `yarnProject` do runner de volta ao `Roteiro.yarnproject` no fim.
- **Uma instância de cada** (um `ScriptScreen`, um `ScreenScriptActions`, um `EventSystem`) por `FindObjectsByType`; dois Plays seguidos, **sem recompilar** entre eles (D-29), repetem o Novo Jogo igual e dão outro `GetInstanceID()` do `ScreenScriptActions.Instance`.
- **Um efeito que termina depois da conversa derruba o runner.** Se a tarefa de um comando de efeito só completa *depois* de a conversa acabar, o `DialogueRunner` chama `Dialogue.Continue()` com o `dialogueCancellationSource` já zerado e loga `DialogueException: Cannot continue running dialogue. No node has been selected.`, que reprova o teste PlayMode que estiver rodando (`Unhandled log message`). Por isso o `ScriptScreen` cancela o efeito em curso no `DialogueEndedMessage` (re-anima as camadas). Funciona porque o cancelamento completa a tarefa **na hora, dentro da publicação da mensagem**, e o runner só zera o `dialogueCancellationSource` logo depois do `onDialogueComplete`, na mesma chamada; um cancelamento que completasse no quadro seguinte não protegeria. Para conferir: num `script eval --file` em Play, `var t = screen.DarkenAsync(5f);`, `MessageBroker.Publish(new DialogueEndedMessage());` e leia `t.IsCompleted` na mesma chamada: `True` (medido na revisão da #71; antes do `Publish`, `False`). Medido sabotando o `OnDialogueEnded` na #71: os quatro testes de `RoomArrivalTests` reprovaram.
- **Antes do commit:** o Play suja os font assets (o `ClearFontAssetData(true)` de [Outras armadilhas](#outras-armadilhas-do-projeto), #113), e, com `capture game_view --output-path` fora de `Assets/` (a recomendada desde a 1.0.0), a captura não deixa `.meta` nem pasta. Se algum `Assets/Temp.meta` aparecer, é de uma captura gravada dentro de `Assets/`: apague a pasta com `asset delete` e confira que o `git status` não o mostra.

### Montar cenas por `script eval --file` e percorrer a fatia vertical (#21)

As salas `[Fatia] Onibus` e `[Fatia] Posto` foram montadas por scripts de Editor (`script eval --file`), com o mesmo resultado serializado que o Inspector. Pontos que custaram tempo:

- **`EditorSceneManager.NewScene` (modo `Single`) descarrega os assets que nenhuma cena usa.** Um `ScriptableObject` ou sprite carregado **antes** dele vira "nulo falso": `SerializedProperty.objectReferenceValue = asset` grava `{fileID: 0}` e nenhum erro aparece (aconteceu com o item da moeda, `Moeda.asset`, nas duas cenas da #21; o asset saiu na #130). Carregue o asset **depois** de criar a cena, e depois de salvar confira por `grep` (`itemData`, `requiredItem`, `clip`, `music`). **E fecha as cenas abertas, descartando o que não foi salvo:** confira antes (ver [Abrir e salvar cena](#abrir-e-salvar-cena)).
- **Instancie o prefab com `PrefabUtility.InstantiatePrefab`**, nunca com `Instantiate`, para o objeto continuar sendo uma instância do `Interactable`. Os campos privados se escrevem por `SerializedObject` e as ligações de `UnityEvent` pela receita de [Ligar um `UnityEvent` por script de Editor](#ligar-um-unityevent-por-script-de-editor-17); aqui os eventos da `InteractableItem` (`OnInteract`) e do `LockedActionBehaviour` (`OnLocked`, `OnUnlocked`, `OnOpened`, `OnAlreadyUnlocked`) são campos **públicos** e se acessam direto no `script eval --file`; a reflexão do Editor fica para o campo `[SerializeField] private`, como na receita da #17.
- **Vários componentes do mesmo tipo no mesmo objeto** (os dois `InteractableDialogueTrigger` do `Door_PortaTrancada` da `[Teste] Mecanicas`): ligue cada evento à instância certa (a variável que guarda o `AddComponent`) e confira no `.unity` que os `m_Target` dos eventos são diferentes.
- **Salvar:** `EditorSceneManager.SaveScene(cena, caminho)` gravou as cenas novas e `SaveScene(cena)` as alterações do `Menu.unity`; confira o retorno `True` e o `git status`.
- **Os nomes de tipo no `eval`:** `PlayerInputGate` está em `ProjetoVN.PointNClick`, `DialogueManager` em `ProjetoVN.Dialogue.Logic`, `DialogueUIController` e `DialogueHistoryView` em `ProjetoVN.Dialogue.UI`, `StoryState` em `ProjetoVN.Core.State` (`Bools`, `Numbers` e `Texts` são só de leitura), `InventoryManager` em `ProjetoVN.Inventory`.

**Percorrer a fatia em Play Mode** (a receita da verificação da #21; copie o `savegame.json` para fora do repositório antes e devolva no fim):

- **Clicar num objeto:** `GameObject.Find("Porta_Onibus").GetComponent<ProjetoVN.PointNClick.InteractableItem>().OnClick()`. Um objeto desativado (o `Item_Chave` da `[Teste] Mecanicas` depois de coletado) não é achado por `Find`: isso é o que se confere.
- **Texto e quem fala:** leia `dialogueText` e `speakerNameText` do `DialogueUIController` por `SerializedObject` (`TMP_Text`). Com `charactersPerSecond` em 1000 (só em Play) cada fala pede **um** `AdvanceDialogue()` por `eval`, e a conversa terminou quando `IsDialogueActive()` é falso.
- **O prólogo inteiro:** do Novo Jogo (`MainMenu.StartNewGame()`; o prólogo abre sozinho quando a tela clareia, e com `fadeSeconds` em 3 isso leva uns 6 s) até as opções do motorista são 32 `AdvanceDialogue()` (um `eval` cada, ~1 s de espera; medido na #127, igual ao que eram a partir do clique na poltrona); aí `MakeChoice(0)` vai direto à folha do nome (`<<assinar_lista>>`), que se assina com a receita de [Testar um campo de texto](#testar-um-campo-de-texto-tmp_inputfield-por-eval-26).
- **As opções:** os botões estão em `choiceButtonObjects` e os textos em `choiceTexts` (`SerializedObject` do `DialogueUIController`); uma opção condicional que não vale aparece com `Button.interactable == false`.
- **O histórico:** `DialogueUIController.OpenHistory()` e leia os `TMP_Text` ativos do `DialogueHistoryView` (cada balão traz o nome em negrito e a fala; a posição X distingue esquerda e direita). Feche pelo `Button` do próprio `DialogueHistoryView`.
- **A tela de personagens:** os `Image` ativos do `CharacterStage` mostram quem está em cada lado (`LeftSlot:luna_neutra`, `RightSlot:protagonista_neutra`).
- **Voltar ao menu e começar de novo:** `PauseMenu.ReturnToMainMenu()` (vale mesmo com a pausa fechada) e `MainMenu.StartNewGame()` / `ContinueGame()`; cada um é uma troca de sala (~6 s). O save só muda quando o jogador **chega** a uma sala: o Continuar volta ao estado da última chegada, sem o que aconteceu depois (a compra com a atendente, por exemplo).
- **Console:** `console clear` depois de entrar no Play e leia os erros no fim: o esperado é zero erros e zero avisos.
- **`EventSystem` e `InMemoryVariableStorage`:** um e zero, em cada sala.

### Montar uma sala só a partir do guia e conferi-la com o despejo (#107)

A `[Fatia] Posto` foi remontada por um agente que só tinha o `salas.md` e esta página, numa cena descartável, e conferida contra a real. O que a montagem por script precisa saber, e que o guia de salas não precisa repetir:

- **Os tipos e os campos.** Um componente de sala se preenche por `SerializedObject` (o nome do campo é o do código, em `camelCase`, e não o rótulo do Inspector) e os eventos por `UnityEventTools.AddVoidPersistentListener(evento, alvo.Metodo)`, direto, sem reflexão (os eventos de sala são públicos). Os nomes completos:

  | Componente (rótulo no Inspector) | Tipo | Campos |
  |---|---|---|
  | Point N Click Selector | `ProjetoVN.PointNClick.PointNClickSelector` | nenhum |
  | Screen Pan Controller | `ProjetoVN.PointNClick.ScreenPanController` | `environmentContainer` (Transform), `background` (Transform), `panSpeed`, `edgeThreshold` |
  | Interactable Item | `ProjetoVN.PointNClick.InteractableItem` | evento público `OnInteract` |
  | Interactable Dialogue Trigger | `ProjetoVN.GameFlow.InteractableDialogueTrigger` | `nodeName`; método `TriggerDialogue` |
  | Locked Action Behaviour | `ProjetoVN.GameFlow.LockedActionBehaviour` | `requiredItem`, `requiredFlagId`, `unlockedFlagId`; eventos públicos `OnLocked`, `OnUnlocked`, `OnOpened`, `OnAlreadyUnlocked`; método `Interact` |
  | Hidden Until Story Flag | `ProjetoVN.GameFlow.HiddenUntilStoryFlag` | `requiredFlagId` (a variável que mostra o objeto; sem eventos nem métodos) |
  | Room Entry Point | `ProjetoVN.GameFlow.Rooms.RoomEntryPoint` | `id` |
  | Room Exit | `ProjetoVN.GameFlow.Rooms.RoomExit` | `destinationRoom`, `entryPointId`; método `Leave` |
  | Room Music | `ProjetoVN.Audio.RoomMusic` | `music` |
  | Sound Effect | `ProjetoVN.Audio.SoundEffect` | `clip`; método `Play` |
  | Collectable Item Behaviour | `ProjetoVN.Inventory.CollectableItemBehaviour` | `itemData`, `persistentId` (o componente gera o id; confira que veio preenchido e é novo, como em [Coletável](../autoria/salas.md#coletável)); método `Collect`. Não montado na #107: o nome dos campos vem do código |

- **A câmera** não nasce com a tag: `go.tag = "MainCamera"` é do script. O **Universal Additional Camera Data** também precisa de `AddComponent` (o Inspector o acrescenta sozinho, o script não). O que o Inspector chama de **Background Type** é `Camera.clearFlags`.
- **O console avisa de campo vazio durante a montagem.** Cada `AddComponent` dispara o `OnValidate` do componente antes de o script preenchê-lo, e o console enche de avisos amarelos (`Id` vazio, **Destination Room** vazio, **Clip** vazio). Eles somem quando o campo é preenchido e a cena é salva: `console clear` no fim e releia, não conclua nada do console no meio.
- **`NewScene` com `NewSceneSetup.EmptyScene`** devolve a cena sem câmera nem luz. Carregue os assets **depois** dela (ver a seção anterior). Ela também fecha as cenas abertas sem perguntar: confira o `isDirty` antes ([Abrir e salvar cena](#abrir-e-salvar-cena)).
- **A suíte de EditMode vê a cena montada** mesmo fora da lista do build: com ela salva em `Assets/Scenes/`, um `nodeName` inexistente e um `entryPointId` inexistente fizeram o teste de nós de roteiro (`DialogueTriggerNodesTests` na época, hoje o `ScriptNodeFieldsTests`, #127) e o `RoomLinksTests` falharem, citando a cena e o objeto.
- **`ScreenPanController.CenterOn(worldX)` é relativo à posição atual do cenário** (o novo X é `containerX + cameraX - worldX`): volte o cenário a 0 antes de conferir o lugar de um `Room Entry Point`, senão a conta sai deslocada pelo X em que o pan o deixou.

**Conferir a sala montada contra uma sala de referência: o despejo.** Percorre cada cena e escreve, por objeto, o caminho, a posição, a escala, o prefab de origem e todos os campos dos componentes (os alvos de evento levam o índice do componente entre os do mesmo tipo, `#0`, `#1`, `#2`). Grave o script num `.cs` **fora do repositório**, rode com `unity command script eval --file <caminho> --timeout 60` e compare com `diff Temp/referencia.txt Temp/montada.txt` (`Temp/` é ignorada pelo git). O `if (!wasLoaded)` importa: sem ele, despejar a cena que está aberta no Editor a fecha.

```csharp
var pairs = new[] {
    ("Assets/Scenes/<cena de referência>.unity", "Temp/referencia.txt"),
    ("Assets/Scenes/<cena montada>.unity", "Temp/montada.txt") };
var skip = new System.Collections.Generic.HashSet<string> { "m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset", "m_GameObject", "m_EditorHideFlags", "m_EditorClassIdentifier", "m_Script" };
foreach (var (scenePath, outPath) in pairs)
{
    var already = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
    bool wasLoaded = already.isLoaded;
    var scene = wasLoaded ? already : UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
    var sb = new System.Text.StringBuilder();
    System.Action<UnityEngine.Transform, string> walk = null;
    walk = (t, prefix) =>
    {
        string path = prefix + t.name;
        var go = t.gameObject;
        string prefab = UnityEditor.PrefabUtility.IsPartOfPrefabInstance(go) ? " [prefab:" + UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) + "]" : "";
        sb.AppendLine("GO " + path + " active=" + go.activeSelf + " tag=" + go.tag + " pos=" + t.localPosition.ToString("F2") + " scale=" + t.localScale.ToString("F2") + prefab);
        foreach (var c in go.GetComponents<UnityEngine.Component>())
        {
            if (c == null) { sb.AppendLine("   MISSING SCRIPT"); continue; }
            if (c is UnityEngine.Transform) continue;
            sb.AppendLine("   C " + c.GetType().Name);
            var it = new UnityEditor.SerializedObject(c).GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = it.propertyType == UnityEditor.SerializedPropertyType.Generic;
                if (skip.Contains(it.name)) continue;
                string v;
                switch (it.propertyType)
                {
                    case UnityEditor.SerializedPropertyType.ObjectReference:
                        var o = it.objectReferenceValue;
                        v = o ? o.name + "(" + o.GetType().Name + ")" : "null";
                        if (o is UnityEngine.Component comp) v += "#" + System.Array.IndexOf(comp.GetComponents(comp.GetType()), comp);
                        break;
                    case UnityEditor.SerializedPropertyType.Float: v = it.floatValue.ToString("F2"); break;
                    case UnityEditor.SerializedPropertyType.Integer: v = it.intValue.ToString(); break;
                    case UnityEditor.SerializedPropertyType.Boolean: v = it.boolValue.ToString(); break;
                    case UnityEditor.SerializedPropertyType.String: v = "\"" + it.stringValue + "\""; break;
                    case UnityEditor.SerializedPropertyType.Enum: v = it.enumValueIndex >= 0 && it.enumValueIndex < it.enumDisplayNames.Length ? it.enumDisplayNames[it.enumValueIndex] : it.intValue.ToString(); break;
                    case UnityEditor.SerializedPropertyType.Vector2: v = it.vector2Value.ToString("F2"); break;
                    case UnityEditor.SerializedPropertyType.Vector3: v = it.vector3Value.ToString("F2"); break;
                    case UnityEditor.SerializedPropertyType.Color: v = it.colorValue.ToString(); break;
                    case UnityEditor.SerializedPropertyType.Generic: v = it.isArray ? "[" + it.arraySize + "]" : ""; break;
                    default: v = it.propertyType.ToString(); break;
                }
                sb.AppendLine("      " + it.propertyPath + " = " + v);
            }
        }
        foreach (UnityEngine.Transform child in t) walk(child, path + "/");
    };
    foreach (var root in scene.GetRootGameObjects()) walk(root.transform, "");
    if (!wasLoaded) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
    System.IO.File.WriteAllText(outPath, sb.ToString());
}
return "ok";
```

Toda diferença do `diff` é um achado (o guia não disse o valor), uma liberdade (nome de um objeto auxiliar) ou um erro de quem montou.

### A captura de 1280×720 some com linha fina

A Game view do Editor costuma ter 1280×720, abaixo dos 1920×1080 da interface, e a captura (`capture game_view --source screen`) reduz tudo a 0,67×. Uma linha de 2 px vira uns 1,3 px e **desaparece em algumas posições** (na folha do nome, duas das cinco pautas sumiram). Use 3 px ou mais para linha fina, ou confira em Full HD.

### Contar o que está visível

O painel de inventário reaproveita slots: um slot devolvido é desativado, não destruído. `transform.childCount` não diz quantos itens estão visíveis. Conte os filhos ativos.

### Gerar a build de Windows (#22)

O roteiro de [ponta-a-ponta.md](ponta-a-ponta.md) roda numa build de Windows 64. Gere-a pelo **Editor aberto** (o `unity build` de linha de comando abre um segundo Editor em batch e não roda com o projeto aberto), fora do Play Mode e com a cena salva (a build usa as cenas do disco):

```bash
unity command build run --target StandaloneWindows64 --output-path "Builds/Windows/ProjetoVN.exe" --confirm true
```

```bash
unity command build status
```

**Os nomes da 1.0.0** (#189): era `build --outputPath` e `build_status`, agora `build run --output-path` e `build status`; a #189 conferiu o comando com `--dry-run true` (`status: dry_run`, `valid: true`, as seis cenas do `EditorBuildSettings` e `build status` em `idle`) e a #186 gerou a build de verdade. O comando volta na hora com `status: queued` e um `buildId`; repita o `build status` até `completed` e confira `result: Succeeded` e `totalErrors: 0`. Medido na #186 (Editor aberto, depois das duas suítes, seis cenas): **27 s** (`buildTimeMs` 26 835), 181 MB (`totalSizeBytes` 181 066 013), `totalWarnings: 0`. Os 13 s e 73 s da #22 eram de outro comando e de outro estado do cache; a build **sem** cache nunca foi medida com o comando novo. O aviso do pacote Pipeline da #22 (`No RuntimePipelineConfig asset found… Pipeline will be disabled in Player builds`) **não aparece mais** no relatório, mas o efeito continua: **o CLI não alcança a build**, nada de `eval` dentro do jogo. A resposta do `build status` passa de 800 KB (lista de arquivos e de assets empacotados): filtre pelos campos (`unity command build status | grep -E '^(status|result|buildTimeMs|totalSizeBytes|totalWarnings|totalErrors):'`, seis linhas). O `| head` não adianta: a lista `files:` vem numa linha só e a saída continua com dezenas de KB.

- **É uma build Release**, mesmo com `EditorUserBuildSettings.development = True` neste Editor: sem `--options`, o comando não liga `Development` (o `boot.config` fica sem `player-connection` e sem `profiler-enable`). O que se verifica é o que o jogador recebe.
- **Sem `--scenes`** a build usa a lista do `EditorBuildSettings` (o menu, as salas `[Fatia]` e as cenas `[Teste]`). Não passe uma lista escrita à mão no comando: ela viraria uma segunda lista de salas, fora dos testes (`MainMenuTests` e `RoomLinksTests` guardam só o `EditorBuildSettings`). A única exceção é o [pacote de entrega](#gerar-o-pacote-de-entrega-109), cuja lista é calculada do `EditorBuildSettings` na hora da build.
- **A build regrava quatro arquivos.** Depois de **cada** build, reverta e confira que o `git status` está limpo:

  ```bash
  git checkout -- ProjectSettings/ProjectSettings.asset ProjectSettings/UnityConnectSettings.asset Assets/Settings/UniversalRP.asset Assets/UniversalRenderPipelineGlobalSettings.asset
  ```

  O que muda neles: o `InputSystem_Actions` entra em `preloadedAssets` e nasce um `m_BuildTargetBatching` de Standalone (`ProjectSettings.asset`), `m_Enabled: 0 → 1` (`UnityConnectSettings.asset`), e os dois assets do URP. Nenhum entra no commit. Conferido na #186: depois da build o `git status` mostrou exatamente esses quatro (mais os seis font assets que a `main` já tinha alterados), e o `git checkout` dos quatro com `Assets/Fonts` deixou só `Adicionar/`. Se o `companyName` ou outra mudança *desejada* estiver no `ProjectSettings.asset`, commite-a **antes** da build, para a reversão devolver o arquivo já com ela.
- **Depois de um Play, a build também grava o kerning nos font assets (#120).** Os font assets dinâmicos que o Play usou ganham a tabela de kerning (`m_GlyphPairAdjustmentRecords`, ~1,9 MB cada; na revisão da #113, `Atkinson SDF.asset` e `Atkinson Italic SDF.asset`, +67 mil linhas cada). A causa está no `TMP_PreBuildProcessor` (`com.unity.ugui`, `Editor/TMP/TMP_PreBuildProcessor.cs`): ele chama `ClearCharacterAndGlyphTablesInternal()`, que limpa glifos, caracteres e atlas e grava o asset, mas **não** chama `ClearFontFeaturesTables()` (`Runtime/TMP/TMP_FontAsset.cs`); só o `ClearFontAssetData` chama. Correção: o Reset (`ClearFontAssetData(true)`, receita em [Font asset dinâmico se suja no Play](#outras-armadilhas-do-projeto), #113) **antes ou depois** da build, ou o `git checkout` dos font assets alterados. O `git status` limpo esperado depois da build inclui os font assets.
- **`Builds/` está no `.gitignore`.** A `Builds/Development/` é uma build antiga e fica onde está.
- **A pasta de dados é `%USERPROFILE%\AppData\LocalLow\NoxStudios\Noturna`** (empresa e produto do Player Settings), a mesma do Editor: o `savegame.json` é um só para o Editor, a build e a suíte PlayMode. O `Player.log` fica ali também e **gira a cada abertura**: a sessão anterior vira `Player-prev.log` e a de antes dela se perde. Copie o log antes de abrir a build de novo.
- **Ler os erros do `Player.log`.** Não há prefixo de nível; um `Debug.LogError` aparece como a mensagem seguida da pilha `UnityEngine.Debug:LogError (object)`. O filtro está no passo 17 do roteiro de ponta a ponta. A linha `D3D12PipelineLibrary must be recreated due to invalid disk cache` (que tem `ERROR`) só aparece na primeira abertura de uma build nova (por isso o aquecimento), e é a única das duas que o filtro mostra. Na #186 ela **não apareceu**, nem no aquecimento de uma build recém-gerada: o cache de shaders do disco já estava quente de builds anteriores. O aquecimento continua barato (10 s), mas não conte com ver a linha. A `d3d12: failed to query info queue interface (0x80004002).` aparece em toda execução (é do driver), mas não tem `ERROR` nem `Exception`: **só se vê lendo o log inteiro** (medido no levantamento da #124: está na linha 16 do `Player.log` e do `Player-prev.log`, e o filtro devolveu 0 ocorrências nos dois).
- **A build abre em tela cheia sem borda.** Para abri-la por terminal sem tomar a tela:

  ```powershell
  Start-Process -FilePath Builds\Windows\ProjetoVN.exe -ArgumentList '-screen-fullscreen','0','-screen-width','960','-screen-height','540' -WindowStyle Minimized
  ```

  **Esses argumentos ficam gravados** em `HKCU\Software\NoxStudios\Noturna` (valores `Screenmanager …`) e valem para as aberturas seguintes, inclusive as sem argumento: a build passa a abrir em janela de 960×540. O passo 0 do roteiro apaga essas preferências (os valores `… Default_h…` ficam).
- **O Esc da pausa funciona na build com foco de verdade.** A ressalva de "foco da Game view" da receita do campo de texto é só do Editor.

#### Jogar a build por script (#22)

Sem o programador e sem a ferramenta de controle do computador, dá para percorrer o roteiro com um script PowerShell que traz a janela do jogo para a frente, tira uma captura dela (`Graphics.CopyFromScreen` sobre a área cliente, depois `Read` no PNG) e envia cliques e teclas pelo `user32.dll`. Chame `SetProcessDPIAware()` antes de medir coordenadas. **Prefira a tela cheia, sem argumentos** (a #186 jogou assim): a área cliente é a tela toda (1920×1080 em (0, 0) neste computador), a captura é a mesma e a interface e os objetos ficam onde `docs/autoria/salas.md` e as frações abaixo dizem. A 1280×720 em janela (`-screen-fullscreen 0 -screen-width 1280 -screen-height 720`, ver acima) a faixa de pan (**Edge Threshold** 80 px) vira 6,25% da largura e o motorista, que na chegada do ônibus fica em 0,050, cai **dentro** dela. O script (funções pequenas: abrir, trazer à frente, capturar, mover, clicar, segurar a borda, Esc, Alt+F4, avançar a fala, folha de contato, varrer quadrados) e o resultado ficam fora do repositório, em `Builds/PontaAPonta/issue-<N>/` (ignorada pelo git, e a sessão de revisão consegue abrir as capturas). O shell do agente não guarda variáveis entre chamadas: carregue o script (`. .\jogar.ps1`) no começo de **cada** chamada. O que custou tempo:

- **O pan por borda só reage a movimento relativo do mouse** (`mouse_event` com `MOUSEEVENTF_MOVE`). `SetCursorPos` sozinho leva o cursor, mas o `ScreenPanController` não desloca o cenário: um laço que alterna `-6` e `+3` px perto da borda (≈ 48 px de cenário por volta de 240 ms) faz o pan andar. Em 1920×1080, cada ~100 ms na borda andam ~140 px de cenário. **Depois de segurar a borda, um `SetCursorPos` de volta ao centro não basta:** o jogo só atualiza a posição com movimento relativo e continua achando que o mouse está na borda, o pan segue até a borda do fundo e o clique seguinte cai noutro objeto (na revisão da #22, na saída do posto em vez da Luna). Depois de **todo** `SetCursorPos`, mande um `mouse_event` de ±1 px (`MOUSEEVENTF_MOVE`). Clique funciona com `SetCursorPos` seguido de `mouse_event` de botão.
- **Esc:** `keybd_event` com o código virtual `0x1B` **e** o scancode `0x01`, segurando ~200 ms. Só o código virtual, ou `SendKeys` com `{ESC}`, não abriu a pausa. Para digitar na folha do nome, `SendKeys.SendWait("Nome")` funciona.
- **Os objetos provisórios são quadrados de 72×72 px a 1280×720, na cor do tipo e com o rótulo** (#191; legenda em [salas.md](../autoria/salas.md#rótulo-de-placeholder-191)): achá-los na captura é varrer a linha `y = 360` atrás de pixels de uma das **cinco cores** (`#E04A4A` porta trancada, `#9B51E0` saída, `#F2C230` item, `#2F80ED` conversa, `#9E9E9E` objeto; `#00FF00` é a porta já aberta) ou ler o rótulo sobre o quadrado, que diz o tipo e o nome (`CONVERSA / Motorista`). Os pixels `0,0,0` de antes não existem mais. A #186 seguiu esta receita (a 1920×1080 o quadrado tem 108 px e fica em `y = 540`).
- **A varredura de cor precisa de tolerância.** O renderizador erra um nível por canal: o roxo da saída saiu `#9A51DF` (não `#9B51E0`), e o azul saiu exato. Compare cada canal com folga de ±4, senão a varredura acha a conversa e não acha a saída (medido na #186).
- **Avance a fala com clique esquerdo fora de qualquer botão**, não num "botão de avançar" (ele não existe como alvo: o `DialogueInputHandler` ignora o clique que cai num `Button`; Espaço e Enter também avançam). O ponto `(0,50; 0,85)` da tela cai dentro da caixa de diálogo e fora de todo botão; com a caixa fechada ele cai no chão do cenário, sem objeto, e não faz nada. Espere ~2 s entre cliques: uma fala longa ainda em revelação (e as falas com pausa, como "Espero ter" … "feito a escolha certa, mãe…") é **completada** pelo clique, não avançada, e o seguinte avança. O prólogo, do Novo Jogo às opções do motorista, pediu **35 cliques** na build (o Editor, com `charactersPerSecond` 1000, pediu 33). Os cliques que o script dá depois de a conversa acabar são inofensivos, e 20 cliques de 2,1 s cabem numa chamada de 90 s.
- **Leia as capturas em folha de contato.** Em vez de um `Read` por captura, junte 6 a 8 PNG de 1920×1080 numa folha de miniaturas 800×450 em duas colunas, com o nome do arquivo escrito em cada uma (`System.Drawing`, `DrawImage` e `DrawString`): o texto da caixa de diálogo, a placa de nome e os rótulos continuam legíveis, e a execução inteira da #186 coube em poucas dezenas de leituras. Ao julgar brilho ou cor, **meça o pixel**: uma miniatura de uma parte escura do fundo parece "escurecida" sem estar (a média de luminância da região, 50,3, deu igual antes e depois de uma conversa).
- **Cliques e posições de interface, em fração da tela (1920×1080, medidas na #186):** menu: NOVO JOGO (0,195; 0,483), CONTINUAR (0,195; 0,568), OPÇÕES (0,195; 0,653), SAIR (0,195; 0,739); com save, o NOVO JOGO pede confirmação (NOVO JOGO em (0,19; 0,536)). Na conversa: HISTÓRICO (0,856; 0,631), Fechar do histórico (0,50; 0,082); opções do motorista (0,50; 0,260) e (0,50; 0,350); opções da atendente (0,50; 0,257 / 0,346 / 0,436 / 0,526, de cima para baixo). Folha do nome: campo (0,521; 0,588), ASSINAR (0,50; 0,811). Pausa (o painel fica no mesmo lugar com e sem conversa aberta): CONTINUAR em (0,471; 0,411) e VOLTAR AO MENU em (0,5125; 0,578); na confirmação, o VOLTAR AO MENU desce para (0,5125; 0,551). A pausa tem PAUSA, CONTINUAR, SALVAR e VOLTAR AO MENU.
- **A folha do nome vem com "Lucas" selecionado.** Clique no campo, mande `^a` e só então o nome (`SendKeys.SendWait`, com `Add-Type -AssemblyName System.Windows.Forms`): funcionou (a linha 4 da lista ficou "Tiago", e o nome chegou à placa de quem fala e ao `$nome_protagonista` do save).
- **Alt+F4 e Alt+Tab reais funcionam.** Alt+F4: `keybd_event` do `VK_MENU` (`0x12`) apertado, `VK_F4` (`0x73`) apertado e solto, Alt solto, com a janela do jogo em primeiro plano; o processo saiu em menos de 1 s. Alt+Tab: Alt apertado, Tab apertado e solto, Alt solto; o primeiro plano passou para o app Claude (conferido por `GetForegroundWindow` e o nome do processo), o jogo seguiu vivo, e a volta foi um toque de Alt mais `SetForegroundWindow`. **Cuidado com a captura feita fora do foco:** ela mostra o que estiver na frente (a tela do programador); conferida a saída, apague o PNG.
- **Posição do pan.** Cada 500 ms na borda andam ~0,32 da largura da tela (~5,8 unidades de cenário, ~1,28 px/ms). O pan não para no tempo exato, então **meça depois de cada trecho**: leve o mouse a `(0,5; 0,3)` (acima de todo objeto), espere ~0,6 s, capture e varra. A posição na tela de um objeto é `0,5 + (x local + s) / 17,78`, onde `s` é o deslocamento do cenário (chegando do ônibus, `s = -20`; chegando da loja, `s = -4`). No limite esquerdo (`s = +22,46` em 1920×1080) a posição fica **estável** com o mouse parado no centro (duas capturas, 0,8 s uma da outra, deram a cadeira em 0,972 nas duas).
- **Fim do pan.** O pan para na borda do fundo, e o limite sai da imagem e da tela (#157): ±22,46 no posto em 1920×1080, ±19,50 em 2560×1080 (o cálculo é do `ScreenPanController`, não há número digitado). **Na borda esquerda ficam a cadeira (em 0,972, dentro da faixa de pan da borda direita) e, depois da conversa com o motorista, a estrada (em 0,41).** Antes da conversa, no limite, não há quadrado nenhum no lugar da estrada (conferido). Para clicar na cadeira, pare o pan antes do limite (com `s` entre +13 e +18 ela fica no meio da tela; na #186, com `s` ≈ +18,4, em 0,746). O motorista, na chegada do ônibus (0,050 a 1920×1080, 96 px da borda, fora da faixa de 80 px), é clicável sem pan, desde que o clique seja no centro do quadrado; na #186 ele foi clicado depois de um pan, em 0,548.
- **O Histórico é o da conversa em curso.** Abra-o **durante** a conversa que quer conferir: depois que ela acaba, a conversa seguinte (a mesma atendente, por exemplo) começa um histórico novo, e `Comprar um salgadinho` / `Você recebeu: Salgadinho` não estão mais lá. Na #186 isso custou uma corrida extra (Novo Jogo, prólogo às cegas com 35 cliques, pátio, loja) só para refazer a compra. Na tela o texto da opção não leva colchetes (`Comprar um salgadinho`, itálico, e `Você recebeu: Salgadinho`).
- **Uma segunda corrida na mesma sessão é barata.** Da pausa (`VOLTAR AO MENU`, confirmar) o NOVO JOGO com save pede confirmação e recomeça; o prólogo às cegas (35 cliques de ~2 s) e o pátio levam poucos minutos. O `StoryState` volta ao zero (a atendente deu o ramo "sem o motorista").
- **O que não se verifica assim:** som e música, e a sensação do pan. O `Player.log` e o `savegame.json` continuam sendo as provas de estado.
- **A máquina fica ocupada.** O script move o mouse e o teclado de verdade e traz o jogo para a frente: o programador não usa o computador durante a execução (a #186 levou cerca de 25 min entre o aquecimento e o SAIR, pelo relógio da máquina), e a tela não pode bloquear (a captura sai preta).

### Gerar o pacote de entrega (#109)

O pacote de entrega é o `.zip` da build de Windows que vai para quem é de fora (itch.io, D-15). Parte da receita de [Gerar a build de Windows (#22)](#gerar-a-build-de-windows-22), com três diferenças: só o menu e as salas `[Fatia]` entram, a build sai numa pasta nova e vazia, e o `.zip` não leva a pasta `*_DoNotShip`. A build de desenvolvimento (`Builds/Windows/`, roteiro de ponta a ponta) não muda. Publicar no itch.io é do programador. Não há item de menu, script de build em `Assets/Scripts/Editor/` nem Build Profile (D-27).

**O produto é `Noturna`** (Player Settings: `productName: Noturna`, `applicationIdentifier` de Standalone `com.noxstudios.noturna`). Por isso o `.exe` do pacote é `Noturna.exe`, a pasta de dados do jogo é `%USERPROFILE%\AppData\LocalLow\NoxStudios\Noturna` e o registro é `HKCU\Software\NoxStudios\Noturna`, no Editor e na build. A pasta de dados e o registro seguem o `productName`; a pasta `Noturna_Data` e o nome do `.exe` vêm do `--output-path`.

1. **Cena aberta limpa, Editor fora do Play.** Mesmas condições da #22. Se o `ProjectSettings.asset` já aparece alterado com um `m_BuildTargetBatching` de Standalone, é o resíduo da última build: reverta (passo 6) antes de começar, senão ele entra no commit.
2. **Calcule a lista de cenas na hora.** Todas as cenas **habilitadas** do `EditorBuildSettings`, na mesma ordem, menos as que têm `[Teste]` no caminho. O `eval` já devolve o array JSON que o passo 3 pede, para colar no comando sem reescrever nada:

   ```bash
   unity command script eval 'return "[" + string.Join(",", UnityEditor.EditorBuildSettings.scenes.Where(s => s.enabled && !s.path.Contains("[Teste]")).Select(s => "\"" + s.path + "\"")) + "]";'
   ```

   Hoje o resultado é `["Assets/Scenes/Menu.unity","Assets/Scenes/[Fatia] Onibus.unity","Assets/Scenes/[Fatia] Posto.unity","Assets/Scenes/[Fatia] Loja.unity"]`: `Menu`, `[Fatia] Onibus`, `[Fatia] Posto` e `[Fatia] Loja`, com o `Menu` em primeiro (é a cena 0). Isso é só a conferência: use a saída do `eval`, não esta linha (uma sala `[Fatia]` nova entra na saída, e não no exemplo). A lista **não fica gravada em lugar nenhum**: não existe uma segunda lista de salas para manter, que é o motivo do aviso da #22. As cenas `[Teste]` continuam no `EditorBuildSettings` de propósito, porque a suíte PlayMode as carrega por nome e o `RoomNavigator.ChangeRoom` só aceita, no Play do Editor, uma cena que esteja ali (`Application.CanStreamedLevelBeLoaded`, D-24); só o pacote as deixa de fora. Sala nova habilitada no `EditorBuildSettings` entra no pacote sozinha. Confira que o resultado é o esperado (uma sala `[Fatia]` que não deveria entrar, ou uma que faltou, aparece aqui).
3. **Confira com o `--dry-run`**, passando a saída do passo 2 entre aspas simples (os colchetes de `[Fatia]` passam sem escape). Cole o texto no comando: não passe o `eval` por `--scenes "$(…)"`, porque se o `eval` falhar a mensagem de erro viraria a lista de cenas. A pasta de saída é **nova e vazia**, `Builds/Entrega/<AAAA-MM-DD>/`: o Unity não reescreve o que já está na pasta, e um `levelN` de uma build anterior com mais cenas poderia ficar lá.

   ```bash
   unity command build run --target StandaloneWindows64 --output-path "Builds/Entrega/<AAAA-MM-DD>/Noturna.exe" --scenes '<saída do passo 2>' --dry-run true
   ```

   Esperado: `status: dry_run`, `valid: true` e `scenes` com as mesmas cenas do passo 2 (hoje, as quatro), o `Menu` primeiro.
4. **Copie os font assets alterados** antes da build, se o `git status` os mostra modificados (armadilha da #191, em [Outras armadilhas](#outras-armadilhas-do-projeto)), e gere a build com o mesmo comando, `--confirm true` no lugar do `--dry-run`. Acompanhe com `unity command build status | grep -E '^(status|result|buildTimeMs|totalSizeBytes|totalWarnings|totalErrors):'` até `completed`: `result: Succeeded` e `totalErrors: 0`. Medido na #109 (quatro cenas): **25 s** (`buildTimeMs` 24 869), 181 MB (`totalSizeBytes` 180 678 505), `totalWarnings: 0`.
5. **Confira a pasta da build.** `Noturna_Data` tem `level0` a `level3` e **nenhum** `level4` ou `level5` (as duas cenas `[Teste]` ficaram de fora). A pasta `Noturna_BurstDebugInformation_DoNotShip` **pode** existir: ela é gerada quando o Burst compila, e uma build com o cache quente pode não gerá-la. Medido na #109: na execução (build de 25 s) a pasta veio; na revisão (build incremental de 5 s, logo depois) não veio, com o `lib_burst_generated.dll` presente. O `--exclude` do passo 7 vale nos dois casos, e quando a pasta existe é a prova de que ele é necessário.
6. **Reverta o que a build regravou**, como na #22 (os quatro arquivos, com o `git checkout` só neles), e devolva a cópia dos font assets do passo 4. O `ProjectSettings.asset` volta já com o `productName` e o identificador novos, porque a mudança foi commitada antes da build. `git status` igual ao de antes da build.
7. **Gere o `.zip`** ao lado da pasta, `Builds/Entrega/Noturna-windows-<AAAA-MM-DD>.zip`, com o `tar.exe` do Windows, de dentro da pasta da build e **excluindo a `*_DoNotShip`**:

   ```powershell
   Push-Location Builds\Entrega\<AAAA-MM-DD>; & "$env:SystemRoot\System32\tar.exe" -a -c -f ..\Noturna-windows-<AAAA-MM-DD>.zip --exclude '*_DoNotShip' *; Pop-Location
   ```

   Por que o `tar` e não o `Compress-Archive`: o do PowerShell 5.1 grava `\` no nome das entradas, que outros sistemas leem errado; o `tar.exe` grava `/`. Por que o `--exclude` e não apagar a pasta: não destrói nada e deixa a pasta da build igual à que o Unity gerou. Os arquivos ficam na **raiz** do `.zip` (o "Extrair tudo" do Windows cria a pasta com o nome do arquivo). Confira com `[IO.Compression.ZipFile]::OpenRead`: entradas com `DoNotShip` = 0, com `\` = 0, `Noturna.exe` e `Noturna_Data/level3` presentes, `level4` ausente. Medido na #109: 221 entradas, 46,7 MB.
8. **Teste o pacote como o jogador o recebe.** Extraia o `.zip` numa pasta do scratchpad (`tar.exe -x -f <zip> -C <pasta>`) e abra o `.exe` de lá, sem argumentos (tela cheia), com a receita de [Jogar a build por script](#jogar-a-build-por-script-22) (captura com `PrintWindow`; o script de cliques toma o mouse e a tela por ~1 min, avise o programador antes). A pasta de dados é a do Editor: **copie o `savegame.json` antes e devolva depois**, e apague-o antes de abrir para o Novo Jogo não pedir confirmação. Clique em NOVO JOGO em (0,195; 0,483) e espere ~6 s. Esperado: o título da janela (`(Get-Process Noturna).MainWindowTitle`) é `Noturna`; o `savegame.json` novo tem `"currentScene": "[Fatia] Onibus"` (a sala inicial carrega no pacote sem as cenas de teste); o filtro de erros do `Player.log` (passo 17 de [ponta-a-ponta.md](ponta-a-ponta.md)) não devolve nada. Feche com Alt+F4 e apague a pasta extraída e as capturas. Na #109 os três bateram, e o filtro não devolveu nem a linha `D3D12PipelineLibrary`.

**A pasta `*_DoNotShip` pode ser gerada** (ver o passo 5), mas fica fora do `.zip`. Não desligue o Burst: ele vem do URP (dependência do `com.unity.render-pipelines.core` e do `com.unity.collections`, no `packages-lock.json`) e a build usa código dele (`Noturna_Data/Plugins/x86_64/lib_burst_generated.dll`). A pasta só tem `lib_burst_generated.txt`, os argumentos do compilador, com caminhos da máquina do programador. Desligar exigiria `ProjectSettings/BurstAotSettings_*.json` e mudaria o desempenho do URP.

**Trocar o `productName` muda a pasta de dados e a chave do registro**, no Editor também: o `savegame.json` de antes (`LocalLow\NoxStudios\ProjetoVN`) ficou órfão e não há migração (D-20, o jogo nunca tinha sido distribuído). Esta é a primeira entrega: a D-20 continua valendo, e uma versão seguinte pode não abrir o save de quem jogou esta.

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
unity command script eval 'UnityEditor.AssetDatabase.ForceReserializeAssets(new[] { "Assets/caminho/do/arquivo.asset" }); return "ok";'
```

### Apagar um script

Antes de apagar um `.cs` de MonoBehaviour ou ScriptableObject:

1. Pegue o GUID no `.meta` do script.
2. Procure esse GUID em `Assets/` (cenas, prefabs, assets).
3. Remova os componentes e assets que o usam, **pelo Editor**, antes de apagar o script. A ordem inversa deixa "Missing Script".

### Mover um script

Mova com `asset move` para que o `.meta` vá junto. Se o `.meta` se perder, o GUID muda e todo componente daquele tipo vira "Missing Script", com as ligações de `UnityEvent` destruídas.

### Um arquivo novo dentro de `Assets/` precisa do `.meta`

Vale também para `.md`, `.yarn` e pastas. Atualize o banco de assets e commite o `.meta` junto.

### Managers e interface de jogo não entram em cena

`DialogueManager`, o `DialogueRunner` do Yarn Spinner, `StoryStateVariableStorage`, `InventoryManager`, `GameStateController`, `RoomNavigator`, `UnfocusedFrameRateLimiter`, `DialogueInputHandler`, `GameSaveManager`, `ItemScriptActions`, `SoundScriptActions`, `ScreenScriptActions`, `ItemCollectedSound` e `AudioManager` (com as cinco `AudioSource`, nos filhos `Music`, `Ambience` e `Effects`) existem só no `Managers.prefab`. Um deles colocado em uma cena sobrescreve o `Instance` do persistente.

O `UnfocusedFrameRateLimiter` é a exceção ao motivo: ele não tem `Instance`, mas um segundo exemplar (por exemplo, um posto em uma cena) faz o limite **não ser desfeito**. A perda de foco chega aos dois, e o segundo guarda o `(0, 30)` do primeiro como original; se ele também for o último a receber o ganho de foco, o jogo **fica a 30 FPS com o foco de volta**.

O mesmo vale para a interface de jogo, que vem aninhada nesse prefab (`Assets/Prefabs/UI/GameUI.prefab`: `Canvas_Game`, com o `ScriptScreen` (#71), `Canvas_Pause`, `Canvas_Fade` e `EventSystem`):

- **Um `EventSystem` em uma cena** soma-se ao persistente e o uGUI loga `There are 2 event systems in the scene. Please ensure there is always exactly one event system in the scene` **a cada frame** (milhares de avisos em segundos). Na verificação, conte os `EventSystem` em Play Mode (deve ser 1) e confira que esse aviso não aparece.
- **Um `Canvas_Game` em uma cena** duplica o apresentador: o da cena se registra por último no `DialogueManager` e, ao ser destruído com a cena, deixa o diálogo sem interface.
- Em Play Mode, `GameObject.Find("Canvas_Game")` devolve o objeto persistente (cena `DontDestroyOnLoad`, pai `GameUI`, avô `Managers`). Para editar a interface fora do Play, abra o `GameUI.prefab`, não a cena.

## Armadilhas do Yarn Spinner

Descobertas nas issues #5 e #6. A prova de conceito (#3) e o código do pacote estão em `Library/PackageCache/dev.yarnspinner.unity@*/`.

- **Comando desconhecido trava o diálogo.** Um `<<comando>>` sem handler faz o runner logar erro e parar sem avançar. O `DialogueManager.SkipUnknownCommand`, ligado ao `onUnhandledCommand` por **ligação persistente** no `Managers.prefab` (um `AddListener` em runtime não conta), ignora o comando com um erro e a conversa segue. Mesmo assim: **não escreva um comando no `.yarn` antes de ele existir** (o compilador não valida o nome).
- **Comando conhecido com número errado de parâmetros trava o diálogo.** `<<dar_item>>` sem id, ou `<<dar_item a b>>`, faz o runner logar `Can't call command <<dar_item>>: dar_item requires 1 parameter, but 0 were provided.` e chamar `Continue()` sem sinalizar o fim do comando: a conversa fica presa na fala anterior, com `IsDialogueRunning` verdadeiro, então nem a rede de segurança do `DialogueManager` nem o `SkipUnknownCommand` atuam (medido na #6; registrado na #37). A causa é do pacote (`DialogueRunner.OnCommandReceivedAsync` chama `Continue()` sem `SignalContentComplete()` nos casos `InvalidParameterCount`, `NoTargetFound` e `TargetMissingComponent`) e não há gancho para consertar de fora; o jogo não corrige isso em runtime (decidido na #37). A proteção é o teste `ProjectScripts_CallEveryCommandWithParametersItAccepts` (`ScriptContentTests`), que confere a contagem de palavras de **todo** comando dos roteiros contra a assinatura do método `[YarnCommand]` (lida pelo `TypeCache`, regra do `Actions.ParameterCount` do pacote) e reprova também um comando que não existe. Para sair de uma conversa presa durante uma verificação, saia do Play Mode.
- **Comando conhecido com parâmetro do tipo errado trava o diálogo, e o log não diz qual comando (#142).** `<<wait abc>>` (o `wait` do pacote recebe `float`) não converte: o `Actions.TryParseArgs` devolve `InvalidParameterType`, o `Invoke` o transforma em `CommandDispatchResult.StatusType.InvalidParameter` e o `switch` de `DialogueRunner.OnCommandReceivedAsync` não tem `case` para ele, então cai no `default:`, que lança `ArgumentOutOfRangeException` dentro de uma tarefa descartada com `.Forget()`. O `Continue()` nunca roda. Medido em Play Mode: `StartDialogue` devolve `true`, a caixa fica na fala anterior, `IsDialogueActive` e `IsDialogueRunning` continuam verdadeiros, o `frameCount` avança (o jogo roda) e o único log é `ArgumentOutOfRangeException ... Unknown command dispatch result status` (`DialogueRunner.cs:737`), **sem o nome do comando**, ao contrário do caso da contagem. Como na #37, não há gancho e o jogo não corrige isso em runtime. A regra de conversão do pacote (`Actions.CreateConverterFunction`): `string`, `GameObject` e `Component` aceitam qualquer palavra (um nome que não existe vira `null`, sem lançar); `bool` aceita o nome do parâmetro ou `true`/`false`; o resto passa por `Convert.ChangeType` em `CultureInfo.InvariantCulture`. Duas consequências: **`1,5` vira `15` sem erro**, porque no formato invariante a vírgula separa milhares (um fade de 15 s em vez de 1,5 s), e o valor de uma expressão (`{$x}`) chega formatado em invariante (`1.5`), então a vírgula só entra pelo literal que o roteirista digita. A proteção é o mesmo teste da contagem (`ProjectScripts_CallEveryCommandWithParametersItAccepts`), que também confere o **tipo** de cada palavra literal (`ScriptCommandParameter`, que repete a regra do pacote) e reprova número com vírgula pedindo o ponto. **Não confere** o valor de expressão, que só existe em runtime. Para ver o teste reprovar, acrescente `<<wait abc>>` numa linha de um `.yarn` que já existe (nunca um `.yarn` novo, #34) e desfaça com `git checkout -- <arquivo>` antes de commitar.
- **Função desconhecida compila sem erro nem aviso.** Um `tem_itm("x")` digitado errado é declarado de forma implícita pelo compilador e só falha em runtime. `ProjectScripts_CompileWithoutErrorsOrWarnings` não pega esse erro.
- **Comando e função novos: C# primeiro, `.yarn` depois.** O comando novo já nasce coberto na contagem e no tipo dos parâmetros pelo teste genérico (#37, #142); o teste dos ids (item, som) continua sendo trabalho de quem cria o comando. Um comando com parâmetro numérico pede, no guia do roteirista, a regra "ponto decimal, nunca vírgula" (escrita em `roteiro.md`, seção "Tela e imagens", pela #71, a primeira issue com comando numérico). O gerador de código do pacote acha métodos **estáticos** com `[YarnCommand]` e `[YarnFunction]` sem registro nosso; o asmdef do módulo precisa referenciar `YarnSpinner.Unity`. Para executar um roteiro fora do runner (o `ScriptRun` dos testes), a função precisa estar registrada na `Library` do `Yarn.Dialogue`, senão a VM lança `Function tem_item is not present in the library`. Em um roteiro em memória no Play Mode, passe `runner.Dialogue.Library` ao `CompilationJob`.
- **`<<jump>>` para nó inexistente compila com aviso.** O compilador dá só o aviso `YS0012`, e em runtime o Yarn lança e para sem chamar o fim da conversa. A rede de segurança no `Update` do `DialogueManager` encerra a conversa com um erro. O `ScriptContentTests` reprova qualquer aviso nos roteiros, o que pega o erro antes do Play.
- **Um `.yarn` só vira conteúdo dentro de um `.yarnproject`.** Os roteiros ficam em `Assets/Roteiro/`, cobertos por `Assets/Roteiro/Roteiro.yarnproject` (`**/*.yarn`). Um `.yarn` fora dessa pasta não é importado.
- **Um `.yarn` novo só entra no `Roteiro.yarnproject` com o Editor em foco (#34).** O `YarnImporter` do pacote agenda a reimportação do projeto num `EditorApplication.delayCall`, e o `delayCall` não roda com o Editor sem foco (o `EditorApplication.update` roda; o `delayCall` fica na fila). Depois de criar um `.yarn` por fora e dar `Refresh`, o arquivo é importado, mas o projeto continua com os arquivos de antes: o nó não existe para o `DialogueRunner` nem para o seletor do Inspector, e os erros de compilação do arquivo não aparecem. O `ScriptContentTests` lê os `.yarn` do disco e vê o arquivo, então os testes passam enquanto o Play Mode discorda deles. **Reimporte o projeto** (o `ImportAsset` do passo 2 de [Integrar um roteiro novo](#integrar-um-roteiro-novo); com ou sem `ForceUpdate`, os dois funcionam). Apagar ou editar um `.yarn` que o projeto já conhece não tem o problema: o projeto depende de cada arquivo (`DependsOnSourceAsset`), e o Unity o reimporta na hora. Com o Editor em foco (o programador usando o Unity), a reimportação acontece sozinha em poucos segundos. Não tente destravar o `delayCall` com `RepaintAllViews` (não adianta) nem com `Application.runInBackground` (a opção já é `1`, D-31, e a atribuição suja o `ProjectSettings`). Para conferir, leia `YarnProjectImporter.ImportData.yarnFiles.Count`, se `AssetDatabase.GetDependencies` do projeto lista o arquivo e se o nó está em `Yarn.Program.Parser.ParseFrom(project.compiledYarnProgram).Nodes`; confira também `UnityEditorInternal.InternalEditorUtility.isApplicationActive == False`, porque um clique no Unity durante a verificação libera a fila e esconde o defeito.
- **Arquivos criados pelo pacote em `ProjectSettings/Packages/dev.yarnspinner/`.** O `YarnSpinnerProjectSettings.json` é commitado (a #5 foi autorizada a isso). O `ProjetoVN.GameFlow-generated.ysls.json` é **gerado e fica fora do git** (`.gitignore`, padrão `*generated.ysls.json`, #217): o Unity o **regenera a cada compilação** (`generateYSLSFile: true` no `YarnSpinnerProjectSettings.json`; lista os comandos e funções de C#, como `dar_item`, `remover_item` e `tem_item`, com parâmetros, arquivo e linha). Ele serve só à extensão do Yarn Spinner no VS Code do programador, para conhecer os comandos do jogo; o importador do Unity não o lê (compila o roteiro com as declarações registradas em C#), e o `Roteiro.yarnproject` importa sem ele (medido na #217: com o arquivo apagado, `ImportStatus` `Succeeded` e 16 arquivos). Não é commitado, nem quando um `[YarnCommand]` ou `[YarnFunction]` muda. A extensão só usa um `.ysls.json` listado no campo `definitions` do `Roteiro.yarnproject`; um arquivo solto na pasta é "não rastreado" e `dar_item` aparece como `Unknown command`. O campo aponta para o arquivo gerado (`../../ProjectSettings/Packages/dev.yarnspinner/ProjetoVN.GameFlow-generated.ysls.json`) e o Unity reimporta o projeto sem reclamar dele. Se um novo asmdef do projeto ganhar comandos, o Unity gera um `<Assembly>-generated.ysls.json` novo: acrescente-o ao `definitions`. O pacote também abre a janela "About Yarn Spinner" na primeira carga.
- **O choque de nome `Dialogue`.** Dentro de `ProjetoVN.*`, `Dialogue` resolve para o namespace `ProjetoVN.Dialogue`, não para a classe do Yarn. Escreva `Yarn.Dialogue` por extenso.
- **`lastline`.** O compilador etiqueta com `lastline` a fala que é o comando **imediatamente anterior** a um bloco de opções, e o apresentador usa isso para mostrar fala e opções juntas. Um `<<set>>` entre a fala e as opções tira a etiqueta, e o jogador passa a precisar de um clique a mais.
- **Narração com dois-pontos vira personagem.** `Atenção: a porta fechou.` é lida como a personagem "Atenção", e `Eram 10:30 da manhã.` como a personagem "Eram 10": **qualquer** dois-pontos numa linha de narração faz o que vem antes virar o nome. Evite-os ou escape com `\:` (`Eram 10\:30 da manhã.`).
- **`variableStorage` nulo no runner** faz o Yarn criar um `InMemoryVariableStorage` em silêncio, e o roteiro passa a ter um estado paralelo (contra a D-18). Na verificação, conte os `InMemoryVariableStorage` em Play Mode: deve ser zero.
- **Não toque em `DialogueRunner.Dialogue` nem em `YarnProject.Program` antes de conferir `compiledYarnProgram`:** com erro de compilação eles lançam.
- **`#nullable`.** O pacote declara `YarnTask<DialogueOption?>`; no nosso código, sem contexto anulável, escreva `YarnTask<DialogueOption>`.
- **Remover vários scripts e assets de uma vez:** apague os assets que os usam antes e faça tudo entre `AssetDatabase.StartAssetEditing()` e `StopAssetEditing()`, com um único `Refresh`. Um erro de compilação no meio deixa o Editor em Safe Mode e o CLI para de conectar (`unity pipeline list` confirma).

### Testar um roteiro quebrado sem criar arquivo

Criar um `.yarn` quebrado em `Assets/Roteiro/` suja o projeto e obriga a apagá-lo depois. Em Play Mode dá para testar os quatro casos de roteiro inválido trocando o `yarnProject` do runner por um criado em memória (`script eval --file`):

- Compile um texto com `Yarn.Compiler.Compiler.Compile(CompilationJob.CreateFromString(...))`.
- Crie `ScriptableObject.CreateInstance<Yarn.Unity.YarnProject>()`, grave `compiledYarnProgram = Google.Protobuf.MessageExtensions.ToByteArray(resultado.Program)` e uma `Yarn.Unity.Localization` com `AddLocalizedStrings` sobre a tabela de textos em `baseLocalization`.
- Troque com `SerializedObject(runner).FindProperty("yarnProject").objectReferenceValue = projeto`. Para o caso do roteiro com erro de compilação, deixe `compiledYarnProgram = null`.
- Volte ao projeto real com `AssetDatabase.LoadAssetAtPath<Yarn.Unity.YarnProject>("Assets/Roteiro/Roteiro.yarnproject")`.
- Grave também `project.lineMetadata = new Yarn.Unity.LineMetadata()` e, para cada entrada da tabela de textos com etiquetas, `AddMetadata(id, etiquetas)`; sem isso a etiqueta `lastline` some e a fala não aparece junto das opções.
- **Pare a conversa em curso antes de trocar o projeto** (`FindFirstObjectByType<Yarn.Unity.DialogueRunner>().Stop()`). Com uma conversa aberta, `StartDialogue` devolve `false` e a interface continua mostrando a fala antiga.
- Para medir como o texto fica na interface (medições da #8), leia os componentes TMP do `DialogueUIController` por `SerializedObject` (`dialogueText`, `speakerNameText`, `choiceTexts`): `preferredHeight` e `preferredWidth` valem, mas **`textInfo.lineCount` devolve 1** em texto que quebra, então não use.

### Integrar um roteiro novo

O roteiro chega como texto do roteirista, que o programador passa a um agente; a sessão de execução da issue escreve os `.yarn` seguindo o [guia de roteiro](../autoria/roteiro.md) (ver [fluxo-de-trabalho.md](fluxo-de-trabalho.md#roteiro-novo)). Depois de criar ou alterar os arquivos em `Assets/Roteiro/`:

1. Atualize o banco de assets para o Unity gerar o `.meta` de cada `.yarn` novo, que vai no mesmo commit:

   ```bash
   unity command script eval 'UnityEditor.AssetDatabase.Refresh(); return "ok";'
   ```

2. Reimporte o projeto de roteiro (com o Editor sem foco, um `.yarn` novo não entra sem isso; causa na [armadilha do Yarn Spinner](#armadilhas-do-yarn-spinner), #34):

   ```bash
   unity command script eval 'UnityEditor.AssetDatabase.ImportAsset("Assets/Roteiro/Roteiro.yarnproject", UnityEditor.ImportAssetOptions.ForceUpdate); return "ok";'
   ```

3. Rode a suíte. O `ScriptContentTests` reprova aviso de compilação, id de item inexistente, nó fora da convenção e variável fora do `variaveis.yarn`:

   ```bash
   unity command test run --mode editor --timeout 180
   ```

4. Abra a conversa em Play Mode (receita acima) e confira falas, opções e tamanho do texto.

## Outras armadilhas do projeto

- **Font asset dinâmico se suja no Play (#113).** Os font assets de `Assets/Fonts/` são de atlas dinâmico: o TextMeshPro acrescenta glifos ao atlas e às tabelas do `.asset` durante o Play no Editor, e o próximo `AssetDatabase.SaveAssets()` (salvar cena ou prefab, `Ctrl+S`, a suíte PlayMode) grava tudo no arquivo. Precedente: `Assets/_Project/UIFramework/Art/Fonts/Grenze/Grenze-SemiBold SDF.asset` foi commitado com 93 glifos. Antes de commitar, `ClearFontAssetData(true)` em cada um (o "Reset" do menu de contexto; por `script eval --file`, percorrendo `AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/Fonts" })`) e `SaveAssets()`; confira `glyphTable.Count == 0` e o `git status`. Na build o `Clear Dynamic Data On Build` limpa o que vai para o jogador (glifos e atlas), mas grava o `.asset` com a tabela de kerning (ver [Gerar a build de Windows](#gerar-a-build-de-windows-22)).
- **Botão que abre "em destaque" sem ninguém ter tocado (#114).** O `InputSystemUIInputModule` guarda a última posição do ponteiro e dispara `OnPointerEnter` no botão que passa a ficar embaixo dela, também com o mouse parado e o Editor sem foco: um bloco de opções abre com o `ChoiceButton0` selecionado se o mouse estava sobre a região. Não é código do jogo (`GetLastRaycastResult(0)` do módulo mostra o botão e a posição). Para verificar "o bloco abre sem destaque", tire o ponteiro de lá antes: `InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = new Vector2(740, 20) })` num `script eval --file`, e confira `GetLastRaycastResult(0).gameObject == null`. Simular ponteiro por `ExecuteEvents` (enter, down, up, exit) e o teclado por `ExecuteEvents.moveHandler` com `AxisEventData` valem; o botão desabilitado não é alvo do `moveHandler` automático.
- **Empate de ordem de desenho e como conferir um contorno pelos pixels (#196).** Dois `Renderer` na mesma camada de ordenação (`Default`, a única do projeto), na mesma ordem e no mesmo `z` não têm quem fica por cima garantido: o Unity decide por um critério interno, e o resultado pode mudar entre execuções (o contorno do hover, ordem 0, empatava com o fundo das salas, também na 0, e aparecia ou sumia). Uma captura não prova a correção de um empate: tire o empate (ordens diferentes) e confira em mais de um Play e em mais de uma sala. Receita para conferir um contorno: (1) num só `script eval --file`, só em Play e **nesta ordem**: (a) desligue o `ScreenPanController` **e o `PointNClickSelector`** (`FindObjectsByType<…>().enabled = false` nos dois; o pan segue o mouse real do Windows e move o cenário entre a captura e a conta, e o seletor, que mora na `Main Camera`, chama `OnHoverExit()` e apaga o contorno quando o mouse real passa pelo objeto e sai); (b) se o objeto estiver fora da tela, leve a câmera até ele (abaixo); (c) chame `OnHoverEnter()` no objeto. O (a) vem antes do (c) porque desligar o seletor chama `OnHoverExit()` no objeto que ele tinha marcado (`OnDisable → ClearHoverState()`): desligado depois do hover, apaga o contorno; desligado antes, não toca mais no objeto, mesmo com o mouse passando por cima (medido na #208); (2) `capture game_view --source screen --output-path Temp/<arquivo>.png`, sem o aviso "not redrawn" (confira o `frameCount`; com o aviso, capture de novo); (3) num `script eval --file`, carregue o PNG (`Texture2D.LoadImage`) e leia os pixels da faixa entre o contorno e o quadrado (esquerda, direita e em cima, pelos `bounds` dos `SpriteRenderer` convertidos por `Camera.WorldToScreenPoint`), **multiplicando pela razão `tex.width / cam.pixelWidth`**: a câmera tem 1920×1080 e a captura 1280×720, e um pixel sem a razão cai em outro objeto. **Confira antes que o objeto está na tela com o pan desligado:** numa sala com fundo largo (`[Fatia] Posto`) o `Motorista` fica em x 2256 (de 1920) e a leitura cai fora da imagem, sem erro; liste `cam.WorldToScreenPoint(i.transform.position)` dos `InteractableItem`, antes do (b): no mesmo `eval`, antes de mover a câmera, ou num `eval` anterior, com o pan já desligado. **Objeto fora da tela (passo (1b)): leve a câmera até ele** (`var cam = Camera.main; var cp = cam.transform.position; cam.transform.position = new Vector3(obj.transform.position.x, cp.y, cp.z);`). O objeto passa para o centro da tela (960, 540 de 1920×1080). O pan move o cenário (`environmentContainer`), não a câmera, e nada no jogo a põe de volta; a posição original volta no `editor.playmode stop`, e a cena não fica suja. Não troque o objeto pedido por outro que já esteja na tela. Com a câmera movida, o aviso "not redrawn" não invalida a captura se a imagem já mostra o objeto no centro: como (b) e (c) rodam no mesmo `eval`, todo quadro que mostra a câmera movida mostra também o contorno, e um quadro de antes do `eval` mostraria a câmera onde estava. Isso distingue só os quadros de antes do `eval`: se outro `eval` mudar algo depois (outro hover, outro objeto), volte a conferir o `frameCount`. Foi assim que a revisão da #196 mediu o `Motorista` (contorno `FFFFFF` nos quatro lados, quadrado `2F80ED`).
- **Texto de mundo (`TextMeshPro` 3D) em prefab (#191).** (1) O `AddComponent<TextMeshPro>()` cria um `MeshFilter` com `HideAndDontSave`, que **não é gravado**: a instância na cena registra então um componente "adicionado" com referência nula (`m_AddedComponents` com `addedObject: {fileID: 0}`) a cada `displayName` gravado. Ponha um `MeshFilter` **antes** do `TextMeshPro` e zere o `hideFlags` dele (`GetComponent<MeshFilter>().hideFlags = HideFlags.None`) antes de `SaveAsPrefabAsset`; com isso o prefab guarda o componente (`!u!33`) e o diff das cenas traz só o override desejado. (2) O `AddComponent` traz a `LiberationSans SDF`: atribua `font` e `fontSharedMaterial`. (3) **O contorno do SDF come a face:** com `_OutlineWidth` 0,45 a face do texto de 28 px ficou escura e ilegível sobre uma estante (build 1920×1080); com `_OutlineWidth` 0,2 e `_FaceDilate` 0,2 ficou branca e nítida. Julgue o contorno na build ou na captura `--source screen`, nunca na `--source camera` (renderiza o texto diferente do jogo). (4) **`OnValidate` roda também ao carregar a cena em Play:** um `EditorApplication.delayCall` agendado ali executa depois do `Start` e desfaz o que o jogo fez (o `PlaceholderLabel` repintava o verde do `PlaceholderTint`); confira `Application.isPlaying` no método agendado. (5) **O `OnValidate` não aplica o rótulo ao abrir a sala** (o `delayCall` não rodou, nem com o Editor em foco): a Scene view ficava com `OBJETO / (sem nome)` e o quadrado preto. O `[ExecuteAlways]` com o `Awake` resolve, sem sujar a cena (`scene list_open` com `isDirty: false` depois de abrir) e sem gravar o texto e a cor como *override*; confira lendo o `label.text` e a cor da raiz por `eval` logo depois do `scene open`. (6) **Reverter o `m_Mesh` de um `MeshFilter` de TextMeshPro por script não limpa a cena:** `PrefabUtility.RevertPropertyOverride` e `PrefabUtility.GetObjectOverrides(...).Revert` fazem o TMP se reinicializar, e o `scene save` grava no lugar um override `m_TextStyleHashCode` (`-1183493901`) e deixa o `m_Mesh`. Os overrides `m_Mesh` vazios (`target` do `MeshFilter` do `Label`) são ruído sem efeito: com a cena fechada (abra o `Menu`), apague à mão os blocos de quatro linhas e reabra a cena; abrir e gravar de novo não os traz de volta (medido na `[Teste] Mecanicas`).
- **Não reverta cena nem prefab por `git checkout` com a cena aberta no Editor (#191).** Com a `[Teste] Sala B` aberta, `git checkout -- Assets/Scenes …` fez o Editor abrir o diálogo de cena alterada no disco, e o CLI passou a `unreachable` (`script eval` e `scene list_open` expiraram) até uma pessoa fechá-lo com **Reload**. Troque para uma cena limpa e sem relação (`scene open --path Assets/Scenes/Menu.unity`, com `isDirty: false`) **antes** de mexer em `.unity` ou `.prefab` pelo `git`.
- **A build regrava os font assets que o programador já tinha sujo (#191).** `git checkout` dos font assets, como a receita da build manda, descartaria o trabalho dele quando o `git status` já os mostrava modificados antes da sessão. Copie os modificados **antes** da build e devolva a cópia depois (com `git checkout` só nos quatro arquivos de configuração).
- **Conferir se uma fonte desenhou o glifo.** O `LiberationSans SDF` (padrão do `TMP Settings`) também tem `“ ” … —` e é a reserva final do TextMeshPro, então um texto "sem quadradinho" não prova que a fonte certa o desenhou. Leia `textInfo.characterInfo[i].fontAsset.name` de cada caractere visível (depois de `ForceMeshUpdate()`), que deve começar por `Atkinson`. O mesmo `characterInfo[i].pointSize` confere o tamanho de um trecho de rich text (`<size=30>`).
- **Criar um font asset por `script eval --file`.** `Selection.activeObject = <Font>` e `EditorApplication.ExecuteMenuItem("Assets/Create/TextMeshPro/Font Asset/SDF")` criam `<fonte> SDF.asset` ao lado do `.ttf`, já em modo **Dynamic**; renomeie com `AssetDatabase.RenameAsset`. O multi-atlas e a tabela de pesos se gravam por `SerializedObject` (`m_IsMultiAtlasTexturesEnabled`, `m_FontWeightTable`, e `m_ClearDynamicDataOnBuild` para ler); `clearDynamicDataOnBuild` não é público.
- **`textureWidth` e `textureHeight` do importador de SVG não se atualizam sozinhos** ao mudar o `m_TextureSize`: só o `sprite.rect` muda. Grave os dois (`m_TextureWidth`, `m_TextureHeight`) para o `.meta` ficar coerente com o sprite.
- **Arte da interface em SVG.** Os SVGs são importados como Textured Sprite e desenhados com `Image` comum. Cada textura tem exatamente o tamanho do elemento em 1080p, porque o importador não gera mipmaps e uma textura maior volta a serrilhar. Detalhes no [README do Dialogue](../../Assets/Scripts/Dialogue/README.md).
- **PNG novo vem como `Sprite Mode: Multiple`.** O importador padrão do projeto 2D importa um PNG como `Sprite (2D and UI)` em modo `Multiple`, e sem fatias o Unity não gera sprite nenhum (`LoadAssetAtPath<Sprite>` devolve `null`). Para uma imagem inteira, ajuste `TextureImporter.spriteImportMode = SpriteImportMode.Single` e chame `SaveAndReimport()` antes de referenciá-la (aconteceu nos retratos da #10).
- **O tamanho de um sprite na tela vem do tamanho em unidades, não do `sprite.rect`.** O `Max Size` do importador reduz a textura: `sprite.rect` passa a ser em pixels da textura reduzida (duas artes mais altas que 2048, de alturas diferentes, ficam as duas com 2048), mas o tamanho em unidades (`sprite.bounds`) não muda. Dimensione um `Image` com `SetNativeSize()` e mantenha **Pixels Per Unit 100** em toda arte de personagem; calcular o tamanho por `sprite.rect` iguala personagens de alturas diferentes (aconteceu na #48).
- **`Image` sem sprite desenha um quadrado branco.** Um lugar vazio se desliga pelo `gameObject`, nunca limpando o `sprite`.
- **Esconder um painel com muito texto: desligue o `Canvas`, não o objeto.** Religar um objeto faz cada `TMP_Text` filho refazer o texto, e o custo cresce com as entradas (~100 ms para 200 falas no histórico, #25). Um `Canvas` aninhado (com `GraphicRaycaster`) com o objeto sempre ativo esconde, deixa de desenhar e de receber clique, e reabre em poucos ms. Ao medir, lembre que o `eval` só enxerga membros públicos (um `internal` dá erro de compilação no `eval`) e que `Time.unscaledDeltaTime` lido em outro `eval` não é o quadro da abertura: instale um registrador em `Application.onBeforeRender` e leia os quadros depois.
- **Balão com largura máxima em layout group (#55).** Para um balão do tamanho do texto, limitado por uma largura máxima, não use `ContentSizeFitter` dentro de layout group (o Unity desaconselha, e ele não tem máximo). A linha é um `HorizontalLayoutGroup` (`childControlWidth/Height` ligados, `childForceExpand*` desligados) com o balão dentro: `Image` fatiada, `VerticalLayoutGroup` com `childForceExpandWidth` **desligado** e um `LayoutElement` com `flexibleWidth = 0`. O código mede o texto com `GetPreferredValues(texto, máximo - padding, 0)` e grava o resultado no `preferredWidth`. Com `childForceExpandWidth` ligado, o balão relata `flexibleWidth = 1`, o `HorizontalLayoutGroup` o estica até a linha toda e o `preferredWidth` é ignorado (medido: 1420 px em vez de 936).
- **Sprite fatiado em 9 partes precisa ser branco puro (#55).** A `Image` multiplica o sprite pela cor: um sprite que não é branco escurece toda cor aplicada (o `Background.psd` embutido do Unity escureceu a Luna de `#D9A6FF` para uns `#B98ADB`). Para um SVG fatiado, importe como Textured Sprite e grave a borda em `m_SpriteData.SpriteBorder` do `SVGImporter` por `SerializedObject` (os nomes têm o prefixo `m_`: `m_SvgType`, `m_TextureSize`, `m_TextureWidth`, `m_TextureHeight`, `m_SampleCount`), depois `SaveAndReimport()`, e confira `sprite.border`.
- **O quadro de uma medição pelo CLI traz ~20 ms de ferramenta (#55).** Um `eval` que só liga um `Canvas` com uma linha ativa mede ~20,5 ms no quadro em que roda, e uma rolagem sem custo próprio mede ~21 ms. Ao medir "o pior quadro" de uma reabertura, subtraia esse piso (o histórico de 200 balões mede ~24 a 25 ms, ou seja, ~4 ms próprios) e compare com o piso medido, não com zero. No `onBeforeRender`, `Time.unscaledDeltaTime` é a duração do quadro **anterior**: o quadro em que a chamada rodou aparece no registro seguinte. `TMPro_EventManager.TEXT_CHANGED_EVENT` é um `FastAction<Object>`. **Método sem o piso (revisão da #55, #59):** no mesmo `eval`, um carimbo de `System.Diagnostics.Stopwatch` antes e outro depois da chamada medida, e um carimbo a cada `Application.onBeforeRender` (a lista guardada com `AppDomain.CurrentDomain.SetData`, como na receita de FPS da #80), lidos em outro `eval`. Remova a assinatura do `onBeforeRender` ao terminar: sem a recarga de domínio ela sobrevive ao Play ([#140](#o-play-mode-não-recarrega-o-domínio-140)). Isso mede o custo da chamada e dos quadros seguintes sem o piso da ferramenta e dispensa a subtração. Na revisão da #55, a reabertura do histórico de 200 balões levou ~2,8 ms e nenhum quadro seguinte passou de 6,3 ms; pela métrica antiga (`Time.unscaledDeltaTime`) a mesma reabertura media 22,1 a 26,9 ms, com um `eval` de controle já em 17,5 a 20,4 ms.
- **Contraste com alfa se mede nos pixels, em espaço linear (#61).** O projeto está em `ColorSpace.Linear` e o Unity mistura o alfa nele: a conta de contraste de um balão translúcido feita com `Color.Lerp` (em gama) deu 6,36:1 e a captura media 3,94:1. Calcule em espaço linear e confira em uma captura (`capture game_view --source screen`), lendo os pixels com `System.Drawing.Bitmap` no PowerShell. Para o pior caso (cena branca atrás de um painel translúcido), ponha uma `Image` branca de tela cheia logo abaixo do painel em Play Mode, por `script eval --file`.
- **Nitidez se julga em Full HD.** No Game view, use 1920×1080, não "16:9 Aspect".
- **Ids de save.** `ItemDataSO.Id` e o `persistentId` dos coletáveis são chaves do save. Um prefab duplicado copia o `persistentId`; use o menu de contexto **Regenerate Persistent Id** no componente. O `CollectableIdsTests` reprova id vazio ou repetido em qualquer cena.
- **Duplicar um objeto como o Ctrl+D, por CLI (#14).** `UnityEditor.Selection.activeGameObject = go; UnityEditor.Unsupported.DuplicateGameObjectsUsingPasteboard();` cria a cópia (`Item_Chave (1)`) e **o `OnValidate` roda**: o aviso de id repetido aparece na hora. Para descartar sem salvar, `DestroyImmediate` na cópia e `scene save`; o `.unity` volta idêntico ao do git (`EditorSceneManager.ClearSceneDirtiness` não existe nesta versão, e reabrir a cena suja abre um diálogo).
- **O Test Runner restaura as cenas abertas.** Uma cena que o `ProjectComponents` fechou por engano volta depois de `test run`, então a suíte sozinha não mostra esse defeito. Para verificar o auxiliar, chame-o por `eval` (`ProjetoVN.Tests.EditMode.ProjectComponents.ReadAll<UnityEngine.Transform, int>(t => 1)`) com uma cena descarregada na Hierarchy (`OpenScene(..., Additive)` seguido de `CloseScene(cena, false)`) e confira `SceneManager.sceneCount`.
- **O material do `Cinzel Bold SDF` muda no primeiro uso (#115).** Medir ou desenhar um texto em Cinzel muda `_ScaleRatioA/B/C` do material do font asset (`1` → `0.9`/`0.73125`/`0.73125`), e o `ClearFontAssetData(true)` **não** desfaz isso: são as razões de escala que o TextMeshPro calcula do material, e não glifos. O `Atkinson SDF` já está commitado com `_ScaleRatioA: 0.9` pelo mesmo motivo. A mudança do Cinzel foi commitada na #115 (é o estado estável do material em uso; sem ela, todo Editor suja o arquivo de novo): o diff do `.asset` tem só essas três linhas, nenhum glifo.
- **A altura preferida de uma `Image` fatiada ignora o multiplicador (#115).** `Image.preferredHeight` (e a largura) de um sprite `Sliced` é a soma das bordas dividida pelo `pixelsPerUnit`, **sem** o `Pixels Per Unit Multiplier`: um balão com borda 48 e multiplicador 2 informa 96 de altura mínima, e com a `Image` no mesmo objeto do `VerticalLayoutGroup` isso vira um piso (balões de uma linha ficaram ~18 px mais altos que o desenho). Ponha a `Image` em um filho esticado com `LayoutElement` `ignoreLayout`. Gravar a altura medida do texto no `LayoutElement` foi tentado e falha quando o texto cabe por décimos de pixel (o `GetPreferredValues` diz uma linha e o layout real quebra em duas).
- **`capture game_view` aceita `--width` e `--height` (#115).** Com `--source screen --width 1920 --height 1080` a captura sai em Full HD mesmo com a Game view pequena (sem eles, saiu 1280×720). Para a Game view também renderizar a 1920×1080 em Play, `UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "FullHD")` por `eval` funciona, e `Screen.width × Screen.height` passa a dar 1920×1080 (a resolução de renderização vale só em Play).
- **Com o limite de 30 FPS do Editor sem foco, "quadro abaixo de 20 ms" não existe (#115).** Quando o `UnfocusedFrameRateLimiter` age (`Application.targetFrameRate` 30), os quadros ociosos medem ~33 ms; sem ele, ~8 ms. Ao medir a abertura do histórico, use o quadro em que a duração volta ao piso do estado, e compare com a `main` **no mesmo Editor**, no mesmo dia: o número absoluto muda entre máquinas e dias (a primeira abertura de 200 balões deu ~450 ms na `main` em 2026-11-13 **com o limite de 30 FPS ativo**, contra 298 a 375 ms na #55; com o Editor com foco, a revisão mediu 380 a 470 ms na `main`, mediana 384).
- **O underlay do TextMeshPro tem teto, e o padrão do font asset não chega a 4 px (#116).** O deslocamento em pixels do atlas é `offset × _ScaleRatioC × _GradientScale` e satura em `|offset| = 1`. O `_ScaleRatioC` vem de `ShaderUtilities.UpdateShaderRatios`: `(GradientScale − 1 − range) / GradientScale`, com `range = max(_WeightNormal, _WeightBold)/4 × (GradientScale − 1)`. No material do `Cinzel Bold SDF` (`_WeightBold` 0,75) dá 0,73125 e o máximo em 46 pt é 3,7 px; com `_WeightBold = 0` no *preset* o `_ScaleRatioC` vai a 0,9 e os 4 px saem com `_UnderlayOffsetY` ≈ −0,87 (medido na captura: 4 px, `#DE7EB8` exato). **Mexer nas propriedades por script não recalcula o `_ScaleRatioC`**: chame `UpdateShaderRatios(mat)` antes de `AssetDatabase.CreateAsset`. Confira também que o `_MainTex` do preset é o atlas do font asset (o `OnValidate` do `MenuButtonUI` avisa).
- **Rótulo de botão com `ContentSizeFitter` dentro de `HorizontalLayoutGroup` (#116).** Com o pivô no centro, o ajuste do fitter cresce para os dois lados em volta do pivô; se o `sizeDelta` gravado no prefab for o antigo (200), o grupo posiciona o rótulo com a largura velha e o texto sai centralizado em vez de alinhado à esquerda. Um `script eval --file` que edita um prefab não roda o layout: ponha o pivô do rótulo em x 0 e chame `LayoutRebuilder.ForceRebuildLayoutImmediate` (no rótulo e no botão, com a janela ativa) antes de salvar.
- **`ForceMeshUpdate` em objeto inativo não mede nada (#116).** O `textInfo.characterInfo[0]` volta zerado; ative a janela (`SetActive(true)`) enquanto mede e desative depois. As coordenadas de `characterInfo[i].topLeft/bottomLeft` são locais ao rótulo e dão o topo e a base das maiúsculas sem precisar de captura (Cinzel Bold 46: topo +19,6 e base −15,2 em relação ao centro do rótulo).
- **`ToString("F2")` em `eval` usa a cultura do Windows (#116).** Com pt-BR sai `19,63`, e um `float.Parse` com `InvariantCulture` lê 1963. Formate e leia com `CultureInfo.InvariantCulture` nos dois lados.
- **Medir uma captura em Full HD sem PIL.** O Python do ambiente não tem Pillow: carregue o PNG em um `script eval --file` (`Texture2D.LoadImage(File.ReadAllBytes(...))`, `GetPixels32`) e varra as linhas atrás da cor desejada (grave a captura fora de `Assets/` com `--output-path`, e o `git status` não mostra nada; só se alguém a gravou dentro de `Assets/` apague a pasta com `asset delete` antes do commit e confira que o `git status` não mostra o `.meta` dela).
- **Rastreamento.** Para ver o fluxo de mensagens e as trocas de estado, acrescente `VN_TRACE_MESSAGES` em Project Settings → Player → Scripting Define Symbols. Isso altera `ProjectSettings`, então não vai para o commit.
