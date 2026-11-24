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
                                                         └─► M9 Acabamento da fatia ──────────────┐
                                                              └─► M10 Efeitos de roteiro          │
                                                                   └─► M11 Prólogo do ato 1       │
                                                                        └─► M12 Primeira entrega  │
                                                                                                  └╌► M13 Camada de horror (portão)

                                                                               M14 Arte pendente (portão, sem dependência)
                                                                               M15 Comandos de roteiro com lista (portão, sem dependência)
                                                                               M16 Roteiro pendente (portão, sem dependência)
```

**A M8 foi reaberta três vezes em 2026-11-17,** para a #175, para a #187 e para a #189, e fechou de novo antes de a M11 terminar. **Em 2026-11-18 ela foi reaberta pela quarta vez,** para a #197, e pela quinta, para a #204, depois de a M12 já ter começado (a #198, a #196 e a #89 estão feitas). A #204 foi feita antes da #157, e a M8 ganhou ainda a #208, que vem da revisão da #204 e é feita antes do que falta da M12 (a #109). **Em 2026-11-23, com a M12 já toda feita,** a M8 ganhou a #214, uma limpeza do repositório, e em 2026-11-24 a #217, que continua essa limpeza. No mesmo dia ela ganhou a #222, que vem da revisão da #213 e é a próxima issue depois dela.

M4 e M5 dependem ambas de M3 e não uma da outra. Pela regra de uma milestone por vez, M4 vem antes.

A M13 depende só da M9 e tem um portão: **abre quando um roteiro pedir a camada de horror** (D-27). Se o pedido chegar antes de a M12 fechar, ela entra na fila na posição que o programador escolher.

A M14 não depende de nenhuma milestone e também tem portão: **abre quando a arte entregar o que as issues dela pedem.** Ela guarda o que espera arte e não exige código; quando a entrega chegar, entra na fila na posição que o programador escolher.

A M15 também não depende de nenhuma milestone aberta e tem portão de roteiro: **abre quando um roteiro pedir um comando que receba uma lista de números ou de booleanos** (um `params` de tipo diferente de `string`), pela D-27. Quando o pedido chegar, ela entra na fila antes da issue que criar esse comando.

A M16 também não depende de nenhuma milestone aberta e tem portão de roteiro, de outro tipo: **abre quando o roteirista entregar o texto que as issues dela pedem.** É a M14 do roteiro: guarda o que espera uma entrega de fora e não exige código. Quando o texto chegar, entra na fila na posição que o programador escolher.

A coluna "Depende de" das tabelas também está registrada no GitHub, como "bloqueada por" entre as issues abertas: a página de cada issue mostra o que precisa fechar antes dela.

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
- **Depois da interface nova, o backlog ficou só com achados das revisões.** Eles foram conferidos contra a `main` na triagem de 2026-11-13: os que tinham ficado datados foram fechados, e os pequenos foram juntados na #124 (só documentação) e na #125 (acabamentos de código e README). As 17 issues que sobraram se dividem entre a M8, a M9, a M10, a M12 e a M13, pela ordem abaixo. Priorizadas pelo programador em 2026-11-13.
- **As ferramentas e a dívida interna (M8) vêm primeiro** porque custam tempo a cada sessão e nada do que vem depois funciona melhor com elas quebradas. Nenhuma muda o que o jogador vê.
  - **A #124 abre a milestone** porque corrige receitas que todas as sessões seguintes usam (o Esc da pausa, o kerning que a build grava, o mouse na build por script). **A #35 vem logo depois** pelo mesmo motivo: ela muda os próprios comandos de trabalho.
  - **A #36 e a #125** são pequenas e sem decisão pendente. A #125 vem depois da #124 porque as duas mexem no README do `Dialogue` (seções diferentes).
  - **A #105, a #37 e a #34 protegem quem produz conteúdo:** sala com nome repetido, comando com o número errado de parâmetros e `.yarn` novo que não entra no jogo. A #37 precisa fechar antes da #71 (M10), que cria comandos novos de roteiro: se ela escolher o teste genérico de parâmetros, os comandos da #71 já nascem protegidos.
  - **A #31** é só uma decisão (vale ou não a D-24 para código chamado de `OnValidate`) e o resultado, um teste ou uma linha em `skills.md`.
  - **A #140** vem depois das que só mexem em documentação e testes, e antes da M9. Ela decide se o Editor pula a recarga de domínio ao entrar no Play, para a D-29 valer na prática. A revisão da #36 mediu que hoje o domínio é recarregado a cada Play, então nenhum dos resets em `SubsystemRegistration` roda de verdade. É ferramenta que custa tempo a cada sessão (o Play e a suíte PlayMode), e muda como toda verificação em Play Mode se comporta: como a #79 na M4, é melhor mudar isso antes das milestones de conteúdo, e um estado estático esquecido aparece antes de a fatia crescer. Priorizada pelo programador em 2026-11-15.
  - **A #144, a #146, a #134 e a #142 vêm depois da #140.** São os achados das sessões da #31, da #37 e da #105, que estavam no backlog com `triagem`; o programador pediu, em 2026-11-15, que nenhuma issue ficasse sem milestone. Elas entram na M8, e não na M9, porque são do mesmo tipo da #105 e da #37: testes e proteções para quem produz conteúdo, sem nada que o jogador veja. A #135 foi fechada como duplicada da #144.
    - **A #144 vem primeiro:** é só uma frase do `Tests/README.md`, e as três seguintes acrescentam testes ao mesmo README.
    - **A #146 vem antes da #134:** ela decide se a leitura de campo por `SerializedObject` que o `RoomLinksTests` e o `MainMenuTests` copiam vira um auxiliar comum, e a #134 é um teste novo de conteúdo que lê campos do mesmo jeito. Feita antes, a #134 já nasce usando o auxiliar, em vez de criar a terceira cópia.
    - **A #134 precisa fechar antes da #128 (M11),** que cria um componente novo com campo de variável de história. Se o teste conferir todo campo `[StoryFlag]`, e não só o do `LockedActionBehaviour`, o componente da #128 já nasce protegido contra nome de variável errado.
    - **A #142 precisa fechar antes da #71 (M10),** pelo mesmo motivo da #37: o fade pelo roteiro pode criar o primeiro comando com parâmetro numérico (a duração), e é aí que um parâmetro do tipo errado passa a travar a conversa.
  - **A #163, a #166 e a #162 vêm em seguida.** Vieram das sessões da #107 e da #52 e foram priorizadas pelo programador na triagem de 2026-11-16, quando a M8 e a M9 já não tinham nada aberto.
    - **A #163 vem primeiro** porque corrige uma premissa que toda sessão lê: `salas.md` e `CLAUDE.md` dizem que uma pessoa monta as salas no Inspector, e quem monta é o agente, pelo CLI. Ela esperava a #107, que reescreveu o passo a passo de `salas.md`. Feita agora, o levantamento da #130 (M11), que monta o pátio e a loja, já parte da premissa certa.
    - **A #166 vem logo depois,** pelo mesmo motivo: também é sobre a montagem de sala pelo agente. O `NewScene`, primeiro passo da montagem em `salas.md`, fecha a cena aberta sem perguntar, e `unity-cli.md` não avisa nem diz o que acontece com uma cena com alteração não salva (a alteração se perde ou abre um diálogo que trava o Editor). Ela mede o caso e escreve a receita antes de a #130 montar duas salas. Mexe em `unity-cli.md`, e a #163 em `salas.md` e `CLAUDE.md`, então não disputam arquivo.
    - **A #162** fecha os dois furos do teste de guarda da regra 8 (classe `[Serializable]` de outro módulo e a base dela). É do mesmo tipo da #105 e da #37, e a M10 e a M11 criam componentes novos que o teste deve cobrir por inteiro.
  - **A #171 fecha a milestone,** pela triagem pedida pelo programador em 2026-11-16, depois do PR #172. Veio da revisão da #166: a receita da suíte em `unity-cli.md` confere só a cena ativa antes do `run_tests`, enquanto a receita de "Abrir e salvar cena", escrita pela #166, confere todas as cenas abertas. A receita da suíte é seguida em toda issue, e um diálogo do Test Runner trava o Editor dirigido pelo CLI. É só documentação, com uma medição antes, e não disputa arquivo com nada aberto.
  - **A #175 reabre a milestone,** pela reorganização de 2026-11-17, em que o programador pediu de novo que nenhuma issue ficasse sem milestone. Veio da revisão da #171: a conferência das cenas abertas antes da suíte está só em `unity-cli.md`, e as portas de entrada que mandam rodar o `run_tests` (`CLAUDE.md` e as skills `executar-issue` e `revisar-issue`) não falam dela. A suíte roda duas vezes por sessão, e um diálogo do Test Runner obriga a reiniciar o Unity: é o tipo de ferramenta que custa tempo a cada sessão, por isso entra na M8 e vem antes do resto da M11. O levantamento começa por uma decisão do programador: pôr a conferência nas três portas de entrada ou só apontar para `unity-cli.md`, como hoje.
  - **A #187 reabre a milestone de novo,** pela reorganização pedida pelo programador em 2026-11-17, depois do PR #185. Veio da revisão da #130: trocar de branch com uma cena aberta que muda entre as duas pontas abre o diálogo "reload the scene(s)" do Editor, que trava o CLI até o programador clicar, e a receita "Antes de trocar de branch" de `unity-cli.md` só confere `Packages/`. Toda revisão termina trocando de branch e toda issue que monta sala muda uma cena, então é o mesmo tipo da #175: custa uma ida e volta com o programador a cada vez que acontece. Vem antes da #186 (M11), cuja revisão também troca de branch. As duas mexem em `unity-cli.md`, em seções diferentes ("Antes de trocar de branch" e "Jogar a build por script"), e a ordem das milestones já as põe uma depois da outra.
  - **A #189 fecha a milestone,** pela reorganização pedida pelo programador em 2026-11-17, depois do PR #190. Veio do levantamento da #187: o projeto usava o `com.unity.pipeline` 0.7.0-exp.1, e o Unity CLI documenta comandos que leem e respondem a um diálogo modal do Editor (`editor dialog`, saída `18`, `AWAITING_INPUT`). As três travas recentes (#171, #175 e #187) foram um diálogo que o agente não vê, e cada uma custa uma ida e volta com o programador ou um reinício do Unity: é o mesmo tipo da #175 e da #187. O levantamento da própria #189 mostrou que a premissa não se sustenta: os comandos de diálogo dependem de uma API do Unity que só existe a partir do 6000.7 (hoje em beta), e o projeto está no 6000.3.9f1. O programador decidiu atualizar mesmo assim, pelos ganhos que valem no 6000.3: o pacote vai para a 1.0.0 (servidor só em `127.0.0.1`, testes com o domínio sem recarga, `recompile` confiável, captura fora de `Assets/`, fim da versão `exp` que o registro não lista mais), todas as receitas de `unity-cli.md` e dos comandos de trabalho passam para os nomes novos (`script eval`, `test run`, `scene list_open`…), e `unity-cli.md` registra que ver e responder diálogos exige o 6000.7+. Vem antes da #191 e da #186 (M11) para que o resto do prólogo e a M12 já corram com o pacote novo, e as receitas de `unity-cli.md` que a #186 segue já estejam nos nomes novos. Atualizar o pacote mexe em `Packages/` e pediu o aval do programador (regra 10); depois do merge, toda troca entre uma branch anterior e a `main` passa pela receita dos pacotes ("Antes de trocar de branch").
  - **A #197 reabre a milestone pela quarta vez,** pela triagem pedida pelo programador em 2026-11-18, depois do PR #199. Veio da linha de base da #186: a suíte PlayMode passou 57/58, com o `SoundTests.AdvancingWithOptionsOnScreen_DoesNotSound` contando um som a mais, e passou 58/58 logo depois, sem nada mudar. A suíte roda duas vezes por sessão, e uma falha que some sozinha obriga a rodar de novo e deixa a revisão sem saber se a mudança quebrou algo: é ferramenta que custa tempo a cada sessão, o mesmo tipo da #175, da #187 e da #189. Vem antes da M12, para que as quatro issues dela já sejam verificadas com uma suíte confiável.
  - **A #204 reabre a milestone pela quinta vez,** pela triagem pedida pelo programador em 2026-11-18, depois do PR #205. Veio da revisão da #196 (PR #203): a receita de conferir um contorno pelos pixels, em `unity-cli.md`, manda trocar um objeto que fica fora da tela com o pan desligado por outro que esteja dentro, e foi assim que o PR #203 deixou o `Motorista` sem verificar. A issue acrescenta uma frase: com o pan desligado, a câmera vai até o `x` do objeto, só em runtime, e a câmera movida na imagem prova que o quadro é novo mesmo com o aviso "not redrawn". É documentação de agentes em `unity-cli.md`, o mesmo tipo da #166 e da #171, e não uma correção do que o jogador vê, por isso entra na M8 e não na M12. Vem antes da #157 e da #109, que ainda faltam na M12, porque é uma frase só e porque a verificação delas também captura salas de fundo largo em Play Mode.
  - **A #208 entra na milestone logo depois da #204,** pela triagem pedida pelo programador em 2026-11-18, depois do PR #209. Veio da revisão da #204 (PR #207), achados 1 e 2, que não bloqueiam: a receita de conferir um contorno pelos pixels, em `unity-cli.md`, não escreve que o `OnHoverEnter()` roda junto com a câmera movida, e desliga só o `ScreenPanController`, deixando o `PointNClickSelector` da câmera livre para apagar o contorno quando o mouse real passa pelo objeto. Os dois casos dão um falso negativo, que parece defeito do jogo. A issue reescreve o passo (1) da receita como uma ordem única. É documentação de agentes em `unity-cli.md`, o mesmo tipo da #204, por isso M8 e não M12. Vem antes da #109, a última da M12; as duas mexem em `unity-cli.md`, em seções diferentes ("Outras armadilhas do projeto" e "Gerar a build de Windows"), e a ordem das milestones já as põe uma depois da outra.
  - **A #214 entra na milestone depois da #208,** priorizada pelo programador em 2026-11-23, quando a M12 já estava toda feita. Veio de uma revisão das pastas da raiz do repositório, pedida pelo programador fora de qualquer issue. A issue remove o pacote embutido `com.merry-yellow.code-assist`, um plugin de IDE que nada no jogo usa, e põe `ProfilerCaptures/` no `.gitignore`. O próprio texto da issue autoriza remover o pacote (regra 10). É limpeza de ferramentas, nada que o jogador veja, por isso entra na M8. A remoção muda `Packages/`, então a troca de branch da execução e da revisão segue a receita dos pacotes ("Antes de trocar de branch", em `unity-cli.md`).
  - **A #217 entra na milestone depois da #214,** priorizada pelo programador em 2026-11-24, logo depois do merge da #214. Ela vem antes de qualquer outra issue, a #213 inclusive, que continua em `triagem`. Veio da mesma revisão do repositório, já sabendo que os roteiristas não mexem no repositório: só o programador e os agentes trabalham nele. A issue manda para o `.gitignore` o que é só desta máquina ou gerado (a `.claude/` inteira, por decisão do programador, `.vscode/`, `.vsconfig`, `Directory.Build.props` e o `.ysls.json` gerado pelo Yarn Spinner), e reescreve a documentação que descreve roteirista clonando o repositório e enviando PR. É ferramenta e documentação, nada que o jogador veja, por isso entra na M8. Tirar um arquivo do índice faz o git apagá-lo do disco na troca de branch, os comandos de trabalho inclusive, então a execução e a revisão precisam guardar uma cópia desses arquivos antes de cada troca.
  - **A #222 entra na milestone depois da #217,** priorizada pelo programador em 2026-11-24, logo depois da revisão da #213 (PR #221), e é a próxima issue a ser feita, logo depois do merge da #213. Veio do achado 1 dessa revisão, que não bloqueia: desde a #213 o campo do nome aceita uma lista fechada de letras (`A`–`Z` e as 26 acentuadas do português, nas duas caixas), e os testes de `ProtagonistName` conferem só 10 das 26 acentuadas. Uma letra que saia da lista, ou um caractere que entre por engano, passaria pela suíte EditMode, e só o jogador descobriria, na folha da lista de passageiros. A issue acrescenta um teste que percorre todos os caracteres e compara os aceitos com a lista escrita no próprio teste. É teste, nada que o jogador veja, por isso entra na M8 e não na M12. Depende da #213, porque o teste parte da regra que o PR #221 traz para a `main`.
- **O acabamento da fatia (M9) vem em seguida** porque é o que o jogador e quem monta sala veem de errado hoje.
  - **A #108 abre a milestone:** é o defeito mais visível (o pan do posto mostra o vazio preto) e corrige os limites que o passo a passo de `salas.md` manda copiar.
  - **A #96 e a #56** mexem no menu, na pausa e na placa de nome. A #56 decidiu a placa com uma cor só.
  - **A #52 vem depois da #108 e da #96:** ela valida `minX` < `maxX` no mesmo componente que a #108 ajusta, e a varredura de tooltips cobre também o que a #96 acrescentar.
  - **A #107 fecha a milestone** porque monta o `[Fatia] Posto` só a partir do passo a passo de `salas.md` e corrige o guia, e a montagem só vale com os limites certos (#108) e com os tooltips no lugar (#52).
- **Os efeitos de roteiro (M10) vêm antes da entrega** porque o prólogo é a abertura do jogo: sem fade nem imagem de tela cheia, a lembrança dos pais aparece por cima do ônibus, sem corte. A #71 depende da #37 e da #142 (M8). O levantamento dela decidiu **não dividir** (o escuro e a imagem são duas camadas do mesmo componente, com a mesma regra de fim de conversa e o mesmo trecho do prólogo): um PR só.
  - **A #127 abre a milestone,** antes da #71. Ela é a abertura automática da conversa ao chegar na sala, que estava na #71 e saiu dela no planejamento de 2026-11-13: não depende do fade, e a M11 precisa dela duas vezes. A #71 passa a depender dela, porque abrir o prólogo com a tela preta só faz sentido com a conversa já abrindo sozinha na chegada.
  - **A #155 e a #156 saíram da M10 e voltaram ao backlog, com o portão (D-27),** pela decisão do levantamento da #71, confirmada pelo programador em 2026-11-16. Elas mexem no teste dos comandos de roteiro (`ScriptCommandSignature` e `ScriptCommandParameter`): a #155 corrige a posição citada na mensagem de erro de um comando com `params`, e a #156 junta a condição da vírgula decimal que está escrita duas vezes. **Nenhum comando novo da #71 usa `params`** (a duração é um `float` opcional, que o `ScriptCommandSignature` já trata), então a #155 continua sem motivo para existir hoje; a #156 cabe no mesmo PR que voltar a mexer nesse arquivo. Seguem com `triagem` e, desde a reorganização de 2026-11-17, ficam na M15, com o mesmo portão.
  - **A M10 fechou com a #127 e a #71** e foi fechada no GitHub na reorganização de 2026-11-17.
- **O prólogo do ato 1 (M11) vem depois dos efeitos e antes da entrega.** O roteirista mandou um texto novo do prólogo em 2026-11-13: a Cena 1 é a que já está no jogo, e a Cena 2 (o pátio do posto e a loja de conveniência) substitui o conteúdo provisório do posto (#21). Ela usa a conversa de chegada (#127), e a entrega sai melhor com o prólogo de verdade do que com a Luna e a moeda provisórias. Decisões do programador no planejamento: as opções do roteiro são objetos clicáveis no cenário, e não opções de diálogo (são cinco, e uma é secreta, o que a D-11 não permite); a compra na loja é um bloco de opções com `<<dar_item>>`, sem tela de loja; e **o roteiro é fiel ao texto**, então a fatia deixa de ter a escolha de afinidade e o puzzle de item, que os roteiristas vão tratar depois. Priorizadas pelo programador em 2026-11-13.
  - **A #128 abre a milestone** porque é código sem portão: o objeto que aparece quando uma variável liga, para a ida a pé, liberada por falar com o motorista. Ela depende da #134 (M8), para o campo de variável dela nascer coberto pelo teste de conteúdo.
  - **A #129 tem portão:** o roteirista envia o texto final ao programador, que o passa ao agente, e o `.yarn` é organizado a partir dele (esse texto entra pela issue). Ela cria também os personagens, os sons e os itens que o roteiro cita, porque os testes dos roteiros reprovam o que não existe.
  - **A #130** monta as salas com os nós da #129, o objeto da #128 e a conversa de chegada da #127, tira o conteúdo provisório do posto e atualiza a fatia vertical e o roteiro de ponta a ponta. Depende também da #108 (M9), para o pan das salas não mostrar o vazio preto.
  - **A #191 vem antes da #186,** pedida pelo programador em 2026-11-17. Os 12 objetos interativos da fatia são quadrados coloridos iguais, e quem joga a build não sabe o que é cada um nem o que o clique faz. A issue põe sobre cada quadrado um rótulo provisório com o tipo do objeto (conversa, saída, item, porta trancada) e um nome legível. Fica na M11, e não na M12, porque a #186 joga a build achando os quadrados nas capturas, e a build que ela verifica deve ser a que vai a outras pessoas. O nome ao passar o mouse, como mecânica definitiva, fica fora: é decisão do game designer.
  - **A #186 fecha a milestone.** Veio da revisão da #130 (PR #185), que pediu mudanças porque o percurso novo foi medido só no Editor, por `eval`, e não foi jogado numa build. Nela, um agente gera a build de Windows e joga o roteiro de `ponta-a-ponta.md` inteiro por script. Fica na M11, e não na M12, porque é a verificação que fecha o prólogo: o que ela achar vira issue antes de a primeira entrega começar.
  - **O texto que o roteirista ainda não mandou (#182 e #183)** fica fora da M11, na M16, pelo mesmo motivo da #131: a #129 entregou nós provisórios com `// PEDIDO` no lugar das três conversas do pátio e dos produtos da loja, o percurso funciona com eles, e a M11 não deve esperar uma entrega de fora para fechar.
  - **A fonte de caneta da lista de passageiros (#131)** fica fora da M11, na M14, até a arte entregar a fonte: ela não muda nada do percurso, e a M11 não deve esperar uma entrega de fora para fechar.
  - **A M11 fechou com a #186** e foi fechada no GitHub na triagem de 2026-11-18. O que a #186 e a revisão da #191 acharam na fatia (a #198 e a #196) foi para o começo da M12.
- **A primeira entrega (M12) vem por último** porque só faz sentido entregar a fatia a alguém de fora depois de ela estar limpa (M9), com a abertura completa (M10) e com o prólogo de verdade (M11). A #89 vem antes da #109: com o jogo nas mãos de outra pessoa, o save passa a ter progresso a perder. A #89 parte do `GameSaveManager` já com o reset da #36; a #109 parte da receita de build corrigida na #124 e do pan corrigido na #108.
  - **A #198 e a #196 abrem a milestone,** pela triagem pedida pelo programador em 2026-11-18. As duas são o que quem joga a build vê de errado nos objetos interativos, e a M12 entrega essa build a alguém de fora. Vieram das duas últimas issues do prólogo (a #198 da build jogada na #186, a #196 da revisão da #191), e a regra da M11 é que o que a verificação do prólogo achar entra antes da primeira entrega.
    - **A #198 vem primeiro:** na build, a porta do ônibus continua vermelha e com o rótulo `PORTA TRANCADA` depois de aberta, porque a `[Fatia] Onibus` não tem o `PlaceholderTint` que `salas.md` manda ligar. É a menor das duas (provavelmente só montagem de sala), e o levantamento começa por uma decisão do programador: ligar o `PlaceholderTint` ou aceitar a aparência.
    - **A #196 vem logo depois:** o contorno branco do hover (`Highlight`) empata com os painéis do fundo na ordem de desenho 0, e o Unity não garante qual fica por cima, então o contorno aparece numa execução e some em outra. O levantamento escolhe entre subir a ordem no prefab `Interactable` ou descer o fundo em todas as salas, e confere se a camada de horror (#117, M13) depende do contorno. Vem depois da #198 porque a segunda saída também mexe na `[Fatia] Onibus`.
  - **A #157 vem em seguida,** pela triagem de 2026-11-16. Em telas mais largas que 16:9 (um monitor 21:9, comum no desktop) o fundo das salas não cobre a tela, e o jogador vê o vazio preto que a #108 corrigiu só para 16:9. Ela começa por uma decisão de política do programador (aceitar só 16:9, tarjas fixas ou arte para 21:9) e pode mudar a câmera de todas as salas. Por isso vem antes da #109: a build entregue já sai com a política escolhida.
  - **A #213 reabre a milestone, depois da #109,** priorizada pelo programador em 2026-11-24, quando a M12 já estava toda feita e a build ainda não tinha ido a ninguém. Veio de um teste manual do programador na folha da lista de passageiros (`<<assinar_lista>>`): depois de o nome chegar à largura máxima, o campo recusa letras mas aceita espaços sem limite, porque a largura é medida sobre o nome normalizado, e a normalização tira os espaços das pontas e junta os repetidos. O nome gravado não muda, mas, para quem joga, o limite do nome parece quebrado. É o tipo de defeito da #198 e da #196, o que quem joga a build vê de errado, por isso entra na M12, e não numa milestone depois da entrega. A #109 já preparou a build, então a build que vai a outras pessoas precisa ser gerada de novo depois da #213.
- **A camada de horror (M13) tem portão,** como a M1 teve: ela abre só quando um roteiro pedir (D-27). Depende da M9, porque a placa de nome tem variantes de horror e a #56 decidiu que o nome na placa normal tem uma cor só (`#FFF3FB`).
- **A arte pendente (M14) tem portão de arte,** e não de roteiro: ela guarda as issues que esperam um arquivo da arte e não exigem código. Nasceu na reorganização de 2026-11-15 para a #131, que antes era a única issue do backlog sem portão próprio. A #177 entrou nela na reorganização de 2026-11-17: veio da execução da #71, que pôs no prólogo uma lembrança dos pais desenhada por script e deixou o ônibus com o fundo provisório (uma sala de aula). Trocar a imagem mantém o id e o roteiro, e trocar o fundo do `[Fatia] Onibus` é montagem de sala pelo agente; nenhuma das duas exige código. As duas issues da M14 não dependem uma da outra: entra primeiro a que tiver a arte entregue antes. Se a arte da #177 chegar antes da primeira entrega (M12), vale pô-la antes da #109, para a build entregue já abrir com a arte definitiva.
- **Os comandos de roteiro com lista (M15) têm portão de roteiro (D-27).** Guardam a #155 e a #156, que mexem no teste dos comandos de roteiro e saíram da M10 sem milestone. A #155 só faz diferença quando existir um comando com `params` de tipo diferente de `string`; a #156 é uma condição repetida no mesmo arquivo e, pela relação registrada no GitHub, vem depois da #155, no mesmo PR ou logo em seguida. Elas não ganharam uma milestone sem portão porque a decisão do levantamento da #71, confirmada pelo programador em 2026-11-16, foi esperar o primeiro comando assim.
- **O roteiro pendente (M16) tem portão de roteiro,** como a M14 tem o de arte: guarda as issues que esperam um texto do roteirista e não exigem código. Nasceu na reorganização de 2026-11-17, quando o programador decidiu que a primeira entrega (M12) não espera o texto final do prólogo. As duas issues vieram da #129, que escreveu em `.yarn` a Cena 2 do prólogo com o que o roteirista tinha mandado e marcou o que faltava com `// PEDIDO`.
  - **A #182** é o texto das três opções do pátio (os caminhoneiros, esperar o mecânico e ir andando) e **onde a Cena 2 termina.** A resposta pode pedir sala nova, fim de capítulo ou mecânica nova, e cada uma vira issue própria, com `triagem`, para o programador priorizar. Com as falas, os caminhoneiros viram `CharacterSO` (D-27).
  - **A #183** é a lista de produtos da loja, que hoje são três provisórios (`salgadinho`, `refrigerante` e `chocolate`). A arte dos ícones fica para a etapa de arte.
  - As duas não dependem uma da outra nem disputam arquivo: entra primeiro a que tiver o texto entregue antes. Se o texto chegar antes da #109 (M12), vale pô-lo antes dela, para a build entregue já ter o prólogo final. Se a #183 trocar o `salgadinho`, os passos de `ponta-a-ponta.md` que o citam (11, 13 e 15) mudam junto.

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
| #140 | Pular a recarga de domínio ao entrar no Play, para a D-29 valer na prática | #36 |
| #144 | Tests/README.md diz que a suíte PlayMode roda 37 testes, mas ela roda 48 | — |
| #146 | RoomLinksTests e MainMenuTests ainda copiam EntryPointIdsByScenePath, ScenePathAndIdOf e StringField | #105 |
| #134 | Teste de conteúdo para os campos de variável de história das cenas | #31, #146 |
| #142 | Comando de roteiro com parâmetro do tipo errado provavelmente trava a conversa | #37 |
| #163 | salas.md e CLAUDE.md dizem que uma pessoa monta as salas no Inspector; quem monta é o agente, pelo CLI | #107 |
| #166 | unity-cli.md não avisa que NewScene fecha a cena aberta, mesmo com alteração não salva | #107 |
| #162 | Teste de guarda dos tooltips não olha classe [Serializable] de outro módulo nem a base dela | #52 |
| #171 | unity-cli.md: a conferência antes da suíte olha só a cena ativa, não as aditivas | #166 |
| #175 | CLAUDE.md e as skills de trabalho rodam a suíte sem conferir as cenas abertas antes | #171 |
| #187 | Trocar de branch com uma cena aberta que muda trava o CLI no diálogo de recarregar a cena | — |
| #189 | Atualizar o com.unity.pipeline para a 1.0.0 e reescrever as receitas do CLI | #187 |
| #197 | SoundTests.AdvancingWithOptionsOnScreen_DoesNotSound é instável na suíte PlayMode | — |
| #204 | unity-cli.md: a receita de conferir o contorno pode medir um objeto fora da tela movendo a câmera | — |
| #208 | unity-cli.md: a receita do contorno desliga o seletor e chama o hover depois de mover a câmera | #204 |
| #214 | Remover o pacote embutido Code Assist e ignorar a pasta ProfilerCaptures | — |
| #217 | Ignorar no git o que é só desta máquina (.claude, .vscode e outros) e alinhar a documentação a roteiristas fora do repositório | — |
| #222 | ProtagonistNameTests não confere a lista fechada inteira das letras do nome | #213 |

## M9 — Acabamento da fatia

O que o jogador e quem monta sala veem de errado na fatia vertical. Nenhuma mecânica nova.

| Issue | Título | Depende de |
|---|---|---|
| #108 | O pan da [Teste] Mecanicas e da [Fatia] Posto passa do fundo, e o guia não diz como calcular os limites | — |
| #96 | Navegação por teclado pousa em botão desabilitado, sem destaque (menu principal e pausa) | — |
| #56 | Nome colorido na placa de nome é difícil de ler | — |
| #52 | Campos do Inspector sem tooltip (regra 8) | #108, #96 |
| #107 | Montar o [Fatia] Posto só a partir do passo a passo de salas.md e corrigir o guia | #108, #52 |

## M10 — Efeitos de roteiro

Os efeitos de roteiro que o prólogo pede: a conversa que abre sozinha ao chegar na sala, o fade e a imagem de tela cheia. O levantamento da #71 decidiu não dividi-la.

| Issue | Título | Depende de |
|---|---|---|
| #127 | Conversa que abre sozinha ao chegar na sala | — |
| #71 | Fade e cenário de tela cheia comandados pelo roteiro (pedidos do prólogo) | #37, #142, #127 |

## M11 — Prólogo do ato 1

O prólogo do ato 1 no texto novo do roteirista: a Cena 1 conferida e a Cena 2, o pátio do posto e a loja de conveniência, no lugar do conteúdo provisório do posto.

| Issue | Título | Depende de |
|---|---|---|
| #128 | Objeto de cena que aparece quando uma variável da história liga | #134 |
| #129 | Prólogo do ato 1: o texto novo do roteirista em .yarn, com personagens, sons e itens | Portão: o texto final do roteirista |
| #130 | Prólogo do ato 1: montar o pátio do posto e a loja de conveniência, e tirar o conteúdo provisório do posto | #127, #128, #129, #108 |
| #191 | Rótulo de placeholder nos objetos interativos: tipo e nome sobre cada quadrado | — |
| #186 | Jogar a fatia vertical numa build de Windows com um agente, por script | #130 |

## M12 — Primeira entrega

A fatia vertical, já com o prólogo do ato 1, pronta para ir a alguém de fora, por download no itch.io (D-15).

| Issue | Título | Depende de |
|---|---|---|
| #198 | Porta do ônibus aberta continua vermelha, com o rótulo PORTA TRANCADA | — |
| #196 | Contorno do hover (Highlight) não aparece: empata com o fundo na ordem de desenho 0 | — |
| #157 | Em telas mais largas que 16:9 o fundo das salas não cobre a tela | #108 |
| #89 | Gravar o save sem risco de deixar o arquivo pela metade | #36 |
| #109 | Preparar a build de Windows para entrega: identificador, cenas de teste e pasta DoNotShip | #124, #108, #157 |
| #213 | Nome do protagonista: o campo aceita espaços sem limite depois de chegar à largura máxima | — |

## M13 — Camada de horror

**Portão:** abre só quando um roteiro pedir (D-27). O guia da camada está em `docs/arte/interface/camada-de-horror.md`, e os arquivos de arte ficam em `Adicionar/horror/`, fora do git, até lá. A #117 lista o que decidir e sugere dividi-la quando for priorizada.

| Issue | Título | Depende de |
|---|---|---|
| #117 | Camada de horror da interface (quando o roteiro pedir) | #56 |

## M14 — Arte pendente

**Portão:** abre quando a arte entregar o que a issue pede. O que espera arte e não exige código. As issues ficam com `triagem` até lá.

| Issue | Título | Depende de |
|---|---|---|
| #131 | Fonte de caneta e arte da folha da lista de passageiros (quando a arte entregar) | Portão: a fonte de caneta e a arte da folha |
| #177 | Arte definitiva da lembrança dos pais e do ônibus por dentro à noite (prólogo) | Portão: a arte da lembrança e do ônibus; #71 |

## M15 — Comandos de roteiro com lista

**Portão:** abre quando um roteiro pedir um comando com `params` de tipo diferente de `string` (D-27). As issues ficam com `triagem` até lá e entram na fila antes da issue que criar esse comando.

| Issue | Título | Depende de |
|---|---|---|
| #155 | Teste dos comandos: num params, o problema de tipo cita o número do parâmetro, não a posição da palavra | Portão: o primeiro comando com `params` de tipo diferente de `string` |
| #156 | ScriptCommandParameter: a condição da vírgula decimal está escrita duas vezes | #155 |

## M16 — Roteiro pendente

**Portão:** abre quando o roteirista entregar o texto que a issue pede. O que espera roteiro e não exige código. As issues ficam com `triagem` até lá; o que o texto pedir de novo (sala, personagem, mecânica) vira issue própria.

| Issue | Título | Depende de |
|---|---|---|
| #182 | Prólogo: o texto final dos caminhoneiros, de esperar o mecânico e de ir andando, e o fim da Cena 2 | Portão: o texto do roteirista |
| #183 | Prólogo: os produtos da loja de conveniência do posto | Portão: a lista de produtos do roteirista |

## Riscos conhecidos

Riscos aceitos pelo programador em 2026-10-01, a tratar conforme aparecerem:

- A prova de conceito do Yarn Spinner pode falhar em algum critério (instalação no Unity 6000.3, testes em EditMode).
- Ids de item, personagem e áudio citados em texto no roteiro só falham ao rodar; a proteção é um teste de validação por tipo de id.
- Sem integração contínua, a qualidade depende da disciplina da sessão de revisão.
- Roteiristas sem hábito de git são um ponto de atrito provável; o guia de autoria (#8) é a mitigação.
