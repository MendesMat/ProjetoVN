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
| D-21 | Personagem é um dado; sprites da conversa em dois lugares, atrás da caixa | Padrão |
| D-22 | Vocabulário do roteiro em português; código em inglês | Padrão |
| D-23 | Navegador fica possível: três cuidados permanentes | Padrão |
| D-24 | TDD para C# puro; testes EditMode e PlayMode; sem integração contínua | Padrão |
| D-25 | Sem `[SerializeReference]` | Padrão |
| D-26 | Decisão do projeto prevalece sobre skill | **Absoluta** |
| D-27 | Nada especulativo | Padrão |
| D-28 | Só o `GameFlow` escreve no `PlayerInputGate` | Padrão |
| D-29 | Todo estado estático é zerado no início do Play | **Absoluta** |
| D-30 | Histórico de falas: só leitura, só da conversa em curso, fora do save | Padrão |
| D-31 | O jogo roda com a janela sem foco (`runInBackground` ligado) | Padrão |

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
- **Objetos de mundo consumidos (#14):** o registro dos ids persistentes dos coletáveis recolhidos mora no `InventoryManager`, de forma definitiva. Não vai para o `Core` porque só `Inventory` e `GameFlow` o usam e o `GameFlow` enxerga o `Inventory`; não vai para o `GameFlow` porque o `CollectableItemBehaviour` teria de mudar de asmdef, o que quebra as ligações de `UnityEvent` das cenas. O id persistente é único no jogo inteiro, não só na sala, porque o registro é um só. Rever só se um objeto de mundo que não é item precisar sumir para sempre.

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
- **A pausa (#18)** segue a regra: o `PauseState.Enter` fecha o gate. Pelo mesmo motivo da armadilha, o bloqueio da fala com a pausa aberta não passa pelo gate: é um estado consultável no dono do diálogo (`DialogueManager.IsPaused`), escrito pelo `PauseState`, como o bloqueio do histórico (D-30). O `PauseState.Exit` não mexe no gate: quem o devolve é o modo coberto, ao receber o `Enter` de volta (`GameplayState` libera, `DialogueState` fecha).

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
- **Registro central (#7):** toda variável é declarada **uma só vez**, em `Assets/Roteiro/variaveis.yarn`, com `<<declare>>` e uma linha `///` de descrição. Um teste EditMode reprova declaração fora desse arquivo, sem descrição ou fora da convenção. Os campos de variável booleana do Inspector (`[StoryFlag]`) escolhem de uma lista feita a partir dele, e um valor não declarado aparece com aviso. A afinidade com um personagem é uma variável numérica (`$afinidade_luna`) nesse mesmo registro.
- **Por quê:** uma fonte de verdade. Com dois armazenamentos, a porta e o roteiro podem discordar. A afinidade com personagens é numérica. O `$` no Inspector evita que a mesma variável tenha duas grafias (uma no roteiro, outra na cena e no save).

### D-22 — Vocabulário do roteiro em português
- **Decisão:** tudo o que o roteirista digita é em português: comandos (`<<dar_item chave>>`), variáveis (`$afinidade_luna`) e nomes de nó. Identificadores de C# são em inglês. Mensagens de log, comentários e documentação são em português.
- **Formato do id de item:** minúsculas sem acento, dígitos e `_` (`chave_teste`), igual aos nomes de nó e de variável. O roteiro cita o id sempre como literal (`<<dar_item chave_teste>>`, `tem_item("chave_teste")`), nunca por variável ou expressão; um teste EditMode confere cada id citado contra o `ItemRegistry`. Não há validação automática do formato no `ItemDataSO`.
- **Formato do id de personagem e do nome de expressão (#10):** o mesmo, minúsculas sem acento, dígitos e `_` (`luna`, `raiva`). O nome exibido é português normal (`Luna`) e é o que o roteiro escreve antes dos dois-pontos. Um teste EditMode confere todo nome de quem fala e toda etiqueta de expressão dos roteiros contra o `CharacterRegistry`; o formato é validado no `OnValidate` do `CharacterSO` e por um teste do registro.
- **Por quê:** os roteiristas escrevem em português. Mudar isso depois que houver roteiro escrito é caro.

### D-21 — Personagem como dado
- **Decisão:** cada personagem é um asset (`CharacterSO`: identificador, nome exibido, cor do nome, sprites por expressão). Durante a conversa, quem fala aparece como sprite grande atrás da caixa de diálogo, em dois lugares (esquerda e direita); quem fala fica em destaque e o outro escurece.
- **Detalhes decididos na #10 (continuam valendo):**
  - **O roteiro cita o personagem pelo nome exibido** (`Luna: ...`), comparado de forma exata. O `id` (`luna`) é para o código. O `CharacterRegistry` é um asset referenciado pelo `DialogueUIController`, sem `Instance` (D-04).
  - **A expressão é uma etiqueta no fim da fala** (`Luna: Sai daqui. #raiva`). Sem etiqueta, aparece a primeira expressão da lista do asset. Expressão que o personagem não tem: aviso e expressão padrão. Descartado: um comando `<<expressao ...>>` que persiste, porque pede estado por personagem, um segundo nome para o personagem no roteiro e tira a etiqueta `lastline` da fala antes de opções.
  - **O protagonista tem sprite**, como qualquer personagem.
  - **Onde a #26 troca o nome do protagonista:** em um único método privado do `DialogueUIController` que devolve o nome a exibir de um `CharacterSO`. Hoje devolve o `DisplayName`; a #26 o faz devolver a variável de texto do `StoryState` quando o personagem for o `Protagonist` do `CharacterRegistry`. O roteiro continua escrevendo `Protagonista:`. O histórico (#25) recebe a fala já com o nome resolvido (D-12).
- **Revisto na #48:**
  - **A tela de personagens.** O retrato quadrado ao lado da caixa deu lugar a sprites de corpo inteiro, do busto para cima, atrás da caixa e na frente do cenário. A caixa de diálogo volta ao centro da tela; o centro livre é das opções de resposta; a placa de nome fica no canto superior esquerdo da caixa, também quando fala quem está à direita. Os sprites não são espelhados e não recebem clique.
  - **Quem ocupa cada lado:** por ordem de chegada. O primeiro a falar na conversa fica à esquerda e o segundo à direita; o protagonista não tem lado fixo. O sprite entra na **primeira fala** do personagem e fica até a conversa acabar, e o roteiro não declara participantes. Com os dois lugares ocupados, um terceiro toma o lugar de **quem falou há mais tempo**; quem saiu volta pela mesma regra.
  - **Destaque:** quem fala fica em destaque e os demais um pouco escurecidos (uma cor no Inspector, com 55% de brilho de partida). **Narração, pensamento, personagem desconhecido e personagem sem sprite** não trazem ninguém e escurecem quem está na tela. **Ao abrir as opções de resposta o protagonista entra e fica em destaque** (opções são sempre dele): é o aviso visual de que é a vez do jogador. Um bloco sem opção disponível não é mostrado (D-11) e não traz ninguém.
  - **A expressão ganhou estado, mas só de apresentação.** A etiqueta continua valendo só para a fala em que está, mas a tela guarda a **última expressão de quem está escurecido**, até a própria fala seguinte decidir de novo. Esse estado (quem está em cada lado, quem falou por último, a última expressão de cada um) mora no apresentador, na classe pura `ConversationStage`, vale só para a conversa em curso e não entra no save. O `CharacterSO` continua somente leitura (D-02).
  - **Duração:** a tela é limpa só quando a conversa termina. `<<jump>>` e `<<detour>>` andam entre nós da mesma conversa e não limpam nada.
  - **Enquadramento:** uma escala e uma linha de chão únicas para todos os personagens, sem ajuste por personagem. Os personagens têm tamanhos diferentes de propósito e as artes são alinhadas pela base da imagem. **A composição é decidida pela arte, não pelo código:** os personagens são desenhados dentro de uma cena de conversa em 3840×2160, exportados em 100% e cortados na borda de baixo da tela; o jogo os desenha na metade, com a base na borda de baixo da tela. Alinhar pela cabeça foi testado e descartado: apaga a diferença de altura que a arte quer mostrar. Entrar, escurecer e trocar de lugar são instantâneos.
  - **Quem é o protagonista:** o campo **Protagonist** do `CharacterRegistry`, com tooltip e validação, e não uma comparação do `Id` com um texto fixo. A #26 usa esse campo.
- **Pensamento (#61):** a linha `Pensamento: texto` do roteiro é o pensamento do protagonista, e `Pensamento` é um **nome reservado**, não um `CharacterSO`. O apresentador o reconhece **antes** da busca no `CharacterRegistry`, com a mesma comparação exata do nome de personagem. Na caixa, aparece sem placa de nome, em itálico e na cor de pensamento (campo do `DialogueUIController`); escurece a tela como a narração e **não** passa por `NameToDisplay`, então a troca do nome do protagonista (#26) não o afeta. Nenhum personagem do registro pode ter esse nome exibido (teste e `OnValidate`), e a verificação dos roteiros reprova etiqueta de expressão nele. Só o protagonista pensa: pensamento de outras personagens foi considerado e ficou de fora (D-27); a alternativa seria uma etiqueta `#pensamento` no fim da fala. Descartados como marca visual: parênteses reconhecidos pelo jogo, porque um parêntese de verdade numa fala viraria pensamento sem aviso.
- **Opção de ação (#65):** uma opção do roteiro com a etiqueta `#acao` no fim (`-> Assinar e sair #acao`) é um **gesto** do protagonista, não uma fala; sem a etiqueta a opção continua sendo fala. O botão mostra o texto em itálico, e o apresentador reconhece a etiqueta em `option.Line.Metadata` (classe pura `ActionOption`). O protagonista entra em destaque ao abrir as opções do mesmo jeito, haja ou não opção de ação. `acao` deixou de ser lida como expressão (`ExpressionTag`), e a verificação dos roteiros aceita `#acao` só em opção (expressão em opção continua proibida; `#acao` em fala, narração ou pensamento é reprovado). Com condição, a ordem é `<<if ...>> #acao`: o Yarn não compila a etiqueta antes do `<<if>>`. A etiqueta é em português e sem acento (D-22). Descartados: toda opção virar ação, e marcar a ação por colchetes no texto (colchete é marcação do Yarn, e um `[` sem par derruba o projeto). Fora do escopo: ícone ou cor própria para a ação (D-27), opção que mistura gesto e fala.
- **Fora do escopo atual:** mais de dois lugares; o roteiro escolher a posição de alguém, tirar alguém de cena ou limpar a tela no meio da conversa; transições suaves (depois da #11); ajuste de enquadramento por personagem; sprite de personagem parado no cenário fora de uma conversa.

### D-30 — Histórico de falas
- **Decisão:** o jogador pode reler a conversa em curso em um histórico, aberto por um botão na caixa de diálogo. O histórico:
  - é **só leitura**: não permite voltar a uma fala nem refazer uma escolha;
  - guarda **só a conversa em curso** e começa vazio a cada conversa;
  - **não entra no save**;
  - só abre **durante uma conversa**, nunca na exploração nem pela pausa;
  - registra falas, a opção escolhida e os itens dados ou retirados pelo roteiro. Sons e efeitos visuais não entram.
- **Por quê:** é uma mecânica esperada do gênero, e o alcance segue o modelo de Persona 5. Guardar a sessão inteira e perdê-la ao reabrir o jogo não faz sentido; gravá-la mudaria o formato do save (D-20); voltar no tempo exigiria desfazer afinidade, itens e variáveis.
- **Chegou na issue #25.**
  - O registro é a classe pura `ConversationLog`, que vive no apresentador (`DialogueUIController`) e é zerado no início de cada conversa (D-12). `<<jump>>` e `<<detour>>` não o zeram.
  - Os itens chegam pelo `GameFlow`: `ItemScriptActions` chama `DialogueManager.RecordItemReceived` e `RecordItemHandedOver` por chamada direta, **só quando o inventário muda**. O `Dialogue` continua sem enxergar o `Inventory` (D-01), e nenhuma mensagem nova foi criada (D-05).
  - O bloqueio com o histórico aberto mora no dono do diálogo (`DialogueManager.AdvanceDialogue` e `DialogueUIController.Choose`), como o do `IsRevealingLine` (D-06). Não há modo novo no `GameFlow`: ele só mexe no `PlayerInputGate`, que o avanço de fala não consulta (D-28).
- **Revisto na #55:** a apresentação do histórico deixou de ser uma lista de linhas `Nome: texto` e virou **balões**, como num app de mensagens. O critério "`Nome: texto`, com o nome na cor do personagem" da #25 foi **substituído** por este; o restante da D-30 não mudou.
  - A fala do protagonista (o `Protagonist` do `CharacterRegistry`) e a opção escolhida ficam à direita; as dos demais personagens, à esquerda, com um personagem desconhecido na cor padrão da placa de nome. Narração e itens ficam centralizados, em um balão neutro.
  - O balão usa a `NameColor` do `CharacterSO`, sem campo novo (D-02, D-16). O nome fica dentro do balão, **sem cor própria**, e só aparece na primeira fala de uma sequência do mesmo personagem; narração e item a interrompem.
  - A cor do texto é escolhida pelo contraste com o balão (`BubbleTextColor`, C# puro): a escura ou a clara, a de maior contraste (WCAG). Uma `NameColor` sem contraste de 4,5:1 com nenhuma das duas (um cinza médio) é reprovada por um teste EditMode do registro de personagens e avisada no `OnValidate` do `CharacterSO`; a correção é ajustar a cor do personagem.
  - O `ConversationLog` guarda entradas estruturadas (`ConversationEntry`: tipo, nome, cor opaca, texto, se é do protagonista e se mostra o nome) com a cor já resolvida, e não o `CharacterSO`: o personagem desconhecido não tem asset e o nome já chega resolvido por `NameToDisplay` (D-21).
  - O painel só acrescenta as entradas novas a cada abertura; o apresentador avisa a view quando uma conversa começa. A lista continua sem virtualização (D-27).
- **Revisto na #61:** o pensamento do protagonista (D-21) entra no histórico como uma entrada de tipo próprio (`Thought`), e não como uma `Speech` marcada.
  - Fica à **direita**, o lado do protagonista, num balão **tingido** pela cor de pensamento (o balão neutro, escuro e translúcido, misturado à cor de pensamento; a força do tingimento é um campo da view, 20% de partida), com o texto em itálico na mesma cor e **sem nome**. Ele não usa a `NameColor` do protagonista: o lado diz de quem é, e a falta da cor dele diz que não foi dito em voz alta.
  - O pensamento **interrompe a sequência**: o nome só aparece na primeira fala de uma sequência, e como o pensamento não é `Speech`, a fala seguinte do protagonista mostra o nome de novo. Por isso a regra de `ContinuesSequenceOf` não mudou.
  - A cor de pensamento vem do apresentador e viaja na entrada, como a `NameColor`; não há uma cor em cada componente, para a caixa e o histórico não divergirem. O contraste do texto com o balão é de pelo menos 4,5:1 no pior caso (painel sobre uma cena branca; medido **6,40:1** nos pixels de uma captura, e 8,12:1 sobre a cena de teste). A primeira versão deixava o balão só translúcido na cor de pensamento e media 3,94:1 nos pixels, não os 6,36:1 da conta feita em gama: o projeto está em espaço linear e o Unity mistura o alfa nele, então a conta de contraste de um balão translúcido tem de ser feita em espaço linear e conferida na captura.
- **Revisto na #65:** a opção de ação (D-21) escolhida entra no histórico como `Narration`, e não como fala do protagonista: no **centro**, em itálico, no balão neutro, sem nome, interrompendo a sequência de falas. Não há tipo de entrada novo: o desenho que o critério pedia ("como a narração") é o da `Narration`, e um tipo próprio só se pagaria se a ação tivesse aparência própria, que está fora do escopo (D-27). A opção de fala continua à direita.
- **Rever só se:** os roteiristas pedirem releitura fora da conversa.

### D-12 — Fala e escolhas chegam à interface direto do Yarn Spinner
- **Decisão:** o `DialogueRunner` entrega cada fala e cada grupo de opções ao apresentador (`DialogueUIController`) por chamada direta. `DialogueLineMessage` e `DialogueChoicesMessage` deixaram de existir: só a interface as escutava, e fala e opção têm um destinatário, não são notificação (D-05). `DialogueStartedMessage` e `DialogueEndedMessage` continuam, publicadas pelo `DialogueManager`, uma vez por conversa (o `GameFlow` depende delas).
- **Por quê:** uma notificação com um único ouvinte, dentro do mesmo módulo, só acrescenta indireção, e o apresentador do Yarn Spinner já recebe fala e opções por chamada.
- **Quem precisar das falas** (o histórico da #25) as recebe do apresentador, dentro do módulo. Foi o que a #25 fez.

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
- **Nota (issue #11):** o typewriter é o `LetterTypewriter` do Yarn Spinner, aguardado dentro do `RunLineAsync` do apresentador. O estado "revelando" mora no apresentador (`IsRevealingLine`), e completar passa pelo dono do diálogo: o `DialogueManager.AdvanceDialogue` pede pressa ao runner (`RequestHurryUpLine`) enquanto a fala é revelada, e a próxima fala (`RequestNextLine`) depois. O token do typewriter é ligado ao `destroyCancellationToken`.

### D-13 — Sem framework de UI
- **Decisão:** não adotar MVVM nem sistema genérico de janelas. `UIWindow` e `UIWindowManager` bastam. Uma **pilha simples** de janelas no `UIWindowManager` é permitida a partir da issue #18 (pausa).
- **Interface em uGUI.** O projeto não usa UI Toolkit em runtime.
- **Cumprida na #18:** a pilha existe e é a classe pura `WindowStack<TWindow>` (em `UI/Framework`, TDD), usada pelo `UIWindowManager`. `OpenWindow` empilha a janela, ou, se ela já está na pilha, volta até ela (é assim que o VOLTAR do menu principal e o da confirmação da pausa funcionam sem crescer a pilha); `CloseCurrentWindow` desempilha e reabre a de baixo; `CloseAllWindows` fecha tudo. `UIWindow.Hide()` solta a seleção do `EventSystem` quando ela é de dentro da janela, senão o botão escondido continuaria selecionado e um Enter o acionaria. Continua valendo: nada de framework de janelas, sem animação de abertura, sem tecla Esc tratada pelo `UIWindowManager` (a pausa lê a própria ação).

### D-11 — Pool de slots e botões fixos
- **Decisão:** o `InventoryPresenter` reaproveita slots; o diálogo mostra as opções em um vetor fixo de quatro botões (`DialogueUIController`). Uma opção cuja condição é falsa aparece **desabilitada** e ocupa um botão, para o jogador ver que existe um caminho fechado. Mais de quatro opções no mesmo bloco, disponíveis ou não, é erro de conteúdo: aparecem as quatro primeiras e sai um erro no console. Se nenhuma opção do bloco estiver disponível, o bloco não é mostrado e a conversa segue. O `OptionsPresenter` do Yarn Spinner não é usado, porque instancia um prefab por opção e obrigaria a refazer a arte dos botões.
- **Rever só se:** o roteiro precisar de mais de quatro opções.

### D-19 — Uma cena por sala, interface persistente
- **Decisão:** cada sala é uma cena Unity que contém só o mundo (fundo, objetos interativos, câmera, pontos de entrada). A interface de jogo (diálogo, inventário, pausa, fade) é um prefab criado uma vez pelo bootstrap, como os managers. A navegação entre salas é por saídas clicáveis no cenário; não há mapa.
- **Por quê:** quem monta a sala não toca em interface e não consegue esquecê-la.
- **Onde mora (#9):** a interface de jogo é o prefab `Assets/Prefabs/UI/GameUI.prefab` (`Canvas_Game`, com o diálogo e o inventário, e o `EventSystem`), aninhado no `Managers.prefab`. O `ManagersBootstrap` cria os dois de uma vez e nenhuma cena contém `Canvas` de jogo nem `EventSystem`.
- **A pausa (#18)** entra nesse prefab como o `Canvas_Pause`, **irmão** do `Canvas_Game` e do `Canvas_Fade` (o precedente do fade), com Sort Order 50: acima do `Canvas_Game` (0) e do `Canvas_Debug` da cena de teste (10), abaixo do fade (100). Um `Canvas` próprio, e não uma janela dentro do `Canvas_Game`, evita um `Canvas` aninhado com override de ordem só para ficar acima do debug. A `Pause Window` e a `Confirm Return Window` ficam inativas no prefab, e o fundo de tela cheia delas engole os cliques na interface de baixo. **Não há modo de jogo para o menu** (D-27): o `PauseMenu` recusa a pausa quando a interface de jogo está escondida, que é como ele sabe que está no menu. Voltar ao menu passa pelo `RoomNavigator` (`GoToMainMenu`), o único que carrega cena, e conversa aberta é encerrada **antes** (a regra "não existem trocas de sala durante uma conversa" não muda).
- **Interface de jogo escondida no menu (#17):** `Menu.unity` é a cena 0 do build e a interface de jogo persiste por cima dela, então o menu a **esconde** em vez de a tirar da cena: o `GameUIVisibility` (no `Canvas_Game`) desliga e liga o `Canvas`, nunca o objeto, para o diálogo e o inventário seguirem registrados e assinando. Esconder é um comando com um dono, chamada direta (D-05), e a troca para a sala inicial passa pelo `RoomNavigator`, o único que carrega cena. Não há modo de jogo para o menu (D-27). Descartado: pôr o canvas do menu acima do `Canvas_Game`, que só esconde enquanto a arte cobrir a tela inteira e deixa a interface no raycast.
- **Fade e troca de sala (#12):** o fade é o `Canvas_Fade` dentro do `GameUI.prefab` (Sort Order 100, acima de qualquer outro canvas), e o comando que troca de sala é o `RoomNavigator.ChangeRoom`, no `Managers.prefab`. A troca passa por um modo próprio, o `RoomTransitionState`, que fecha o `PlayerInputGate` (D-28). **Uma conversa aberta recusa a troca de sala** (`ChangeRoom` devolve `false` com aviso e nada muda), inclusive o Reload Scene do debug: decisão do programador no levantamento da #12, "não devem existir trocas de sala durante uma conversa".
- **Saídas e pontos de entrada (#13):** a saída é o componente `RoomExit` (sala de destino e id do ponto de entrada), ligado por `UnityEvent` ao **On Interact** de um objeto ou ao **On Already Unlocked** de uma porta (D-07); o ponto de entrada é o componente `RoomEntryPoint`, um marcador com id na cena da sala. O ponto marca o lugar do cenário que fica no centro da tela na chegada, e o `RoomNavigator` o acha na cena nova, entre o carregamento e o clarear, e pede ao `ScreenPanController` que centralize o cenário nele (chamada direta, D-05). Destino vazio e ponto inexistente são avisados no `OnValidate`, reprovados pelo `RoomLinksTests` e, na chegada, erro no console, sem seletor no Inspector (D-27).
- **Falha na troca de sala (#13), regra decidida pelo programador no levantamento:** uma exceção inesperada durante a troca é registrada no console e **não deixa o jogo preso**: a tela clareia na hora e o jogo volta à exploração na sala que tiver ficado carregada. Contexto: um `Awaitable` descartado engole a exceção sem log, e o código novo da chegada procura objetos numa cena montada por outra pessoa. Descartado: só registrar, que deixaria o jogo em `RoomTransitionState` com a tela preta.

### D-08 — Input
- **Decisão:** `InputActionReference` para avançar o diálogo; leitura direta de `Mouse.current` para o mundo; EventSystem para a interface. O `PlayerInputGate` é a única arbitragem.
- **Nota (#25):** um clique de **ponteiro** (mouse ou toque) sobre um botão da interface não avança a fala. O `DialogueInputHandler` confere, por `RaycastAll` do `EventSystem`, se o primeiro objeto sob o ponteiro é de um `Selectable`. Teclado e controle não mudam. Sem isso, o clique no botão do histórico avançaria a fala ao apertar, antes de o botão agir ao soltar.
- **Rever só se:** entrar navegação do mundo por teclado ou controle.

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
- **Estado atual (#16):** o salvar automático ao entrar na sala existe, e carregar recarrega a cena: `Load()` pede ao `RoomNavigator` a troca para a sala salva e só restaura o estado se a troca for aceita (tudo ou nada diante de uma recusa; uma falha inesperada na troca já aceita segue a regra de falha da D-19 e deixa o estado restaurado na sala que tiver ficado carregada), então os objetos da cena nova leem o estado certo no `Start()`. `Save()` devolve `bool` e é recusado com uma conversa aberta. O salvar pela pausa chegou na #18 (abaixo).
- **Onde o jogador acorda ao carregar (#16, decidido pelo programador no levantamento):** no **ponto de entrada por onde chegou à sala**. O save guarda `currentEntryPointId` (o `RoomNavigator` o lembra a cada chegada) e o `Load()` chama `ChangeRoom(sala, ponto)`. Sem ponto (o jogo começou numa sala sem passar pela troca, ou foi o Reload Scene), o jogador chega na posição em que a cena foi salva no Editor. Descartados: guardar a posição exata do pan, que pede código novo no `PointNClick` e não ajuda o salvar automático (que acontece na chegada); e sempre a posição da cena, que dá lugares diferentes para quem foi e voltou por uma porta. Um save manual da pausa (#18) depois de o jogador andar pelo cenário volta a ele no ponto de chegada, não onde estava.
- **Novo Jogo (#17):** `GameSaveManager.StartNewGame(sala, ponto)` pede a troca de sala primeiro e, só se ela for aceita, restaura um `GameState` vazio (tudo ou nada, como o `Load()`); **não apaga o arquivo**, porque a chegada na sala inicial o sobrescreve (slot único). `HasSave` é "o arquivo existe **e** é legível": um save corrompido conta como sem save, então o Continuar fica desabilitado e o Novo Jogo não pede confirmação. O formato do save não mudou.
- **Salvar pela pausa e voltar ao menu (#18):** o SALVAR da pausa é o "manual na pausa" da decisão: chama `GameSaveManager.Save()`, que grava a sala atual e o ponto de entrada por onde o jogador chegou nela (ver acima: salvar depois de andar pelo cenário volta ao ponto de chegada), e é recusado com conversa aberta (o botão fica desabilitado). **Voltar ao menu não grava o save:** toda chegada do `RoomNavigator.ChangeRoom` salva e o `Save()` grava a cena ativa, então voltar ao menu por `ChangeRoom("Menu")` gravaria `"currentScene": "Menu"` e o Continuar abriria o menu. Por isso há o `RoomNavigator.GoToMainMenu`, com as mesmas recusas e o mesmo fade, e sem salvar na chegada. O progresso desde o último save se perde, e a pausa pede confirmação antes. O Novo Jogo depois disso não herda nada: o que entra no save é restaurado vazio pelo `StartNewGame`, e o modo de jogo, a pausa e o histórico da conversa não entram no save e já estão limpos.
- **Save corrompido (#16):** um arquivo ilegível não derruba o jogo. O `GameStateJson.TryRead` devolve `false` (o `JsonUtility` lança `ArgumentException` em JSON inválido), e o `Load()` loga um aviso e não muda nada. Uma sala salva que não existe mais no build também só loga e não restaura nada.

### D-15 — Mono, desktop
- **Decisão:** backend de script Mono. Alvo: Windows, Mac e Linux, distribuído por **download** no itch.io.
- **Rever só se:** entrar um alvo que exija IL2CPP (navegador, celular, console).

### D-31 — O jogo roda com a janela sem foco
- **Decisão:** `runInBackground` ligado no Player Settings. O jogo continua rodando quando a janela perde o foco, na build e no Play Mode do Editor.
- **Por quê:**
  - *Desenvolvimento:* um Editor dirigido pelo CLI está sempre sem foco, e com a opção desligada o Play Mode já foi visto parado nas verificações da M3 (`Start`, `Update` e corrotinas não rodavam), o que dava falso negativo. Com a opção ligada ninguém precisa atribuir nada antes de verificar. Na #79 o congelamento **não se reproduziu** com a opção desligada (o `Time.frameCount` avançou), então a condição exata que o provocava não está isolada; a decisão não depende dela.
  - *Jogador:* o jogo espera o clique e não tem nada que dependa do tempo, então a jogabilidade não muda. Muda que a música (quando existir) continua no alt-tab, e que o jogo gasta CPU, GPU e bateria em segundo plano. Com a configuração padrão do Input System, mouse e teclado são desligados sem foco, então um clique fora da janela não chega ao jogo.
- **Não fazer:** `Application.runInBackground = …` por `eval` ou em código de Editor. No Editor essa propriedade é a própria opção do Player Settings (`SetPlayerSettingsRunInBackground`), e a atribuição suja o `ProjectSettings.asset` (medido na #79: `runInBackground: 1 → 0` no arquivo depois de um `SaveAssets`).
- **Vale porque:** o alvo é desktop (D-15), onde a opção é respeitada.
- **Limite sem foco (#81):** com a janela sem foco, o `UnfocusedFrameRateLimiter` (`Managers.prefab`) desliga o VSync e limita o jogo a **30 FPS**, e devolve os dois valores de antes quando o foco volta. O VSync precisa ir junto porque, no desktop, o `targetFrameRate` é ignorado com `vSyncCount != 0`. Um valor só (campo do componente): o Unity não tem API que diferencie janela minimizada de janela sem foco. Vale na build e no Editor; no Editor, "foco" é o da Game view. **Exceção aceita pelo programador na #81:** uma janela que abre sem nunca receber o foco (atrás de outra) fica sem limite até a primeira perda de foco depois de ter tido foco. O Unity não manda `OnApplicationFocus(false)` nesse caso, e o `Application.isFocused` já vale `True` (medido numa Development Build). Consertar exigiria uma consulta nativa por sistema (contra a D-15 e a D-27); detalhes no [README do GameFlow](../../Assets/Scripts/GameFlow/README.md).
- **Rever só se:** os 30 FPS sem foco incomodarem (um valor para a janela minimizada exigiria chamada nativa por sistema) ou entrar um alvo onde a opção não vale.

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
  - há duas suítes automatizadas. A **EditMode** (`Tests`) cobre C# puro e conteúdo. A **PlayMode** (`Tests.PlayMode`, nascida na #15) cobre o que só existe com o jogo rodando, a troca de sala e o ciclo de vida dos managers, sobre o `Managers.prefab` real e as cenas de teste; ela é de **caracterização**, e o resto de MonoBehaviour, cena e prefab continua no roteiro de Play Mode da issue;
  - **sem integração contínua** por enquanto: a sessão de revisão roda os testes e anexa o resultado ao PR.
- **Regras da suíte EditMode:** todo estado estático é limpo no `SetUp` e no `TearDown`; comando e consulta se testam chamando o método, não publicando mensagem.
- **Regras da suíte PlayMode:** o estado do jogo se zera **pelos donos** (`InventoryManager.ReplaceAll` e `ReplaceConsumedWorldObjectIds`), nunca por `MessageBroker.Clear()`, que apagaria as assinaturas dos managers vivos, nem escrevendo no `PlayerInputGate` (D-28); os managers sobrevivem de um teste para o outro, então o `SetUp` zera o inventário e o `TearDown` deixa o jogo fora de transição; o teste não instancia manager nem põe nada em cena; um teste que muda o FPS ou o VSync os aplica como última instrução do `SetUp` e os devolve no `TearDown`, antes de qualquer espera (#80).

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
| 2026-10-08 | **Restauradas:** as seções de detalhe da D-19 e da D-08, que saíram do arquivo no PR da #5 (#33) sem que este registro citasse a remoção, enquanto as duas continuavam no resumo e sendo citadas por issues e READMEs. O texto é o mesmo de antes; nenhuma decisão mudou. Encontrado no levantamento da #9. |
| 2026-10-08 | **Cumprida em parte (#9):** D-19. A interface de jogo (diálogo e inventário) e o `EventSystem` saem das cenas e viram o `GameUI.prefab`, aninhado no `Managers.prefab` por decisão do programador no levantamento da #9 (a alternativa, um segundo prefab em `Resources`, exigiria rever a D-09). Nenhuma decisão mudou. |
| 2026-10-08 | **Revistas (#10):** **D-21** ganha o que o levantamento decidiu com o programador: o roteiro cita o personagem pelo nome exibido; a expressão é uma etiqueta no fim da fala, sem persistir, e o padrão é a primeira da lista; o retrato fica à esquerda, fora da caixa, que se desloca 150 px; o protagonista tem retrato; a troca do nome do protagonista (#26) mora em um único método do apresentador. **D-22** ganha o formato do id de personagem e do nome de expressão. |
| 2026-10-08 | **Revista (#48):** **D-21** troca o retrato ao lado da caixa pela tela de personagens: sprites de corpo inteiro em dois lugares (esquerda e direita), atrás da caixa, que volta ao centro; quem fala fica em destaque e o outro escurece; o primeiro a falar fica à esquerda e um terceiro toma o lugar de quem falou há mais tempo; ao abrir as opções o protagonista entra em destaque; a tela só é limpa no fim da conversa. A expressão deixa de ser "sem estado em lugar nenhum": a última expressão de quem está escurecido fica guardada no `ConversationStage` (apresentador, só da conversa em curso, fora do save). O protagonista é o campo `Protagonist` do `CharacterRegistry`, e a #26 passa a usá-lo em vez de comparar o `Id`. "Sprites de corpo inteiro" e "vários personagens na tela" saem do fora do escopo; fica fora mais de dois lugares, o roteiro posicionar alguém, transições e ajuste por personagem. |
| 2026-10-09 | **Detalhada (#48):** **D-21**, enquadramento. O programador rejeitou a primeira composição (arte de corpo inteiro a 39%, calibrada pelo protagonista) e decidiu com os artistas que a composição é da arte: personagens desenhados em uma cena de 3840×2160, exportados em 100% e cortados na borda de baixo da tela, desenhados pelo jogo na metade. Continua valendo uma escala e uma linha de chão únicas, sem ajuste por personagem, e a diferença de altura entre os personagens é mantida de propósito. |
| 2026-10-08 | **Exemplos trocados (#47):** a personagem de exemplo passa do nome provisório ao definitivo, Luna (id `luna`, nós `luna_*`, `$afinidade_luna`) em D-18, D-21 e D-22, porque o nome definitivo da personagem chegou com a arte dela. Nenhuma decisão mudou. |
| 2026-10-14 | **Nota (#11)** na D-06: o typewriter, onde mora o estado da revelação e como completar passa pelo `DialogueManager`. Nenhuma decisão mudou. |
| 2026-10-14 | **Cumprida (#25):** D-30. O histórico de falas existe: registro no apresentador, itens pelo `GameFlow` só quando o inventário muda, bloqueio no dono do diálogo, sem modo novo no `GameFlow`. **Nota na D-08:** um clique de ponteiro sobre um botão da interface não avança a fala. Nenhuma decisão mudou. |
| 2026-10-14 | **Revista (#55):** D-30. O histórico de falas passa de lista `Nome: texto` a balões com lados (protagonista à direita, os demais à esquerda, narração e item ao centro), com a cor do balão vinda da `NameColor` e o texto escolhido pelo contraste. **O critério "`Nome: texto`, com o nome na cor do personagem" da #25 foi substituído:** o nome fica dentro do balão, sem cor própria, só na primeira fala de uma sequência. O `ConversationLog` guarda entradas estruturadas. Nenhuma outra decisão mudou. |
| 2026-10-16 | **Revistas (#61):** **D-21** ganha o pensamento do protagonista: `Pensamento:` é um nome reservado, reconhecido no apresentador antes do registro, sem placa, em itálico e na cor de pensamento, escurece a tela, não passa por `NameToDisplay` e não pode ser o nome de um personagem. **D-30** ganha a entrada `Thought` no histórico: à direita, balão neutro tingido pela cor de pensamento, sem nome, interrompendo a sequência de falas. Decidido com o programador no levantamento da #61 (cor `#C9DCFF`; balão na cor de pensamento, opção (a), trocado na reexecução por o balão neutro tingido, porque a transparência não dava o contraste de 4,5:1). Nenhuma outra decisão mudou. |
| 2026-10-16 | **Revistas (#65):** **D-21** ganha a opção de ação: a etiqueta `#acao` no fim de uma opção a torna um gesto do protagonista (itálico no botão), `acao` não é expressão, e a verificação dos roteiros aceita `#acao` só em opção. **D-30**: a opção de ação escolhida entra no histórico como narração (centro, itálico, balão neutro), sem tipo novo de entrada. Decidido com o programador no planejamento e no levantamento da #65 (opção (a) da pergunta 1: `Narration`, e não um tipo `Action`). Nenhuma outra decisão mudou. |
| 2026-10-20 | **Referência de asmdef autorizada (#32):** `ProjetoVN.Editor` passa a listar `YarnSpinner.dll` e `Yarn.Google.Protobuf.dll` em `precompiledReferences` (com `overrideReferences`), o que o `Tests.asmdef` já faz, para o seletor de nó ler o programa compilado do Yarn (`ParseFrom`). Nenhuma seta entre módulos mudou, nenhum ciclo nasceu. Autorizado pelo programador na execução da #32. Nenhuma decisão mudou. |
| 2026-10-20 | **Cumprida em parte (#12):** D-19. O fade e o comando de troca de sala existem: o `Canvas_Fade` entra no `GameUI.prefab` e o `RoomNavigator` no `Managers.prefab`, com um modo novo, `RoomTransitionState`. **Regra nova, decidida pelo programador no levantamento da #12:** uma conversa aberta recusa a troca de sala, inclusive o Reload Scene do painel de debug (a nota da #9 deixava a decisão para esta issue). A lista de cenas do build ganhou `[Teste] Sala B`, autorizado pela issue. Nenhuma outra decisão mudou. |
| 2026-10-20 | **Cumprida em parte (#13):** D-19. As saídas (`RoomExit`) e os pontos de entrada (`RoomEntryPoint`) existem, e a troca de sala ganha a chegada no ponto. **Regra nova, decidida pelo programador no levantamento da #13 (opção (b)):** uma falha inesperada na troca é registrada e o jogo volta à exploração com a tela clara, em vez de ficar preso na transição. Ponto de entrada inexistente é avisado pelo `OnValidate`, reprovado por teste e vira erro no console na chegada; sem seletor no Inspector. Nenhuma outra decisão mudou. |
| 2026-10-20 | **Cumprida (#15):** D-24. A suíte PlayMode existe (`Tests.PlayMode`, três testes de caracterização da troca de sala) e a D-24 passa a ter duas suítes e as regras próprias da PlayMode. **Referências de asmdef autorizadas pelo programador na #15:** `Tests.PlayMode` → `ProjetoVN.GameFlow`, `ProjetoVN.Inventory`, `ProjetoVN.PointNClick`, `ProjetoVN.Dialogue`, `UnityEngine.UI` e o pacote Yarn Spinner (este por o `DialogueUIController` herdar de uma classe do Yarn, autorizado durante a execução). O assembly fica no topo do grafo: nenhum módulo de jogo o referencia, nenhuma seta entre módulos mudou, nenhum ciclo nasceu. Nenhuma outra decisão mudou. |
| 2026-10-20 | **Nota (#14)** na D-10: o registro de objetos consumidos fica no `InventoryManager`, de forma definitiva, e o id persistente de um coletável é único no jogo inteiro. Nenhuma decisão mudou. |
| 2026-10-21 | **Nova (#79):** D-31, decidida pelo programador na revisão da #15 ("vamos deixar em 1"). A atribuição `Application.runInBackground` no Editor grava o Player Settings (medido na #79), então a "armadilha do Editor sem foco" deixa de valer para o projeto e a receita de atribuir a opção sai da documentação. O congelamento do Play Mode com a opção desligada não se reproduziu na #79. Nenhuma outra decisão mudou. |
| 2026-10-21 | **Nota (#80)** na D-24: a suíte PlayMode roda em 10 FPS e sem limite, e o teste que muda FPS ou VSync os devolve no `TearDown`. Nenhuma decisão mudou. |
| 2026-10-21 | **Nota (#81)** na D-31: com a janela sem foco o jogo roda a 30 FPS com o VSync desligado (`UnfocusedFrameRateLimiter`), e o "Rever só se" deixa de citar a #81 como futura. A janela que abre sem nunca receber o foco fica sem limite até a primeira perda de foco: limitação aceita pelo programador na #81. Nenhuma decisão mudou. |
| 2026-10-22 | **Cumprida em parte (#16):** D-20. O salvar automático ao entrar na sala e o carregar que recarrega a cena existem; falta o salvar pela pausa (#18). **Regra nova, decidida pelo programador no levantamento da #16 (opção (a)):** ao carregar, o jogador chega no ponto de entrada por onde entrou na sala, guardado no save (`currentEntryPointId`); sem ponto, na posição da cena. O `Load()` passa a ser tudo ou nada (troca de sala aceita primeiro, estado restaurado depois), e um save corrompido é avisado em vez de lançar exceção. A suíte PlayMode e a verificação passam a sobrescrever o `savegame.json` do Editor, aceito pelo programador. Nenhuma outra decisão mudou. |
| 2026-10-22 | **Cumprida em parte (#17):** D-19 e D-20. `Menu.unity` passa a ser a primeira cena do build (`ProjectSettings/EditorBuildSettings.asset`, só a lista de cenas, autorizado pela issue) e esconde a interface de jogo pelo `Canvas` do `Canvas_Game` (`GameUIVisibility`); o Novo Jogo (`StartNewGame`, sem apagar o arquivo; save corrompido conta como sem save), o Continuar e o Sair funcionam. **Referência de asmdef autorizada pelo programador na #17:** `Tests.PlayMode` → `ProjetoVN.Core`, para o teste do `StartNewGame` escrever e limpar o `StoryState`; o assembly continua no topo do grafo, nenhuma seta entre módulos mudou, nenhum ciclo nasceu. Nenhuma decisão mudou. |
| 2026-10-23 | **Cumpridas (#18):** D-13 (a pilha simples de janelas, `WindowStack` no `UIWindowManager`), D-19 (o `Canvas_Pause` no `GameUI.prefab`) e D-20 (salvar pela pausa; **voltar ao menu não grava o save**, por isso o `RoomNavigator.GoToMainMenu`). D-28 ganha a nota do `PauseState`. **Referências de asmdef autorizadas pelo programador no levantamento da #18:** `ProjetoVN.GameFlow` → `Unity.InputSystem` (o `PauseMenu` lê a ação Pause por `InputActionReference`, D-08) e `Tests` → `ProjetoVN.UI` (TDD da `WindowStack`, D-03 e D-24). Nenhuma seta entre módulos mudou e nenhum ciclo nasceu: a primeira é a um pacote e a segunda sai do topo do grafo. **Respostas do programador:** a pausa congela o jogo (`Time.timeScale = 0`, devolvido no `Exit`); voltar ao menu é permitido durante uma conversa, que é encerrada depois de retomar a pausa e antes do `GoToMainMenu`; voltar ao menu pede confirmação (a `Confirm Return Window`, empilhada sobre a pausa); "Jogo salvo." aparece depois de salvar. Nenhuma outra decisão mudou. |

O histórico de execução das refatorações antigas (itens `ARCH-01` a `ARCH-22`, citados em alguns comentários de código) estava no roadmap removido. Para consultá-lo:

```bash
git show 391bf88:ARCHITECTURE_ROADMAP.md
```
