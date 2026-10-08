# Arquitetura: visão geral

Unity 6000.3.9f1, URP 2D, Input System, uGUI com TextMeshPro, backend Mono. O código do jogo fica em `Assets/Scripts/`, dividido em módulos por feature. As decisões por trás desta estrutura estão em [decisoes.md](decisoes.md).

## Módulos

| Módulo | Pasta | Responsabilidade | Enxerga |
|---|---|---|---|
| `Core` | [Core/](../../Assets/Scripts/Core/README.md) | Barramento de notificações, máquina de estados, estado da história | nada |
| `Dialogue` | [Dialogue/](../../Assets/Scripts/Dialogue/README.md) | Adaptador do Yarn Spinner: iniciar, avançar e escolher; input e interface do diálogo | `Core` e o pacote Yarn Spinner |
| `Inventory` | [Inventory/](../../Assets/Scripts/Inventory/README.md) | Itens do jogador, objeto coletável, registro de itens | `Core` |
| `PointNClick` | [PointNClick/](../../Assets/Scripts/PointNClick/README.md) | Clique e hover no mundo, pan de borda, trava de input | `Core` |
| `GameFlow` | [GameFlow/](../../Assets/Scripts/GameFlow/README.md) | Orquestrador: modos de jogo, bootstrap, save, portões, gatilho de diálogo, comandos de roteiro | `Core`, `Dialogue`, `Inventory`, `PointNClick` e o pacote Yarn Spinner |
| `UI` | [UI/](../../Assets/Scripts/UI/README.md) | Janelas de menu, painel de inventário | `Core`, `Inventory` |
| `Editor` | [Editor/](../../Assets/Scripts/Editor/README.md) | Código que só existe no Editor: o seletor de variável de história do Inspector | `Core` e o pacote Yarn Spinner (runtime e Editor) |
| `Tests` | [Tests/](../../Assets/Scripts/Tests/README.md) | Testes EditMode | `Core`, `Dialogue`, `GameFlow`, `Inventory` e o pacote Yarn Spinner |

```
Core ◄── Dialogue ◄──┐
  ▲◄──── Inventory ◄──┼── GameFlow
  ▲◄──── PointNClick ◄┘
  ▲◄──── UI (enxerga também Inventory)
  ▲◄──── Editor (só Editor; enxerga também o Yarn Spinner)
```

