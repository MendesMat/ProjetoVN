# Guia de autoria: salas e objetos

**Para quem:** o agente que monta as salas, os itens e os personagens pelo Unity CLI, com o material que o programador fornece, e o programador, que confere o resultado pelo Inspector. O guia dá os campos e os menus pelo nome que o Inspector mostra (**Pan Speed**, **Create → Items → Item**). O nome do campo no código, o tipo de cada componente e o que a montagem por script precisa saber estão em [unity-cli.md](../agentes/unity-cli.md#montar-uma-sala-só-a-partir-do-guia-e-conferi-la-com-o-despejo-107); onde um nome não se deduz do outro, o guia dá os dois (**Background Type**, `clearFlags`).

> **Este guia cobre o que existe hoje.** A cena de referência, com um exemplo de cada coisa descrita aqui, é `Assets/Scenes/[Teste] Mecanicas.unity`. As salas do percurso do jogador, `[Fatia] Onibus`, `[Fatia] Posto` e `[Fatia] Loja`, foram montadas por script de Editor (as duas primeiras na #21; o pátio da Cena 2 do prólogo e a loja, na #130), e o [passo a passo](#passo-a-passo-a-fatia-vertical) no fim foi escrito a partir dessa montagem. Na #107 o passo a passo foi conferido: a `[Fatia] Posto` foi remontada do zero, por um agente que só tinha este guia e as receitas de [unity-cli.md](../agentes/unity-cli.md), e o que a montagem não achou no texto foi corrigido aqui. O `[Fatia] Onibus` não foi remontado.

## Regras que valem sempre

- **Não coloque managers na cena.** `DialogueManager`, o `DialogueRunner` do Yarn Spinner, `StoryStateVariableStorage`, `InventoryManager`, `GameStateController`, `RoomNavigator`, `DialogueInputHandler`, `GameSaveManager`, `ItemScriptActions`, `AudioManager`, `SoundScriptActions` e `ItemCollectedSound` são criados sozinhos quando o jogo começa. Um deles dentro da cena quebra o jogo de forma difícil de perceber.
- **Não coloque `Canvas` de jogo nem `EventSystem` na cena.** A caixa de diálogo, os botões de escolha, o painel de inventário, o fade de tela da troca de sala e o `EventSystem` também são criados sozinhos quando o jogo começa, e são os mesmos em todas as salas. Se o console mostrar `There are 2 event systems in the scene` sem parar, há um `EventSystem` sobrando na cena: apague-o.
- **Não altere um asset durante o Play.** Itens são dados somente leitura.
- **Leia os avisos do console ao salvar.** Os componentes avisam quando um campo obrigatório ficou vazio.
- **Todo id é único.** O id de um item e o id persistente de um coletável são gravados no save. O id persistente é único no **jogo inteiro**, não só na sala.

## Objeto interativo

Qualquer objeto do cenário em que o jogador pode clicar.

1. Use o prefab `Assets/Prefabs/Gameplay/Interactable.prefab`, ou acrescente a um objeto um `Collider2D` e o componente **Interactable Item**.
2. Em **On Interact**, ligue o que deve acontecer no clique (veja as seções abaixo).
3. **Outline Object** é opcional: um objeto filho que aparece quando o mouse passa por cima. Ele **não pode ser o próprio objeto nem um pai dele**: o objeto se desligaria ao entrar na sala e sumiria (o Inspector avisa).

O objeto cresce um pouco no hover (**Highlight Scale Multiplier**: 1 = não cresce, 1,15 = 15% maior, o valor do prefab; o campo não aceita menos que 1).

Todo campo do componente tem tooltip: passe o mouse sobre o nome do campo no Inspector para ver o que ele é, um exemplo e o que acontece vazio.

## Rótulo de placeholder (#191)

Enquanto um objeto interativo ainda é o quadrado do prefab `Interactable`, ele mostra sobre si um **rótulo sempre visível, de duas linhas**: o **tipo** (`CONVERSA`, `SAÍDA`, `ITEM`, `PORTA TRANCADA`, `OBJETO`) e um **nome** legível (`Motorista`, `Loja`). Quem testa uma build sabe o que existe na sala e o que cada clique faz, sem passar o mouse em cada quadrado. O prefab já traz o componente **Placeholder Label** e o filho `Label` (um `TextMeshPro` de mundo, Atkinson Hyperlegible Next, com contorno escuro): uma sala nova **já nasce com o rótulo**, e quem monta só escreve o nome.

- **Display Name** é o único campo que você preenche, **à mão e em português**. Numa saída, escreva **para onde ela leva** (`Loja`, `Pátio`). Vazio: o rótulo mostra `(sem nome)`, o console avisa e o teste `PlaceholderLabelsTests` reprova a sala (a cena e o caminho do objeto). O campo **Label** (o filho de texto) já vem ligado no prefab; vazio = objeto sem rótulo, com erro no console.
- **O tipo vem dos componentes que o objeto já tem**, não de um campo, para nunca ficar desatualizado. Um objeto com mais de um papel mostra **um tipo só**, o primeiro desta ordem (a ordem em que o clique decide: o portão confere a tranca antes de tudo):

  | Ordem | Componente no objeto | Tipo | Cor do quadrado |
  |---|---|---|---|
  | 1 | **Locked Action Behaviour** | `PORTA TRANCADA` | vermelho `#E04A4A` |
  | 2 | **Room Exit** | `SAÍDA` | roxo `#9B51E0` |
  | 3 | **Collectable Item Behaviour** | `ITEM` | amarelo `#F2C230` |
  | 4 | **Interactable Dialogue Trigger** com **Node Name** preenchido | `CONVERSA` | azul `#2F80ED` |
  | 5 | nenhum dos quatro | `OBJETO` | cinza `#9E9E9E` |

  O gatilho só conta **com nó**, porque todo objeto feito do prefab traz um gatilho vazio (sem isso, toda saída e todo objeto novo seriam `CONVERSA`). Uma saída que leva a outra sala por uma porta trancada (`Porta_Onibus`) é `PORTA TRANCADA`; um item com fala de reação (`Item_Chave`) é `ITEM`.
- **O quadrado muda para a cor do tipo** quando a sala carrega, e a cor que estiver gravada na instância não conta (o `Item_Chave`, a `Door_PortaTrancada` e o `Exit_Passagem` da `[Teste] Mecanicas` tinham cor própria e voltaram à do prefab). **O verde é do `PlaceholderTint`** e quer dizer "porta aberta": nenhum tipo usa verde, e a porta aberta continua mostrando `PORTA TRANCADA` (quem diz que abriu é o verde; o tipo não muda com o estado). Para quem não distingue vermelho de verde, a diferença entre trancada e aberta fica fraca: aceito por ser provisório.
- **A Scene view mostra o rótulo sem apertar Play.** O **Placeholder Label** é `[ExecuteAlways]`: ao abrir a sala, ou ao criar o objeto, ele já escreve o texto e a cor do tipo, e ao digitar o **Display Name** o texto se atualiza em seguida. Isso fica só na memória do Editor: a cena não fica suja por abrir (o texto e a cor da instância não viram *override*), e um quadrado preto com `OBJETO / (sem nome)` na Scene view quer dizer que o componente ou o campo **Label** está faltando.
- **Sem seta no nome.** `→` e `←` **não existem** na Atkinson Hyperlegible Next (têm `› » · •` e os acentos). Um caractere que a fonte não tem faria o TextMeshPro criar, em tempo de execução, um filho de reserva que o **Hidden Until Story Flag** não esconde. O `PlaceholderLabelsTests` reprova o nome com um caractere assim.
- **Objeto escondido e coletável recolhido:** nada a fazer. O rótulo é um **filho do prefab**, não criado em tempo de execução: o **Hidden Until Story Flag** o esconde e o mostra junto com o quadrado, e o coletável recolhido desativa o objeto inteiro, rótulo incluso.
- **Hover:** o objeto cresce 15% e o rótulo cresce junto; o destaque (`Highlight`) continua atrás do quadrado e o rótulo na frente (ordem de desenho 0, 1 e 2).
- **Tirar o placeholder quando a arte chegar**, objeto a objeto: (1) na instância, remova o componente **Placeholder Label** e o filho `Label`; (2) troque o sprite; (3) volte a **Color** do **Sprite Renderer** para branco (o componente pintava o quadrado). O clique, o hover, o portão e o escondido não conhecem o rótulo e seguem funcionando. O teste deixa de cobrar o objeto porque o sprite deixou de ser o quadrado.
- **O que o teste confere:** em toda cena do build, todo objeto com collider cujo sprite é o quadrado de placeholder tem o rótulo (componente e filho), um **Display Name** preenchido e só com caracteres que a fonte desenha. Collider é o critério porque é por ele que o mouse acha o objeto: o contorno e o fundo da `[Teste] Sala B` usam o mesmo sprite sem collider.

Os nomes das salas de teste (as da fatia estão nas tabelas de [Passo a passo: a fatia vertical](#passo-a-passo-a-fatia-vertical)):

| Cena | Objeto | Tipo | Display Name |
|---|---|---|---|
| `[Teste] Mecanicas` | `NPC_Luna` | `CONVERSA` | `Luna` |
| | `Item_Chave` | `ITEM` | `Chave` |
| | `Door_PortaTrancada` | `PORTA TRANCADA` | `Porta Sala B` |
| | `Exit_Passagem` | `SAÍDA` | `Sala B (passagem)` |
| | `Exit_Janela` | `SAÍDA` | `Sala B (janela)` |
| `[Teste] Sala B` | `Porta_Trancada` | `CONVERSA` | `Porta` |
| | `Exit_Porta` | `SAÍDA` | `Mecânicas (porta)` |
| | `Exit_Janela` | `SAÍDA` | `Mecânicas (janela)` |

## Coletável

Um objeto que vira item do inventário ao ser clicado.

1. Crie o item: botão direito no Project → **Create → Items → Item**. Preencha **Id** (único, sem espaços), **Item Name**, **Description** e **Icon**. A **Description** ainda não aparece no jogo (examinar um item pelo painel está fora do escopo); o **Item Name** vazio é avisado no Inspector.
2. Acrescente o item à lista do asset `Assets/Scripts/ScriptableObjects/Items/ItemRegistry.asset`. Sem isso, o item não é restaurado ao carregar um save. O registro avisa se dois itens têm o mesmo id.
3. No objeto interativo, acrescente o componente **Collectable Item Behaviour** e aponte **Item Data** para o item.
4. Em **On Interact**, ligue `CollectableItemBehaviour.Collect`.

O campo **Persistent Id** é gerado sozinho e é **único no jogo inteiro**: é por ele que o save lembra que o objeto foi coletado, em qualquer sala. **Se você duplicar um coletável, ou copiá-lo para outra sala, o id vem copiado:** clique com o botão direito no componente e escolha **Regenerate Persistent Id** na cópia.

O componente avisa no console e no Inspector quando **Item Data** ou **Persistent Id** está vazio e quando outro coletável **da mesma cena** tem o mesmo id (o aviso cita o outro objeto). O aviso não enxerga outras salas: depois de duplicar ou copiar coletáveis, rode a suíte de testes, cujo `CollectableIdsTests` reprova id vazio ou repetido em qualquer sala e diz a cena e o caminho de cada objeto.

## Diálogo ao clicar

1. No objeto interativo, acrescente o componente **Interactable Dialogue Trigger** e escolha em **Node Name**, na lista, o nó do roteiro que ele inicia (por exemplo `porta_trancada`). A lista traz os nós dos arquivos `.yarn` em `Assets/Roteiro/`, em ordem alfabética; o nome do nó de entrada vem no PR do roteirista (formato `<quem_ou_onde>_<assunto>`, ver [as convenções do guia](roteiro.md#convenções-de-nome)). Um nó novo aparece na lista depois que o Unity reimporta o roteiro (selecione outro objeto e volte).
2. Em **On Interact**, ligue `InteractableDialogueTrigger.TriggerDialogue`.

**(nenhum)** deixa o campo vazio: o clique não inicia conversa e avisa no console. Um nó que deixou de existir (o roteirista o renomeou ou apagou) continua no campo, aparece no fim da lista e ganha um aviso no Inspector; o nó `variaveis`, que só declara variáveis, não está na lista e, se estiver gravado, recebe o mesmo aviso. Se o roteiro não compila, o campo vira texto comum, com o motivo no aviso. Além do aviso, o teste de EditMode `ScriptNodeFieldsTests` reprova qualquer gatilho de cena ou prefab cujo nó não existe, e diz a cena (ou o prefab), o caminho do objeto e o nome do nó: rode a suíte depois de uma mudança no roteiro.

Os diálogos não são mais assets do Inspector: quem escreve a conversa é o roteirista, em arquivos de texto; ver [roteiro.md](roteiro.md).

## Conversa ao chegar na sala (#127)

Uma sala pode abrir uma conversa sozinha quando o jogador chega nela, sem clique: é o caso do prólogo no ônibus. Não exige código.

1. Crie um objeto vazio **na raiz** da cena, chamado `Room Arrival` (ao lado do `Room Music`), e acrescente o componente **Room Arrival Dialogue**.
2. Em **Node Name**, escolha na lista o nó do roteiro que a sala abre (por exemplo `onibus_chegada`). A lista é a mesma do gatilho de clique, e um nó novo aparece depois que o Unity reimporta o roteiro.
3. Se a sala deve chegar **com a tela escura** (a conversa começa no preto e clareia, como o prólogo), ligue **Starts Dark**. Deixe desligado nas demais: a sala chega com a tela clara, como sempre.

**(nenhum)** deixa o campo vazio: a sala não abre conversa, e o Inspector e o console avisam. Um nó que deixou de existir continua no campo, com aviso, e o teste `ScriptNodeFieldsTests` o reprova. **Uma conversa de chegada por sala:** o `RoomArrivalDialogueTests` reprova uma cena com dois.

- **Quando abre:** em **toda chegada por troca de sala**: o Novo Jogo, uma saída (`RoomExit`), o Continuar e o Reload Scene. Abre **depois** de a tela clarear e de o jogo voltar à exploração, e depois do save automático da chegada. **Dar Play direto na sala no Editor não é chegada** e não abre nada.
- **Quando não abre:** numa troca que falhou (a tela clareia e o jogo volta à exploração, sem conversa de chegada) e na volta ao menu.
- **Se repete?** A sala abre o nó a cada chegada, sem condição: quem decide se a conversa se repete é o **roteiro** (ver [roteiro.md](roteiro.md#conversa-de-chegada-a-sala-abre-o-nó-e-o-nó-decide)). O `onibus_chegada` é o exemplo: chama o prólogo só se `$prologo_concluido` ainda é falso. Um nó que, na segunda chegada, não tem nada a dizer abre e fecha a conversa sem mostrar a caixa.
- **Uma conversa de chegada que termina** devolve o jogo à exploração, como qualquer outra; a pausa e o histórico funcionam nela. Quem sai no meio dela deixa o save de antes dela (salvar é recusado durante uma conversa), então no Continuar ela abre de novo.
- **Uma sala sem objeto `Room Arrival`** não abre nada, sem aviso.

### Chegar com a tela escura (#71)

Com **Starts Dark** ligado, na troca de sala o jogo liga o escuro do roteiro **antes** de a tela clarear (a troca em si continua clareando em 0,4 s, agora **sobre o preto**, então o jogador não vê a sala). Quando a conversa de chegada abre, o nó clareia com `<<clarear>>` ([roteiro.md](roteiro.md#tela-e-imagens)). O escuro mora no `GameUI.prefab`, **não na cena**: você só liga o campo.

- **Todo o resto da chegada não muda:** a conversa abre depois de a tela clarear e do jogo sair da transição, e o save da chegada acontece antes dela.
- **O escuro vale em toda chegada naquela sala**, inclusive ao **voltar** a ela por uma saída (o ônibus, depois do prólogo): a tela chega preta, a conversa de chegada abre e fecha sem mostrar a caixa e o preto some em 0,4 s. É a chegada comum, 0,4 s mais longa.
- **Se a conversa de chegada não abrir** (nó vazio ou ausente), o escuro é desfeito na hora, então a sala nunca fica presa no preto.
- **Sem o campo ligado, ou sem `Room Arrival`,** nada muda para a sala.

## Portão (porta trancada)

Um objeto que só libera uma ação com um item, uma flag ou os dois, e que lembra que foi aberto.

1. No objeto interativo, acrescente o componente **Locked Action Behaviour**.
2. Preencha pelo menos um requisito:
   - **Required Item:** o item exigido. Ele é **consumido** ao abrir.
   - **Required Flag Id:** uma variável de história que precisa estar ligada. Ela **não** é consumida.
3. Preencha **Unlocked Flag Id** com uma variável **única para este portão** (por exemplo `$porta_biblioteca_destrancada`). É o que mantém o portão aberto depois. Dois portões com a mesma variável abrem juntos. Se a **mesma porta** é vista das duas salas que ela liga, use a **mesma** variável nos dois lados: é isso que a faz continuar aberta de ambos. Por isso nenhum teste reprova uma variável repetida entre portões.

**Os dois campos de variável são listas.** Eles mostram as variáveis **booleanas** declaradas em `Assets/Roteiro/variaveis.yarn`, em ordem alfabética, mais **(nenhuma)** para deixar o campo vazio. Abaixo do campo aparece a descrição da variável escolhida. As variáveis numéricas (afinidade) e de texto não aparecem.

- **Uma variável nova** (a do seu portão) entra primeiro em `variaveis.yarn`, com uma linha `///` de descrição e `<<declare $nome = false>>`; veja [roteiro.md](roteiro.md#variáveis-e-afinidade). Depois do Unity importar o arquivo, ela aparece na lista.
- **Um valor que não está declarado** (a variável foi renomeada ou nunca existiu) continua no campo e aparece com um aviso amarelo logo abaixo. Escolha outra na lista ou declare a variável. Além do aviso, o teste de EditMode `StoryFlagFieldsTests` reprova o valor e diz a cena, o objeto e o campo; rode a suíte depois de renomear ou apagar uma variável do `variaveis.yarn`.
- Se o roteiro não compila, ou o projeto não tem exatamente um `YarnProject`, a lista dá lugar a um aviso e a um campo de texto comum.
- O formato do nome é `$` seguido de minúsculas sem acento, dígitos e `_`. O componente ainda avisa no console se um valor digitado por fora da lista fugir desse formato, e o `StoryFlagFieldsTests` o reprova.
4. Em **On Interact**, ligue `LockedActionBehaviour.Interact`.
5. Ligue os eventos:

| Evento | Quando dispara | Use para |
|---|---|---|
| **On Locked** | O jogador clicou e faltou o requisito | A fala de "está trancada" |
| **On Unlocked** | O instante em que destrancou, uma vez só | A fala de "a chave serviu" |
| **On Opened** | Ao destrancar **e** toda vez que a cena carrega com o portão já aberto | Aparência: trocar o sprite, desligar um collider |
| **On Already Unlocked** | O jogador clicou em um portão já aberto | Ação: atravessar, ir para outra sala (ligue `RoomExit.Leave`, veja [Saídas e pontos de entrada](#saídas-e-pontos-de-entrada)) |

**Cuidado:** nunca ligue em **On Opened** algo que o jogador deveria iniciar. Ele dispara sozinho quando a cena carrega; uma troca de sala ali levaria o jogador embora sem ele clicar. Isso pertence a **On Already Unlocked**.

O componente `PlaceholderTint`, que pinta a porta de verde, é provisório: troque pela arte de porta aberta.

## Objeto que aparece depois (#128)

Um objeto interativo que **não existe para o jogador** até uma variável de história ligar: invisível, sem destaque ao passar o mouse e sem clique. É o caso de uma saída secreta, que só aparece depois de uma conversa.

1. No objeto interativo, acrescente o componente **Hidden Until Story Flag**.
2. Em **Required Flag Id**, escolha a variável que o mostra. O campo é a mesma lista do portão: as variáveis **booleanas** declaradas em `Assets/Roteiro/variaveis.yarn`, mais **(nenhuma)**. Uma variável nova entra antes em `variaveis.yarn` (veja [roteiro.md](roteiro.md#variáveis-e-afinidade)). Campo vazio = o objeto **nunca** aparece, e o componente avisa no console. Um valor fora do formato ou não declarado é avisado do mesmo modo que no portão (`StoryFlagFieldsTests`).
3. Ligue o que o objeto faz normalmente em **On Interact** do Interactable Item (por exemplo, `RoomExit.Leave`). O componente não precisa de ligação.

**Quando o objeto aparece:**
- Ao carregar a sala com a variável já ligada: voltar à sala, carregar um save. O valor está no estado da história, então ele continua visível. O Novo Jogo zera o estado e volta a escondê-lo.
- Quando uma conversa **desta mesma sala** liga a variável (`<<set $falou_com_motorista to true>>`): o objeto aparece **ao fim da conversa**, nunca no meio dela, e fica dali em diante, sem trocar de sala.

**Cuidados:**
- Ele só **aparece**; não some de novo quando a variável desliga.
- Um **portão sem conversa** que ligue a variável de um objeto escondido na mesma sala só o mostra quando o jogador sai e volta. Se o portão abre uma conversa (**On Unlocked**), o objeto aparece ao fim dela.
- **Não combine no mesmo objeto** um portão cujo **On Opened** liga ou desliga o renderer ou o collider: os dois disputariam o mesmo componente.
- Um coletável já recolhido continua sumido: o componente de coleta desativa o objeto, e isso vence.
- O componente esconde o `Sprite Renderer` e o `Box Collider 2D` (e os dos filhos) **que estão ligados**; o que a arte deixou desligado de propósito continua desligado.

Exemplo na `[Teste] Mecanicas`: o `Exit_Passagem` (o quadrado roxo `SAÍDA / Sala B (passagem)`, à direita da porta) só aparece depois de falar com a Luna e escolher "Sim" (`$falou_com_luna`); clicar nele leva à `[Teste] Sala B`. Outro exemplo é a `Estrada` do pátio, em `[Fatia] Posto`: só aparece ao fim da conversa com o motorista (`$falou_com_motorista`, ligada pelo roteiro), e clicar nela abre a narração `posto_ir_andando`.

## Pan do cenário

O componente **Screen Pan Controller** desloca o objeto apontado em **Environment Container** quando o mouse encosta na borda. **Min X** e **Max X** são até onde o cenário anda. Para a tela nunca passar do fundo:

- **Max X** = a borda esquerda do fundo, sem o sinal, menos a metade da largura da tela;
- **Min X** = a metade da largura da tela menos a borda direita do fundo.

A metade da largura da tela é o **Size** da câmera vezes 16/9: **8,89** com Size 5. As bordas do fundo são medidas com a câmera e o **Environment Container** em x 0, como as salas são salvas. O **Max X** é ligado à borda **esquerda** porque o cenário anda para a direita quando o jogador olha para a esquerda.

Exemplo, a `[Teste] Mecanicas`: três painéis de 20,9 de largura (sprite de 16,72 em escala 1,25) em x -20,9, 0 e 20,9 cobrem de -31,35 a 31,35. Então **Max X** = 31,35 − 8,89 = 22,46 e **Min X** = 8,89 − 31,35 = -22,46; a cena usa **22,4** e **-22,4**, arredondados para dentro.

A conta vale para telas 16:9. Uma tela mais larga mostra mais cenário e pode ver a borda (#157).

Os valores das salas são **Pan Speed** 10 (unidades do mundo por segundo; também é o padrão de um componente novo) e **Edge Threshold** 80 (pixels da faixa junto à borda). Nenhum dos dois aceita negativo. O componente avisa no Inspector quando **Min X** é maior que **Max X** (o pan pularia entre os dois limites) e quando **Environment Container** está vazio; **Min X** igual a **Max X** vale e é a sala sem pan.

## Criar uma sala nova

Uma cena de sala contém **só o mundo**:

- **Uma câmera**, no objeto `Systems/Main Camera` (`Systems` é um objeto vazio na raiz), com estes valores, que são os das salas `[Fatia]`:
  - **Position** (0, 0, -10) e a tag `MainCamera`;
  - **Projection** ortográfica, **Size** 5, **Depth** 0 (o padrão);
  - **Background Type** `Solid Color`, preto. É o nome que a câmera do URP mostra em **Environment**; no código, e em quem monta por script, é `clearFlags` (**Clear Flags** no Inspector da câmera antiga);
  - um `AudioListener` (um só na cena);
  - o componente **Point N Click Selector**, que transforma o clique em interação e não tem campo para preencher;
  - o **Universal Additional Camera Data**, que o Inspector da câmera do URP acrescenta sozinho ao mostrá-la. Quem monta por script precisa acrescentá-lo.

  **Não copie a câmera da `[Teste] Mecanicas`:** ela usa **Depth** -1 e fundo azul (**Skybox**), e as salas da fatia não. Copiar a câmera de teste dá uma sala com o fundo e o **Depth** diferentes dos das outras salas.
- **O fundo** (um ou mais sprites).
- **Os objetos interativos**, montados como descrito em [Objeto interativo](#objeto-interativo).
- Opcional: o **Screen Pan Controller**, se o fundo for mais largo que a tela.
- Um **Room Entry Point** para cada lugar em que o jogador pode chegar vindo de outra sala, e um **Room Exit** em cada saída (veja [Saídas e pontos de entrada](#saídas-e-pontos-de-entrada)).

E **não** contém: managers, `Canvas` de jogo (diálogo, inventário) nem `EventSystem`. Dê Play direto na cena nova: a interface aparece sozinha e um objeto com **Interactable Dialogue Trigger** já abre a conversa.

O `Canvas_Debug` (botões Save, Load, Reset Session e Reload Scene) é ferramenta da cena de teste e não vai para uma sala.

### Registrar a sala

Uma cena só pode ser alvo de uma troca de sala se estiver na lista de cenas do build:

1. Salve a cena em `Assets/Scenes/`.
2. Abra **File → Build Profiles → Scene List** e acrescente a cena (**Add Open Scenes** com ela aberta).

**O nome da sala é o nome da cena** (`[Teste] Sala B`), exatamente como está no arquivo. **Ele é único no jogo inteiro, mesmo em pastas diferentes:** o jogo e o save acham a sala só pelo nome, então `Capitulo1/Corredor` e `Capitulo2/Corredor` seriam a mesma sala para eles, e só uma seria alcançável. As cenas de sala podem ficar em subpastas de `Assets/Scenes/` (por capítulo, por exemplo), desde que nenhum nome se repita. O `RoomLinksTests` reprova duas cenas habilitadas com o mesmo nome e mostra os dois caminhos (#105). Uma cena que existe no projeto mas não está na lista é uma sala inexistente para o jogo: o console mostra `A sala '…' não existe. A cena existe e está na lista de cenas do build?` e o jogo fica onde está. A sala de teste `[Teste] Sala B` é o exemplo mais enxuto: câmera, fundo e um objeto que abre `porta_trancada`.

Durante uma conversa aberta o jogo recusa a troca de sala.

**A sala onde o Novo Jogo começa** é o campo **Start Room** do objeto `Main Menu`, na cena `Menu.unity` (e **Start Entry Point Id**, o ponto de chegada; vazio = a posição em que a cena foi salva). É o único lugar onde ela é configurada: o texto é o nome exato da cena (hoje `[Fatia] Onibus`, com o ponto vazio). Um teste reprova uma sala que não está na lista do build, o próprio menu, ou um ponto que a sala não tem.

## Saídas e pontos de entrada

A navegação do jogo é só por saídas no cenário: não há mapa. Uma **saída** (`Room Exit`) é o que o jogador clica para ir a outra sala, e um **ponto de entrada** (`Room Entry Point`) é onde ele chega na sala de destino.

### Criar um ponto de entrada

1. Na cena da **sala de destino**, crie um objeto vazio e acrescente o componente **Room Entry Point**. Um objeto filho do cenário que se move (o **Environment Container** do pan) é o mais prático, porque anda junto com o fundo; mas funciona em qualquer lugar.
2. Preencha **Id**: o nome do ponto, **único nesta sala**, em minúsculas e sem espaços (por exemplo `porta_biblioteca`). As saídas o citam por esse nome.
3. Posicione o objeto. **O que importa é o X:** o lugar do cenário que fica **no centro da tela** quando o jogador chega, medido com o cenário na posição em que a cena foi salva. Na Scene view o ponto aparece como uma linha vertical ciano.

Se a sala não tem **Screen Pan Controller** (o fundo tem a largura da tela), o ponto não move nada: ele só existe para a saída achar onde chegar. Mesmo assim ele precisa existir, com id.

Se o pan limita o movimento (**Min X** e **Max X**), um ponto fora dos limites faz o jogador chegar na borda mais próxima.

### Criar uma saída

1. No objeto que o jogador clica, acrescente o componente **Room Exit** (o prefab `Interactable` serve de base).
2. Preencha:

| Campo | O que escrever | Se ficar vazio ou errado |
|---|---|---|
| **Destination Room** | O nome da cena de destino, **exatamente como o arquivo** (`[Teste] Sala B`). A cena precisa estar na [lista de cenas do build](#registrar-a-sala) | Vazio: aviso no Inspector, e o clique avisa no console e nada acontece. Nome que não é uma cena do build: erro no console ao clicar, e o jogo fica onde está |
| **Entry Point Id** | O **Id** de um Room Entry Point da sala de destino | Vazio: aviso no Inspector, e o jogador chega na posição em que a cena foi salva. Id que a sala de destino não tem: erro no console ao chegar (diz a sala e o id), e o jogador chega na posição em que a cena foi salva |

3. Ligue `RoomExit.Leave`:
   - em um objeto comum, em **On Interact** do **Interactable Item**;
   - em uma porta trancada, em **On Already Unlocked** do **Locked Action Behaviour** (a porta só leva a outra sala depois de aberta).

   **Não ligue em On Opened**: ele dispara sozinho quando a cena carrega, e o jogador seria levado embora sem clicar.

Para ir e voltar entre duas salas, cada sala tem a sua saída e o seu ponto de entrada, e cada saída cita o ponto **da outra sala**. Exemplo da cena de teste: a porta de `[Teste] Mecanicas` (destino `[Teste] Sala B`, ponto `porta_mecanicas`) e, na `Sala B`, `Exit_Porta` (destino `[Teste] Mecanicas`, ponto `porta_sala_b`, que na Mecanicas fica na posição da porta).

### O que confere por você

- O **Inspector** avisa destino vazio, ponto de entrada vazio e id vazio no ponto.
- O teste `RoomLinksTests`, na suíte de EditMode, reprova saída com destino que não é uma cena do build, saída que cita um ponto que a sala de destino não tem, ponto sem id ou com id repetido na mesma sala, e duas cenas habilitadas do build com o mesmo nome. A mensagem diz a cena, o caminho do objeto e o nome; no nome de cena repetido, ela diz o nome e os caminhos das cenas que o repetem. **Rode a suíte depois de montar ou renomear uma saída ou um ponto, e depois de acrescentar uma sala à lista do build.**
- Se algo falhar de forma inesperada no meio da troca, o erro aparece no console e o jogo volta a ficar jogável na sala que ficou carregada, em vez de travar com a tela preta.

## Personagens

Um personagem é um asset **Character** (`CharacterSO`) e uma linha no registro. Não precisa de código nem de cena: personagem novo é um asset, uma linha no registro e uma linha nas tabelas do guia de roteiro. Os assets ficam em `Assets/Scripts/ScriptableObjects/Characters/`.

### Criar um personagem

1. **A arte.** Os personagens são desenhados **dentro de uma cena de conversa composta em 3840×2160** (16:9), todos na mesma cena, de frente, com os pés na mesma linha: é ali que a arte decide o tamanho de cada um e a diferença de altura entre eles. A cena atual é a referência [`docs/arte/referencia_cena_de_conversa.png`](../arte/referencia_cena_de_conversa.png). Cada expressão é entregue como um PNG **com fundo transparente, exportado em 100% (sem redimensionar), cortado rente à figura nos lados e em cima, e cortado embaixo exatamente na borda de baixo da tela**. O jogo desenha todas as imagens na mesma escala e alinhadas pela borda de baixo, então uma imagem fora desse padrão aparece no tamanho ou na altura errados. Todas as expressões do mesmo personagem têm o mesmo tamanho de imagem. A imagem fica em `Assets/Sprites/Personagens/<personagem>/`, uma pasta por personagem, com o nome `<personagem>_<expressão>.png`, tudo em minúsculas sem acento (`Assets/Sprites/Personagens/luna/luna_raiva.png`). Na tela só aparece do busto para cima: a caixa de diálogo cobre o resto.
   No Inspector da imagem (**Import Settings**), confira antes de usá-la:

   | Campo | Valor | Por quê |
   |---|---|---|
   | **Texture Type** | `Sprite (2D and UI)` | |
   | **Sprite Mode** | `Single` | O padrão do projeto vem `Multiple`, e com ele o Unity fatia a imagem sozinho (ou não gera sprite nenhum). Uma imagem inteira é `Single` |
   | **Pixels Per Unit** | `100` | O jogo desenha o sprite pelo tamanho em unidades, que **não muda** quando o Unity reduz a textura. Com outro valor, esse personagem aparece num tamanho diferente dos outros |
   | **Generate Mipmaps** | desligado | |
   | **Max Size** | `2048` | Uma arte mais alta que isso é reduzida na importação; como o jogo a desenha na metade do tamanho, em Full HD continua nítida |

   Os dois personagens com arte (`luna` e `protagonista`) são o modelo. Não existe ajuste de tamanho ou de posição por personagem: a escala e a linha de chão são as mesmas para todos, definidas no `GameUI.prefab`. Se um personagem ficar estranho, peça um ajuste à arte ou abra uma issue.
2. **O personagem.** Botão direito na pasta `Characters/` → **Create → Characters → Character**. Preencha:

   | Campo | O que escrever | Se errar |
   |---|---|---|
   | **Id** | Minúsculas sem acento, dígitos e `_` (`luna`). É o que o código usa; único no projeto | O console avisa |
   | **Display Name** | O nome **exatamente como o roteiro o escreve** antes dos dois-pontos, e como aparece na placa (`Luna`). Único no projeto | Vazio ou com espaço nas pontas: o roteiro não acha o personagem, e o console avisa |
   | **Name Color** | A cor do balão do personagem no histórico de falas (a placa de nome usa sempre a mesma cor, para todos) | O texto do balão fica escuro ou claro conforme a cor, e a cor precisa permitir contraste de 4,5:1 com um dos dois. Um cinza médio não permite: o console avisa e um teste da suíte reprova |
   | **Expressions** | Uma entrada por expressão: **Expression** (minúsculas sem acento, dígitos e `_`: `raiva`) e **Sprite**. **A primeira é a expressão padrão**, mostrada nas falas sem etiqueta | Expressão vazia, fora do formato ou repetida, ou sem Sprite: o console avisa. Sem nenhuma entrada, o personagem fala sem aparecer na tela: é assim que um personagem entra antes da arte (`mae`, `pai`, `motorista`, `desconhecido` e `atendente`), e quando ela chegar acrescentam-se as expressões, sem mexer nos roteiros |

3. **O registro.** Abra `CharacterRegistry.asset` (na mesma pasta) e acrescente o personagem à lista **Characters**. Um personagem fora do registro aparece no jogo só com o nome, sem sprite, e o console avisa. O registro acusa id e nome exibido repetidos. O campo **Protagonist** do registro aponta o personagem que o jogo trata como protagonista: é ele quem entra na tela quando as opções de resposta abrem. Ele precisa estar na lista **Characters**; vazio ou fora da lista, o console avisa e as opções não trazem ninguém.
4. **O guia de roteiro.** Acrescente uma linha para o personagem na tabela "Quem fala" e as expressões dele na tabela "Expressões" de [roteiro.md](roteiro.md). É dali que o roteirista copia os nomes.

Uma **expressão nova** de um personagem existente é uma imagem nova na pasta dele, uma entrada em **Expressions** e uma linha na tabela "Expressões" do guia.

O jogo troca o sprite quando o roteiro escreve a expressão no fim da fala (`Luna: Sai daqui. #raiva`). O teste de conteúdo (`ScriptContentTests`) reprova nome de quem fala que não está no registro e expressão que o personagem não tem, com o nó e a linha.

Os sprites aparecem atrás da caixa de diálogo, um de cada lado, e nada some nem entra em narração (quem já está na tela só escurece). Quem decide o lado é a ordem em que os personagens falam na conversa, não o asset. A tela de personagens vive no `GameUI.prefab` (`Canvas_Game/DialogueUI/CharacterStage`), não na cena da sala.

## Música e sons

### Música da sala (#19)

Cada sala declara a sua música, e a troca de uma sala para outra é suave: a música antiga baixa enquanto a nova sobe (1 segundo, o campo **Crossfade Seconds** do `AudioManager`, no `Managers.prefab`). Pôr música numa sala não exige código:

1. Importe a faixa em `Assets/Audio/Musica/` (WAV, sem compressão prévia: o Unity comprime na importação).
2. Selecione o arquivo e, no Inspector, ajuste a importação: **Load Type** = `Streaming`, **Compression Format** = `Vorbis` e **Preload Audio Data** desligado. Assim a cena guarda só a referência, e a faixa inteira não vai para a memória. Clique em **Apply**.
3. Na cena da sala, crie um objeto vazio chamado `Room Music` e acrescente o componente **Room Music**. Arraste a faixa para o campo **Music**.
4. Dê Play direto na cena: a música entra sozinha. `[Teste] Mecanicas` e `[Teste] Sala B` são os exemplos.

Regras:

- **Sala sem música:** não crie o objeto, ou deixe **Music** vazio. O jogo fica em silêncio nessa sala, sem erro. Um objeto `Room Music` desativado também conta como sem música. O `Menu.unity` é assim: não tem `Room Music`.
- **A mesma música em duas salas não reinicia.** Se a sala A e a sala B declaram a mesma faixa, quem passa de uma para a outra continua ouvindo a música de onde estava. Recarregar a mesma sala também não reinicia. Para duas salas soarem como a mesma música, arraste **o mesmo arquivo** nas duas.
- **No máximo um `Room Music` por cena.** O componente se recusa a ser acrescentado duas vezes ao mesmo objeto, e um teste reprova uma cena com dois objetos.
- **Música em loop:** a faixa toca em loop. Faça o final emendar com o começo, ou a emenda estala. Num WAV gerado por script, use duração e frequências inteiras.
- **Não ponha `AudioListener`, `AudioSource` nem o `AudioManager` na cena.** O `AudioListener` já vem na câmera da sala, e o `AudioManager` e as fontes de música, ambiente e efeitos vivem no `Managers.prefab`. A cena leva só o `Room Music` e o `Sound Effect` (abaixo).
- **A pausa não cala a música:** com a pausa aberta ela continua tocando.

### Som em um objeto ou em uma porta (#20)

O componente **Sound Effect** toca um som quando um evento do objeto dispara, sem código (D-07):

1. Importe o som em `Assets/Audio/Efeitos/` (WAV). No Inspector do arquivo: **Load Type** = `Decompress On Load`, **Preload Audio Data** ligado, **Compression Format** = `Vorbis`. Clique em **Apply**.
2. No objeto (por exemplo, a porta), crie um **filho** vazio (`Som Trancada`) e acrescente o componente **Sound Effect**. Arraste o som para o campo **Clip**. Vazio = não toca nada, e o componente avisa.
3. No evento que deve soar (**On Locked**, **On Unlocked**, **On Interact**...), ligue o filho e escolha **SoundEffect → Play**. `Door_PortaTrancada`, em `[Teste] Mecanicas`, tem dois filhos assim: `Som Trancada` (no **On Locked**) e `Som Abrindo` (no **On Unlocked**).

Não renomeie nem mova o componente `SoundEffect` depois de ligar: a ligação do evento guarda o tipo pelo nome e quebra em silêncio.

Dois sons já têm dono e **não** se ligam à mão: o de **coletar item** (toca a cada item que entra no inventário, inclusive o que o roteiro dá com `<<dar_item>>`) e o de **avançar fala**. Eles são campos do `AudioManager`, no `Managers.prefab` (**Button Hover Sound**, **Button Click Sound**, **Line Advance Sound**, **Item Collected Sound**). Trocar um som é substituir o arquivo, mantendo o nome e o `.meta`; um campo vazio deixa aquele som mudo.

### Botões (#20)

Todo `Button` do jogo leva o componente **Button Sounds**: ele toca o som de passar o mouse (só em botão habilitado) e o de clique. Os botões do `GameUI.prefab`, do `Menu.unity`, da cena de teste e dos prefabs do `UIFramework` já levam. **Um botão novo leva o componente**; o teste `ButtonSoundsTests` reprova o que esquecer e diz o caminho. Em um botão que vem de um prefab, ponha o componente **no prefab**, não na instância da cena (o componente não aceita duas cópias no mesmo botão).

### Sons para o roteiro (#20)

O roteiro cita o som por um **id** (`<<tocar_efeito freada_batida>>`; ver [roteiro.md](roteiro.md#música-e-sons)). Para um som novo ficar disponível:

1. Importe o arquivo: efeitos em `Assets/Audio/Efeitos/` (como acima); sons de fundo em `Assets/Audio/Ambiente/` e músicas em `Assets/Audio/Musica/`, com **Load Type** = `Streaming`, **Compression Format** = `Vorbis` e **Preload Audio Data** desligado. O som de fundo toca em loop: faça o final emendar com o começo.
2. Abra `Assets/Scripts/ScriptableObjects/Audio/SoundRegistry.asset` e acrescente uma linha na lista certa (**Music**, **Ambience** ou **Effects**): o **Id** (minúsculas sem acento, dígitos e `_`, único no registro inteiro) e o **Clip**. O registro avisa id vazio, fora do formato ou repetido, e clipe vazio.
3. Acrescente uma linha à tabela [Sons que existem](roteiro.md#música-e-sons) do guia de roteiro.

O teste `ScriptContentTests` reprova um id do roteiro que não esteja na lista certa, e um id repetido ou sem clipe no registro.

### Imagens para o roteiro (#71)

O roteiro cita a imagem de tela cheia por um **id** (`<<mostrar_imagem lembranca_pais>>`; ver [roteiro.md](roteiro.md#tela-e-imagens)). Para uma imagem nova ficar disponível:

1. Ponha o PNG em `Assets/Sprites/Imagens/<id>.png`, em **16:9** (3840×2160 é o tamanho da arte de referência; a imagem é esticada para a tela inteira). No Inspector do arquivo, **Texture Type** = `Sprite (2D and UI)` e **Sprite Mode** = `Single` (o padrão é `Multiple`, e sem fatias o Unity não gera sprite nenhum), **Generate Mip Maps** desligado. **Max Size** fica no padrão, `2048`: uma arte de 3840×2160 entra reduzida a 2048×1152, sem aviso. Isso basta para Full HD (a tela tem 1920 de largura) e perde detalhe em telas maiores, de 1440p ou 4K. Ao contrário do personagem (tabela de importação em [Criar um personagem](#criar-um-personagem)), a imagem é desenhada na tela inteira, não na metade; se a arte precisar ficar nítida acima de Full HD, o Max Size passa a `4096`, que pesa cerca de quatro vezes mais na memória. Clique em **Apply**.
2. Abra `Assets/Scripts/ScriptableObjects/Pictures/PictureRegistry.asset` e acrescente uma linha: o **Id** (minúsculas sem acento, dígitos e `_`, único no registro) e o **Sprite**. O registro avisa id vazio, fora do formato ou repetido, e sprite vazio.
3. Acrescente uma linha à tabela [Imagens que existem](roteiro.md#imagens-que-existem) do guia de roteiro.

O teste `ScriptContentTests` reprova um id do roteiro que não esteja no registro, e um id repetido, fora do formato ou sem sprite no registro. A imagem aparece atrás dos personagens e da caixa de diálogo, e **sem** o `Canvas` da cena: ela vive no `GameUI.prefab` (`Canvas_Game/ScriptScreen`).

## Passo a passo: a fatia vertical

A fatia vertical é o conteúdo provisório que exercita todos os sistemas juntos (#21, refeita na #130 com a Cena 2 do prólogo): três salas, `[Fatia] Onibus`, `[Fatia] Posto` (o pátio) e `[Fatia] Loja`, ligadas por saídas, com a porta do ônibus abrindo só depois do prólogo. No pátio, cada opção da Cena 2 é um objeto clicável (a estrada, só depois de falar com o motorista), e a loja tem o atendente, a máquina de café e o banheiro. O prefixo `[Fatia]` marca conteúdo provisório, como `[Teste]`. Nada disso exigiu código: é Inspector e roteiro. Os arquivos são `Assets/Scenes/[Fatia] Onibus.unity`, `Assets/Scenes/[Fatia] Posto.unity`, `Assets/Scenes/[Fatia] Loja.unity` e `Assets/Roteiro/Testes/fatia_vertical.yarn` (só os nós do ônibus); os nós do pátio e da loja são o texto do roteirista, em `Assets/Roteiro/ato_1/prologo/episodio_2/` (#129). Não há mais a Luna do posto, a moeda nem a máquina de café com moeda (saíram na #130): a `[Teste] Mecanicas` guarda os exemplos de afinidade, item e portão.

### Ordem da montagem

1. **O roteiro primeiro.** Os nós vêm de `ato_1/prologo/episodio_2/` (o pátio e a loja) e de `fatia_vertical.yarn` (o ônibus); declare em `variaveis.yarn` cada variável nova (`$prologo_concluido`, `$porta_onibus_aberta`, `$falou_com_motorista`), com a linha `///`. Reimporte o `Roteiro.yarnproject`, senão os nós novos não aparecem na lista do gatilho. O prólogo (`onibus_viagem`) **não é alterado por quem monta a sala** (é um roteiro; os efeitos de tela dele vieram na #71): o nó `onibus_chegada`, que o `Room Arrival` do ônibus abre na chegada, o chama com `<<detour onibus_viagem>>` e, quando ele termina, liga `$prologo_concluido` com `<<set>>`; isso impede repetir o prólogo e a folha do nome, e o `<<if not $prologo_concluido>>` do nó deixa a conversa de chegada vazia nas chegadas seguintes. O `onibus_poltrona` é só o pensamento do protagonista.
2. **O item, só se a sala tem um coletável.** `Create → Items → Item` e uma linha no `ItemRegistry.asset` (ver [Coletável](#coletável)). A fatia não tem nenhum: o `salgadinho` que a atendente dá vem do roteiro (`<<dar_item>>`), e o item já está no registro.
3. **Cena nova** salva em `Assets/Scenes/`. Dentro dela, na ordem: `Systems/Main Camera`, `Environment` (o fundo), `Interactables`, `EntryPoints`, `Room Music` e, se a sala abre uma conversa ao chegar, `Room Arrival` (ver [Conversa ao chegar na sala](#conversa-ao-chegar-na-sala-127)). Onde `Interactables` e `EntryPoints` ficam depende do pan: **sem pan** são irmãos de `Environment`, na raiz (`[Fatia] Onibus`); **com pan** são filhos do `ScrollingBackground`, para andarem com ele (`[Fatia] Posto`).
   - **Comece pela cena vazia.** No Editor, **File → New Scene** com o modelo do projeto (**Lit 2D (URP)**) traz uma `Main Camera` com fundo azul, fora de `Systems`, e um **Global Light 2D**; nenhuma sala do projeto tem esse objeto de luz, então apague os dois e monte a câmera como em [Criar uma sala nova](#criar-uma-sala-nova). Por script, `EditorSceneManager.NewScene` com `NewSceneSetup.EmptyScene` já devolve a cena sem nada. Antes dele, confira que nenhuma cena aberta tem alteração não salva: o `NewScene` fecha todas e descarta o que não foi gravado, sem perguntar (receita em [unity-cli.md](../agentes/unity-cli.md#abrir-e-salvar-cena)).
   - **Sem `Canvas`, sem `EventSystem`, sem manager:** o modelo não traz nenhum dos três, mas outro modelo pode trazer.
4. **Os objetos**, um a um, a partir do prefab `Interactable`: ele já traz o sprite (um quadrado de 1 × 1, provisório, que o **Placeholder Label** pinta na cor do tipo), o **Box Collider 2D** de 0,8 × 0,8 marcado como trigger, o **Highlight Scale Multiplier** 1,15, o **Outline Object** (o filho `Highlight`), um **Interactable Dialogue Trigger** vazio e o **Placeholder Label** com o filho `Label` ([Rótulo de placeholder](#rótulo-de-placeholder-191)). Esses valores **ficam como estão**, e o objeto entra na cena com escala 1. **Escreva o Display Name de cada objeto** (as tabelas abaixo têm os da fatia); sem ele o teste reprova a sala.
5. **A lista de cenas do build** e, no `Menu.unity`, o **Start Room** do `Main Menu`.
6. **Rodar a suíte de EditMode**: ela reprova gatilho ou conversa de chegada com nó inexistente (`ScriptNodeFieldsTests`), saída para sala ou ponto que não existe, id de coletável repetido, mais de um `Room Music` e mais de um `Room Arrival` por cena, e (`PlaceholderLabelsTests`, #191) objeto interativo de uma sala do build que usa o quadrado de placeholder sem rótulo, sem **Display Name** ou com um caractere que a fonte não tem. **A suíte lê toda cena e todo prefab de `Assets/`, esteja a cena na lista do build ou não** (a #107 provou: com a cena fora da lista, um nó e um ponto errados foram reprovados), então dá para conferir uma sala antes do passo 5. A lista do build continua necessária para a sala ser **destino** de uma troca, e o `RoomLinksTests` reprova a saída que cita uma sala fora dela.

### `[Fatia] Onibus` (sem pan)

- **Câmera:** a descrita em [Criar uma sala nova](#criar-uma-sala-nova) (`Systems/Main Camera`, **Background Type** `Solid Color` preto, **Depth** 0). Não é a câmera da cena de teste.
- **Fundo:** um objeto `Environment/Background` com um **Sprite Renderer**. O sprite é `testebckg_0`, o único sprite de `Assets/_Tests/Background/testebckg.png` (a textura está em `Sprite Mode: Multiple`: arraste o sprite, não a textura), em escala 1,25. Sem **Screen Pan Controller**, e `Interactables` e `EntryPoints` ficam na raiz da cena.
- **`Room Music`:** um objeto na raiz com o componente **Room Music** e `teste_musica_a`.
- **`Room Arrival`:** um objeto na raiz com o componente **Room Arrival Dialogue**, **Node Name** `onibus_chegada` e **Starts Dark** ligado (#71: o prólogo começa no preto e clareia). É ele que abre o prólogo sozinho no Novo Jogo (e a cada chegada no ônibus, com o nó decidindo se repete).
- **`EntryPoints/EntryPoint_Porta`:** **Room Entry Point** de id `porta`. Sem pan, o X dele não move nada; só precisa existir.
- **`Interactables/Poltrona`** (`CONVERSA`, **Display Name** `Poltrona`): o gatilho com o nó `onibus_poltrona`, ligado em **On Interact** (`InteractableDialogueTrigger.TriggerDialogue`). Depois da #127 ela só mostra o pensamento "O motorista mandou todo mundo descer. Melhor ir até a porta."; o prólogo abre pelo `Room Arrival`.
- **`Interactables/Porta_Onibus`** (`PORTA TRANCADA`, **Display Name** `Porta do ônibus`): um portão **só de flag**, com **Locked Action Behaviour** e **Room Exit**:
  - **Required Flag Id** `$prologo_concluido` e **Unlocked Flag Id** `$porta_onibus_aberta`, **sem item**. As duas variáveis precisam ser **diferentes**, senão o **On Unlocked** nunca dispara.
  - **Room Exit:** destino `[Fatia] Posto`, ponto `onibus`.
  - **On Interact** → `LockedActionBehaviour.Interact`.
  - **On Locked** → o gatilho com `onibus_porta_fechada` e o `Som Trancada` (filho com **Sound Effect**).
  - **On Unlocked** → o `Som Abrindo` e `RoomExit.Leave`. **On Already Unlocked** → `RoomExit.Leave`. **On Opened** fica vazio.

### `[Fatia] Posto` (com pan)

- **Fundo e pan:** a hierarquia é `Environment/ScrollingBackground`, com `Backgrounds`, `Interactables` e `EntryPoints` como filhos. O `ScrollingBackground` fica em (0, 0, 0) e leva o **Screen Pan Controller** (**Environment Container** = ele mesmo, **Pan Speed** 10, **Edge Threshold** 80, **Min X** -22,4, **Max X** 22,4; a conta está em [Pan do cenário](#pan-do-cenário) e vale para estes três painéis de 20,9 de largura). `Backgrounds` é um objeto vazio só para agrupar os três painéis, que têm o mesmo sprite (`testebckg_0`) e escala Y 1,25: `LeftPanel` (x -20,9, escala X -1,25, o sprite espelhado), `MiddlePanel` (x 0, escala X 1,25) e `RightPanel` (x 20,9, escala X -1,25). O agrupamento em `Backgrounds` e a escala em Z são escolha de quem monta: nada depende deles. `Interactables` e `EntryPoints` são **filhos do `ScrollingBackground`**, para andarem com o pan, e as posições abaixo são **locais** a ele (como ele está em 0, valem também como posição no mundo).
- **`Room Music`:** um objeto na raiz da cena (não dentro do `ScrollingBackground`), com **Room Music** e `teste_musica_b`.
- **`EntryPoints/EntryPoint_Onibus`:** id `onibus`, em x 20 (o mesmo X do `Exit_Onibus`), y 0: é o lugar que fica no centro da tela quando o jogador chega do ônibus.
- **`Interactables/Exit_Onibus`:** em x 20, y 0, escala 1. Uma saída comum (**Room Exit**, destino `[Fatia] Onibus`, ponto `porta`), com `RoomExit.Leave` em **On Interact**; o gatilho vazio do prefab fica. É a porta do ônibus vista de fora, que já está aberta.
- **`EntryPoints/EntryPoint_Loja`:** id `loja`, em x 4 (o mesmo X do `Exit_Loja`): é o lugar que fica no centro da tela quando o jogador volta da loja.
- **`Room Arrival`:** um objeto na raiz da cena, ao lado do `Room Music`, com **Room Arrival Dialogue**, **Node Name** `posto_chegada` e **Starts Dark** **desligado** (a sala chega com a tela clara). É a narração da chegada; o nó decide se se repete (ver [Conversa ao chegar na sala](#conversa-ao-chegar-na-sala-127)).
- **Os objetos da Cena 2** (#130) nascem do prefab `Interactable`, como filhos de `Interactables`, em y 0 e escala 1, e cada um liga o próprio nó em **On Interact**. As posições são locais ao `ScrollingBackground`, na ordem da estrada (esquerda) até o ônibus (direita):

  | Objeto | X | Display Name | Componentes e ligação |
  |---|---|---|---|
  | `Estrada` | -24 | `Estrada` | **Interactable Dialogue Trigger** `posto_ir_andando` (`TriggerDialogue`); **Hidden Until Story Flag** com **Required Flag Id** `$falou_com_motorista`: só aparece ao fim da conversa com o motorista (o rótulo some e volta junto) |
  | `Cadeira` | -14 | `Cadeira` | gatilho `posto_esperar_mecanico` |
  | `Caminhoneiros` | -6 | `Caminhoneiros` | gatilho `posto_caminhoneiros` |
  | `Exit_Loja` | 4 | `Loja` | **Room Exit**, destino `[Fatia] Loja`, ponto `patio`, com `RoomExit.Leave` em **On Interact**; o gatilho vazio do prefab fica |
  | `Motorista` | 12 | `Motorista` | gatilho `posto_motorista` |
  | `Exit_Onibus` | 20 | `Ônibus` | a saída descrita acima |

  Por que essas posições: chegando do ônibus (centro em 20) a tela mostra de 11,1 a 28,9, então o motorista e a porta do ônibus aparecem juntos; chegando da loja (centro em 4), a porta da loja e os caminhoneiros; com o pan no limite (+22,4) a tela mostra de -31,3 a -13,5, e a estrada (-24) e a cadeira (-14) cabem. **O pan não mudou:** os três painéis continuam e os limites ±22,4 também.

Os objetos são o quadrado do prefab, na cor do tipo e com o rótulo, na escala 1: o motorista, os caminhoneiros e o atendente são só a área clicável sobre o fundo provisório, sem sprite de personagem (fora do escopo, [visao-geral.md](../jogo/visao-geral.md#fora-do-escopo-atual)). Um objeto maior que isso precisa de escala **e** de collider acompanhando; o **Box Collider 2D** do prefab segue a escala do objeto.

### `[Fatia] Loja` (sem pan)

Uma sala só de objetos, como o ônibus: o fundo é o mesmo `testebckg_0`, em escala 1,25 (cobre de -10,45 a 10,45, mais largo que a tela de 17,78), então não há **Screen Pan Controller**, e `Interactables` e `EntryPoints` ficam na raiz. Câmera e fundo como em [`[Fatia] Onibus`](#fatia-onibus-sem-pan).

- **`Room Music`:** `teste_musica_b`, **a mesma do pátio**: entre as duas salas a música não reinicia, o que se espera de entrar numa loja do mesmo posto.
- **`Room Arrival`:** **Node Name** `loja_chegada`, **Starts Dark** desligado.
- **`EntryPoints/EntryPoint_Patio`:** id `patio`, em x -7. Sem pan não move nada, mas precisa existir.
- **`Interactables`**, do prefab, y 0, escala 1:

  | Objeto | X | Display Name | Ligação |
  |---|---|---|---|
  | `Exit_Patio` | -7 | `Pátio` | **Room Exit**, destino `[Fatia] Posto`, ponto `loja`, com `RoomExit.Leave` em **On Interact** |
  | `Atendente` | -3 | `Atendente` | gatilho `loja_atendente` |
  | `Maquina_Cafe` | 1 | `Máquina de café` | gatilho `loja_maquina_cafe` |
  | `Banheiro` | 5 | `Banheiro` | gatilho `loja_banheiro` (o som da porta trancada vem do roteiro, `<<tocar_efeito porta_trancada>>`: nenhum **Sound Effect** na cena) |

A `[Fatia] Loja` está na lista de cenas do build, depois do `[Fatia] Posto`.

### O que a montagem precisou e o guia não dizia

- **Uma porta que só leva embora depois de aberta** liga `RoomExit.Leave` em **On Unlocked** (o instante em que abriu) **e** em **On Already Unlocked** (as vezes seguintes). Só o segundo deixaria a primeira abertura sem efeito.
- **Nunca ligue fala e saída no mesmo evento.** Uma conversa aberta recusa a troca de sala. Por isso o **On Unlocked** da `Porta_Onibus` leva só o som e a saída; a fala de "porta aberta" não existe.
- **Um portão só de flag é aberto pelo roteiro:** quem liga `$prologo_concluido` é o `<<set>>` no fim do nó `onibus_chegada` (o `onibus_poltrona` não liga mais). O jogador clica na porta depois, e o portão confere a flag.
- **Mais de uma fala no mesmo portão:** acrescente um **Interactable Dialogue Trigger** por fala no **mesmo objeto** (não em filhos). O menu do evento mostra os componentes **com o mesmo nome**, e o que vale é a **ordem deles no Inspector** (o primeiro da lista é o de cima). Confira em cada um o **Node Name**.
- **O prefab `Interactable` traz um gatilho vazio** que não avisa. Em uma saída ou em um objeto sem fala, ele pode ficar.
- **A câmera de uma cena vazia** precisa dos valores de [Criar uma sala nova](#criar-uma-sala-nova) (ortográfica, tamanho 5, fundo preto em **Background Type**, **Depth** 0, posição (0, 0, -10), um só `AudioListener`), e **não** dos da câmera de teste (#107).
- **Um portão tem de ser ligado ao próprio clique** (#107): **On Interact** do **Interactable Item** → `LockedActionBehaviour.Interact`, também no portão só de item (o `Door_PortaTrancada` da `[Teste] Mecanicas`, que tem dois gatilhos e é portão). Sem isso o clique não chega ao portão.
- **O que a remontagem da #107 achou** (despejo das duas cenas e `diff`): a `[Fatia] Posto` montada por script a partir só deste guia ficou igual à real em tudo o que o guia dizia. As diferenças que sobraram eram o que ele não dizia (**Depth** da câmera, posição e escala dos três objetos, ordem dos eventos do **On Unlocked** da máquina, o objeto `Backgrounds`), e agora diz. A remontagem foi por script: montar pelo Inspector não foi testado.
- **Ao montar por script, o console avisa de campo vazio** (`Id`, **Destination Room**, **Clip**, **Environment Container**) a cada `AddComponent`, porque o componente se valida antes de o script preenchê-lo. Os avisos somem quando o campo é preenchido e a cena é salva; confira o console no fim, não no meio da montagem (receita em [unity-cli.md](../agentes/unity-cli.md)).
- **Uma sala sem pan não precisa do `ScrollingBackground`**, mas o ponto de entrada precisa existir mesmo assim, com id.
- **Item novo, flag nova e nó novo** têm uma ordem: o item no registro, a variável em `variaveis.yarn` e o nó no `.yarn` **antes** de preencher o campo que os usa, porque as listas do Inspector só mostram o que o Unity já importou.
- **Salve as cenas antes de rodar a suíte.** Com uma cena suja, o Test Runner abre um diálogo e trava o Editor.
