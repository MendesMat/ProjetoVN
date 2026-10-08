# Mecânicas

Cada seção descreve o que a mecânica faz para o jogador, o que já existe em `main`, o que falta e em que issue isso entra. As decisões citadas (`D-xx`) estão em [../arquitetura/decisoes.md](../arquitetura/decisoes.md).

**Estado:** `existe` (funciona e está testado ou verificado), `parcial` (funciona, mas falta parte), `planejada` (nada no código).

| Mecânica | Estado | Módulo | Issues |
|---|---|---|---|
| [Exploração por clique](#exploração-por-clique) | existe | `PointNClick` | — |
| [Inventário](#inventário) | existe | `Inventory`, `UI` | — |
| [Portões](#portões) | existe | `GameFlow`, `Editor` | — |
| [Diálogo](#diálogo) | parcial | `Dialogue` | #11 |
| [Histórico de falas](#histórico-de-falas) | planejada | `Dialogue` | #25 |
| [Estado da história e afinidade](#estado-da-história-e-afinidade) | existe | `Core`, `Editor` | — |
| [Personagens](#personagens) | parcial | `Dialogue` | #26 |
| [Navegação entre salas](#navegação-entre-salas) | planejada | `GameFlow` | #12, #13, #14 |
| [Salvar e carregar](#salvar-e-carregar) | parcial | `GameFlow` | #16 |
| [Menu e pausa](#menu-e-pausa) | parcial | `UI` | #17, #18, #26 |
| [Áudio](#áudio) | planejada | — | #19, #20 |

A cena de referência, que exercita tudo o que existe, é `Assets/Scenes/[Teste] Mecanicas.unity`.

---

## Exploração por clique

**Para o jogador:** passar o mouse sobre um objeto interativo o destaca; clicar aciona o objeto. Encostar o mouse na borda esquerda ou direita da tela desloca o cenário.

**Existe:**
- Detecção por `Physics2D.OverlapPoint` sob o cursor; destaque por escala e contorno.
- Pan de borda com limites mínimo e máximo.
- Cliques e pan param durante diálogos, e um clique sobre a interface não atinge o mundo.

**Regras:**
- O clique que encerra um diálogo não aciona o objeto que está sob o cursor.
- O módulo não sabe o que o objeto faz. O comportamento é ligado no Inspector (D-07).

**Limitação conhecida:** o destaque ainda aparece em um objeto que esteja atrás de um painel de interface; só o clique é barrado.

## Inventário

**Para o jogador:** itens coletados aparecem em um painel. Alguns são consumidos ao abrir um portão.

**Existe:**
- Coletar, usar (consumir) e consultar.
- O mesmo item não entra duas vezes; não há quantidades.
- O painel sempre reflete o inventário, mesmo se o item foi coletado com o painel fechado.
- Um objeto coletado não reaparece ao recarregar a cena.
- O roteiro dá, tira e consulta itens: `<<dar_item id>>`, `<<remover_item id>>` e `tem_item("id")`. Dar um item que o jogador já tem e remover um que ele não tem não são erro, e `dar_item` repetido nunca deixa duas cópias. Na cena de teste, a Luna entrega a chave pelo roteiro e a porta abre com ela.

**Fora do escopo:** usar ou examinar um item pelo painel; combinar itens.

## Portões

**Para o jogador:** uma porta trancada diz que está trancada. Com a chave, ela abre, a chave é consumida e a porta continua aberta dali em diante.

**Existe:**
- Requisito por item (consumido), por flag (não consumida) ou pelos dois.
- O portão lembra que foi aberto, inclusive depois de recarregar a cena ou carregar um save.
- Quatro eventos para quem monta a cena: trancado, destrancou agora, está aberto (estado), jogador interagiu com portão aberto (ação).
- Os dois campos de variável (a exigida e a que guarda a memória) escolhem de uma lista das variáveis booleanas declaradas em `Assets/Roteiro/variaveis.yarn`, mostram a descrição da escolhida e avisam de um valor que não está declarado.

**Regras:**
- A flag é conferida antes do item, para não gastar a chave à toa.
- Uma ação iniciada pelo jogador (atravessar a porta) pertence ao evento de ação, nunca ao de estado, que também dispara sozinho ao carregar a cena.

**Falta:**
- Atravessar uma porta aberta levar a outra sala (#13).

**Limitação conhecida:** renomear uma variável em `variaveis.yarn` deixa órfão o campo de uma cena que a usava, e o aviso só aparece com o objeto selecionado no Inspector.

## Diálogo

**Para o jogador:** uma caixa mostra quem fala e o que diz. Um clique avança. Em alguns pontos aparecem escolhas.

**Existe (Yarn Spinner, D-17):**
- Os roteiros são arquivos de texto `.yarn` em `Assets/Roteiro/`, escritos por quem não usa o Unity. Uma cena inicia um diálogo pelo nome de um nó.
- Falas em sequência, escolhas (até quatro opções por vez), saltos e desvios entre nós.
- Falas e opções condicionais: uma opção cuja condição é falsa aparece **desabilitada** (esmaecida, sem clique), para o jogador ver que existe um caminho fechado, e ocupa um dos quatro botões. Um bloco em que nenhuma opção está disponível não é mostrado e a conversa segue pela fala depois dele.
- As variáveis do roteiro (`$falou_com_luna`) leem e gravam no estado da história.
- Narração (fala sem nome de personagem) esconde a placa de nome e o retrato.
- O nome de quem fala aparece na cor do personagem, e o retrato dele aparece ao lado da caixa ([Personagens](#personagens)).
- Conteúdo inválido nunca trava o jogo: roteiro com erro de compilação, nó inexistente, `<<jump>>` para nó inexistente e comando desconhecido avisam no console e o jogo segue em exploração.
- Comandos de roteiro para dar, tirar e consultar itens, implementados no `GameFlow`. Um id de item que não existe loga erro com o nome do nó e a conversa segue; um teste automático acusa o id errado antes do Play.

**Falta:**
- Texto revelado aos poucos: o primeiro clique completa a fala, o segundo avança (#11).
- O histórico da conversa em curso (#25), descrito em [Histórico de falas](#histórico-de-falas).

**Limitação conhecida:** um comando com o número errado de parâmetros (`<<dar_item>>` sem id, ou com dois) trava a conversa (#37). Para os comandos de item, o teste automático dos roteiros reprova esse erro antes do Play.

**Regras:**
- Iniciar um diálogo é um comando direto ao dono.
- O jogo sabe que um diálogo começou e acabou por `DialogueStartedMessage` e `DialogueEndedMessage`; uma conversa, por mais longa que seja, publica cada uma só uma vez.
- Durante o diálogo, o mundo não aceita cliques.
- Não se salva durante um diálogo (D-20).

**Fora do escopo:** avanço automático, pular texto já lido, voz.

## Histórico de falas

**Para o jogador:** durante uma conversa, um botão na caixa de diálogo abre a lista do que já foi dito nela. Ele relê, fecha e a conversa continua de onde estava.

**Planejada (#25, D-30):**
- Registra as falas, a opção escolhida e os itens dados ou retirados pelo roteiro durante a conversa.
- Cada fala aparece como `Nome: texto`, com o nome na cor do personagem. A narração aparece sem nome, e a opção escolhida aparece como fala do protagonista.
- O botão fica desabilitado enquanto o texto da fala está sendo revelado; funciona com a fala completa e com as opções na tela.
- Abre na entrada mais recente; a roda do mouse e a barra de rolagem levam às anteriores.

**Regras:**
- É só leitura: nada no histórico altera o estado do jogo.
- Guarda só a conversa em curso. Começa vazio a cada conversa e não entra no save.
- Com o histórico aberto, um clique não avança a fala e não escolhe opção.

**Fora do escopo:** voltar a uma fala anterior, histórico da sessão inteira, abrir na exploração ou pela pausa, coleta feita no cenário, eventos de som e de efeito visual, retrato por entrada, busca, atalho de teclado.

## Estado da história e afinidade

**Para o jogador:** o jogo lembra o que ele fez e escolheu, e reage a isso.

**Existe:**
- Um estado único (`StoryState`, D-18) com valores booleanos, numéricos e de texto por nome, salvo e restaurado.
- O roteiro lê e grava nesse estado: uma variável do roteiro é uma entrada do `StoryState`, e o contador de visitas dos nós (`visited()`) também. Portões e save enxergam o mesmo valor.
- Afinidade: uma variável numérica por personagem (`$afinidade_luna`), alterada por uma escolha e lida por condições de fala e de opção. Salvar, zerar a sessão e carregar preserva o valor.
- Um registro central, `Assets/Roteiro/variaveis.yarn`, onde toda variável é declarada uma única vez, com descrição. Um teste automático reprova declaração fora dele, declaração sem descrição e nome fora do formato.

**Regras:**
- Um nome de variável vazio ou em branco é sempre inválido: nunca é gravado e sempre lê como falso.
- O nome começa com `$` e quem monta cena usa minúsculas sem acento, dígitos e `_` (`$falou_com_luna`). Os nomes são em português (D-22).
- Um nome guarda um tipo só: gravar de outro tipo troca o valor, e ler como outro tipo não converte.

**Fora do escopo:** mostrar a afinidade ao jogador.

## Personagens

**Para o jogador:** o retrato de quem fala aparece ao lado da caixa de diálogo e muda de expressão.

**Existe (#10, D-21):**
- Cada personagem é um asset (`CharacterSO`): identificador, nome exibido, cor do nome, retratos por expressão. Os personagens ficam reunidos em um registro (`CharacterRegistry`); personagem novo é um asset e uma linha no registro, sem código.
- O roteiro cita o personagem pelo nome exibido (`Luna: ...`). A placa de nome mostra o nome na cor do personagem, e o retrato aparece à esquerda da caixa de diálogo. Em narração, em personagem desconhecido e em personagem sem retrato, o retrato some; a caixa não muda de lugar.
- O roteiro troca a expressão com uma etiqueta no fim da fala (`Luna: Sai daqui. #raiva`). Ela vale só para aquela fala; sem etiqueta, aparece a primeira expressão do asset.
- Um nome desconhecido no roteiro aparece em texto puro, sem cor e sem retrato, com aviso no console. Uma expressão que o personagem não tem mostra a expressão padrão, com aviso. Nenhum dos dois trava o jogo.
- Um teste automático reprova nome de quem fala fora do registro e etiqueta de expressão que o personagem não tem, em narração, em opção ou repetida na fala.
- O protagonista também é um personagem, com retrato.

**Limitação conhecida:** os retratos são placeholders (um quadrado colorido com o nome do arquivo), em 300×300.

**Planejada (#26):**
- O nome do protagonista é escolhido pelo jogador ao começar um jogo novo, com um nome padrão.
- O nome é uma variável de texto do estado da história: entra no save, pode ser usado pelo roteiro e aparece na placa de nome e no histórico. O roteiro continua escrevendo `Protagonista:`; a troca pelo nome escolhido acontece na hora de mostrar a placa (ver D-21).

**Fora do escopo:** sprites de corpo inteiro sobre o cenário, vários personagens na tela, animação de entrada e saída, trocar o nome do protagonista depois de o jogo começar.

## Navegação entre salas

**Para o jogador:** clicar em uma saída escurece a tela e o leva a outra sala. O que ele mudou em uma sala continua mudado quando volta.

**Existe (D-19):**
- A interface de jogo (diálogo e inventário) é persistente: é criada uma vez, junto com os managers, e continua a mesma quando a cena é recarregada ou trocada. Uma cena só com câmera, fundo e objetos interativos já tem diálogo e inventário, sem montar interface.

**Planejada (D-19):**
- Uma cena por sala, que contém só o mundo.
- Troca com fade; nenhum clique passa durante a troca (#12).
- Saídas clicáveis e pontos de entrada (#13). Não há mapa.
- Itens coletados e portões abertos persistem entre salas (#14).

## Salvar e carregar

**Para o jogador:** o jogo salva sozinho ao entrar em cada sala, e ele pode salvar pela pausa. Continuar o devolve aonde estava.

**Existe:**
- Salvar e carregar em JSON, com itens, objetos consumidos, o estado da história (booleanos, números e textos) e o nome da cena. O save tem um formato único (D-20).
- Acionado só pelo painel de debug da cena de teste.

**Falta (D-20):**
- Save automático ao entrar na sala; carregar leva à sala salva e recarrega a cena (#16).
- Salvar manual pela pausa (#18).

**Limitação conhecida, resolvida pela #16:** carregar no meio da cena restaura o estado, mas a aparência de portões e coletáveis só se acerta ao recarregar a cena.

**Fora do escopo:** vários slots, salvar durante um diálogo.

## Menu e pausa

**Para o jogador:** o jogo abre em um menu com Novo Jogo, Continuar e Sair. Esc pausa.

**Existe:**
- A cena `Menu.unity`, com janelas de título e de opções, navegação por mouse e teclado. Só os botões Opções e Voltar estão ligados a algo, e a cena está fora do Build Settings.

**Falta:**
- Novo Jogo, Continuar e Sair funcionando; menu como primeira cena (#17).
- Novo Jogo pedir o nome do protagonista (#26).
- Pausa com Continuar, Salvar e Voltar ao menu (#18).

**Fora do escopo:** tela de configurações (volume, velocidade do texto).

## Áudio

**Para o jogador:** cada sala tem música; interface e interações têm som.

**Planejada:**
- Música por sala, com troca suave (#19).
- Sons de interface e de interação; comandos de roteiro para tocar música e efeito (#20).

**Fora do escopo:** voz, controle de volume pelo jogador.
