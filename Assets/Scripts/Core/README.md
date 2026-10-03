# Core

O módulo **Core** guarda o que todos os outros módulos podem usar e que não pertence a nenhuma feature. Ele não referencia nenhum outro módulo do projeto.

> **Antes de mudar qualquer coisa aqui:** [regras de comunicação](../../../docs/arquitetura/visao-geral.md#regras-de-comunicação) · [decisões](../../../docs/arquitetura/decisoes.md) · [fluxo de trabalho](../../../docs/agentes/fluxo-de-trabalho.md)

---

## O que há aqui

| Pasta | Conteúdo | Detalhes |
|---|---|---|
| `Messaging/` | `MessageBroker` e `IMessage`: o barramento de **notificações** | [Messaging/README.md](Messaging/README.md) |
| `StateMachine/` | `StateMachine` e `BaseState`: a máquina de modos de jogo, em C# puro | [StateMachine/README.md](StateMachine/README.md) |
| `State/` | `StoryState`, `StoryVariableName` e `StoryFlagAttribute`: o estado global da história, a regra dos nomes e o marcador de campo de variável booleana | abaixo |

## O critério para algo morar no Core

Uma classe entra aqui só se **dois ou mais módulos que não se enxergam** precisam dela. `StoryState` está aqui porque `Dialogue`, `GameFlow`, `PointNClick` e `UI` podem precisar ler uma variável de história, e nenhum deles referencia todos os outros. Uma classe usada por um módulo só mora nesse módulo.

O Core não conhece diálogo, inventário nem cena. Se uma classe daqui precisar de um tipo de outro módulo, ela está no lugar errado.

## `StoryState`

Estado global da história (decisão D-18): booleanos, números e textos por nome ("falou com a gótica", "afinidade com a gótica", "nome do protagonista"). É estático pelo mesmo motivo que o `MessageBroker`: o dono é o jogo inteiro, e quem lê não precisa de referência serializada. Ele é a fonte única que o roteiro, os portões e o save leem e gravam.

```csharp
StoryState.IsTrue("$falou_com_gotica");                 // booleano gravado e verdadeiro
StoryState.TryGetBool("$falou_com_gotica", out bool b); // distingue "falso gravado" de "nunca gravado"
StoryState.TryGetNumber("$afinidade_gotica", out float n);
StoryState.TryGetText("$nome_jogador", out string t);

StoryState.SetBool("$falou_com_gotica", true);          // true se gravou
StoryState.SetNumber("$afinidade_gotica", 2.5f);
StoryState.SetText("$nome_jogador", "Ana");             // null vira ""

StoryState.Bools; StoryState.Numbers; StoryState.Texts; // IReadOnlyDictionary, usados pelo save
StoryState.ReplaceAll(bools, numbers, texts);           // usado ao carregar um save; cada um aceita null
StoryState.ClearAll();
```

Regras:
- **Um nome mora em um só tipo.** Gravar com outro tipo tira o nome dos demais (o último tipo gravado vence). Ler como um tipo diferente do gravado devolve `false`; não há conversão.
- **Falso gravado não é nunca gravado.** `SetBool(nome, false)` deixa `TryGetBool` devolvendo `true` com valor `false`; o Yarn Spinner precisa dessa diferença para não voltar ao valor inicial.
- **Nome vazio ou em branco** é sempre inválido: nada é gravado, a leitura devolve `false`, **sem log**. Componentes de cena contam com isso para degradar sem erro quando um campo de variável fica vazio.
- **Nome preenchido sem `$`** não é gravado e loga um aviso (`[StoryState] Nome de variável inválido`). A leitura devolve `false` sem log.
- O número é `float`, porque o Yarn só tem `float`.
- É zerado no início de cada Play (`SubsystemRegistration`), como todo estado estático do projeto.
- É persistido pelo `GameSaveManager` (módulo `GameFlow`), nos campos `storyBools`, `storyNumbers` e `storyTexts` do `GameState`.
- **Não existe mensagem de "variável mudou".** Nada a escutaria hoje, e uma mensagem sem assinante falha em silêncio. Quem precisa do valor consulta.

Quem escreve hoje: o roteiro, pelo `StoryStateVariables` do módulo `Dialogue` (o armazenamento de variáveis do Yarn Spinner, que não guarda valor nenhum e vai direto ao `StoryState`, inclusive o contador de visitas `$Yarn.Internal.Visiting.<nó>`), e o `LockedActionBehaviour` (ao destrancar), do `GameFlow`.

## `StoryVariableName`

Duas regras de nome, de propósito diferentes:

| Método | O que aceita | Quem usa |
|---|---|---|
| `IsValid(nome)` | Não vazio, começa com `$` e tem pelo menos um caractere depois | O `StoryState`, ao gravar. É frouxa porque o Yarn Spinner grava nomes internos fora da convenção (`$Yarn.Internal.Visiting.<nó>`, que sustenta `visited()`) |
| `FollowsConvention(nome)` | `$`, depois minúscula ASCII ou `_`, depois minúsculas ASCII, dígitos e `_` (ex.: `$porta_biblioteca_destrancada`) | O `OnValidate` dos componentes de cena. Todo nome que passa aqui é uma variável válida no roteiro |

O nome é guardado **com o `$`** em todo lugar: roteiro, Inspector e save.

## `StoryFlagAttribute`

Um `PropertyAttribute` sem parâmetros que marca um campo `string` com o nome de uma **variável booleana de história**:

```csharp
[SerializeField, StoryFlag] private string unlockedFlagId;
```

O Core só guarda o marcador. Quem o desenha é o módulo [Editor](../Editor/README.md): o campo vira uma lista das variáveis `Bool` declaradas em `Assets/Roteiro/variaveis.yarn`, com a descrição da escolhida e um aviso para um valor que não está declarado. O atributo não muda nome nem tipo do campo, então cenas e prefabs já gravados continuam valendo. Hoje é usado pelo `LockedActionBehaviour`.

## O registro de variáveis

O registro das variáveis de história é o arquivo `Assets/Roteiro/variaveis.yarn`: cada variável é declarada ali, uma só vez, com `<<declare>>` e uma linha `///` de descrição. Ele não é código do Core; o Core só guarda os valores. A regra é cobrada por testes (`ScriptContentTests`): toda declaração está nesse arquivo, tem descrição e segue `StoryVariableName.FollowsConvention`. Como declarar: [docs/autoria/roteiro.md](../../../docs/autoria/roteiro.md#variáveis-e-afinidade).
