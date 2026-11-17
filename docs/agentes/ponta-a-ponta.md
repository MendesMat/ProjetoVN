# Verificação de ponta a ponta da fatia vertical

Roteiro para provar, numa **build de Windows** (não no Editor), o percurso descrito em [visao-geral.md](../jogo/visao-geral.md#o-alvo-a-fatia-vertical): menu, novo jogo, três salas (o ônibus, o pátio do posto e a loja), a narração de chegada, as opções da Cena 2 como objetos, a compra, salvar, fechar e continuar.

Quem joga é o programador, com mouse, teclado e fone. O agente prepara e devolve o save, gera a build, faz o aquecimento, confere o `savegame.json` nos passos 8, 10 e 13 e lê os logs. O Unity CLI não alcança a build (o pacote Pipeline é desligado no Player), então não há `eval` dentro do jogo: o que se confere é a tela, o som, o `Player.log` e o `savegame.json`.

Quando o programador não puder jogar, o agente pode percorrer o roteiro por script, com cliques, teclas e capturas da janela do jogo ([receita em unity-cli.md](unity-cli.md#jogar-a-build-por-script-22)). Isso cobre tudo menos o que exige ouvido ou o alt-tab real (os sons e a música dos passos 4 a 13, e o passo 7); o registro do resultado diz o que ficou sem verificar.

Refaça o roteiro sempre que uma issue mudar a fatia (sala, conversa, puzzle, menu, save). O que muda de uma execução para a outra é o conteúdo provisório; o percurso é o mesmo.

## Onde ficam os arquivos

| O quê | Onde |
|---|---|
| A build | `Builds/Windows/ProjetoVN.exe` (receita em [unity-cli.md](unity-cli.md#gerar-a-build-de-windows-22)) |
| O save e o `Player.log` | `%USERPROFILE%\AppData\LocalLow\NoxStudios\ProjetoVN` |
| As preferências de tela da build | `HKCU\Software\NoxStudios\ProjetoVN` |

O Editor e a build usam **a mesma pasta de dados**, então dividem o `savegame.json`. A pasta antiga `LocalLow\DefaultCompany\ProjetoVN` (de antes de o nome da empresa mudar) e a chave `HKCU\Software\DefaultCompany\ProjetoVN` não são mais usadas; não apague.

## Preparação

**0. O estado de partida.** Fora do Play Mode, com `unity status` em `ready` e a cena salva.

- Rode as duas suítes **antes** deste passo ou **depois** do passo 18, nunca no meio do roteiro: elas sobrescrevem o `savegame.json`.
- Copie o `savegame.json` para fora do repositório e apague-o do lugar, para o jogador ser novo. Se a pasta nunca recebeu um save, não há o que copiar.
- Apague as preferências de tela que uma abertura anterior tenha gravado, para a build abrir como para um jogador novo (os valores `… Default_h…` ficam):

  ```powershell
  $k = 'HKCU:\Software\NoxStudios\ProjetoVN'; Get-Item $k | Select-Object -ExpandProperty Property | Where-Object { $_ -like 'Screenmanager*' -and $_ -notlike '*Default_h*' } | ForEach-Object { Remove-ItemProperty -Path $k -Name $_ }
  ```

**1. Gere a build** pela receita de [unity-cli.md](unity-cli.md#gerar-a-build-de-windows-22). Esperado: `Succeeded`, 0 erros, e `git status` limpo depois de reverter os quatro arquivos que a build regrava.

**2. Aquecimento.** Abra `Builds/Windows/ProjetoVN.exe` e feche no menu (Alt+F4). Esperado: a janela abre no menu. Motivo: a primeira execução de uma build nova pode escrever no log `d3d12: ERROR D3D12PipelineLibrary must be recreated due to invalid disk cache` (o cache de shaders é refeito). Essa linha é do motor gráfico e é registrada à parte; depois do aquecimento ela não deve voltar.

## Sessão 1

**3. Menu.** Abra a build. Esperado: tela cheia sem borda; menu principal com o primeiro botão selecionado; **CONTINUAR desabilitado** (não há save); interface de jogo escondida.

**4. Novo jogo.** Clique em NOVO JOGO. Esperado: sem confirmação (não há save); fade para `[Fatia] Onibus`; música da sala; **depois de a tela clarear**, o prólogo abre sozinho (a conversa de chegada, #127), sem clique no mundo.

**5. O prólogo.** Esperado:
- texto revelado aos poucos (um clique completa a fala, o seguinte avança);
- o som de estrada entra, some na lembrança, volta e some na freada com batida;
- personagens atrás da caixa, quem fala em destaque; a placa do protagonista mostra `???`.

**No meio da conversa:** abra o HISTÓRICO e feche; aperte Esc (a pausa abre, o texto para) e CONTINUAR. Nas opções do motorista, "Assinar e sair" aparece em itálico. Na folha, escreva um nome e clique em ASSINAR. Esperado: a conversa termina (a assinatura é a última linha do prólogo), o jogador volta a explorar o ônibus e a porta do ônibus passa a abrir. O nome aparece na placa de quem fala a partir da conversa com o motorista, no pátio (passo 9).

**6. A poltrona.** Clique na poltrona. Esperado: só o pensamento "O motorista mandou todo mundo descer. Melhor ir até a porta.", sem repetir o prólogo. (A fala "A porta do ônibus está fechada" deixou de ser alcançável na fatia: antes de o prólogo acabar o jogador está na conversa e não pode clicar na porta, e depois a porta já abre. O portão trancado continua coberto na `[Teste] Mecanicas`.) Não há mais moeda no corredor (saiu na #130).

**7. Alt-tab.** Troque para outra janela por alguns segundos e volte. Esperado: o jogo continua de onde estava, sem travar nem perder o input. Com a janela sem foco o jogo roda a 30 FPS (limite do jogo, não defeito).

**8. O pátio.** Clique na porta do ônibus. Esperado: som da porta abrindo, fade para `[Fatia] Posto`, centrado na porta do ônibus, a música troca com suavidade, e **a narração de chegada abre sozinha** depois de a tela clarear: "Descendo os degraus, sinto o frio daquele lugar no meio do nada…" (sem placa) e o pensamento "Nossa... Que lugar deserto...". Ao fim, o jogador volta a explorar.

Conferência do agente: o `savegame.json` passa a existir, com `currentScene` `[Fatia] Posto`, `currentEntryPointId` `onibus`, o nome escolhido em `storyTexts` e `ownedItemIds` vazio.

**9. Os objetos do pátio.** Passe o mouse pelo cenário (o pan anda nas bordas): há quadrados com rótulo (#191): azuis `CONVERSA` para o motorista, os caminhoneiros e a cadeira, e um roxo `SAÍDA / Loja` para a porta da loja, e **não** há a estrada. Clique no **motorista**: som de passos no cascalho e a conversa; ao **fim** dela (não no meio), um quadrado novo, a estrada (azul, `CONVERSA / Estrada`), aparece na ponta esquerda do cenário, **com o rótulo junto**. Clique nos **caminhoneiros**, na **cadeira** e na **estrada**: cada um mostra a narração do roteiro e devolve o jogador ao pátio.

**10. A loja.** Clique na porta da loja. Esperado: fade para `[Fatia] Loja`, a música **não reinicia** (é a mesma do pátio), e a narração de chegada abre depois de clarear: "Indo até a loja…" e, ao avançar, o sininho da porta e "Só consigo ver um homem em frente à máquina de café…".

Conferência do agente: `currentScene` `[Fatia] Loja`, `currentEntryPointId` `patio` e `falou_com_motorista` em `storyBools`.

**11. A atendente.** Clique na atendente: "Boa noite, quer o quê?" com quatro opções em itálico (são gestos). Escolha "Comprar um salgadinho": som de coleta, o salgadinho no inventário e, no HISTÓRICO, o item recebido; depois a resposta do ramo do motorista ("O cara atropelou uma anta." e "Caraca... Que papelão, hein..."). Clique de novo: "Comprar um salgadinho" aparece **desabilitada**, e a conversa termina sem repetir a pergunta do acidente.

**12. A máquina e o banheiro.** Clique na máquina de café: as duas narrações, a fala do `???` ("Caafé... Caaféé...") e o pensamento. Clique no banheiro **duas vezes**: o som da porta trancada nas duas, "Tem gente..." na primeira e "Pô, cara, já falei que tem gente!" na segunda.

**13. Ida e volta.** Pela saída da loja, volte ao pátio: a narração de chegada **não** repete nada (a conversa abre e fecha sem caixa) e a estrada continua visível. Entre na loja de novo: sem narração e sem o sininho. Pela porta do ônibus, volte ao ônibus e pela porta volte ao pátio. Esperado: cada chegada salva, e o ônibus não repete o prólogo.

Conferência do agente: `currentScene` `[Fatia] Posto`, `currentEntryPointId` `onibus`, `salgadinho` em `ownedItemIds`, `tentativas_banheiro` em `storyNumbers`.

**14. Feche o jogo** com Alt+F4, no pátio, fora de conversa. O agente copia o `Player.log` (sessão 1) para fora do repositório **antes** de a build ser aberta de novo: a abertura seguinte o renomeia para `Player-prev.log`, e a de depois o perde.

## Sessão 2

**15. Continuar.** Abra a build. Esperado: CONTINUAR **habilitado**. Clique nele: `[Fatia] Posto`, no ponto da última chegada, com o salgadinho no inventário, a estrada **visível** desde o início da sala e nenhuma narração de chegada. Na loja, a atendente mostra "Comprar um salgadinho" **desabilitada** e a conversa não repete a pergunta do acidente.

**16. Sair.** Esc → VOLTAR AO MENU → confirmar → SAIR. Esperado: o menu volta, e SAIR fecha o jogo.

## Os logs

**17. Filtre os dois logs** (o da sessão 1, copiado no passo 14, e o `Player.log` da sessão 2):

```powershell
Select-String -Path <log> -Pattern 'Debug:LogError|Debug:LogWarning|Debug:LogException|Exception|ERROR' -Context 3,0
```

Esperado: **nenhuma ocorrência** de erro, aviso ou exceção de script. No `Player.log` de uma build não há prefixo de nível: um `Debug.LogError` aparece como a mensagem seguida da pilha `UnityEngine.Debug:LogError (object)`, e uma exceção como `NullReferenceException: …`.

Linhas do motor gráfico não contam como defeito do jogo, mas são registradas à parte no resultado. Só a `D3D12PipelineLibrary must be recreated…` (que tem `ERROR`, na primeira abertura de uma build nova) aparece no filtro acima. A `d3d12: failed to query info queue interface (0x80004002).` aparece em toda execução (é do driver), mas não tem `ERROR` nem `Exception`: **só se vê lendo o log inteiro**, então registre-a depois de conferir o log, não pelo filtro.

**18. Devolva o save.** Devolva o `savegame.json` copiado no passo 0 e confira que `git status` está limpo.

## Registrar o resultado

Cada erro, aviso ou exceção de script, e cada passo com resultado diferente do esperado, vira uma issue com a label `triagem`, com o passo do roteiro, o trecho do log e a sala. O PR da issue que executou o roteiro traz a tabela do roteiro (passo, resultado, issue aberta) e os logs filtrados.
