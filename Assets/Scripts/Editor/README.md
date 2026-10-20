# Editor

Código que **só existe no Editor do Unity** e não entra na build do jogo. Hoje tem dois assuntos: o seletor de variável de história e o seletor de nó do roteiro, os dois no Inspector. O asmdef `ProjetoVN.Editor` é restrito à plataforma Editor e referencia `ProjetoVN.Core` e o Yarn Spinner (`YarnSpinner.Unity` e `YarnSpinner.Unity.Editor`, mais as DLLs `YarnSpinner.dll` e `Yarn.Google.Protobuf.dll`, que o asmdef lista em `precompiledReferences` para ler o programa compilado). Nenhum módulo de runtime o referencia, nem pode: um asmdef de runtime não pode depender de um de Editor sem quebrar a build.

> **Antes de mudar qualquer coisa aqui:** [decisões](../../../docs/arquitetura/decisoes.md) (D-01, D-18) · [fluxo de trabalho](../../../docs/agentes/fluxo-de-trabalho.md)

---

## O que há aqui

| Classe | Papel |
|---|---|
| `StoryFlagDrawer` | `PropertyDrawer` do `[StoryFlag]` (do `Core`). Desenha o campo `string` como um dropdown de UI Toolkit |
| `DeclaredStoryFlags` | Lê do Yarn Spinner as variáveis booleanas declaradas. Devolve a lista, ou o motivo de não ter conseguido |
| `ScriptNodeDrawer` | `PropertyDrawer` do `[ScriptNode]` (do `Core`). Desenha o campo `string` como um dropdown de UI Toolkit com os nós do roteiro |
| `ScriptNodes` | Lê do programa compilado do `YarnProject` os nomes dos nós. Devolve a lista, ou o motivo de não ter conseguido |
| `TheYarnProject` | Acha o único `YarnProject` e confere que o roteiro foi importado sem erro. É o que `DeclaredStoryFlags` e `ScriptNodes` têm em comum |
| `NameChoiceField` | O campo de UI Toolkit dos dois seletores: lista de nomes com o rótulo do vazio, descrição opcional e aviso de valor ausente. Traz também o texto comum com aviso, usado quando a lista não pode ser montada |

## O seletor de variável de história

`[StoryFlag]` em um campo `string` o transforma, no Inspector, em uma lista:

- as opções são **(nenhuma)** (grava texto vazio, que os campos aceitam) e as variáveis `Bool` **explícitas** de `Assets/Roteiro/variaveis.yarn`, em ordem alfabética. Numéricas, de texto e as internas do Yarn (`$Yarn.Internal.*`) não aparecem;
- a **descrição** (`///`) da variável escolhida aparece abaixo do campo, e o `[Tooltip]` do campo é preservado;
- um valor que **não está declarado** continua no campo, entra na lista e ganha um `HelpBox` de aviso;
- se o projeto não tem exatamente um `YarnProject`, ou o roteiro não compila, o campo vira um texto comum com um `HelpBox` dizendo o motivo, para não travar quem monta a cena.

Como funciona:

- `DeclaredStoryFlags` acha o único `YarnProject` por `AssetDatabase.FindAssets("t:YarnProject")` e lê `YarnProjectImporter.ImportData.serializedDeclarations` **a cada vez que o Inspector é montado**. Não usa `YarnProject.InitialValues` nem `Program`, que guardam cache até o próximo `Awake`.
- Depois de editar `variaveis.yarn`, o Unity reimporta o `YarnProject`; a lista se atualiza na próxima vez que o Inspector é montado (selecionar outro objeto e voltar basta).
- `CreatePropertyGUI` só é usado quando o Inspector do componente é de UI Toolkit, que é o caso do `LockedActionBehaviour` (sem editor customizado). Não crie um `Editor` IMGUI para um componente que usa `[StoryFlag]`.

## O seletor de nó do roteiro

`[ScriptNode]` em um campo `string` o transforma, no Inspector, em uma lista dos nós de `Assets/Roteiro/` (hoje, o campo **Node Name** do `InteractableDialogueTrigger`):

- as opções são **(nenhum)** (grava texto vazio) e os nós do `Roteiro.yarnproject`, em ordem alfabética. A lista **não traz o nó `variaveis`**: ele só declara variáveis (`variaveis.yarn`) e não tem fala, então escolhê-lo abriria e fecharia uma conversa vazia;
- um valor que **não está na lista** (nó renomeado, apagado, ou o próprio `variaveis`) continua no campo, entra no fim da lista e ganha um `HelpBox` de aviso;
- se o projeto não tem exatamente um `YarnProject`, ou o roteiro não compila, o campo vira um texto comum com um `HelpBox` dizendo o motivo, como no `[StoryFlag]`.

Como funciona:

- `ScriptNodes` lê `Yarn.Program.Parser.ParseFrom(project.compiledYarnProgram).Nodes` **a cada vez que o Inspector é montado**, sem cache próprio. Não usa `YarnProject.Program` nem `YarnProject.NodeNames`, que guardam cache (o achado da #3). **Só chame `ParseFrom` depois de `TheYarnProject` conferir o `ImportStatus`:** com erro de compilação `compiledYarnProgram` é `null` e o parse lança. É por esse `ParseFrom` que o asmdef lista `Yarn.Google.Protobuf.dll`.
- Depois de criar, renomear ou apagar um nó, o Unity reimporta o `YarnProject`; a lista se atualiza na próxima vez que o Inspector é montado.
- Como no `[StoryFlag]`, `CreatePropertyGUI` só roda em Inspector de UI Toolkit. O `InteractableDialogueTrigger` não tem `Editor` customizado; não crie um IMGUI.
- A lista é plana, sem busca nem agrupamento por arquivo (D-27). Com o roteiro crescendo, uma lista com busca vira issue quando ficar difícil de usar.

## Limitações

- Só cobre variáveis **booleanas**. Um seletor para número ou texto fica para quando uma issue pedir (D-27).
- Renomear uma variável em `variaveis.yarn` deixa órfão o campo de uma cena que a usava, e o aviso só aparece com o objeto selecionado. O mesmo vale para um nó renomeado no `.yarn`: quem acusa, sem ninguém abrir o objeto, é o teste `DialogueTriggerNodesTests` (ver o [README dos testes](../Tests/README.md)).
- É código de Editor: não tem teste automatizado (D-24). Foi verificado lendo o Inspector dos dois seletores por `eval`; o clique no dropdown e a aparência ficam para o programador.
