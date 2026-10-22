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
```

M4 e M5 dependem ambas de M3 e não uma da outra. Pela regra de uma milestone por vez, M4 vem antes.

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

## Riscos conhecidos

Riscos aceitos pelo programador em 2026-10-01, a tratar conforme aparecerem:

- A prova de conceito do Yarn Spinner pode falhar em algum critério (instalação no Unity 6000.3, testes em EditMode).
- Ids de item, personagem e áudio citados em texto no roteiro só falham ao rodar; a proteção é um teste de validação por tipo de id.
- Sem integração contínua, a qualidade depende da disciplina da sessão de revisão.
- Roteiristas sem hábito de git são um ponto de atrito provável; o guia de autoria (#8) é a mitigação.
