# O jogo: visão geral

## O que é

Um híbrido de **visual novel** e **point-and-click** em 2D, de tema escolar. O jogador explora salas clicando em objetos e personagens, conversa em diálogos com escolhas, coleta itens e resolve travas com eles. As escolhas alteram a **afinidade** com os personagens, e a afinidade bloqueia ou libera trechos do roteiro.

- **Plataforma:** desktop (Windows, Mac, Linux), distribuído por download no itch.io. Jogar no navegador é uma possibilidade futura, não um alvo (decisão D-23).
- **Idioma:** português brasileiro. Localização está fora do escopo atual.
- **Tamanho:** desconhecido. Número de capítulos, salas e personagens ainda não foi definido por quem escreve o jogo. Por isso o projeto mede escalabilidade por volume de conteúdo (decisão D-16).

## Quem faz o quê

| Papel | Pessoa | Ferramenta | Entrega |
|---|---|---|---|
| Programação | Programador, com agentes de IA | Unity, Claude Code | Sistemas e ferramentas de autoria |
| Roteiro | Roteiristas (não usam o Unity) | VS Code com a extensão do Yarn Spinner, GitHub Desktop | Arquivos `.yarn` em `Assets/Roteiro/` |
| Design de jogo | Game designer (não usa o Unity) | — | O desenho das salas, dos puzzles, dos itens e dos personagens, entregue ao programador |
| Montagem de salas, itens e personagens | Agentes de IA, com o material do programador | Unity CLI; o programador confere pelo Inspector | Cenas, itens, personagens, sons |

Consequência para o código: o **Inspector e o arquivo de roteiro são as interfaces de autoria**. Um campo exposto precisa de nome claro, tooltip e validação: o programador confere as salas pelo Inspector, e o aviso do `OnValidate` no console é o que mostra a quem monta por script um campo esquecido.

## O alvo: a fatia vertical

A fatia vertical é este percurso funcionando de ponta a ponta, em um build de desktop:

1. O jogo abre no **menu principal**.
2. **Novo Jogo** leva à primeira sala (o ônibus). O prólogo abre sozinho ao chegar no ônibus, sem clique (#127), e o jogador escreve o **nome do protagonista** dentro da história, ao assinar a lista de passageiros no fim dele (#26).
3. O jogador explora **três salas**, ligadas por saídas: o interior do ônibus, o pátio do posto e a loja de conveniência. A porta do ônibus só abre depois do prólogo (#21). Ao chegar no pátio e na loja, uma **narração abre sozinha** (#127), e o que o roteiro não manda repetir não se repete.
4. No pátio, as opções da Cena 2 do prólogo são **objetos clicáveis**: o motorista, a porta da loja, os caminhoneiros e a cadeira (esperar o mecânico). A estrada (ir andando até a cidade) **só aparece depois de falar com o motorista** (#128). Na loja estão o atendente, a máquina de café e o banheiro (#130).
5. Conversa com personagens que aparecem como **sprites atrás da caixa de diálogo**, com destaque para quem fala, e texto revelado aos poucos. Durante a conversa, o jogador abre o **histórico** e relê o que foi dito.
6. **Compra** um item com o atendente (o item é dado pelo roteiro) e tenta entrar no banheiro trancado. Não há mais a escolha de afinidade nem o puzzle de item da primeira versão da fatia (a Luna, a moeda e a máquina de café com moeda saíram na #130, por decisão do programador no planejamento de 2026-11-13: o roteiro é fiel ao texto e não muda para caber na fatia; a afinidade virá dos roteiristas).
7. O jogo **salva** ao trocar de sala; o jogador fecha o jogo.
8. **Continuar** devolve o jogador à sala em que estava, com tudo como deixou.

O conteúdo da fatia é provisório (arte provisória, e o motorista, os caminhoneiros e o atendente são só uma área clicável sobre o fundo, sem sprite de personagem no cenário). O que ela prova é que os sistemas funcionam juntos e que conteúdo novo se faz sem código.

O percurso é verificado numa build de Windows pelo roteiro de [ponta-a-ponta.md](../agentes/ponta-a-ponta.md), executado na #22.

Depois da fatia, a milestone M7 troca a arte provisória da interface (caixa de diálogo, botões de escolha, histórico e menu principal) pela entrega da arte, sem mudar o percurso. Em seguida vêm as ferramentas e a dívida interna (M8), o acabamento da fatia (M9: o pan que mostra o vazio preto no posto, a navegação por teclado, a placa de nome, os tooltips), os efeitos de tela que o prólogo pede e a conversa que abre sozinha ao chegar na sala (M10), o prólogo do ato 1 no texto novo do roteirista, que troca o conteúdo provisório do posto pelo pátio e pela loja de conveniência (M11), e a preparação da primeira entrega no itch.io (M12). A camada de horror da interface (M13) só abre quando um roteiro pedir. A sequência está em [milestones.md](../planejamento/milestones.md).

## Fora do escopo atual

Localização, voz, mapa ou viagem rápida, vários slots de save, salvar no meio de um diálogo, sprite de personagem parado no cenário fora de uma conversa, tela de configurações, build para navegador e integração contínua.

Fora do escopo não quer dizer proibido para sempre: quer dizer que nenhuma issue atual pede isso, e que um agente não deve construir nem preparar terreno para isso (decisão D-27).

## Glossário

| Termo | Significado neste projeto |
|---|---|
| **Sala** | Um ambiente explorável. Cada sala é uma cena Unity |
| **Saída** | Objeto clicável que leva a outra sala |
| **Ponto de entrada** | Onde o jogador "chega" em uma sala, que define a posição inicial do pan |
| **Pan** | Deslocamento lateral do cenário quando o mouse encosta na borda da tela |
| **Objeto interativo** | Objeto do cenário com `InteractableItem`: reage a hover e a clique |
| **Coletável** | Objeto interativo que vira item do inventário ao ser clicado |
| **Portão** | Objeto que só libera uma ação com um item, uma flag ou os dois (`LockedActionBehaviour`). Uma porta trancada é um portão |
| **Flag** | Um fato booleano da história ("falou com a Luna") |
| **Variável** | Um valor da história: booleano, número ou texto. Uma flag é uma variável booleana |
| **Afinidade** | Variável numérica por personagem, alterada por escolhas e lida por condições |
| **Estado da história** | O conjunto de todas as variáveis. Entra no save |
| **Roteiro** | Os arquivos `.yarn` com os diálogos |
| **Nó** | Um trecho nomeado do roteiro; é por ele que uma cena inicia um diálogo |
| **Fala** | Uma linha de diálogo, dita por um personagem ou pela narração |
| **Opção / escolha** | Alternativa oferecida ao jogador em um diálogo |
| **Histórico** | A lista, só de leitura, do que já foi dito na conversa em curso (decisão D-30) |
| **Protagonista** | O personagem do jogador. O nome dele é escolhido pelo jogador ao começar um jogo novo |
| **Comando** | Instrução do roteiro que age sobre o jogo (`<<dar_item chave>>`) |
| **Modo** | Em que situação o jogo está: exploração, diálogo, pausa. Um estado da máquina de estados |
| **Manager** | Componente global e persistente, no prefab `Managers` |
| **Fatia vertical** | O percurso mínimo jogável descrito acima |
