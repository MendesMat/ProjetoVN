# Planejamento: milestones

A sequência de trabalho até a [fatia vertical](../jogo/visao-geral.md#o-alvo-a-fatia-vertical) e o motivo da ordem.

**Este arquivo guarda a sequência e o porquê. O andamento vive só no GitHub:**

```bash
gh issue list --state open --milestone "M1 - Diálogo em Yarn Spinner"
```

## Regras

- Uma issue em andamento por vez; uma milestone só abre quando a anterior fecha.
- Dentro de uma milestone, as issues são feitas **na ordem numérica**, que já respeita as dependências.
- Toda issue passa pelas três sessões descritas em [../agentes/fluxo-de-trabalho.md](../agentes/fluxo-de-trabalho.md).
- Uma issue nova entra com a label `triagem` e só ganha milestone quando o Matheus a prioriza.

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
| #11 | Typewriter com completar e avançar | #5, #9 |
| #25 | Histórico de falas da conversa em curso | #5, #6, #9, #10, #11 |

## M3 — Navegação entre salas

| Issue | Título | Depende de |
|---|---|---|
| #12 | Troca de sala com fade | #9 |
| #13 | Saídas e pontos de entrada das salas | #12 |
| #14 | Persistência dos objetos de mundo entre salas | #12 |
| #15 | Suíte de testes PlayMode para troca de sala | #12, #13, #14 |

## M4 — Ciclo de jogo

| Issue | Título | Depende de |
|---|---|---|
| #16 | Save automático por sala e carregamento que recarrega a cena | #12, #14 |
| #17 | Menu principal ligado ao jogo | #16 |
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

Riscos aceitos pelo Matheus em 2026-10-01, a tratar conforme aparecerem:

- A prova de conceito do Yarn Spinner pode falhar em algum critério (instalação no Unity 6000.3, testes em EditMode).
- Ids de item, personagem e áudio citados em texto no roteiro só falham ao rodar; a proteção é um teste de validação por tipo de id.
- Sem integração contínua, a qualidade depende da disciplina da sessão de revisão.
- Roteiristas sem hábito de git são um ponto de atrito provável; o guia de autoria (#8) é a mitigação.
