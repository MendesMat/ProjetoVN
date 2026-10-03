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

**Salve a cena antes de rodar os testes.** Com a cena aberta suja, o Test Runner abre o diálogo "Scene(s) Have Been Modified" e bloqueia a thread principal do Editor: `run_tests` e `eval` expiram sem erro claro, e só um clique no diálogo destrava (medido na execução da #3). Rode `save_scene` depois de qualquer alteração de cena e antes do `run_tests`.

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

### A armadilha do Editor sem foco

**Um Editor dirigido pelo CLI está sem foco, e o projeto tem `runInBackground` desligado. Nessa situação o Play Mode congela:** nenhum frame avança, então `Start()`, `Update()` e corrotinas não rodam. O sintoma engana, porque `SceneManager.LoadScene` ainda conclui e os objetos são recriados, e parece que o código de restauração é que está quebrado.

Toda verificação que dependa de frames começa com:

```bash
unity command eval 'UnityEngine.Application.runInBackground = true; return UnityEngine.Time.frameCount;'
```

Rode de novo alguns segundos depois. Se o número não mudou, o jogo está parado e qualquer conclusão sobre `Start()` é falsa. A atribuição é só de runtime e não altera `ProjectSettings`.

Asserções feitas só por chamada direta de método (`porta.Interact()`, `GameSaveManager.Instance.Save()`) não dependem de frames e valem mesmo com o jogo parado.

### Comandos que falham com "Network error" logo depois de criar ou apagar scripts

Depois de mexer em `.cs`, o Editor recarrega o domínio, e um `eval` ou `recompile` enviado nesse intervalo falha com `Network error: An error occurred while sending the request`. Não é erro do código: espere e rode `unity command recompile_status` até `completed`, depois repita o comando.

### O `$` e o shell

Os nomes de variável de história começam com `$`. Dentro de aspas duplas o shell o expande para vazio e o `eval` roda com o nome errado, sem erro. Todo `eval` com um nome como `$falou_com_gotica` vai entre aspas simples.

### O erro de console que é do CLI, não do jogo

Um `eval` enviado enquanto o Editor ainda está entrando em Play Mode pode estourar o tempo de resposta. O comando falha e o pacote Pipeline deixa um erro no console:

```
Failed to handle /api/exec request: Main thread operation timed out after 5000ms
```

Esse erro não vem do jogo. Espere uns dez segundos depois do `editor_play` antes do primeiro `eval`. Se o erro aparecer mesmo assim, ele não conta como falha de "console sem erro"; para uma leitura limpa, saia do Play Mode, rode `clear_console` e repita.

### O que não dá para simular

O CLI não simula clique de mouse. Um comportamento que dependa do clique real (como o clique que encerra um diálogo não atingir o mundo) é verificado chamando os métodos na mesma ordem e conferindo o estado, e a issue registra que o teste com mouse de verdade fica para o Matheus.

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

### Managers não entram em cena

`DialogueManager`, o `DialogueRunner` do Yarn Spinner, `StoryStateVariableStorage`, `InventoryManager`, `GameStateController`, `DialogueInputHandler` e `GameSaveManager` existem só no `Managers.prefab`. Um deles colocado em uma cena sobrescreve o `Instance` do persistente.

## Armadilhas do Yarn Spinner

Descobertas na issue #5. A prova de conceito (#3) e o código do pacote estão em `Library/PackageCache/dev.yarnspinner.unity@*/`.

- **Comando desconhecido trava o diálogo.** Um `<<comando>>` sem handler faz o runner logar erro e parar sem avançar. O `DialogueManager.SkipUnknownCommand`, ligado ao `onUnhandledCommand` por **ligação persistente** no `Managers.prefab` (um `AddListener` em runtime não conta), ignora o comando com um erro e a conversa segue. Mesmo assim: **não escreva um comando no `.yarn` antes de ele existir** (o compilador não valida o nome).
- **`<<jump>>` para nó inexistente compila com aviso.** O compilador dá só o aviso `YS0012`, e em runtime o Yarn lança e para sem chamar o fim da conversa. A rede de segurança no `Update` do `DialogueManager` encerra a conversa com um erro. O `ScriptContentTests` reprova qualquer aviso nos roteiros, o que pega o erro antes do Play.
- **Um `.yarn` só vira conteúdo dentro de um `.yarnproject`.** Os roteiros ficam em `Assets/Roteiro/`, cobertos por `Assets/Roteiro/Roteiro.yarnproject` (`**/*.yarn`). Um `.yarn` fora dessa pasta não é importado.
- **Arquivos criados pelo pacote em `ProjectSettings/Packages/dev.yarnspinner/`.** O `YarnSpinnerProjectSettings.json` é commitado (a #5 foi autorizada a isso); `*.ysls.json`, se aparecerem, ficam fora do commit até a #8 decidir. O pacote também abre a janela "About Yarn Spinner" na primeira carga.
- **O choque de nome `Dialogue`.** Dentro de `ProjetoVN.*`, `Dialogue` resolve para o namespace `ProjetoVN.Dialogue`, não para a classe do Yarn. Escreva `Yarn.Dialogue` por extenso.
- **`lastline`.** O compilador etiqueta com `lastline` a fala que é o comando **imediatamente anterior** a um bloco de opções, e o apresentador usa isso para mostrar fala e opções juntas. Um `<<set>>` entre a fala e as opções tira a etiqueta, e o jogador passa a precisar de um clique a mais.
- **Narração com dois-pontos vira personagem.** `Atenção: a porta fechou.` é lida como a personagem "Atenção". Evite dois-pontos no começo de uma fala de narração.
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

## Outras armadilhas do projeto

- **Arte da interface em SVG.** Os SVGs são importados como Textured Sprite e desenhados com `Image` comum. Cada textura tem exatamente o tamanho do elemento em 1080p, porque o importador não gera mipmaps e uma textura maior volta a serrilhar. Detalhes no [README do Dialogue](../../Assets/Scripts/Dialogue/README.md).
- **Nitidez se julga em Full HD.** No Game view, use 1920×1080, não "16:9 Aspect".
- **Ids de save.** `ItemDataSO.Id` e o `persistentId` dos coletáveis são chaves do save. Um prefab duplicado copia o `persistentId`; use o menu de contexto **Regenerate Persistent Id** no componente.
- **Rastreamento.** Para ver o fluxo de mensagens e as trocas de estado, acrescente `VN_TRACE_MESSAGES` em Project Settings → Player → Scripting Define Symbols. Isso altera `ProjectSettings`, então não vai para o commit.
