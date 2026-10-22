# Guia de autoria: salas e objetos

**Para quem:** quem monta cenas no Unity Editor. Você não precisa ler código para seguir este guia.

> **Este guia cobre o que existe hoje.** As seções marcadas com *a preencher* são escritas pela issue indicada, quando a mecânica existir. A cena de referência, com um exemplo de cada coisa descrita aqui, é `Assets/Scenes/[Teste] Mecanicas.unity`.

## Regras que valem sempre

- **Não coloque managers na cena.** `DialogueManager`, o `DialogueRunner` do Yarn Spinner, `StoryStateVariableStorage`, `InventoryManager`, `GameStateController`, `RoomNavigator`, `DialogueInputHandler`, `GameSaveManager` e `ItemScriptActions` são criados sozinhos quando o jogo começa. Um deles dentro da cena quebra o jogo de forma difícil de perceber.
- **Não coloque `Canvas` de jogo nem `EventSystem` na cena.** A caixa de diálogo, os botões de escolha, o painel de inventário, o fade de tela da troca de sala e o `EventSystem` também são criados sozinhos quando o jogo começa, e são os mesmos em todas as salas. Se o console mostrar `There are 2 event systems in the scene` sem parar, há um `EventSystem` sobrando na cena: apague-o.
- **Não altere um asset durante o Play.** Itens são dados somente leitura.
- **Leia os avisos do console ao salvar.** Os componentes avisam quando um campo obrigatório ficou vazio.
- **Todo id é único.** O id de um item e o id persistente de um coletável são gravados no save. O id persistente é único no **jogo inteiro**, não só na sala.

## Objeto interativo

Qualquer objeto do cenário em que o jogador pode clicar.

1. Use o prefab `Assets/Prefabs/Gameplay/Interactable.prefab`, ou acrescente a um objeto um `Collider2D` e o componente **Interactable Item**.
2. Em **On Interact**, ligue o que deve acontecer no clique (veja as seções abaixo).
3. **Outline Object** é opcional: um objeto filho que aparece quando o mouse passa por cima.

O objeto cresce um pouco no hover (**Highlight Scale Multiplier**).

## Coletável

Um objeto que vira item do inventário ao ser clicado.

1. Crie o item: botão direito no Project → **Create → Items → Item**. Preencha **Id** (único, sem espaços), **Item Name**, **Description** e **Icon**.
2. Acrescente o item à lista do asset `Assets/Scripts/ScriptableObjects/Items/ItemRegistry.asset`. Sem isso, o item não é restaurado ao carregar um save. O registro avisa se dois itens têm o mesmo id.
3. No objeto interativo, acrescente o componente **Collectable Item Behaviour** e aponte **Item Data** para o item.
4. Em **On Interact**, ligue `CollectableItemBehaviour.Collect`.

O campo **Persistent Id** é gerado sozinho e é **único no jogo inteiro**: é por ele que o save lembra que o objeto foi coletado, em qualquer sala. **Se você duplicar um coletável, ou copiá-lo para outra sala, o id vem copiado:** clique com o botão direito no componente e escolha **Regenerate Persistent Id** na cópia.

O componente avisa no console e no Inspector quando **Item Data** ou **Persistent Id** está vazio e quando outro coletável **da mesma cena** tem o mesmo id (o aviso cita o outro objeto). O aviso não enxerga outras salas: depois de duplicar ou copiar coletáveis, rode a suíte de testes, cujo `CollectableIdsTests` reprova id vazio ou repetido em qualquer sala e diz a cena e o caminho de cada objeto.

## Diálogo ao clicar

