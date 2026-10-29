# Verificação de ponta a ponta da fatia vertical

Roteiro para provar, numa **build de Windows** (não no Editor), o percurso descrito em [visao-geral.md](../jogo/visao-geral.md#o-alvo-a-fatia-vertical): menu, novo jogo, duas salas, diálogo condicional, puzzle, salvar, fechar e continuar.

Quem joga é o programador, com mouse, teclado e fone. O agente prepara e devolve o save, gera a build, faz o aquecimento, confere o `savegame.json` nos passos 9, 12 e 14 e lê os logs. O Unity CLI não alcança a build (o pacote Pipeline é desligado no Player), então não há `eval` dentro do jogo: o que se confere é a tela, o som, o `Player.log` e o `savegame.json`.

Quando o programador não puder jogar, o agente pode percorrer o roteiro por script, com cliques, teclas e capturas da janela do jogo ([receita em unity-cli.md](unity-cli.md#jogar-a-build-por-script-22)). Isso cobre tudo menos o que exige ouvido ou o alt-tab real (os sons e a música dos passos 4 a 11, e o passo 8); o registro do resultado diz o que ficou sem verificar.

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

- Rode as duas suítes **antes** deste passo ou **depois** do passo 17, nunca no meio do roteiro: elas sobrescrevem o `savegame.json`.
- Copie o `savegame.json` para fora do repositório e apague-o do lugar, para o jogador ser novo. Se a pasta nunca recebeu um save, não há o que copiar.
- Apague as preferências de tela que uma abertura anterior tenha gravado, para a build abrir como para um jogador novo (os valores `… Default_h…` ficam):

  ```powershell
  $k = 'HKCU:\Software\NoxStudios\ProjetoVN'; Get-Item $k | Select-Object -ExpandProperty Property | Where-Object { $_ -like 'Screenmanager*' -and $_ -notlike '*Default_h*' } | ForEach-Object { Remove-ItemProperty -Path $k -Name $_ }
  ```

**1. Gere a build** pela receita de [unity-cli.md](unity-cli.md#gerar-a-build-de-windows-22). Esperado: `Succeeded`, 0 erros, e `git status` limpo depois de reverter os quatro arquivos que a build regrava.

**2. Aquecimento.** Abra `Builds/Windows/ProjetoVN.exe` e feche no menu (Alt+F4). Esperado: a janela abre no menu. Motivo: a primeira execução de uma build nova pode escrever no log `d3d12: ERROR D3D12PipelineLibrary must be recreated due to invalid disk cache` (o cache de shaders é refeito). Essa linha é do motor gráfico e é registrada à parte; depois do aquecimento ela não deve voltar.

## Sessão 1

**3. Menu.** Abra a build. Esperado: tela cheia sem borda; menu principal com o primeiro botão selecionado; **CONTINUAR desabilitado** (não há save); interface de jogo escondida.

**4. Novo jogo.** Clique em NOVO JOGO. Esperado: sem confirmação (não há save); fade para `[Fatia] Onibus`; música da sala.

**5. Porta trancada.** Clique na porta do ônibus. Esperado: som de porta trancada e a fala "A porta do ônibus está fechada. A viagem ainda não acabou."; continua no ônibus.

**6. A moeda.** Clique na moeda do corredor. Esperado: som de coleta, moeda no inventário, o objeto some, fala "Uma moeda perdida no chão do corredor. Vou guardar."

**7. O prólogo.** Clique na poltrona. Esperado:
- texto revelado aos poucos (um clique completa a fala, o seguinte avança);
- o som de estrada entra, some na lembrança, volta e some na freada com batida;
- personagens atrás da caixa, quem fala em destaque; a placa do protagonista mostra `???`.

**No meio da conversa:** abra o HISTÓRICO e feche; aperte Esc (a pausa abre, o texto para) e CONTINUAR. Nas opções do motorista, "Assinar e sair" aparece em itálico. Na folha, escreva um nome e clique em ASSINAR. Esperado: a conversa termina (a assinatura é a última linha do prólogo) e a porta do ônibus passa a abrir. O nome aparece na placa de quem fala a partir da conversa com a Luna.

**8. Alt-tab.** Troque para outra janela por alguns segundos e volte. Esperado: o jogo continua de onde estava, sem travar nem perder o input. Com a janela sem foco o jogo roda a 30 FPS (limite do jogo, não defeito).

**9. Mudar de sala.** Clique na porta do ônibus. Esperado: som da porta abrindo, fade para `[Fatia] Posto`, centrado na porta do ônibus, a música troca com suavidade.

Conferência do agente: o `savegame.json` passa a existir, com `currentScene` `[Fatia] Posto`, `currentEntryPointId` `onibus`, `moeda` em `ownedItemIds` e o nome escolhido em `storyTexts`.

**10. A Luna.** Clique na Luna. Esperado: Luna à esquerda, protagonista à direita com o nome escolhido. Escolha "Volta, sim. Logo a gente segue viagem." e aparece a fala condicional "Se precisar de alguma coisa, é só chamar." Abra o HISTÓRICO e confira a opção escolhida à direita. Clique na Luna de novo: "Quer um café?" **habilitada**.

**11. O puzzle.** Clique na máquina de café. Esperado: "A máquina engole a moeda…" e a moeda sai do inventário. Segundo clique: "Um café já basta por hoje."

**12. Ida e volta.** Pela saída do posto para o ônibus, volte ao ônibus (a moeda não reaparece) e pela porta volte ao posto. Esperado: cada chegada salva.

Conferência do agente: o `savegame.json` tem a hora nova e `cafe_do_posto` em `storyBools`.

**13. Feche o jogo** com Alt+F4, no posto, fora de conversa. O agente copia o `Player.log` (sessão 1) para fora do repositório **antes** de a build ser aberta de novo: a abertura seguinte o renomeia para `Player-prev.log`, e a de depois o perde.

## Sessão 2

**14. Continuar.** Abra a build. Esperado: CONTINUAR **habilitado**. Clique nele: `[Fatia] Posto`, no ponto `onibus`, sem moeda no inventário, a máquina com "Um café já basta por hoje.", a Luna com "E aí, <nome>?" e "Quer um café?" habilitada.

**15. Sair.** Esc → VOLTAR AO MENU → confirmar → SAIR. Esperado: o menu volta, e SAIR fecha o jogo.

## Os logs

**16. Filtre os dois logs** (o da sessão 1, copiado no passo 13, e o `Player.log` da sessão 2):

```powershell
Select-String -Path <log> -Pattern 'Debug:LogError|Debug:LogWarning|Debug:LogException|Exception|ERROR' -Context 3,0
```

Esperado: **nenhuma ocorrência** de erro, aviso ou exceção de script. No `Player.log` de uma build não há prefixo de nível: um `Debug.LogError` aparece como a mensagem seguida da pilha `UnityEngine.Debug:LogError (object)`, e uma exceção como `NullReferenceException: …`.

Linhas do motor gráfico não contam como defeito do jogo, mas são registradas à parte no resultado: `d3d12: failed to query info queue interface (0x80004002).` aparece em toda execução (é do driver) e a `D3D12PipelineLibrary` só na primeira abertura de uma build nova.

**17. Devolva o save.** Devolva o `savegame.json` copiado no passo 0 e confira que `git status` está limpo.

## Registrar o resultado

Cada erro, aviso ou exceção de script, e cada passo com resultado diferente do esperado, vira uma issue com a label `triagem`, com o passo do roteiro, o trecho do log e a sala. O PR da issue que executou o roteiro traz a tabela do roteiro (passo, resultado, issue aberta) e os logs filtrados.