Regras da estrutura:
- Uma seta nunca é invertida. Se o módulo A precisa de algo do módulo B que não pode referenciar, a solução é uma das linhas da [tabela de comunicação](#regras-de-comunicação), não uma referência nova no asmdef.
- Acrescentar uma referência a um asmdef é uma mudança de arquitetura: o agente pergunta antes.
- Namespaces seguem `ProjetoVN.<Módulo>[.<Subpasta>]`.
- Os assets criados por quem faz conteúdo (itens, diálogos) ficam em [`Assets/Scripts/ScriptableObjects/`](../../Assets/Scripts/ScriptableObjects/README.md), separados das classes.

## Como um sistema é montado

O padrão se repete em todos os módulos (decisão D-03):

1. **Dados autorados:** ScriptableObjects somente leitura em runtime (`ItemDataSO`, `CharacterSO`) ou arquivos de texto (os roteiros `.yarn` do diálogo).
2. **Lógica:** classe C# comum, criada com `new`, testável sem cena (`InventoryService`, `StoryStateVariables`, `StateMachine`).
3. **Ponte com o Unity:** um MonoBehaviour fino que expõe a API (`InventoryManager`, `DialogueManager`).
4. **Objetos de cena:** componentes pequenos ligados por `UnityEvent` (`CollectableItemBehaviour`, `LockedActionBehaviour`).
5. **Interface:** lê o estado do manager e reage a notificações.

## Ciclo de vida

- Todos os managers persistentes moram em um único prefab, `Assets/Prefabs/Resources/Managers.prefab`: `GameStateController`, `DialogueManager`, o `DialogueRunner` do Yarn Spinner, `StoryStateVariableStorage`, `DialogueInputHandler`, `InventoryManager`, `GameSaveManager` e `ItemScriptActions`.
- O mesmo prefab traz, como filho aninhado, a **interface de jogo**: `Assets/Prefabs/UI/GameUI.prefab`, com o `Canvas_Game` (diálogo e inventário) e o `EventSystem` (D-19). O nome `Managers.prefab` ficou, mas ele carrega os managers **e** a interface.
- O `ManagersBootstrap` cria esse prefab uma vez por sessão, em `RuntimeInitializeOnLoadMethod(AfterSceneLoad)`, e o marca `DontDestroyOnLoad`. **Nenhuma cena contém esses componentes, nem `Canvas` de jogo, nem `EventSystem`.** Um `EventSystem` a mais em uma cena faz o uGUI logar `There are 2 event systems in the scene` a cada frame; um `Canvas_Game` a mais duplica o apresentador do diálogo.
- `AfterSceneLoad` roda depois do `Awake` e do `OnEnable` da primeira cena e antes do `Start`. Consequência prática: **um componente de cena só pode ler um manager a partir do `Start`**, nunca no `Awake` ou no `OnEnable` da primeira ativação.
- A interface de jogo também só se liga aos managers no próprio `Start` (o `DialogueUIController` se registra no `DialogueManager`; o `InventoryPresenter` se reconstrói). Na primeira cena, os `Start` dos objetos de cena entram na fila **antes** dos `Start` do prefab, porque o prefab é instanciado depois. Consequência: **um componente de cena não inicia diálogo no próprio `Start`**; receberia "Nenhuma interface de diálogo registrada". Nada faz isso hoje.
- Dar Play a partir de qualquer cena funciona, porque o bootstrap não depende de uma cena inicial.

## Regras de comunicação

Esta tabela é a referência única do projeto (decisão D-05).

| Interação | Use | Exemplo |
|---|---|---|
| **Comando:** exatamente um dono executa | Chamada direta de método no dono | `DialogueManager.Instance.StartDialogue("porta_trancada")`, `InventoryManager.Instance.TryUse(item)` |
| **Consulta:** você precisa de uma resposta | Chamada direta ou propriedade. **Nunca** pergunta e resposta pelo barramento | `InventoryManager.Instance.HasItem(item)` |
| **Estado que quem chega depois precisa conhecer** | Propriedade consultável como fonte de verdade, opcionalmente com uma notificação de mudança | `PlayerInputGate.IsEnabled`, `InventoryManager.Items` |
| **Notificação:** "X aconteceu", zero ou mais ouvintes, cruza módulos | Mensagem no `MessageBroker` (`readonly struct`, nome no passado) | `DialogueStartedMessage`, `ItemCollectedMessage` |
| **Efeito de história:** um diálogo muda o estado do jogo | Variável do roteiro (`<<set $x to true>>`), que grava direto no `StoryState`; comando de roteiro para o resto, implementado no `GameFlow` como chamada direta ao dono | `$falou_com_luna`, `<<dar_item chave_teste>>` |
| **Composição de objetos de cena** | `UnityEvent` no Inspector | `InteractableItem.OnInteract` → `Collect`, `Interact`, `TriggerDialogue` |
| **Um módulo precisa consultar outro que não pode referenciar** | Interface pequena, de posse do módulo que consulta | Nenhum caso hoje |

Regras adicionais:
- A interface pode **ler** estado dos managers; ela **modifica** apenas chamando os métodos do dono.
- Uma interface se monta **reconstruindo do estado e aplicando as notificações de forma incremental**. Montar só por eventos perde o que aconteceu enquanto ela estava escondida.
- Um handler de mensagem não assume que outro handler rodou antes ou depois dele.
- As mensagens vivem no módulo que as publica.
- Antes de criar uma mensagem, confirme que existe quem a escute. Uma mensagem sem assinante falha em silêncio.

## Modos de jogo

O `GameStateController` (em `GameFlow`) possui uma `StateMachine` (em `Core`). Hoje há dois modos:

| Modo | Entra quando | Efeito ao entrar |
|---|---|---|
| `GameplayState` | o jogo começa; chega `DialogueEndedMessage` | libera o `PlayerInputGate` |
| `DialogueState` | chega `DialogueStartedMessage` | bloqueia o `PlayerInputGate` |

Cada estado é dono dos seus efeitos colaterais, ligados no `Enter()` e desfeitos no `Exit()`. Um modo sobreposto (pausa) usa `Push`/`Pop`. Estados são criados com `new`, sem fábrica nem registro.

## Onde mora cada estado

| Estado | Dono | Entra no save? |
|---|---|---|
| Itens do jogador | `InventoryManager` (via `InventoryModel`) | Sim, por id |
| Objetos de mundo já consumidos | `InventoryManager` (provisório; a issue #14 decide o lugar definitivo) | Sim |
| Estado da história (booleanos, números, textos) | `StoryState`, estático em `Core` | Sim, em três listas de pares nome e valor |
| Input do mundo liberado ou não | `PlayerInputGate`, estático em `PointNClick` | Não |
| Modo de jogo atual | `GameStateController` | Não |
| Posição do diálogo em curso | `DialogueRunner` (Yarn Spinner), dentro do `Managers.prefab` | Não (salvar em diálogo será bloqueado, D-20) |
| Quem está na tela de personagens (lado, destaque, última expressão) | `DialogueUIController`, por um `ConversationStage` (D-21); só da conversa em curso | Não |
| Cena atual | `SceneManager` | Gravada; ainda não é consumida ao carregar |

`GameSaveManager` grava um `GameState` em JSON em `Application.persistentDataPath/savegame.json`. Itens são resolvidos por id pelo `ItemRegistry`.

## Fluxos principais

**Clique em um objeto do mundo**
1. `PointNClickSelector` (no `Update`) acha o `InteractableItem` sob o cursor e trata o hover.
2. No clique, confere `PlayerInputGate.CanClickThisFrame` (input liberado, não é o frame da liberação, ponteiro fora da interface).
3. Chama `InteractableItem.OnClick()`, que invoca o `UnityEvent` `OnInteract`.
4. O que acontece depende do que quem montou a cena ligou ali.

**Diálogo**
1. `InteractableDialogueTrigger.TriggerDialogue()` chama `DialogueManager.Instance.StartDialogue(nodeName)`, com o nome de um nó de um roteiro `.yarn`.
2. Nome vazio, nó inexistente, roteiro com erro de compilação, conversa já em curso ou nenhuma interface registrada: aviso ou erro no console, retorna `false`, nada muda.
3. Caso contrário o `DialogueRunner` começa a conversa: `DialogueStartedMessage` é publicada **antes** da primeira fala; o `GameFlow` entra em `DialogueState`.
4. O runner entrega cada fala e cada grupo de opções ao `DialogueUIController` (D-12), que resolve o nome de quem fala no `CharacterRegistry`, mostra o nome na cor do personagem e põe o sprite dele em um dos dois lugares da tela de personagens, atrás da caixa (quem decide o lugar, o destaque e a expressão é o `ConversationStage`). Ao abrir as opções, o protagonista entra em destaque. O clique esquerdo avança (`DialogueInputHandler` → `AdvanceDialogue`); os botões escolhem (`DialogueChoiceButton` → `MakeChoice`). As variáveis do roteiro leem e gravam no `StoryState`.
5. Ao acabar, `DialogueEndedMessage`; o `GameFlow` volta a `GameplayState`. O clique que encerrou o diálogo não atinge o mundo, porque o gate ignora o frame em que foi liberado. Se o roteiro parar sem terminar (por exemplo um `<<jump>>` para um nó que não existe), o `DialogueManager` registra o erro e encerra a conversa do mesmo jeito.

**Coleta**
1. `CollectableItemBehaviour.Collect()` chama `InventoryManager.Instance.Collect(item)`.
2. Se o item entrou, o objeto é marcado como consumido e desativado, e `ItemCollectedMessage` é publicada.
3. O `InventoryPresenter` acrescenta o slot.

**Portão**
1. `LockedActionBehaviour.Interact()` confere, nesta ordem: já aberto → flag exigida → item exigido.
2. A flag é conferida **antes** do item porque conferir a flag não consome nada e usar o item consome.
3. Ao abrir, grava a variável de memória no `StoryState` e dispara `OnOpened` (estado) e `OnUnlocked` (o momento).

## Como estender

| Para acrescentar | Faça |
|---|---|
| Um modo de jogo | Uma classe que herda de `BaseState`, criada com `new` no `GameStateController`. O módulo dono publica notificações de início e fim |
| Uma notificação | Um `readonly struct` que implementa `IMessage`, no módulo que publica, com nome no passado |
| Um diálogo | Um nó em um arquivo `.yarn` de `Assets/Roteiro/`, sem código. A cena o chama pelo nome no `InteractableDialogueTrigger` |
| Um comportamento de objeto de cena | Um MonoBehaviour pequeno com um método público, ligado ao `OnInteract` |
| Um item | Um asset `ItemDataSO` com id único, acrescentado ao `ItemRegistry` |
| Um personagem | Um asset `CharacterSO` (id, nome exibido, cor, sprites por expressão) acrescentado ao `CharacterRegistry`, mais uma linha nas tabelas "Quem fala" e "Expressões" de [roteiro.md](../autoria/roteiro.md). O roteiro o cita pelo nome exibido. Passo a passo em [salas.md](../autoria/salas.md#personagens) |
| Uma expressão | Uma imagem de corpo inteiro em `Assets/Sprites/Personagens/<personagem>/`, uma entrada em **Expressions** do `CharacterSO` e uma linha na tabela "Expressões" do guia de roteiro. O roteiro a pede com `#nome` no fim da fala |
| Uma variável de história | Uma linha `<<declare $nome = valor>>` com `/// descrição` em `Assets/Roteiro/variaveis.yarn`, sem código. Se for booleana, ela aparece sozinha nos campos de portão do Inspector |
| Um comando ou uma função de roteiro | Um método estático com `[YarnCommand("nome_em_portugues")]` ou `[YarnFunction]` em um componente do `GameFlow`, como o `ItemScriptActions`. O comando entra na tabela de [roteiro.md](../autoria/roteiro.md#comandos-disponíveis) |
| Um manager global | Um componente no `Managers.prefab`, com `Instance` atribuído no `Awake` e limpo no `OnDestroy` |
| Um elemento de interface de jogo | Um objeto dentro do `Canvas_Game` de `Assets/Prefabs/UI/GameUI.prefab`. Nunca em uma cena |

## O que vai mudar

A arquitetura acima descreve o que está em `main`. As mudanças planejadas estão em [../planejamento/milestones.md](../planejamento/milestones.md); as que mais alteram este documento:

| Mudança | Issue |
|---|---|
| Troca de sala e estado de transição | #12 |
| Save automático e carregamento que recarrega a cena | #16 |

Quem implementa cada uma atualiza este documento no mesmo PR.
