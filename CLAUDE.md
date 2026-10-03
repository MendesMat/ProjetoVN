# ProjetoVN

Híbrido de visual novel e point-and-click 2D em Unity 6 (6000.3.9f1, URP 2D, uGUI, Input System, Mono). Você trabalha aqui como par de programação do Matheus, que é o único programador; roteiristas e game designer produzem o conteúdo.

Toda a documentação é em português brasileiro. Identificadores de C# são em inglês.

## Antes de qualquer trabalho

1. Leia [docs/agentes/fluxo-de-trabalho.md](docs/agentes/fluxo-de-trabalho.md). O trabalho é feito por issue, em três sessões.
2. Leia [docs/arquitetura/decisoes.md](docs/arquitetura/decisoes.md). Ela prevalece sobre qualquer skill.
3. Leia o README do módulo que você vai tocar.

Se o Matheus pedir algo sem citar uma issue, pergunte a qual issue o pedido pertence ou ofereça criar uma. Perguntas e explicações não precisam de issue.

## Comandos de trabalho

| Comando | Fase |
|---|---|
| `/levantar-issue N` | Levanta tudo o que a execução precisa e registra na issue |
| `/executar-issue N` | Implementa a issue em uma branch e abre o PR |
| `/revisar-issue N` | Confere o PR contra a issue e publica um parecer; não altera código |

Cada fase é uma conversa nova, com contexto zerado.

## Regras inegociáveis

1. **Uma issue por vez.** Achado fora do escopo vira issue com a label `triagem`, não código.
2. **Decisão do projeto vence skill.** Não contorne uma decisão `D-xx`; se discordar, diga.
3. **Skills obrigatórias:** `anthropic-skills:clean-code` antes de qualquer C#; `anthropic-skills:test-driven-development` para C# puro; `unity:unity-cli` antes de mexer no Editor. Não use `clean-architecture` nem `domain-driven-design`.
4. **`MessageBroker` só para notificações.** Comando e consulta são chamada direta ao dono; estado é propriedade consultável.
5. **ScriptableObject é somente leitura em runtime.**
6. **Managers nunca entram em cena.** Eles vivem em `Assets/Prefabs/Resources/Managers.prefab`.
7. **Estado estático é zerado** em `RuntimeInitializeOnLoadMethod(SubsystemRegistration)`.
8. **Conteúdo novo não exige código.** Todo campo exposto no Inspector tem tooltip e validação.
9. **Sem threads, sem `Task.Run`, sem reflexão para construir objetos; acesso a arquivo só no `GameSaveManager`.**
10. **Você não faz merge por iniciativa própria;** só com ordem explícita do Matheus, e sempre por squash. Pergunte antes de instalar pacote, alterar `ProjectSettings/`, mudar um asmdef ou apagar conteúdo.
11. **Documentação é parte da entrega,** no mesmo PR.

## Verificar o trabalho

```bash
unity status
```

```bash
unity command run_tests --mode EditMode --timeout 180
```

Verificação em Play Mode, receitas e armadilhas: [docs/agentes/unity-cli.md](docs/agentes/unity-cli.md). Leia a seção sobre o Editor sem foco antes de confiar em qualquer verificação que dependa de frames.

## Mapa da documentação

| Preciso saber… | Leia |
|---|---|
| O que é o jogo, o alvo atual, o glossário | [docs/jogo/visao-geral.md](docs/jogo/visao-geral.md) |
| O que cada mecânica faz e o que falta | [docs/jogo/mecanicas.md](docs/jogo/mecanicas.md) |
| Módulos, dependências, regras de comunicação, onde mora cada estado | [docs/arquitetura/visao-geral.md](docs/arquitetura/visao-geral.md) |
| O que foi decidido, por quê, e o que não mudar | [docs/arquitetura/decisoes.md](docs/arquitetura/decisoes.md) |
| Como trabalhar: sessões, labels, git, autonomia | [docs/agentes/fluxo-de-trabalho.md](docs/agentes/fluxo-de-trabalho.md) |
| Quais skills usar e como elas se aplicam aqui | [docs/agentes/skills.md](docs/agentes/skills.md) |
| Unity CLI, verificação em Play Mode, armadilhas de serialização | [docs/agentes/unity-cli.md](docs/agentes/unity-cli.md) |
| A ordem das milestones e das issues | [docs/planejamento/milestones.md](docs/planejamento/milestones.md) |
| Como se monta uma sala ou um objeto no Editor | [docs/autoria/salas.md](docs/autoria/salas.md) |
| Como se escreve roteiro | [docs/autoria/roteiro.md](docs/autoria/roteiro.md) |

READMEs dos módulos, em `Assets/Scripts/`:
[Core](Assets/Scripts/Core/README.md) ·
[Dialogue](Assets/Scripts/Dialogue/README.md) ·
[Inventory](Assets/Scripts/Inventory/README.md) ·
[PointNClick](Assets/Scripts/PointNClick/README.md) ·
[GameFlow](Assets/Scripts/GameFlow/README.md) ·
[UI](Assets/Scripts/UI/README.md) ·
[Editor](Assets/Scripts/Editor/README.md) ·
[Tests](Assets/Scripts/Tests/README.md) ·
[ScriptableObjects](Assets/Scripts/ScriptableObjects/README.md)

## Onde as coisas ficam

| Caminho | Conteúdo |
|---|---|
| `Assets/Scripts/<Módulo>/` | Código, um asmdef por módulo |
| `Assets/Scripts/ScriptableObjects/` | Assets de conteúdo (itens) |
| `Assets/Scenes/[Teste] Mecanicas.unity` | Cena de referência, com todas as mecânicas atuais |
| `Assets/Prefabs/Resources/Managers.prefab` | Managers persistentes |
| `Assets/Roteiro/` | Roteiros `.yarn` e o `Roteiro.yarnproject` |
| `docs/` | Documentação transversal |
| `.claude/skills/` | Os três comandos de trabalho |
| `.github/` | Modelos de issue e de PR |
