# Guia de autoria: salas e objetos

**Para quem:** quem monta cenas no Unity Editor. Você não precisa ler código para seguir este guia.

> **Este guia cobre o que existe hoje.** A cena de referência, com um exemplo de cada coisa descrita aqui, é `Assets/Scenes/[Teste] Mecanicas.unity`. As duas salas do percurso do jogador, `[Fatia] Onibus` e `[Fatia] Posto`, foram montadas seguindo este guia, e o [passo a passo](#passo-a-passo-a-fatia-vertical) no fim registra a ordem e o que a montagem precisou além do que as seções diziam.

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

**O nome da sala é o nome da cena** (`[Teste] Sala B`), exatamente como está no arquivo. **Ele é único no jogo inteiro:** guarde todas as cenas de sala direto em `Assets/Scenes/`, sem subpasta, porque duas cenas de mesmo nome em pastas diferentes derrubam o `RoomLinksTests` e o `MainMenuTests` sem dizer qual sala se repete (#105). Uma cena que existe no projeto mas não está na lista é uma sala inexistente para o jogo: o console mostra `A sala '…' não existe. A cena existe e está na lista de cenas do build?` e o jogo fica onde está. A sala de teste `[Teste] Sala B` é o exemplo mais enxuto: câmera, fundo e um objeto que abre `porta_trancada`.

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

## Passo a passo: a fatia vertical

A fatia vertical é o conteúdo provisório que exercita todos os sistemas juntos (#21): duas salas, `[Fatia] Onibus` e `[Fatia] Posto`, ligadas por uma porta que só abre depois do prólogo, e uma moeda achada no ônibus que abre a máquina de café do posto. O prefixo `[Fatia]` marca conteúdo provisório, como `[Teste]`. Nada disso exigiu código: é Inspector e um arquivo de roteiro. Os arquivos são `Assets/Scenes/[Fatia] Onibus.unity`, `Assets/Scenes/[Fatia] Posto.unity` e `Assets/Roteiro/Testes/fatia_vertical.yarn`; as variáveis novas estão em `variaveis.yarn` e o item é `Moeda.asset`.

### Ordem da montagem

1. **O roteiro primeiro.** Escreva os nós em `fatia_vertical.yarn` e declare em `variaveis.yarn` cada variável nova (`$prologo_concluido`, `$porta_onibus_aberta`, `$conheceu_luna`, `$cafe_do_posto`), com a linha `///`. Reimporte o `Roteiro.yarnproject`, senão os nós novos não aparecem na lista do gatilho. O prólogo (`onibus_viagem`) **não é alterado**: o nó `onibus_poltrona` o chama com `<<detour onibus_viagem>>` e, quando ele termina, liga `$prologo_concluido` com `<<set>>`; isso impede repetir o prólogo e a folha do nome.
2. **O item.** `Create → Items → Item` (`moeda`) e uma linha no `ItemRegistry.asset`. O ícone é provisório.
3. **Cena nova** salva em `Assets/Scenes/`. Dentro dela, na ordem: `Systems/Main Camera`, `Environment` (o fundo), `Interactables`, `EntryPoints` e `Room Music`.
4. **Os objetos**, um a um, a partir do prefab `Interactable`: ele já traz o sprite, o collider, o realce e um **Interactable Dialogue Trigger** vazio.
5. **A lista de cenas do build** e, no `Menu.unity`, o **Start Room** do `Main Menu`.
6. **Rodar a suíte de EditMode**: ela reprova gatilho com nó inexistente, saída para sala ou ponto que não existe, id de coletável repetido e mais de um `Room Music` por cena.

### `[Fatia] Onibus` (sem pan)

- **Câmera:** tag `MainCamera`, **Projection** ortográfica, **Size** 5, **Clear Flags** `Solid Color` preto, um `AudioListener`, o **Universal Additional Camera Data** e o **Point N Click Selector**. É a câmera das cenas de teste; copie os valores dela.
- **Fundo:** um objeto `Environment/Background` com um **Sprite Renderer**. O sprite é `testebckg_0`, um dos sprites já fatiados de `Assets/_Tests/Background/testebckg.png` (a textura está em `Sprite Mode: Multiple`: arraste o sprite, não a textura), em escala 1,25. Sem **Screen Pan Controller**, e `Interactables` e `EntryPoints` ficam na raiz da cena.
- **`Room Music`:** um objeto na raiz com o componente **Room Music** e `teste_musica_a`.
- **`EntryPoints/EntryPoint_Porta`:** **Room Entry Point** de id `porta`. Sem pan, o X dele não move nada; só precisa existir.
- **`Interactables/Poltrona`:** o gatilho com o nó `onibus_poltrona`, ligado em **On Interact** (`InteractableDialogueTrigger.TriggerDialogue`). É por ela que o prólogo abre, até a #71 abrir a conversa sozinha ao chegar na sala.
- **`Interactables/Item_Moeda`:** acrescente **Collectable Item Behaviour** com **Item Data** `Moeda`, e confira que **Persistent Id** veio preenchido e é novo (**Regenerate Persistent Id** se o objeto nasceu de uma cópia). Em **On Interact**, ligue `Collect` e depois `TriggerDialogue` (nó `onibus_moeda`), como o `Item_Chave` da cena de teste.
- **`Interactables/Porta_Onibus`:** um portão **só de flag**, com **Locked Action Behaviour** e **Room Exit**:
  - **Required Flag Id** `$prologo_concluido` e **Unlocked Flag Id** `$porta_onibus_aberta`, **sem item**. As duas variáveis precisam ser **diferentes**, senão o **On Unlocked** nunca dispara.
  - **Room Exit:** destino `[Fatia] Posto`, ponto `onibus`.
  - **On Interact** → `LockedActionBehaviour.Interact`.
  - **On Locked** → o gatilho com `onibus_porta_fechada` e o `Som Trancada` (filho com **Sound Effect**).
  - **On Unlocked** → o `Som Abrindo` e `RoomExit.Leave`. **On Already Unlocked** → `RoomExit.Leave`. **On Opened** fica vazio.

### `[Fatia] Posto` (com pan)

- **Fundo e pan:** `Environment/ScrollingBackground` leva o **Screen Pan Controller** (**Environment Container** = ele mesmo, **Pan Speed** 10, **Edge Threshold** 80, **Min X** -40, **Max X** 40) e três painéis com o mesmo sprite, `LeftPanel` (x -20,9, escala -1,25 em X), `MiddlePanel` (x 0) e `RightPanel` (x 20,9). `Interactables` e `EntryPoints` são **filhos do `ScrollingBackground`**, para andarem com o pan.
- **`Room Music`:** `teste_musica_b`.
- **`EntryPoints/EntryPoint_Onibus`:** id `onibus`, no X do `Exit_Onibus` (20): é o lugar que fica no centro da tela quando o jogador chega do ônibus.
- **`Interactables/NPC_Luna`:** o gatilho com `posto_luna`, em **On Interact**.
- **`Interactables/Maquina_Cafe`:** um portão **só de item**, **Required Item** `Moeda` (consumida ao abrir) e **Unlocked Flag Id** `$cafe_do_posto`. Ela tem **três** falas, uma para cada situação, então o objeto leva **três** **Interactable Dialogue Trigger**: o do prefab (`posto_maquina_sem_moeda`, ligado em **On Locked**), `posto_maquina_cafe` (em **On Unlocked**, com um `Sound Effect`) e `posto_maquina_vazia` (em **On Already Unlocked**).
- **`Interactables/Exit_Onibus`:** uma saída comum (**Room Exit**, destino `[Fatia] Onibus`, ponto `porta`), com `RoomExit.Leave` em **On Interact**. É a porta do ônibus vista de fora, que já está aberta.

### O que a montagem precisou e o guia não dizia

- **Uma porta que só leva embora depois de aberta** liga `RoomExit.Leave` em **On Unlocked** (o instante em que abriu) **e** em **On Already Unlocked** (as vezes seguintes). Só o segundo deixaria a primeira abertura sem efeito.
- **Nunca ligue fala e saída no mesmo evento.** Uma conversa aberta recusa a troca de sala. Por isso o **On Unlocked** da `Porta_Onibus` leva só o som e a saída; a fala de "porta aberta" não existe.
- **Um portão só de flag é aberto pelo roteiro:** quem liga `$prologo_concluido` é o `<<set>>` no fim do nó `onibus_poltrona`. O jogador clica na porta depois, e o portão confere a flag.
- **Mais de uma fala no mesmo portão:** acrescente um **Interactable Dialogue Trigger** por fala no **mesmo objeto** (não em filhos). O menu do evento mostra os componentes **com o mesmo nome**, e o que vale é a **ordem deles no Inspector** (o primeiro da lista é o de cima). Confira em cada um o **Node Name**.
- **O prefab `Interactable` traz um gatilho vazio** que não avisa. Em uma saída ou em um objeto sem fala, ele pode ficar.
- **A câmera de uma cena vazia** precisa dos valores da câmera de teste (ortográfica, tamanho 5, fundo preto, um só `AudioListener`).
- **Uma sala sem pan não precisa do `ScrollingBackground`**, mas o ponto de entrada precisa existir mesmo assim, com id.
- **Item novo, flag nova e nó novo** têm uma ordem: o item no registro, a variável em `variaveis.yarn` e o nó no `.yarn` **antes** de preencher o campo que os usa, porque as listas do Inspector só mostram o que o Unity já importou.
- **Salve as cenas antes de rodar a suíte.** Com uma cena suja, o Test Runner abre um diálogo e trava o Editor.
