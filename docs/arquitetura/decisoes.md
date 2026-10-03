# Decisões do projeto

Este é o registro das decisões de arquitetura e de processo do ProjetoVN. Cada decisão tem um identificador estável (`D-xx`), o motivo e a condição para ser revista.

**Como usar:**
- Antes de "melhorar" qualquer coisa, procure aqui. Várias coisas que parecem melhoráveis foram mantidas de propósito.
- Uma decisão daqui **prevalece sobre qualquer skill** (ver D-26).
- Um agente **nunca muda uma decisão por conta própria**. Se uma issue parece exigir isso, ele para, pergunta ao programador e, com a resposta, atualiza esta página e o [registro de mudanças](#registro-de-mudanças).
- As regras marcadas como **absoluta** não têm exceção. As demais são padrões com gatilho de revisão.

## Resumo

| ID | Decisão | Tipo |
|---|---|---|
| D-01 | Módulos por feature com asmdef, sem ciclos e sem sub-asmdefs | Padrão |
| D-02 | ScriptableObject é somente leitura em runtime | **Absoluta** |
| D-03 | Lógica em C# puro atrás de MonoBehaviours finos; interface só com motivo concreto | Padrão |
| D-04 | Poucos singletons para managers globais; sem DI e sem service locator | Padrão |
| D-05 | `MessageBroker` só para notificações | **Absoluta** |
| D-06 | Lógica de jogo síncrona; apresentação e carregamento podem ser assíncronos | Padrão |
| D-07 | Objetos de cena se compõem por `UnityEvent` | Padrão |
| D-08 | Input: ação para avançar diálogo, leitura direta do mouse para o mundo, EventSystem para UI | Padrão |
| D-09 | Sem Addressables até haver medição | Padrão |
| D-10 | Inventário em modelo, serviço e manager; sem camadas a mais | Padrão |
| D-11 | Pool de slots no inventário; botões de escolha em vetor fixo, opção indisponível desabilitada | Padrão |
| D-12 | Fala e escolhas chegam à interface direto do Yarn Spinner | Padrão |
| D-13 | Sem framework de UI; pilha simples de janelas permitida | Padrão |
| D-14 | Sem Clean Architecture, DDD ou camadas hexagonais | Padrão |
| D-15 | Backend Mono; alvo desktop, distribuído por download | Padrão |
| D-16 | "Escalável" significa escala de conteúdo | Padrão |
| D-17 | Diálogo pelo Yarn Spinner (confirmada pela prova de conceito da #3) | Padrão |
| D-18 | Estado da história único no `Core` | Padrão |
| D-19 | Uma cena por sala; interface de jogo persistente; navegação por saídas | Padrão |
| D-20 | Save automático por sala e manual na pausa, slot único | Padrão |
| D-21 | Personagem é um dado; retrato ao lado da caixa | Padrão |
| D-22 | Vocabulário do roteiro em português; código em inglês | Padrão |
| D-23 | Navegador fica possível: três cuidados permanentes | Padrão |
| D-24 | TDD para C# puro; testes EditMode; sem integração contínua | Padrão |
| D-25 | Sem `[SerializeReference]` | Padrão |
| D-26 | Decisão do projeto prevalece sobre skill | **Absoluta** |
| D-27 | Nada especulativo | Padrão |
| D-28 | Só o `GameFlow` escreve no `PlayerInputGate` | Padrão |
| D-29 | Todo estado estático é zerado no início do Play | **Absoluta** |
| D-30 | Histórico de falas: só leitura, só da conversa em curso, fora do save | Padrão |

---

## Estrutura do código

### D-01 — Módulos por feature com asmdef
- **Decisão:** cada feature é uma pasta em `Assets/Scripts/` com o seu asmdef. O grafo de dependências não tem ciclos: `Core` ← `Dialogue`, `Inventory`, `PointNClick` ← `GameFlow`; `UI` enxerga só `Core` e `Inventory`. Todo script do projeto pertence a um asmdef de módulo. O módulo `Editor` (`ProjetoVN.Editor`) é só de Editor: enxerga `Core` e o Yarn Spinner, e nenhum módulo de runtime o referencia.
- **Por quê:** o compilador garante as fronteiras a custo baixo.
- **Rever só se:** um módulo virar depósito de coisas sem relação. **Não** dividir um módulo em sub-asmdefs de dados, lógica e interface.

### D-03 — C# puro atrás de MonoBehaviours finos
- **Decisão:** a regra de negócio fica em classes C# comuns (`InventoryService`, `InventoryModel`, `StateMachine`), criadas com `new`. O MonoBehaviour só liga a classe ao Unity (`InventoryManager`).
- **Interfaces:** só com duas implementações reais, ou quando um módulo precisa consultar outro que não pode referenciar (a interface pertence ao módulo que consulta). **Não** criar interface "para testabilidade".
- **Por quê:** as classes já são testáveis diretamente; interfaces sem segunda implementação só acrescentam arquivos.
- **Rever só se:** aparecer um caso que a regra acima não cubra.

### D-04 — Singletons, sem DI
- **Decisão:** os managers globais (`DialogueManager`, `InventoryManager`, `GameSaveManager`) expõem `Instance`. Input e interface chamam esses managers diretamente. Quem chama trata `Instance == null` logando um erro com contexto.
- **Não fazer:** container de injeção de dependência (VContainer, Zenject) nem service locator. Um objeto de "contexto" que expõe vários serviços é um service locator.
- **Por quê:** fluxo explícito, fácil de depurar, sem cerimônia.
- **Rever só se:** vários managers precisarem ser trocados em runtime.

### D-10 — Camadas do inventário
- **Decisão:** manter `InventoryModel` (dados), `InventoryService` (regras) e `InventoryManager` (API). Não fundir e não acrescentar camadas (repositórios, casos de uso, agregados).
- **Por quê:** tem um pouco mais de camadas do que precisa, mas fundir não paga o custo da mudança.

### D-14 — Sem Clean Architecture nem DDD
- **Decisão:** o projeto não usa camadas de Clean Architecture, arquitetura hexagonal nem modelagem tática de DDD. As skills `clean-architecture` e `domain-driven-design` **não são usadas** aqui, mesmo que a skill `clean-code` as sugira.
- **Por quê:** cerimônia sem problema concreto para um jogo deste porte.

### D-27 — Nada especulativo
- **Decisão:** só se constrói o que uma issue pede. Um sistema não ganha pontos de extensão para funcionalidades que ainda não existem.
- **Limite:** preparar para **volume de conteúdo** não é especulação (ver D-16).

---

## Comunicação entre sistemas

### D-05 — `MessageBroker` só para notificações (absoluta)
- **Decisão:** o barramento carrega apenas notificações ("X aconteceu", com zero ou mais ouvintes). Comando, consulta e estado **nunca** passam por ele. A tabela completa está em [visao-geral.md](visao-geral.md#regras-de-comunicação).
- **Por quê:** uma consulta por barramento falha em silêncio com zero ouvintes e responde duas vezes com dois; um estado enviado como evento nunca chega a quem ainda não existia. O projeto já teve esses bugs.
- **Também:** não substituir o broker por outra biblioteca, não torná-lo assíncrono, enfileirado ou com prioridade. Antes de criar uma mensagem, confirme que existe quem a escute.

### D-07 — Composição por `UnityEvent`
- **Decisão:** um objeto interativo liga o seu comportamento no Inspector (`InteractableItem.OnInteract` → `Collect`, `Interact`, `TriggerDialogue`).
- **Por quê:** é a ferramenta certa para conteúdo de point-and-click: quem monta a cena itera sem código.
- **Rever só se:** a ligação manual ficar ingovernável em centenas de objetos.

### D-28 — Um único escritor do `PlayerInputGate`
- **Decisão:** só o `GameFlow` chama `PlayerInputGate.SetEnabled`, sempre de dentro do `Enter()` de um estado. Um sistema que precise bloquear o input pede uma troca de modo ao `GameFlow`.
- **Armadilha conhecida:** o `DialogueInputHandler` **não** consulta o gate. O gate fica desligado durante todo o diálogo, então um avanço de fala condicionado a ele nunca dispararia. O gate cobre só cliques no mundo.

### D-29 — Estado estático é zerado no início do Play (absoluta)
- **Decisão:** toda classe com estado estático tem um método `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` que o zera.
- **Por quê:** o projeto precisa funcionar com o recarregamento de domínio desligado no Editor.

---

## Conteúdo e dados

### D-02 — ScriptableObject somente leitura (absoluta)
- **Decisão:** nenhum código escreve em um campo de ScriptableObject durante o jogo. O estado de runtime mora em modelos (`InventoryModel`) ou em estado estático que entra no save.
- **Por quê:** escrever em um asset durante o Play suja o arquivo no Editor e vaza entre sessões.

### D-16 — Escalável significa escala de conteúdo
- **Decisão:** o critério de escalabilidade do projeto é: **adicionar uma sala, um diálogo, um item ou um personagem não exige código novo**, e o projeto continua utilizável com centenas deles. Um tipo novo de comando ou de condição é uma classe só.
- **Não significa:** preparar o código para funcionalidades futuras, nem para várias pessoas programando.
- **Por quê:** o volume de conteúdo é desconhecido, e quem produz conteúdo não programa.

### D-25 — Sem `[SerializeReference]`
- **Decisão:** não usar. Polimorfismo em dados autorados usa referência a asset.
- **Por quê:** `[SerializeReference]` grava `"Tipo, Assembly"` dentro do asset e quebra em silêncio quando a classe é renomeada ou movida. A necessidade que motivaria usá-lo (efeitos de diálogo embutidos) desaparece com o Yarn Spinner (D-17).
- **Rever só se:** a prova de conceito (#3) falhar e o plano B exigir efeitos embutidos na fala.

### D-09 — Sem Addressables
- **Decisão:** referências diretas e `Resources` só para o prefab de managers.
- **Rever só se:** uma medição (Memory Profiler) mostrar problema de memória ou de tempo de carga com fundos, CGs ou áudio.

---

## Diálogo e história

### D-17 — Yarn Spinner para o diálogo
- **Decisão:** o diálogo será executado pelo Yarn Spinner 3, com roteiros em arquivos `.yarn` dentro de `Assets/Roteiro/`. O módulo `Dialogue` vira um adaptador fino. A instalação é pelo caminho gratuito (URL de git ou OpenUPM).
- **Por quê:** os roteiristas não usam o Unity. O Yarn Spinner é gratuito (licença MIT), cobre falas, escolhas, saltos, variáveis e condições, e tem extensão de VS Code com verificação de erros, grafo e pré-visualização.
- **Portão cumprido:** a prova de conceito da issue #3 cumpriu os critérios obrigatórios e o programador confirmou o veredito **adotar** em 2026-10-02. A versão provada é a `v3.2.8`, instalada pela URL de git com a tag fixa. O relatório, com as medições e os ajustes das issues seguintes, está nos comentários da #3.
- **Instalado pela issue #5:** o pacote `dev.yarnspinner.unity` v3.2.8 entra pela URL de git com a tag fixa (hash `bfc5b6a` no `packages-lock.json`), e o código da prova (PR #28, fechado sem merge) foi refeito no padrão do projeto. O `DialogueManager` é o adaptador, o `DialogueUIController` é o apresentador e o `DialogueRunner` vive no `Managers.prefab`.
- **Plano B (não acionado):** manter o sistema próprio (`DialogueController` e `DialogueData`) e permitir saltos entre falas dentro do mesmo asset. O sistema próprio foi removido na #5, então o plano B deixou de existir.

### D-18 — Estado da história único
- **Decisão:** um único `StoryState`, estático, no `Core`, guarda valores booleanos, numéricos e de texto. O roteiro (pelo adaptador do Yarn Spinner), as portas e o save leem e gravam nele. Ele substituiu o antigo `StoryFlags` (issue #4).
- **Formato do nome:** o nome de uma variável é guardado **com o `$`** em todo lugar (roteiro, Inspector, save). Quem monta cena digita `$` seguido de minúsculas sem acento, dígitos e `_` (por exemplo `$porta_biblioteca_destrancada`). O `StoryState` só exige o `$`, porque o Yarn Spinner grava nomes internos fora dessa convenção (`$Yarn.Internal.Visiting.<nó>`); a convenção é cobrada no `OnValidate` do portão e dos efeitos, pelo teste do registro e pelo seletor do Inspector (#7).
- **Registro central (#7):** toda variável é declarada **uma só vez**, em `Assets/Roteiro/variaveis.yarn`, com `<<declare>>` e uma linha `///` de descrição. Um teste EditMode reprova declaração fora desse arquivo, sem descrição ou fora da convenção. Os campos de variável booleana do Inspector (`[StoryFlag]`) escolhem de uma lista feita a partir dele, e um valor não declarado aparece com aviso. A afinidade com um personagem é uma variável numérica (`$afinidade_gotica`) nesse mesmo registro.
- **Por quê:** uma fonte de verdade. Com dois armazenamentos, a porta e o roteiro podem discordar. A afinidade com personagens é numérica. O `$` no Inspector evita que a mesma variável tenha duas grafias (uma no roteiro, outra na cena e no save).

### D-22 — Vocabulário do roteiro em português
- **Decisão:** tudo o que o roteirista digita é em português: comandos (`<<dar_item chave>>`), variáveis (`$afinidade_gotica`) e nomes de nó. Identificadores de C# são em inglês. Mensagens de log, comentários e documentação são em português.
- **Formato do id de item:** minúsculas sem acento, dígitos e `_` (`chave_teste`), igual aos nomes de nó e de variável. O roteiro cita o id sempre como literal (`<<dar_item chave_teste>>`, `tem_item("chave_teste")`), nunca por variável ou expressão; um teste EditMode confere cada id citado contra o `ItemRegistry`. Não há validação automática do formato no `ItemDataSO`.
- **Por quê:** os roteiristas escrevem em português. Mudar isso depois que houver roteiro escrito é caro.

### D-21 — Personagem como dado
- **Decisão:** cada personagem é um asset (`CharacterSO`: identificador, nome exibido, cor do nome, retratos por expressão). O diálogo mostra o retrato de quem fala ao lado da caixa.
- **Fora do escopo atual:** sprites de corpo inteiro sobre o cenário e vários personagens na tela.

### D-30 — Histórico de falas
- **Decisão:** o jogador pode reler a conversa em curso em um histórico, aberto por um botão na caixa de diálogo. O histórico:
  - é **só leitura**: não permite voltar a uma fala nem refazer uma escolha;
  - guarda **só a conversa em curso** e começa vazio a cada conversa;
  - **não entra no save**;
  - só abre **durante uma conversa**, nunca na exploração nem pela pausa;
  - registra falas, a opção escolhida e os itens dados ou retirados pelo roteiro. Sons e efeitos visuais não entram.
- **Por quê:** é uma mecânica esperada do gênero, e o alcance segue o modelo de Persona 5. Guardar a sessão inteira e perdê-la ao reabrir o jogo não faz sentido; gravá-la mudaria o formato do save (D-20); voltar no tempo exigiria desfazer afinidade, itens e variáveis.
- **Chega na issue #25.** Até lá, nada no código prepara terreno para ele (D-27).
- **Rever só se:** os roteiristas pedirem releitura fora da conversa.

### D-12 — Fala e escolhas chegam à interface direto do Yarn Spinner
- **Decisão:** o `DialogueRunner` entrega cada fala e cada grupo de opções ao apresentador (`DialogueUIController`) por chamada direta. `DialogueLineMessage` e `DialogueChoicesMessage` deixaram de existir: só a interface as escutava, e fala e opção têm um destinatário, não são notificação (D-05). `DialogueStartedMessage` e `DialogueEndedMessage` continuam, publicadas pelo `DialogueManager`, uma vez por conversa (o `GameFlow` depende delas).
- **Por quê:** uma notificação com um único ouvinte, dentro do mesmo módulo, só acrescenta indireção, e o apresentador do Yarn Spinner já recebe fala e opções por chamada.
- **Quem precisar das falas** (o histórico da #25) as recebe do apresentador, dentro do módulo.

---

## Apresentação e interface

### D-06 — Síncrono na lógica, assíncrono na apresentação
- **Decisão:** a lógica de jogo roda de forma síncrona na thread principal. **Apresentação** (typewriter, fades) e **carregamento de cena** podem usar corrotinas ou `Awaitable`.
- **Regras para o que for assíncrono:**
  - o estado do efeito mora na view; a view não dirige a lógica;
  - depois de cada `await`, reconferir se o objeto ainda existe e se o estado ainda é o mesmo (`destroyCancellationToken`);
  - pular ou completar um efeito passa pelo dono do diálogo, nunca direto na view;
  - **nunca** threads, `Task.Run`, Jobs ou Burst (ver também D-23).
- **Por quê:** não há trabalho pesado de CPU num jogo deste gênero; o risco real é uma continuação rodar depois que o objeto foi destruído.
- **Nota (issue #5):** o diálogo é entregue pelo Yarn Spinner por tarefas assíncronas (`YarnTask`, que no Unity 6 é `Awaitable`, na thread principal). O apresentador segue as regras acima, e a API do `DialogueManager` continua síncrona (`StartDialogue` devolve `bool` na hora).

### D-13 — Sem framework de UI
- **Decisão:** não adotar MVVM nem sistema genérico de janelas. `UIWindow` e `UIWindowManager` bastam. Uma **pilha simples** de janelas no `UIWindowManager` é permitida a partir da issue #18 (pausa).
- **Interface em uGUI.** O projeto não usa UI Toolkit em runtime.

### D-11 — Pool de slots e botões fixos
- **Decisão:** o `InventoryPresenter` reaproveita slots; o diálogo mostra as opções em um vetor fixo de quatro botões (`DialogueUIController`). Uma opção cuja condição é falsa aparece **desabilitada** e ocupa um botão, para o jogador ver que existe um caminho fechado. Mais de quatro opções no mesmo bloco, disponíveis ou não, é erro de conteúdo: aparecem as quatro primeiras e sai um erro no console. Se nenhuma opção do bloco estiver disponível, o bloco não é mostrado e a conversa segue. O `OptionsPresenter` do Yarn Spinner não é usado, porque instancia um prefab por opção e obrigaria a refazer a arte dos botões.
- **Rever só se:** o roteiro precisar de mais de quatro opções.

---

## Persistência e plataforma

### D-20 — Política de save
- **Decisão:**
  - o jogo salva **automaticamente ao entrar em cada sala** e **manualmente pelo menu de pausa**;
  - há **um único slot**;
  - salvar fica **bloqueado durante um diálogo**, então a posição da conversa não é salva;
  - **carregar sempre recarrega a cena**, para que os objetos apareçam no estado certo;
  - todo acesso a arquivo fica **apenas** no `GameSaveManager`;
  - o formato é JSON via `JsonUtility` (listas, não dicionários), sem framework genérico de save;
  - há **um único formato de save**: quando uma issue muda o formato, o código passa a conhecer só o novo. Não se mantém campo legado em `GameState`, conversão de arquivo anterior nem teste de formato anterior.
- **Por quê:** bloquear o save no diálogo elimina a parte mais cara de um save. Concentrar o acesso a arquivo permite trocar a gravação num lugar só (D-23). O jogo nunca foi distribuído, então não existe save de jogador a preservar, e o formato ainda vai mudar; código e testes para um formato datado só acrescentariam o que manter.
- **Rever o formato único só se:** o jogo tiver sido entregue a alguém de fora cujo save precise continuar abrindo.
- **Estado atual:** só existe salvar e carregar pelo painel de debug. A política acima chega nas issues #16 e #18.

### D-15 — Mono, desktop
- **Decisão:** backend de script Mono. Alvo: Windows, Mac e Linux, distribuído por **download** no itch.io.
- **Rever só se:** entrar um alvo que exija IL2CPP (navegador, celular, console).

### D-23 — Navegador fica possível
- **Decisão:** jogar no navegador não é alvo agora, mas a porta fica aberta com três cuidados permanentes:
  1. acesso a arquivo só no `GameSaveManager` (D-20);
  2. nada de threads nem `Task.Run` (D-06);
  3. nada de construir objetos por reflexão (`Activator.CreateInstance` e afins), porque o navegador obriga IL2CPP, que remove código alcançado só por reflexão.
- **Por quê:** são baratos hoje e são exatamente o que encarece essa migração depois.

---

## Processo

### D-24 — Testes
- **Decisão:**
  - **TDD é obrigatória para C# puro** (classes sem `MonoBehaviour`): o teste é escrito antes do código;
  - MonoBehaviours, cenas e prefabs são verificados em **Play Mode pelo Unity CLI**, seguindo o roteiro de verificação da issue;
  - os testes automatizados são **EditMode**; a suíte PlayMode nasce na issue #15;
  - **sem integração contínua** por enquanto: a sessão de revisão roda os testes e anexa o resultado ao PR.
- **Regras da suíte:** todo estado estático é limpo no `SetUp` e no `TearDown`; comando e consulta se testam chamando o método, não publicando mensagem.

### D-26 — Decisão do projeto prevalece sobre skill (absoluta)
- **Decisão:** quando uma skill recomenda algo que uma decisão desta página proíbe, vale a decisão. O agente segue a decisão e, se achar que ela está errada, **diz isso ao programador** em vez de contorná-la.
- **Detalhes e exemplos:** [../agentes/skills.md](../agentes/skills.md).

---

## Registro de mudanças

| Data | Mudança |
|---|---|
| 2026-10-01 | Página criada a partir do `ARCHITECTURE_ROADMAP.md`, que foi removido. D-01 a D-15 vieram de lá. **Revistas:** D-06 (assíncrono permitido na apresentação e no carregamento), D-13 (pilha simples de janelas permitida), D-15 (itch.io por download). **Novas:** D-16 a D-29, da entrevista de planejamento com o programador. D-11 e D-12 ficam marcadas para revisão na issue #5. |
| 2026-10-02 | **Nova:** D-30 (histórico de falas), decidida com o programador durante o levantamento da #2. O histórico de falas sai do "fora do escopo" e vira a issue #25; o nome do protagonista definido pelo jogador vira a issue #26. As duas entram na fatia vertical. |
| 2026-10-02 | **Confirmada:** D-17. A prova de conceito da #3 terminou com o veredito "adotar", confirmado pelo programador. O plano B não foi acionado. As notas sobre D-06, D-11 e D-12 que o relatório da #3 propõe ficam para a issue #5, que é quem decide essas três. |
| 2026-10-02 | **Revista:** D-20 ganha a regra do formato único de save, decidida pelo programador no levantamento da #4: o código conhece só o formato atual, sem campo legado, conversão nem teste de formato anterior. |
| 2026-10-02 | **Revista:** D-18 ganha o formato do nome das variáveis (`$` + minúsculas sem acento, dígitos e `_`, guardado com o `$` em todo lugar), aprovado pelo programador no levantamento da #4. `StoryFlags` foi substituído por `StoryState`. |
| 2026-10-02 | **Revistas:** D-11 e D-12 saem de "a rever", com os textos aprovados pelo programador no levantamento da #5. D-12: fala e escolhas passam do `MessageBroker` para chamada direta do Yarn Spinner ao apresentador, e `DialogueLineMessage` e `DialogueChoicesMessage` deixam de existir. D-11: o vetor fixo de quatro botões vale para o apresentador do Yarn. **Nota nova na D-06** sobre o diálogo assíncrono (`YarnTask`). **D-17:** o pacote entra em `main` e o plano B deixa de existir. |
| 2026-10-03 | **Revista:** D-22 ganha o formato do id de item (minúsculas sem acento, dígitos e `_`; sempre literal no roteiro), decidido pelo programador no levantamento da #6. O único item do projeto passou de `item-teste-01` para `chave_teste`. **D-01:** duas referências de asmdef autorizadas pelo programador na #6, sem inverter seta nem criar ciclo: `GameFlow` → pacote Yarn Spinner (para `[YarnCommand]` e `[YarnFunction]`) e `Tests` → `Inventory` (para o teste de ids usar o `ItemRegistry`). |
| 2026-10-03 | **Revistas (#7):** **D-11** deixa de esconder a opção indisponível: ela aparece desabilitada e conta para o limite de quatro botões; um bloco sem opção disponível não é mostrado (decisão do programador no levantamento da #7: mostrar que existem caminhos bloqueados faz o jogador entender que as escolhas têm peso). **D-18** ganha o registro central `variaveis.yarn`. **D-01:** o asmdef `ProjetoVN.Editor` (só Editor; referencia `Core`, `YarnSpinner.Unity` e `YarnSpinner.Unity.Editor`), autorizado pelo programador na #7. |

O histórico de execução das refatorações antigas (itens `ARCH-01` a `ARCH-22`, citados em alguns comentários de código) estava no roadmap removido. Para consultá-lo:

```bash
git show 391bf88:ARCHITECTURE_ROADMAP.md
```
