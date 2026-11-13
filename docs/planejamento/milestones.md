# Planejamento: milestones

A sequência de trabalho até a [fatia vertical](../jogo/visao-geral.md#o-alvo-a-fatia-vertical) e o motivo da ordem.

**Este arquivo guarda a sequência e o porquê. O andamento vive só no GitHub:**

```bash
gh issue list --state open --milestone "M1 - Diálogo em Yarn Spinner"
```

## Regras

- Uma issue em andamento por vez; uma milestone só abre quando a anterior fecha.
- Dentro de uma milestone, as issues são feitas **na ordem da tabela**, que respeita as dependências. Em geral é a ordem numérica; uma issue priorizada depois (vinda da triagem) entra na posição que o programador escolher.
- Toda issue passa pelas três sessões descritas em [../agentes/fluxo-de-trabalho.md](../agentes/fluxo-de-trabalho.md).
- Uma issue nova entra com a label `triagem` e só ganha milestone quando o programador a prioriza.

## Sequência

```
M0 Fundação
 └─► M1 Diálogo em Yarn Spinner
      └─► M2 Apresentação do diálogo
           └─► M3 Navegação entre salas
                ├─► M4 Ciclo de jogo ─┐
                └─► M5 Áudio ─────────┴─► M6 Fatia vertical
                                               └─► M7 Interface nova
                                                    └─► M8 Ferramentas e dívida interna
                                                         └─► M9 Acabamento da fatia ──────────┐
                                                              └─► M10 Efeitos de roteiro      │
                                                                   └─► M11 Primeira entrega   │
                                                                                              └╌► M12 Camada de horror (portão)
```

M4 e M5 dependem ambas de M3 e não uma da outra. Pela regra de uma milestone por vez, M4 vem antes.

A M12 depende só da M9 e tem um portão: **abre quando um roteiro pedir a camada de horror** (D-27). Se o pedido chegar antes de a M11 fechar, ela entra na fila na posição que o programador escolher.

## Por que esta ordem

- **O diálogo vem primeiro (M1)** porque a migração para o Yarn Spinner muda o estado da história, o formato do save e a interface de diálogo, e as milestones seguintes constroem em cima dos três. Fazer navegação e save antes significaria refazê-los. Além disso, quando a M1 fecha, os roteiristas já podem escrever em paralelo.
- **A apresentação (M2) vem antes da navegação (M3)** porque a troca de sala depende de a interface de jogo já ser persistente (#9).
- **O histórico em balões (#55) fecha a M2,** logo depois da #25, porque mexe nos mesmos arquivos (o `ConversationLog`, o `DialogueHistoryView` e o `GameUI.prefab`) e parte da `main` com o histórico já pronto. Ela foi pedida pelo programador em 2026-10-14, durante a revisão da #25, e atrasa a abertura da M3 pelo tempo de uma issue.
- **O pensamento do protagonista (#61), a opção de ação (#65) e os personagens sem arte (#62) abrem a M3, antes do seletor de nó,** porque o primeiro roteiro de verdade (o prólogo do ato 1, "A viagem") precisa dos três para passar na verificação, e ele vai servir de exemplo para os roteiristas. Mudar o jeito de escrever o pensamento ou as opções depois obrigaria a reescrever tudo o que já tivesse sido escrito. A #65 vem logo depois da #61 porque as duas mexem no histórico e no apresentador do diálogo. O PR do roteiro fica em rascunho até as três fecharem. Priorizadas pelo programador em 2026-10-15 e 2026-10-16; atrasam a #32 pelo tempo de três issues pequenas.
- **O seletor de nó (#32) vem logo depois, ainda no começo da M3,** porque é nela que as salas se multiplicam, cada uma com os seus gatilhos de diálogo. O seletor e o teste que confere os nós citados em cenas e prefabs precisam existir antes disso, e esse teste também serve à #14 para conferir os ids persistentes. Ela veio da triagem e foi priorizada pelo programador em 2026-10-14.
- **A janela sem foco (#79), os testes com FPS baixo e alto (#80) e o limite de FPS em segundo plano (#81) abrem a M4, antes do save,** nessa ordem. A #79 vem primeiro porque, com o jogo rodando sem foco, o Play Mode dirigido pelo CLI deixa de congelar, e isso simplifica a verificação de todas as issues seguintes. A #80 vem antes da #81 porque o limite põe o jogo de propósito em FPS baixo, e o teste que mostra o que quebra com FPS baixo precisa existir antes. Priorizadas pelo programador em 2026-10-20, durante a revisão da #15; atrasam a #16 pelo tempo de três issues pequenas.
- **A seleção inicial do menu (#88) vem antes do menu principal (#17).** Ao abrir `Menu.unity`, nenhum botão fica selecionado, porque o `Start` da interface de jogo limpa a seleção que o menu fez, e com a #17 o menu passa a ser a primeira cena do jogo: quem joga com teclado ou controle abriria o jogo sem conseguir navegar. Ela é de outro módulo (`Dialogue`) e cabe num PR pequeno, e feita antes deixa a verificação da #17 conferir o menu já selecionado. Encontrada no levantamento da #17 e priorizada pelo programador em 2026-10-22; atrasa a #17 pelo tempo de uma issue pequena.
- **O ciclo de jogo (M4) vem depois da navegação** porque menu, continuar e save não têm o que carregar sem troca de sala.
- **O áudio (M5) vem por último entre os sistemas** porque depende de sala (música por sala) e de roteiro (comandos), e nada depende dele.
- **A interface nova (M7) vem depois da fatia vertical** porque troca a arte provisória da interface pela entrega da arte e não muda nenhum sistema. A posição e a organização da interface seguem a arte. A #113 vem primeiro porque leva a entrega para o repositório (ela está em `Adicionar/`, fora do git) e cria as fontes que as outras usam. A #114 vem antes da #115 porque o componente que muda a cor do texto por estado nasce nos botões de escolha e o histórico o reaproveita. O menu (#116) fecha a milestone. A camada de horror (#117) fica no backlog até um roteiro pedir (D-27). Priorizadas pelo programador em 2026-11-09.
- **Depois da interface nova, o backlog ficou só com achados das revisões.** Eles foram conferidos contra a `main` na triagem de 2026-11-13: os que tinham ficado datados foram fechados, e os pequenos foram juntados na #124 (só documentação) e na #125 (acabamentos de código e README). As 17 issues que sobraram se dividem da M8 à M12, pela ordem abaixo.
- **As ferramentas e a dívida interna (M8) vêm primeiro** porque custam tempo a cada sessão e nada do que vem depois funciona melhor com elas quebradas. Nenhuma muda o que o jogador vê.
  - **A #124 abre a milestone** porque corrige receitas que todas as sessões seguintes usam (o Esc da pausa, o kerning que a build grava, o mouse na build por script). **A #35 vem logo depois** pelo mesmo motivo: ela muda os próprios comandos de trabalho.
  - **A #36 e a #125** são pequenas e sem decisão pendente. A #125 vem depois da #124 porque as duas mexem no README do `Dialogue` (seções diferentes).
  - **A #105, a #37 e a #34 protegem quem produz conteúdo:** sala com nome repetido, comando com o número errado de parâmetros e `.yarn` novo que não entra no jogo. A #37 precisa fechar antes da #71 (M10), que cria comandos novos de roteiro: se ela escolher o teste genérico de parâmetros, os comandos da #71 já nascem protegidos.
  - **A #31 fecha a milestone** porque é só uma decisão (vale ou não a D-24 para código chamado de `OnValidate`) e o resultado, um teste ou uma linha em `skills.md`.
- **O acabamento da fatia (M9) vem em seguida** porque é o que o jogador e quem monta sala veem de errado hoje.
  - **A #108 abre a milestone:** é o defeito mais visível (o pan do posto mostra o vazio preto) e corrige os limites que o passo a passo de `salas.md` manda copiar.
  - **A #96 e a #56** mexem no menu, na pausa e na placa de nome. A #56 depende de uma decisão do programador (nome com ou sem cor), que o levantamento pergunta.
  - **A #52 vem depois da #108 e da #96:** ela valida `minX` < `maxX` no mesmo componente que a #108 ajusta, e a varredura de tooltips cobre também o que a #96 acrescentar.
  - **A #107 fecha a milestone** porque percorre o guia de `salas.md` no Inspector, e o percurso só vale com os limites certos (#108) e com os tooltips no lugar (#52).
- **Os efeitos de roteiro (M10) vêm antes da entrega** porque o prólogo é a abertura do jogo: sem fade nem imagem de tela cheia, a lembrança dos pais aparece por cima do ônibus, sem corte. A #71 depende da #37 (M8). O levantamento dela decide se ela se divide (fade, cenário de tela cheia e abertura automática da conversa ao chegar na sala), e as partes novas entram nesta milestone.
- **A primeira entrega (M11) vem por último** porque só faz sentido entregar a fatia a alguém de fora depois de ela estar limpa (M9) e com a abertura completa (M10). A #89 vem antes da #109: com o jogo nas mãos de outra pessoa, o save passa a ter progresso a perder. A #89 parte do `GameSaveManager` já com o reset da #36; a #109 parte da receita de build corrigida na #124 e do pan corrigido na #108.
- **A camada de horror (M12) tem portão,** como a M1 teve: ela abre só quando um roteiro pedir (D-27). Depende da M9, porque a placa de nome tem variantes de horror e a #56 decide a cor do nome na placa normal.

## M0 — Fundação

Documentação, guia do agente, modelos e limpeza. Sem mudança de comportamento do jogo.

| Issue | Título | Depende de |
|---|---|---|
| #1 | Documentação por módulo e guia do agente | — |
| #2 | Limpeza: cena CameraPan, comentários e configurações obsoletas | #1 |

## M1 — Diálogo em Yarn Spinner

| Issue | Título | Depende de |
|---|---|---|
| #3 | Prova de conceito do Yarn Spinner (portão de decisão) | #1 |
| #4 | Estado da história único no Core (`StoryState`) | #3 |
| #5 | Migrar o módulo Dialogue para o Yarn Spinner | #3, #4 |
| #6 | Comandos e funções de roteiro para o inventário | #5 |
| #7 | Condições e afinidade no roteiro | #4, #5 |
| #8 | Guia de autoria de roteiro e ambiente dos roteiristas | #6, #7 |

**Portão:** a #3 termina com um veredito. Se for "não adotar", as issues #5 a #8, #10, #11, #20, #25 e #26 (com a label `depende-da-prova-yarn`) são reescritas para o plano B da decisão D-17 antes de qualquer execução. A #4 vale nos dois casos.

## M2 — Apresentação do diálogo

| Issue | Título | Depende de |
|---|---|---|
| #9 | Interface de jogo em prefab persistente | #5 |
| #10 | Personagem como dado e retrato no diálogo | #5, #9 |
| #48 | Sprites da conversa: dois lugares na tela, com destaque para quem fala | #10, #47 |
| #11 | Typewriter com completar e avançar | #5, #9 |
| #25 | Histórico de falas da conversa em curso | #5, #6, #9, #10, #11 |
| #55 | Histórico de falas em balões, com lados por quem fala | #10, #25 |

## M3 — Navegação entre salas

| Issue | Título | Depende de |
|---|---|---|
| #61 | Pensamento do protagonista: nome reservado no roteiro, itálico e cor na caixa, lado dele no histórico | #10, #48, #55 |
| #65 | Opção de ação no roteiro: etiqueta #acao, botão em itálico, narração no histórico | #61 |
| #62 | Personagens sem arte do prólogo: Mãe, Pai, Motorista, Homem e Mulher | #10, #48 |
| #32 | Seletor de nó do roteiro no InteractableDialogueTrigger | #5 |
| #12 | Troca de sala com fade | #9 |
| #13 | Saídas e pontos de entrada das salas | #12 |
| #14 | Persistência dos objetos de mundo entre salas | #12 |
| #15 | Suíte de testes PlayMode para troca de sala | #12, #13, #14 |

## M4 — Ciclo de jogo

| Issue | Título | Depende de |
|---|---|---|
| #79 | Jogo roda com a janela sem foco (runInBackground: 1) | — |
| #80 | Testes do jogo com FPS baixo e alto | #15 |
| #81 | Limitar o FPS com a janela sem foco | #79, #80 |
| #16 | Save automático por sala e carregamento que recarrega a cena | #12, #14 |
| #88 | Menu abre sem botão selecionado: o Start do apresentador de diálogo apaga a seleção | — |
| #17 | Menu principal ligado ao jogo | #16, #88 |
| #18 | Pausa com salvar manual e voltar ao menu | #16, #17 |
| #26 | Nome do protagonista definido pelo jogador | #4, #10, #16, #17, #25 |

## M5 — Áudio

| Issue | Título | Depende de |
|---|---|---|
| #19 | Base de áudio e música por sala | #12 |
| #20 | Efeitos sonoros e comandos de áudio no roteiro | #5, #19 |

## M6 — Fatia vertical

| Issue | Título | Depende de |
|---|---|---|
| #21 | Conteúdo provisório da fatia vertical | #8, #11, #13, #14, #18, #20, #25, #26 |
| #22 | Verificação de ponta a ponta e build de desktop | #21 |

## M7 — Interface nova

| Issue | Título | Depende de |
|---|---|---|
| #113 | Entrega da arte no repositório, fontes Atkinson e Cinzel, e caixa de diálogo no tamanho da referência | — |
| #114 | Botões de escolha com a arte nova | #113 |
| #115 | Botão Histórico e painel do histórico com a arte nova | #113, #114 |
| #116 | Menu principal com a arte nova | #113 |

## M8 — Ferramentas e dívida interna

O que custa tempo a cada sessão e o que protege quem produz conteúdo. Nada muda para o jogador.

| Issue | Título | Depende de |
|---|---|---|
| #124 | Documentação dos agentes: armadilhas pendentes do unity-cli.md e do roteiro de ponta a ponta | — |
| #35 | Comandos de trabalho trocam de branch com o Editor aberto, contra a armadilha dos pacotes | — |
| #36 | Managers com Instance estático não seguem a D-29 (reset em SubsystemRegistration) | — |
| #125 | Acabamentos das revisões: mensagens, tooltip, README do Dialogue e struct repetido no Editor | #124 |
| #105 | Testes de sala quebram sem mensagem com duas cenas de mesmo nome no build | — |
| #37 | Comando de roteiro com número errado de parâmetros trava a conversa | — |
| #34 | Um .yarn novo não entra no Roteiro.yarnproject sem reimportar o projeto | — |
| #31 | Decidir se StoryVariableNameCheck precisa de teste (D-24) | — |

## M9 — Acabamento da fatia

O que o jogador e quem monta sala veem de errado na fatia vertical. Nenhuma mecânica nova.

| Issue | Título | Depende de |
|---|---|---|
| #108 | O pan da [Teste] Mecanicas e da [Fatia] Posto passa do fundo, e o guia não diz como calcular os limites | — |
| #96 | Navegação por teclado pousa em botão desabilitado, sem destaque (menu principal e pausa) | — |
| #56 | Nome colorido na placa de nome é difícil de ler | — |
| #52 | Campos do Inspector sem tooltip (regra 8) | #108, #96 |
| #107 | Percorrer no Inspector o passo a passo de salas.md da fatia vertical | #108, #52 |

## M10 — Efeitos de roteiro

Os efeitos de tela que o prólogo pede. As partes em que o levantamento da #71 a dividir entram aqui.

| Issue | Título | Depende de |
|---|---|---|
| #71 | Fade e cenário de tela cheia comandados pelo roteiro (pedidos do prólogo) | #37 |

## M11 — Primeira entrega

A fatia vertical pronta para ir a alguém de fora, por download no itch.io (D-15).

| Issue | Título | Depende de |
|---|---|---|
| #89 | Gravar o save sem risco de deixar o arquivo pela metade | #36 |
| #109 | Preparar a build de Windows para entrega: identificador, cenas de teste e pasta DoNotShip | #124, #108 |

## M12 — Camada de horror

**Portão:** abre só quando um roteiro pedir (D-27). O guia da camada está em `docs/arte/interface/camada-de-horror.md`, e os arquivos de arte ficam em `Adicionar/horror/`, fora do git, até lá. A #117 lista o que decidir e sugere dividi-la quando for priorizada.

| Issue | Título | Depende de |
|---|---|---|
| #117 | Camada de horror da interface (quando o roteiro pedir) | #56 |

## Riscos conhecidos

Riscos aceitos pelo programador em 2026-10-01, a tratar conforme aparecerem:

- A prova de conceito do Yarn Spinner pode falhar em algum critério (instalação no Unity 6000.3, testes em EditMode).
- Ids de item, personagem e áudio citados em texto no roteiro só falham ao rodar; a proteção é um teste de validação por tipo de id.
- Sem integração contínua, a qualidade depende da disciplina da sessão de revisão.
- Roteiristas sem hábito de git são um ponto de atrito provável; o guia de autoria (#8) é a mitigação.
