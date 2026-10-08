# Tests

Este módulo é reservado para os testes automatizados do projeto (Unity Test Framework).

> **Antes de escrever testes:** [regras de comunicação](../../../docs/arquitetura/visao-geral.md#regras-de-comunicação) · [decisão D-24, sobre testes](../../../docs/arquitetura/decisoes.md#d-24--testes) · [como a TDD se aplica aqui](../../../docs/agentes/skills.md#como-a-tdd-se-aplica-aqui)

Os testes vivem em `EditMode/` e rodam pela janela **Window → General → Test Runner**, aba *EditMode*, ou pelo Unity CLI:

```bash
unity command run_tests --mode EditMode --timeout 180
```

- **`MessageBrokerTests`**: entrega, cancelamento de assinatura, assinatura duplicada, isolamento de exceções, `Clear`, e o comportamento de snapshot quando alguém assina durante um despacho.
- **`StoryStateVariablesTests`**: o armazenamento de variáveis do Yarn sobre o `StoryState`: gravar e ler cada tipo, ler o que foi gravado por fora, sem conversão silenciosa de tipo, nome sem `$` com aviso, `Clear`.
- **`ScriptVariablesTests`**: roteiros curtos de verdade sobre o `StoryState`: `<<set>>`, `<<if>>`, valor inicial declarado, incremento, `visited()` (com o contador no `StoryState`), texto interpolado, a etiqueta `lastline` que o apresentador usa, opção condicional (indisponível enquanto a condição é falsa, disponível quando o `StoryState` a satisfaz, bloco todo indisponível) e a descrição `///` de uma declaração.
- **`ScriptContentTests`**: os arquivos reais de `Assets/Roteiro/` compilam sem erro **nem aviso** (pega `<<jump>>` para nó inexistente, variável não declarada e código inalcançável antes do Play), contêm os nós de teste (`ProjectScripts_ContainTheTestNodes`, sem fixar o tamanho do projeto: um roteiro novo não exige mexer no teste), todo nó do projeto segue a convenção de nome (`ProjectScripts_AllNodesFollowTheNamingConvention`) e dizem o que o jogo mostra. Todo id de item citado existe no único `ItemRegistry` do projeto e é literal (`ProjectScripts_CiteOnlyItemIdsThatExistInTheItemRegistry`). O registro de variáveis é cobrado aqui: toda variável é declarada em `variaveis.yarn`, tem descrição e segue a convenção de nome, e as três variáveis da cena de teste existem com o tipo certo. A conversa da Gótica tem os testes da afinidade (sobe no "Sim", não sobe no "Não") e da opção condicional (bloqueada na primeira conversa, disponível na segunda). Os personagens são cobrados contra o único `CharacterRegistry` do projeto: todo nome antes dos dois-pontos é o nome exibido de um personagem do registro (`ProjectScripts_NameOnlyCharactersInTheCharacterRegistry`, a mensagem traz nó, linha e nome); toda etiqueta de expressão está em fala com nome, é a única da fala e existe nos retratos de quem fala (`ProjectScripts_UseOnlyExpressionsTheSpeakerHas`); o registro tem ids únicos no formato, nomes exibidos únicos, e retratos com expressão no formato, sem repetição e com sprite (`CharacterRegistry_HasUniqueIdsAndNamesInTheConvention`); e a Gótica pede `raiva` em "Ele falou não." (`GoticaRespostaNao_AsksForTheAngryExpression`).
- **`ExpressionTagTests`**: o `ExpressionTag`, que separa a etiqueta de expressão das que o Yarn põe sozinho (`lastline`, `line:…`).
- **`ScriptSpeakersTests`**: o `ScriptSpeakers`, que lista, por linha do roteiro compilado, o nó, o número da linha, quem fala e as etiquetas de expressão. Lê o personagem com o `LineParser` do próprio Yarn, então o escape `\:` e o nome vindo de expressão (`{0}`) se comportam como no jogo. Narração e opção não têm personagem; `TaggedWithoutSpeaker` lista as etiquetas nelas.
- **`ScriptItemReferencesTests`**: o `ScriptItemReferences`, que lê do roteiro compilado cada `dar_item`, `remover_item` e `tem_item` com o nó e o id, e separa as referências sem id literal (variável, expressão, id ausente, parâmetro a mais). Lê o programa, não o texto, então um comando em comentário não conta.
- **`ScriptItemCommandsTests`**: `tem_item` em uma condição, com e sem o item, e o texto que `dar_item` e `remover_item` entregam ao jogo.
- **`ConversationNotifierTests`**: um `DialogueStartedMessage` e um `DialogueEndedMessage` por conversa, e nunca um Ended sem Started.
- **`ScriptNodeNameTests`**: o formato dos nomes de nó.
- **`StoryVariableNameTests`**: as duas regras de nome: o que o armazenamento aceita (inclusive os nomes internos do Yarn) e o que quem monta cena pode digitar.
- **`StoryStateTests`**: booleano, número e texto: leitura e escrita, falso gravado × nunca gravado, nomes inválidos (vazio em silêncio, sem `$` com aviso), um nome em um só tipo, substituição total e limpeza.
- **`StoryStatePersistenceTests`**: a cópia entre `StoryState` e `GameState` (`Capture` e `Restore`), incluindo a ida e volta pelo JSON com afinidade, falso gravado e acentos.
- **`GameStateTests`**: ida e volta do JSON, com os três tipos de estado da história.

> **Lacuna conhecida e deliberada:** `LockedActionBehaviour`, o `DialogueManager`, o apresentador
> `DialogueUIController` e o `InteractableDialogueTrigger` não têm teste de EditMode. Todos exigiriam um `GameObject` (o `InventoryManager`, o `DialogueRunner`), o que quebraria
> a regra "C# puro, sem GameObjects" desta suíte; o `LockedActionBehaviour` ainda por cima é um
> `MonoBehaviour` cujas asserções interessantes são sobre `UnityEvent` disparando. O `ItemScriptActions`
> é cola entre três singletons e fica na mesma lacuna. A `Tests.asmdef` referencia `ProjetoVN.Inventory`
> só para o teste de ids usar o `ItemRegistry`. São verificados em Play Mode na cena `[Teste] Mecanicas`
> (o roteiro de verificação de cada issue). O que o `DialogueManager` decide sem Unity mora em classes
> puras testadas aqui: `ConversationNotifier` e `ScriptNodeName`.
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
...")` compila um texto, `ScriptRun.FromProjectFiles()` compila os `.yarn` de `Assets/Roteiro/`, e `Start`, `Choose`, `Lines`, `OptionTexts`, `Completed` e `ErrorsAndWarnings` dizem o que o jogador veria; `Declarations` lista as variáveis explícitas do roteiro (nome, tipo, descrição, arquivo), `UnavailableOptionTexts` guarda as opções do bloco em espera cuja condição é falsa (elas também estão em `OptionTexts`; `Choose(i)` indexa todas, inclusive as indisponíveis), `Commands` guarda o texto de cada comando, `OwnedItems` é o inventário que `tem_item` consulta, `Program` é o roteiro compilado e `StringTable` é a tabela de textos da compilação (texto, nó, linha e etiquetas de cada linha), que o `ScriptSpeakers` lê. Ele roda sobre o `StoryState` de verdade, então o `SetUp` e o `TearDown` chamam `StoryState.ClearAll()`. Escreva `Yarn.Dialogue` por extenso: dentro de `ProjetoVN.*`, `Dialogue` é o namespace.
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
