# Skills

Quais skills o agente usa neste projeto, quando, e o que fazer quando uma skill e o projeto discordam.

## Regra de precedência

**Uma decisão do projeto prevalece sobre qualquer skill** (decisão D-26). A ordem, da mais forte para a mais fraca:

1. As decisões de [decisoes.md](../arquitetura/decisoes.md) e as regras de [visao-geral.md](../arquitetura/visao-geral.md).
2. As convenções já presentes no código vizinho.
3. A skill.

Se o agente achar que uma decisão está errada, ele diz isso ao Matheus. Ele não a contorna com o argumento de que a skill manda.

## Skills obrigatórias

| Skill | Quando |
|---|---|
| `anthropic-skills:clean-code` | Antes de escrever ou alterar **qualquer** código C#, e ao revisar código |
| `anthropic-skills:test-driven-development` | Para toda classe C# pura (sem `MonoBehaviour`): o teste vem antes do código |
| `unity:unity-cli` | Antes de qualquer interação com o Editor ou com o Unity CLI |

## Skills por situação

| Situação | Skill |
|---|---|
| Criar ou alterar interface (menus, HUD, painéis) | `unity:ui`, que encaminha para `unity:ui-ugui`. O projeto usa **uGUI** |
| Fontes, atlas de fonte, texto com acentos | `unity:optimize-text-mesh-pro` |
| Instalar, remover ou atualizar um pacote | `unity:unity-package-management` (e perguntar antes, ver [fluxo-de-trabalho.md](fluxo-de-trabalho.md#autonomia)) |
| Mixer e roteamento de áudio (milestone M5) | `unity:audio-setup-mixers` |
| Importação e memória de áudio (milestone M5) | `unity:optimize-audio` |
| Recortar ou configurar sprites | `unity:sprite-editor` |
| Atlas de sprites | `unity:manage-sprite-atlas` |
| Achar assets ou objetos de cena no Editor | `unity:generate-editor-search-query` |
| Revisar um PR (sessão de revisão) | `code-review`, como apoio ao roteiro de `/revisar-issue` |
| O Matheus pede para testar uma ideia ou um plano | `anthropic-skills:grill-me` |

## Skills que este projeto não usa

| Skill | Motivo |
|---|---|
| `anthropic-skills:clean-architecture` | Decisão D-14. Não carregar, mesmo que a `clean-code` a sugira |
| `anthropic-skills:domain-driven-design` | Decisão D-14 |
| `unity:ui-uitk` em runtime | A interface do jogo é uGUI (D-13) |
| `unity:localization` | Localização está fora do escopo atual |
| `unity:optimize-web` | Navegador não é alvo (D-23) |

## Como a `clean-code` se aplica aqui

A skill é um conjunto de heurísticas, e ela mesma diz que a convenção do projeto vence. Estes são os pontos em que isso acontece neste repositório.

### Onde o projeto decide diferente

| A skill recomenda | Neste projeto |
|---|---|
| Depender de abstrações (DIP, OCP) para esconder detalhes | Classe concreta, criada com `new`. Interface só com duas implementações reais ou dependência invertida (D-03). Sem injeção de dependência (D-04) |
| Preferir exceção a código de erro | **Conteúdo inválido nunca lança exceção para dentro do jogo.** O método loga um aviso ou erro com contexto e retorna `bool` (`StartDialogue`, `Collect`, `TryUse`, `Load`). Exceção fica para erro de programação |
| Separar comando de consulta | O padrão `Try…` do C#, que executa e devolve `bool`, é aceito e é o padrão dos managers |
| Não retornar `null` | `Instance` de um manager pode ser `null`. Quem chama confere e loga um erro com contexto (`Debug.LogError(msg, this)`) |
| Sem prefixos em membros | Campos privados usam `_camelCase` |
| Evitar palavras de ruído como `Data` | Nomes existentes ficam (`ItemDataSO`, `DialogueData`). Nomes novos evitam o ruído |
| Combinar com `clean-architecture` e `domain-driven-design` | Não combinar (D-14) |

### Onde a skill e o projeto concordam, com detalhe local

- **Comentários.** O padrão é não comentar. Fica o comentário que explica **por que** uma ordem ou escolha não óbvia existe, como o de `LockedActionBehaviour` sobre conferir a flag antes do item. Comentários são em português.
- **`[Tooltip]` e `[Header]` não são comentários.** São a interface de autoria de quem monta a cena, e todo campo exposto no Inspector deve ter um tooltip que diga o que acontece quando o campo fica vazio.
- **`#region`.** O código antigo usa bastante. Não criar novos; remover os existentes só no arquivo que a issue já estiver tocando.
- **Escopo.** A regra do escoteiro vale para o trecho tocado, não para o arquivo todo. Um problema fora do escopo vira issue com a label `triagem`.
- **Cláusulas de guarda.** É o estilo do projeto: validar e sair cedo, sem `else`.
- **Log.** O console fica quieto por padrão. Não escrever `Debug.Log` de fluxo. Avisos e erros passam `this` como contexto. O rastreamento de mensagens e de trocas de estado mora atrás do define `VN_TRACE_MESSAGES`.
- **Validação de autoria.** Um campo que quem monta a cena pode esquecer ganha um aviso em `OnValidate`, dentro de `#if UNITY_EDITOR`.

### Convenções de nome

- Identificadores de C# em inglês; mensagens de log, comentários e tooltips em português (D-22).
- Um ScriptableObject de dados novo termina em `SO` (`ItemDataSO`, `CharacterSO`).
- Mensagem do barramento termina em `Message` e tem nome no passado (`ItemCollectedMessage`).
- Componente de cena ligado por `UnityEvent` costuma terminar em `Behaviour` ou `Trigger` (`CollectableItemBehaviour`, `InteractableDialogueTrigger`).
- Manager global termina em `Manager` e expõe `Instance`.
- Campo privado: `_camelCase`. Campo serializado: `camelCase`, com `[SerializeField] private`. Membro público: `PascalCase`.
- Prefixo de log entre colchetes com o nome da classe: `[InventoryManager] …`.

## Como a TDD se aplica aqui

- **Obrigatória** para C# puro: `InventoryService`, `InventoryModel`, `StateMachine`, e o que vier com a mesma natureza. O ciclo é vermelho, verde, refatorar, e o teste que falha vem primeiro.
- **Dispensada** para MonoBehaviours, cenas, prefabs e código de Editor. Esses são verificados em Play Mode pelo roteiro da issue.
- Na sessão de execução, a skill roda no **modo autônomo**: o agente conduz o ciclo inteiro sozinho, e o registro do ciclo aparece nos commits.
- As regras da suíte estão em [Tests/README.md](../../Assets/Scripts/Tests/README.md): estado estático limpo no `SetUp` e no `TearDown`, comando testado por chamada de método, notificação verificada assinando o `MessageBroker`.
- Ao alterar código sem teste, o primeiro passo é um teste de caracterização que fixa o comportamento atual.