1. No objeto interativo, acrescente o componente **Interactable Dialogue Trigger** e escolha em **Node Name**, na lista, o nó do roteiro que ele inicia (por exemplo `porta_trancada`). A lista traz os nós dos arquivos `.yarn` em `Assets/Roteiro/`, em ordem alfabética; o nome do nó de entrada vem no PR do roteirista (formato `<quem_ou_onde>_<assunto>`, ver [as convenções do guia](roteiro.md#convenções-de-nome)). Um nó novo aparece na lista depois que o Unity reimporta o roteiro (selecione outro objeto e volte).
2. Em **On Interact**, ligue `InteractableDialogueTrigger.TriggerDialogue`.

**(nenhum)** deixa o campo vazio: o clique não inicia conversa e avisa no console. Um nó que deixou de existir (o roteirista o renomeou ou apagou) continua no campo, aparece no fim da lista e ganha um aviso no Inspector; o nó `variaveis`, que só declara variáveis, não está na lista e, se estiver gravado, recebe o mesmo aviso. Se o roteiro não compila, o campo vira texto comum, com o motivo no aviso. Além do aviso, o teste de EditMode `DialogueTriggerNodesTests` reprova qualquer gatilho de cena ou prefab cujo nó não existe, e diz a cena (ou o prefab), o caminho do objeto e o nome do nó: rode a suíte depois de uma mudança no roteiro.

Os diálogos não são mais assets do Inspector: quem escreve a conversa é o roteirista, em arquivos de texto; ver [roteiro.md](roteiro.md).

## Portão (porta trancada)

Um objeto que só libera uma ação com um item, uma flag ou os dois, e que lembra que foi aberto.

1. No objeto interativo, acrescente o componente **Locked Action Behaviour**.
2. Preencha pelo menos um requisito:
   - **Required Item:** o item exigido. Ele é **consumido** ao abrir.
   - **Required Flag Id:** uma variável de história que precisa estar ligada. Ela **não** é consumida.
3. Preencha **Unlocked Flag Id** com uma variável **única para este portão** (por exemplo `$porta_biblioteca_destrancada`). É o que mantém o portão aberto depois. Dois portões com a mesma variável abrem juntos. Se a **mesma porta** é vista das duas salas que ela liga, use a **mesma** variável nos dois lados: é isso que a faz continuar aberta de ambos. Por isso nenhum teste reprova uma variável repetida entre portões.

**Os dois campos de variável são listas.** Eles mostram as variáveis **booleanas** declaradas em `Assets/Roteiro/variaveis.yarn`, em ordem alfabética, mais **(nenhuma)** para deixar o campo vazio. Abaixo do campo aparece a descrição da variável escolhida. As variáveis numéricas (afinidade) e de texto não aparecem.

- **Uma variável nova** (a do seu portão) entra primeiro em `variaveis.yarn`, com uma linha `///` de descrição e `<<declare $nome = false>>`; veja [roteiro.md](roteiro.md#variáveis-e-afinidade). Depois do Unity importar o arquivo, ela aparece na lista.
- **Um valor que não está declarado** (a variável foi renomeada ou nunca existiu) continua no campo e aparece com um aviso amarelo logo abaixo. Escolha outra na lista ou declare a variável.
- Se o roteiro não compila, ou o projeto não tem exatamente um `YarnProject`, a lista dá lugar a um aviso e a um campo de texto comum.
- O formato do nome é `$` seguido de minúsculas sem acento, dígitos e `_`. O componente ainda avisa no console se um valor digitado por fora da lista fugir desse formato.
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

## Pan do cenário

O componente **Screen Pan Controller** desloca o objeto apontado em **Environment Container** quando o mouse encosta na borda. **Min X** e **Max X** são os limites; ajuste conforme a largura do fundo.

## Criar uma sala nova

Uma cena de sala contém **só o mundo**:

- **Uma câmera** com a tag `MainCamera`, ortográfica, com o componente **Point N Click Selector** (é ele que transforma o clique em interação).
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

**O nome da sala é o nome da cena** (`[Teste] Sala B`), exatamente como está no arquivo. Uma cena que existe no projeto mas não está na lista é uma sala inexistente para o jogo: o console mostra `A sala '…' não existe. A cena existe e está na lista de cenas do build?` e o jogo fica onde está. A sala de teste `[Teste] Sala B` é o exemplo mais enxuto: câmera, fundo e um objeto que abre `porta_trancada`.

Durante uma conversa aberta o jogo recusa a troca de sala.

**A sala onde o Novo Jogo começa** é o campo **Start Room** do objeto `Main Menu`, na cena `Menu.unity` (e **Start Entry Point Id**, o ponto de chegada; vazio = a posição em que a cena foi salva). É o único lugar onde ela é configurada: quando existir a primeira sala de verdade, troque o texto ali, pelo nome exato da cena. Um teste reprova uma sala que não está na lista do build, o próprio menu, ou um ponto que a sala não tem.

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
- O teste `RoomLinksTests`, na suíte de EditMode, reprova saída com destino que não é uma cena do build, saída que cita um ponto que a sala de destino não tem, e ponto sem id ou com id repetido na mesma sala. A mensagem diz a cena, o caminho do objeto e o nome. **Rode a suíte depois de montar ou renomear uma saída ou um ponto.**
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
   | **Name Color** | A cor do nome na placa de nome **e do balão do personagem** no histórico de falas | O texto do balão fica escuro ou claro conforme a cor, e a cor precisa permitir contraste de 4,5:1 com um dos dois. Um cinza médio não permite: o console avisa e um teste da suíte reprova |
   | **Expressions** | Uma entrada por expressão: **Expression** (minúsculas sem acento, dígitos e `_`: `raiva`) e **Sprite**. **A primeira é a expressão padrão**, mostrada nas falas sem etiqueta | Expressão vazia, fora do formato ou repetida, ou sem Sprite: o console avisa. Sem nenhuma entrada, o personagem fala sem aparecer na tela: é assim que um personagem entra antes da arte (`mae`, `pai`, `motorista`, `homem` e `mulher`), e quando ela chegar acrescentam-se as expressões, sem mexer nos roteiros |

3. **O registro.** Abra `CharacterRegistry.asset` (na mesma pasta) e acrescente o personagem à lista **Characters**. Um personagem fora do registro aparece no jogo só com o nome, sem cor e sem sprite, e o console avisa. O registro acusa id e nome exibido repetidos. O campo **Protagonist** do registro aponta o personagem que o jogo trata como protagonista: é ele quem entra na tela quando as opções de resposta abrem. Ele precisa estar na lista **Characters**; vazio ou fora da lista, o console avisa e as opções não trazem ninguém.
4. **O guia de roteiro.** Acrescente uma linha para o personagem na tabela "Quem fala" e as expressões dele na tabela "Expressões" de [roteiro.md](roteiro.md). É dali que o roteirista copia os nomes.

Uma **expressão nova** de um personagem existente é uma imagem nova na pasta dele, uma entrada em **Expressions** e uma linha na tabela "Expressões" do guia.

O jogo troca o sprite quando o roteiro escreve a expressão no fim da fala (`Luna: Sai daqui. #raiva`). O teste de conteúdo (`ScriptContentTests`) reprova nome de quem fala que não está no registro e expressão que o personagem não tem, com o nó e a linha.

Os sprites aparecem atrás da caixa de diálogo, um de cada lado, e nada some nem entra em narração (quem já está na tela só escurece). Quem decide o lado é a ordem em que os personagens falam na conversa, não o asset. A tela de personagens vive no `GameUI.prefab` (`Canvas_Game/DialogueUI/CharacterStage`), não na cena da sala.

## Música e sons

*A preencher pelas issues #19 e #20.*
