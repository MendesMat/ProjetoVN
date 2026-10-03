# Fluxo de trabalho do agente

Como um agente de IA trabalha neste repositório, em par com o programador. Vale para qualquer sessão, de qualquer fase.

## O princípio

**Tudo o que um agente precisa saber está escrito no repositório ou na issue.** Cada sessão começa com contexto zerado. O que uma sessão descobre e não escreve está perdido para a próxima.

## As três sessões de uma issue

Cada issue passa por três sessões separadas, cada uma em uma conversa nova.

| Fase | Comando | Entra com a label | Sai com a label | Produz |
|---|---|---|---|---|
| 1. Levantamento | `/levantar-issue N` | `estado:levantamento` | `estado:pronta` ou `estado:aguardando-resposta` | Um comentário na issue com tudo o que a execução precisa |
| 2. Execução | `/executar-issue N` | `estado:pronta` ou `estado:mudancas-pedidas` | `estado:em-revisao` | Uma branch e um PR |
| 3. Revisão | `/revisar-issue N` | `estado:em-revisao` | `estado:aprovada` ou `estado:mudancas-pedidas` | Um parecer no PR |

```
levantamento ──► pronta ──► em-execucao ──► em-revisao ──► aprovada ──► (programador faz o merge)
     │              ▲                            │
     ▼              │                            ▼
aguardando-resposta ┘                    mudancas-pedidas ──► (nova execução)
```

**Uma sessão recusa começar se a issue não estiver na label de entrada dela.** Ela diz em que estado a issue está e qual é o comando certo. A única pessoa que pode mandar pular uma fase é o programador, e a sessão registra isso em um comentário na issue.

### 1. Levantamento

Objetivo: que a sessão de execução não precise descobrir nada.

O agente lê a issue, a documentação que ela cita e o código envolvido, e publica **um comentário** na issue com estas seções:

- **Arquivos envolvidos:** o que será criado, alterado e removido, com caminho.
- **Decisões aplicáveis:** quais `D-xx` de [decisoes.md](../arquitetura/decisoes.md) limitam a solução, e como.
- **Abordagem:** a solução proposta em poucas linhas, e a alternativa descartada com o motivo.
- **Plano de testes:** os testes a escrever primeiro (para C# puro) e o que cada um prova.
- **Roteiro de verificação em Play Mode:** passos concretos e o resultado esperado de cada um.
- **Skills a usar:** conforme [skills.md](skills.md).
- **Documentação a atualizar:** quais arquivos mudam com esta issue.
- **Perguntas em aberto:** só decisões que são do programador. Fatos, o agente levanta sozinho.

Regras:
- O levantamento **não altera código**. Se a documentação estiver errada ou incompleta, ele a corrige em uma branch `docs/issue-N-levantamento` e abre um PR só de documentação.
- Se houver perguntas em aberto, a issue vai para `estado:aguardando-resposta`. O programador responde na issue e troca a label para `estado:pronta`.
- Se os critérios de aceite da issue estiverem errados ou incompletos, o levantamento propõe a correção no comentário; quem edita o corpo da issue é o programador, ou o agente com o aval dele.

### 2. Execução

Objetivo: cumprir os critérios de aceite, nada além.

1. Conferir a label e ler a issue **inteira, com todos os comentários**.
2. Trocar a label para `estado:em-execucao`.
3. Criar a branch `issue-N-resumo-curto` a partir de `main` atualizada.
4. Implementar seguindo o comentário de levantamento, com as skills de [skills.md](skills.md).
5. Rodar os testes e o roteiro de verificação em Play Mode ([unity-cli.md](unity-cli.md)).
6. Atualizar a documentação listada no levantamento.
7. Abrir o PR com o modelo do repositório, citando `Closes #N`, e trocar a label para `estado:em-revisao`.

Regras:
- **Se a issue estiver errada, parar.** Quando a execução descobre que o plano não funciona ou que um critério não faz sentido, ela comenta na issue o que encontrou, devolve a label para `estado:aguardando-resposta` e encerra. Ela não improvisa uma solução diferente.
- **Achado fora do escopo vira issue**, com a label `triagem`. Não é corrigido junto.
- Numa reexecução (`estado:mudancas-pedidas`), o agente lê o parecer da revisão no PR, corrige na mesma branch e devolve para `estado:em-revisao`.

### 3. Revisão

Objetivo: um segundo par de olhos independente. **A revisão só relata; ela não altera código.**

O agente lê a issue, os comentários, a documentação citada e o diff do PR, e confere:

1. Cada critério de aceite foi cumprido? (um por um, com evidência)
2. O código segue o levantamento? Se desviou, o desvio está explicado no PR?
3. Alguma decisão `D-xx` foi violada?
4. Os testes passam? (rodar de verdade e colar o resultado)
5. O roteiro de verificação em Play Mode passa? (executar de verdade)
6. A documentação foi atualizada?
7. Há algo no diff que a issue não pediu?

O parecer vai como comentário no PR, com veredito **aprovado** ou **mudanças pedidas**, e os achados em ordem de gravidade, cada um com arquivo e linha. A label da issue é trocada conforme o veredito. O merge é do programador.

## PR de roteiro

Quem escreve roteiro entrega por um fluxo à parte, descrito para o roteirista em [roteiro.md](../autoria/roteiro.md). Um agente que encontrar um PR assim deve saber que:

- Ele vem de um roteirista (colaborador do repositório), em uma branch `roteiro/<nome>-<assunto>`, **sem issue e sem as três sessões**. O título do PR traz ato, capítulo, episódio e o título do episódio (`Ato 1, Cap. 1, Ep. 2: A chave perdida`); o texto lista os arquivos, os nós de entrada, onde a conversa entra na história, as variáveis novas e os pedidos.
- A regra "uma issue por vez" **não conta** PR de roteiro: ele não disputa lugar com a issue em andamento.
- Quem integra é o programador: gera os `.meta`, reimporta o `Roteiro.yarnproject`, roda os testes e abre a conversa no jogo (receita em [unity-cli.md](unity-cli.md#receber-um-roteiro-pr-de-roteirista)), e faz o merge por *squash* com a branch apagada. O agente só faz o que o programador pedir.
- O PR de roteiro mexe só em `Assets/Roteiro/` (e nos `.meta`), em pastas `ato_N/capitulo_N/episodio_N/`, um arquivo por conversa, cada um começando pela linha de título descrita no guia (`// ATO 1 · CAPÍTULO 1 · EPISÓDIO 2 · A chave perdida`). Se tocar em qualquer outra coisa, não é PR de roteiro: devolva ao roteirista.
- Um pedido de comando, item, personagem ou som que o roteirista deixou em `// PEDIDO` vira issue (com a label `triagem`), não é feito no PR de roteiro.

## Regras de foco

O projeto tem histórico de frentes abertas e não terminadas. Estas regras existem para isso, e o agente as aplica também quando o pedido vem do programador:

1. **Uma issue em andamento por vez.** A próxima só começa com o PR da anterior mesclado.
2. **Uma milestone só abre quando a anterior fecha.** A ordem está em [../planejamento/milestones.md](../planejamento/milestones.md).
3. **Ideia nova vira issue no backlog,** com a label `triagem`, e não trabalho imediato.
4. **Pronto significa:** testes passando, verificação em Play Mode feita, README do módulo atualizado e PR mesclado.

Quando o programador pedir algo fora da issue atual, o agente lembra a regra, oferece registrar o pedido como issue e segue o que ele decidir.

## Autonomia

O critério é: livre o que é reversível e fica dentro da branch; perguntar o que afeta o projeto inteiro.

| Ação | Regra |
|---|---|
| Criar branch, commitar, dar push e abrir PR | Livre |
| Comentar na issue e no PR; trocar labels de estado | Livre |
| Editar cenas e prefabs pelo Unity CLI | Livre, dentro do escopo da issue |
| Rodar testes e entrar em Play Mode | Livre |
| Criar issue para achado fora de escopo | Livre, com a label `triagem` |
| Instalar ou remover pacotes | Perguntar antes |
| Alterar `ProjectSettings/` | Perguntar antes |
| Acrescentar referência a um asmdef | Perguntar antes |
| Mudar uma decisão `D-xx` | Perguntar antes, e registrar em `decisoes.md` |
| Apagar assets de conteúdo (roteiro, itens, salas, arte) | Perguntar antes |
| Fazer merge de PR | **Nunca por iniciativa própria.** A decisão é do programador; o agente só mescla com ordem explícita dele na conversa, para aquele PR |
| `git push --force`, reescrever histórico de `main` | **Nunca** |

Uma issue pode conceder uma autorização específica no próprio texto (a #3 autoriza instalar o Yarn Spinner). Essa autorização vale só para aquela issue.

## Git

- **Branch:** `issue-<número>-<resumo-em-minúsculas>`, criada a partir de `main` atualizada. Exemplo: `issue-4-story-state`.
- **Commits:** em português, no imperativo, com o número da issue no início. Exemplo: `#4: StoryState guarda número e texto`. Commits pequenos, um assunto cada.
- **PR:** um por issue, com o modelo de `.github/PULL_REQUEST_TEMPLATE.md`. O corpo cita `Closes #N`.
- **Merge:** sempre **squash and merge**, com a branch apagada em seguida. Cada issue vira um único commit em `main`, com o título `#N: <título da issue> (#<número do PR>)`. Os commits intermediários (inclusive os da fase vermelha da TDD) ficam só no PR.
- **Arquivos do Unity:** todo arquivo novo dentro de `Assets/` tem um `.meta` que precisa ser commitado junto. Antes de commitar, confira `git status` por `.meta` sem par e por arquivos que o Editor alterou sem relação com a issue (esses não entram no commit).
- **Cenas:** arquivos `.unity` não se mesclam bem. Trabalhar uma issue por vez é o que evita o conflito.

## Documentação é parte da entrega

- Mudou o comportamento de um módulo: o README dele muda no mesmo PR.
- Mudou a arquitetura ou onde mora um estado: [visao-geral.md](../arquitetura/visao-geral.md) muda no mesmo PR.
- Uma mecânica mudou de estado (planejada, parcial, existe): [mecanicas.md](../jogo/mecanicas.md) muda no mesmo PR.
- Descobriu uma armadilha do Unity ou do CLI que custou tempo: ela entra em [unity-cli.md](unity-cli.md).
- O andamento das issues **não** é escrito em documento nenhum. Ele vive só no GitHub.

A documentação descreve o que está em `main`. O que ainda não existe é marcado como planejado, com o número da issue.
