# ProjetoVN

Um híbrido de **visual novel** e **point-and-click** em 2D, de tema escolar, feito em Unity. O jogador explora salas clicando em objetos e personagens, conversa em diálogos com escolhas, coleta itens e resolve travas com eles. As escolhas mudam a **afinidade** com os personagens, e a afinidade abre ou fecha trechos do roteiro.

O jogo é para desktop (Windows, Mac e Linux), em português brasileiro, e será distribuído por download no itch.io.

> **Em desenvolvimento.** O alvo atual é a [fatia vertical](docs/jogo/visao-geral.md#o-alvo-a-fatia-vertical): um percurso curto, do menu principal ao "Continuar", que prova que os sistemas funcionam juntos e que conteúdo novo se faz sem código. O que cada mecânica já faz está em [docs/jogo/mecanicas.md](docs/jogo/mecanicas.md).

## Por onde começar

Cada função da equipe tem um ponto de entrada próprio. Não é preciso ler o resto para começar.

| Você é… | Comece por | Ferramentas |
|---|---|---|
| **Roteirista** | [Guia de autoria: roteiro](docs/autoria/roteiro.md), a partir de [Na primeira vez: prepare o computador](docs/autoria/roteiro.md#na-primeira-vez-prepare-o-computador) | VS Code com a extensão Yarn Spinner, GitHub Desktop. Não precisa do Unity |
| **Game designer** (monta salas, itens e personagens) | [Guia de autoria: salas e objetos](docs/autoria/salas.md) | Unity Editor, pelo Inspector |
| **Programador** | [Arquitetura: visão geral](docs/arquitetura/visao-geral.md) e [Decisões](docs/arquitetura/decisoes.md) | Unity, Claude Code |
| **Agente de IA** | [CLAUDE.md](CLAUDE.md), depois o [fluxo de trabalho](docs/agentes/fluxo-de-trabalho.md) | Claude Code, Unity CLI |

## Abrir o projeto

1. Instale o **Unity 6000.3.9f1** pelo Unity Hub. Outra versão pode reimportar e alterar arquivos do projeto.
2. Instale o **Git**. O Unity precisa dele para baixar o pacote do Yarn Spinner, que vem de um repositório e não da loja de pacotes.
3. Clone este repositório e abra a pasta pelo Unity Hub (*Add project from disk*). A primeira importação demora.
4. Abra a cena `Assets/Scenes/[Teste] Mecanicas.unity` e dê Play. Ela é a cena de referência, com um exemplo de cada mecânica que existe hoje.

Os testes ficam na janela **Window → General → Test Runner**, aba *EditMode*. Como rodá-los pelo terminal está no [README dos testes](Assets/Scripts/Tests/README.md).

## Tecnologia

| | |
|---|---|
| Motor | Unity 6000.3.9f1, URP 2D, backend Mono |
| Interface | uGUI com TextMeshPro |
| Entrada | Input System |
| Diálogo | [Yarn Spinner](https://www.yarnspinner.dev/) 3.2, com o roteiro em arquivos `.yarn` |
| Testes | Unity Test Framework, em EditMode |

O código fica em `Assets/Scripts/`, um módulo por pasta, cada um com o seu asmdef e o seu README:

| Módulo | O que faz |
|---|---|
| [Core](Assets/Scripts/Core/README.md) | Barramento de notificações, máquina de estados e estado da história |
| [Dialogue](Assets/Scripts/Dialogue/README.md) | Adaptador do Yarn Spinner e a interface do diálogo |
| [Inventory](Assets/Scripts/Inventory/README.md) | Itens do jogador e objetos coletáveis |
| [PointNClick](Assets/Scripts/PointNClick/README.md) | Clique e destaque no mundo, deslocamento do cenário pela borda da tela |
| [GameFlow](Assets/Scripts/GameFlow/README.md) | Modos de jogo, save, portões e comandos de roteiro; liga os outros módulos |
| [UI](Assets/Scripts/UI/README.md) | Menus e painel de inventário |
| [Editor](Assets/Scripts/Editor/README.md) | Ferramentas do Inspector para quem monta conteúdo |
| [Tests](Assets/Scripts/Tests/README.md) | Testes EditMode |
| [ScriptableObjects](Assets/Scripts/ScriptableObjects/README.md) | Os assets de conteúdo: itens e personagens |

Como os módulos se enxergam e conversam está em [docs/arquitetura/visao-geral.md](docs/arquitetura/visao-geral.md).

## Onde as coisas ficam

| Caminho | Conteúdo |
|---|---|
| `Assets/Roteiro/` | O roteiro, em pastas por ato, capítulo e episódio |
| `Assets/Scenes/` | A cena de referência e o menu. Cada sala será uma cena |
| `Assets/Scripts/ScriptableObjects/` | Itens e personagens |
| `Assets/Sprites/Personagens/<personagem>/` | Arte de personagem, um arquivo por expressão |
| `Assets/Prefabs/Resources/Managers.prefab` | Os sistemas globais e a interface de jogo, criados sozinhos ao dar Play. Nunca vão dentro de uma cena |
| `docs/` | Toda a documentação do projeto |

## Como o trabalho anda

- **Planejamento:** o trabalho é dividido em milestones, feitas uma por vez, na ordem de [docs/planejamento/milestones.md](docs/planejamento/milestones.md). O andamento de cada issue vive só no GitHub, nas [issues](https://github.com/MendesMat/ProjetoVN/issues) e nas [milestones](https://github.com/MendesMat/ProjetoVN/milestones).
- **Código:** cada issue passa por três sessões (levantamento, execução e revisão) e chega à `main` por um único PR. O processo está em [docs/agentes/fluxo-de-trabalho.md](docs/agentes/fluxo-de-trabalho.md).
- **Roteiro:** o roteirista envia por um PR próprio, sem issue, como descrito em [Como enviar](docs/autoria/roteiro.md#como-enviar). O programador confere e junta ao jogo.
- **Ideias e problemas novos** viram uma issue com a label `triagem`, e o programador decide quando entram.

## Mapa da documentação

| Preciso saber… | Leia |
|---|---|
| O que é o jogo, o alvo atual, o glossário | [docs/jogo/visao-geral.md](docs/jogo/visao-geral.md) |
| O que cada mecânica faz e o que falta | [docs/jogo/mecanicas.md](docs/jogo/mecanicas.md) |
| Como escrever roteiro | [docs/autoria/roteiro.md](docs/autoria/roteiro.md) |
| Como montar uma sala ou um objeto no Editor | [docs/autoria/salas.md](docs/autoria/salas.md) |
| Módulos, dependências e onde mora cada estado | [docs/arquitetura/visao-geral.md](docs/arquitetura/visao-geral.md) |
| O que foi decidido, e por quê | [docs/arquitetura/decisoes.md](docs/arquitetura/decisoes.md) |
| A ordem das milestones | [docs/planejamento/milestones.md](docs/planejamento/milestones.md) |
| Como os agentes de IA trabalham aqui | [docs/agentes/fluxo-de-trabalho.md](docs/agentes/fluxo-de-trabalho.md), [skills.md](docs/agentes/skills.md), [unity-cli.md](docs/agentes/unity-cli.md) |
