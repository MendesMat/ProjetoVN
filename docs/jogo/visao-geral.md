# O jogo: visão geral

## O que é

Um híbrido de **visual novel** e **point-and-click** em 2D, de tema escolar. O jogador explora salas clicando em objetos e personagens, conversa em diálogos com escolhas, coleta itens e resolve travas com eles. As escolhas alteram a **afinidade** com os personagens, e a afinidade bloqueia ou libera trechos do roteiro.

- **Plataforma:** desktop (Windows, Mac, Linux), distribuído por download no itch.io. Jogar no navegador é uma possibilidade futura, não um alvo (decisão D-23).
- **Idioma:** português brasileiro. Localização está fora do escopo atual.
- **Tamanho:** desconhecido. Número de capítulos, salas e personagens ainda não foi definido por quem escreve o jogo. Por isso o projeto mede escalabilidade por volume de conteúdo (decisão D-16).

## Quem faz o quê

| Papel | Pessoa | Ferramenta | Entrega |
|---|---|---|---|
| Programação | Matheus, com agentes de IA | Unity, Claude Code | Sistemas e ferramentas de autoria |
| Roteiro | Roteiristas (não usam o Unity) | VS Code com a extensão do Yarn Spinner, GitHub Desktop | Arquivos `.yarn` em `Assets/Roteiro/` |
| Design de jogo e montagem de salas | Game designer | Unity Editor (Inspector) | Cenas, itens, personagens |

Consequência para o código: o **Inspector e o arquivo de roteiro são as interfaces de autoria**. Um campo exposto precisa de nome claro, tooltip e validação, porque quem o preenche não lê o código.

## O alvo: a fatia vertical

O planejamento atual termina quando este percurso funciona de ponta a ponta, em um build de desktop:

1. O jogo abre no **menu principal**.
2. **Novo Jogo** leva à primeira sala.
3. O jogador explora **duas salas**, ligadas por uma porta.
4. Conversa com um personagem com **retrato** e texto revelado aos poucos; uma **escolha altera a afinidade** e um trecho do diálogo só aparece com afinidade suficiente.
5. Resolve um **puzzle de item**: algo obtido numa sala abre algo na outra.
6. O jogo **salva** ao trocar de sala; o jogador fecha o jogo.
7. **Continuar** devolve o jogador à sala em que estava, com tudo como deixou.

O conteúdo da fatia é provisório. O que ela prova é que os sistemas funcionam juntos e que conteúdo novo se faz sem código.

## Fora do escopo atual

Localização, voz, mapa ou viagem rápida, vários slots de save, salvar no meio de um diálogo, histórico de falas, sprites de corpo inteiro, tela de configurações, build para navegador e integração contínua.

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
| **Flag** | Um fato booleano da história ("falou com a Gótica") |
| **Variável** | Um valor da história: booleano, número ou texto. Uma flag é uma variável booleana |
| **Afinidade** | Variável numérica por personagem, alterada por escolhas e lida por condições |
| **Estado da história** | O conjunto de todas as variáveis. Entra no save |
| **Roteiro** | Os arquivos `.yarn` com os diálogos |
| **Nó** | Um trecho nomeado do roteiro; é por ele que uma cena inicia um diálogo |
| **Fala** | Uma linha de diálogo, dita por um personagem ou pela narração |
| **Opção / escolha** | Alternativa oferecida ao jogador em um diálogo |
| **Comando** | Instrução do roteiro que age sobre o jogo (`<<dar_item chave>>`) |
| **Efeito** | No sistema atual, o equivalente a um comando: um asset `DialogueEffectSO` |
| **Modo** | Em que situação o jogo está: exploração, diálogo, pausa. Um estado da máquina de estados |
| **Manager** | Componente global e persistente, no prefab `Managers` |
| **Fatia vertical** | O percurso mínimo jogável descrito acima |
