# Core

O módulo **Core** guarda o que todos os outros módulos podem usar e que não pertence a nenhuma feature. Ele não referencia nenhum outro módulo do projeto.

> **Antes de mudar qualquer coisa aqui:** [regras de comunicação](../../../docs/arquitetura/visao-geral.md#regras-de-comunicação) · [decisões](../../../docs/arquitetura/decisoes.md) · [fluxo de trabalho](../../../docs/agentes/fluxo-de-trabalho.md)

---

## O que há aqui

| Pasta | Conteúdo | Detalhes |
|---|---|---|
| `Messaging/` | `MessageBroker` e `IMessage`: o barramento de **notificações** | [Messaging/README.md](Messaging/README.md) |
| `StateMachine/` | `StateMachine` e `BaseState`: a máquina de modos de jogo, em C# puro | [StateMachine/README.md](StateMachine/README.md) |
| `State/` | `StoryFlags`: o conjunto global de flags de história | abaixo |

## O critério para algo morar no Core

Uma classe entra aqui só se **dois ou mais módulos que não se enxergam** precisam dela. `StoryFlags` está aqui porque `Dialogue`, `GameFlow`, `PointNClick` e `UI` podem precisar ler uma flag, e nenhum deles referencia todos os outros. Uma classe usada por um módulo só mora nesse módulo.

O Core não conhece diálogo, inventário nem cena. Se uma classe daqui precisar de um tipo de outro módulo, ela está no lugar errado.

## `StoryFlags`

Conjunto estático de flags booleanas ("falou com a gótica", "porta destrancada"). É estático pelo mesmo motivo que o `MessageBroker`: o dono é o jogo inteiro, e quem lê não precisa de referência serializada.

```csharp
StoryFlags.IsSet("falou-com-gotica");   // bool
StoryFlags.Set("falou-com-gotica");     // true se mudou de estado
StoryFlags.Clear("falou-com-gotica");   // true se mudou de estado
StoryFlags.All;                         // IReadOnlyCollection<string>, usado pelo save
StoryFlags.ReplaceAll(flags);           // usado ao carregar um save; aceita null
StoryFlags.ClearAll();
```

Regras:
- Um id vazio ou em branco é sempre inválido: `IsSet` devolve `false` e `Set` não grava nada. Componentes de cena contam com isso para degradar sem erro quando um campo de flag fica vazio.
- É zerado no início de cada Play (`SubsystemRegistration`), como todo estado estático do projeto.
- É persistido pelo `GameSaveManager` (módulo `GameFlow`), no campo `storyFlagIds`.
- **Não existe mensagem de "flag mudou".** Nada a escutaria hoje, e uma mensagem sem assinante falha em silêncio. Quem precisa do valor consulta.

Quem escreve hoje: `SetFlagEffect` e `ClearFlagEffect` (efeitos de diálogo) e `LockedActionBehaviour` (ao destrancar), todos em `GameFlow`.

## Mudanças planejadas

- **Issue #4:** `StoryFlags` é substituído por `StoryState`, que guarda booleano, número e texto e passa a ser a fonte única para o roteiro, as portas e o save (decisão D-18).
