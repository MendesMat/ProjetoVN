# Guia de autoria: roteiro

**Para quem:** roteiristas. Você não precisa do Unity, nem de saber programar, nem de ter usado Git antes.

**Como usar este guia:** na primeira vez, leia só [Sua primeira entrega](#sua-primeira-entrega) e faça um passo de cada vez. O resto é consulta: volte aqui quando precisar de uma regra. A última seção é um [exemplo de roteiro](#exemplo-de-roteiro) completo.

## O que já está decidido

- O roteiro é escrito em arquivos de texto `.yarn`, que ficam em `Assets/Roteiro/`.
- A ferramenta é o **VS Code** com a extensão do **Yarn Spinner**. Ela marca erros enquanto você escreve, mostra o grafo das conversas e deixa pré-visualizar o diálogo sem abrir o jogo.
- Você entrega pelo **GitHub Desktop**, em uma branch própria, abrindo um PR. O Matheus junta ao projeto. Você nunca envia direto para a `main`: ela é protegida, e o GitHub Desktop vai recusar. Isso é esperado, não é defeito.
- **Tudo o que você digita é em português:** nomes de nó, variáveis e comandos. Minúsculas, sem acento, com `_` entre as palavras (a exceção é o nome de quem fala, que leva o acento: `Gótica`).
- Uma conversa não é salva pela metade. Se o jogador fechar o jogo no meio de um diálogo, ele volta ao começo da sala.
- O que o editor mostra como certo **não garante** que o jogo aceita. Veja [O que o editor não confere](#o-que-o-editor-não-confere).

## Sua primeira entrega

Duas partes: a **preparação**, que se faz uma vez, e a **rotina**, que se repete a cada entrega.

### Preparação (uma vez só)

1. **Conta no GitHub.** Crie em [github.com](https://github.com) e mande o nome de usuário ao Matheus. Ele te convida como colaborador do repositório `MendesMat/ProjetoVN`. Aceite o convite pelo e-mail que o GitHub enviar.
2. **GitHub Desktop.** Baixe e instale em [desktop.github.com](https://desktop.github.com) e entre com a sua conta (*Sign in to GitHub.com*).
3. **VS Code.** Baixe e instale em [code.visualstudio.com](https://code.visualstudio.com).
4. **Clone o repositório.** No GitHub Desktop: *File → Clone repository*, aba *GitHub.com*, escolha `MendesMat/ProjetoVN`, escolha uma pasta no seu computador e clique em *Clone*. Demora um pouco: é o projeto inteiro do jogo. Você só vai mexer em uma pasta dele.
5. **Abra a pasta no VS Code.** *File → Open Folder* e escolha a pasta **raiz** do clone (a que se chama `ProjetoVN` e tem `Assets` dentro). Abrir a raiz, e não só `Assets/Roteiro`, é o que faz o editor conhecer os comandos do jogo.
6. **Instale a extensão.** Ao abrir a pasta, o VS Code oferece instalar as extensões recomendadas: clique em *Install*. Se não oferecer, abra *Extensions* (Ctrl+Shift+X; Cmd+Shift+X no Mac), procure **Yarn Spinner** e instale. Na primeira vez, a extensão pode baixar uma ferramenta do .NET: espere terminar, precisa de internet.
7. **Teste.** Abra `Assets/Roteiro/exemplo_comentado.yarn`. O texto deve aparecer colorido. Abra o painel *Problems* (*View → Problems*, ou Ctrl+Shift+M): ele deve estar **sem vermelho e sem amarelo** (erros e avisos). Um item azul dizendo que uma variável "is declared but never used" pode aparecer: ignore. Se aparecer `Unknown command 'dar_item'`, confira que você abriu a **raiz** do clone (passo 5) e que a extensão terminou de baixar a ferramenta do .NET (passo 6); se continuar, avise o Matheus.

> **Dica sobre a tela.** Na árvore de arquivos à esquerda, você vai ver pastas do jogo (`Assets`, `Packages` e outras). Ignore todas, exceto `Assets/Roteiro`.

### Rotina (a cada entrega)

1. **Atualize.** No GitHub Desktop, confira que a branch atual é `main`, clique em *Fetch origin* e, se aparecer, em *Pull origin*. Assim você parte do jogo mais novo.
2. **Crie a sua branch.** *Current Branch → New Branch*. Nome: `roteiro/<seu-nome>-<assunto>`, por exemplo `roteiro/ana-primeira-conversa-gotica`. A base deve ser a `main`. Uma branch nova para cada entrega.
3. **Escreva.** No VS Code, crie o arquivo (botão direito na pasta → *New File*) dentro de `Assets/Roteiro/<sala ou área>/`, com nome em minúsculas, sem acento: `Assets/Roteiro/sala_de_aula/gotica_primeiro_oi.yarn`. Se a pasta da sala ainda não existe, crie. Uma conversa mínima, para começar:

   ```
   title: gotica_primeiro_oi
   ---
   Gótica: Oi. Você é novo aqui?
   Protagonista: Sou. Cheguei hoje.
   Gótica: Então senta, que a aula já vai começar.
   ===
   ```

4. **Confira.** Salve (Ctrl+S) e olhe o painel *Problems*. Tem que ficar sem vermelho e sem amarelo. Se não ficar, vá para [Deu erro, e agora?](#deu-erro-e-agora).
5. **Pré-visualize (opcional).** Ctrl+Shift+P (Cmd+Shift+P no Mac), digite `Preview Dialogue` e escolha o comando. `Show Project Graph` mostra o mapa de todas as conversas. A pré-visualização serve para ler o fluxo; ela **não** mostra o jogo de verdade ([o que o editor não confere](#o-que-o-editor-não-confere)).
6. **Commit.** No GitHub Desktop, a lista à esquerda mostra os arquivos alterados. **Deixe marcados só os que estão em `Assets/Roteiro/`.** Desmarque qualquer outro. Escreva um resumo curto (ex.: `Primeira conversa da Gótica`) e clique em *Commit to roteiro/...*.
7. **Publique.** Clique em *Publish branch*.
8. **Abra o PR.** Clique em *Create Pull Request* (ele abre o navegador). O GitHub traz um texto pronto, escrito para programadores: **apague tudo e escreva três linhas**: (1) qual arquivo; (2) qual conversa e o nome do nó de entrada (o que está em `title:`); (3) onde ela entra na história (que sala, que personagem, quando). Clique em *Create pull request*.
9. **Espere o Matheus.** Ele roda a verificação do jogo e responde no PR. Se pedir mudança, edite o arquivo, faça um novo commit (passo 6) e clique em *Push origin*. O PR atualiza sozinho.
10. **Depois do merge.** Volte para a `main` no GitHub Desktop, clique em *Pull origin* e apague a sua branch (*Branch → Delete*).

**Regras da rotina:**

- Mexa só em `Assets/Roteiro/`. **Nunca edite** `Roteiro.yarnproject`.
- Não use os comandos da extensão que alteram vários arquivos de uma vez: `Add Line Tags`, `Create New Yarn Project`, `Create New Yarn File`, `Set as Start Node`, `Open in Project Editor`. O jogo não precisa de nenhum deles.
- **Conflito de merge: você não resolve.** Se o GitHub Desktop ou o PR avisarem de conflito, pare e chame o Matheus.
- Se o GitHub Desktop disser que não pode publicar na `main`, você está na branch errada: crie a sua (passo 2).
- Peça ao Matheus qualquer coisa que o guia não cobre. Um pedido novo vai como [comentário `// PEDIDO`](#como-pedir-o-que-ainda-não-existe).

## Estrutura de um arquivo

Um arquivo `.yarn` é uma lista de **nós**. Cada nó é uma conversa, ou um pedaço de conversa, e tem sempre este formato:

```
title: gotica_primeiro_oi
---
Gótica: Oi. Você é novo aqui?
===
```

- `title:` é o **nome do nó**. Vem em primeiro lugar.
- `---` (três hífens) separa o nome do texto.
- `===` (três iguais) fecha o nó. **Sem ele, o nó não fecha** e o editor acusa `Missing node delimiter`.
- Um arquivo pode ter vários nós, um depois do outro, cada um com o seu `title:` e o seu `===`.
- Uma linha que começa com `//` é comentário: o jogador não vê e o jogo ignora. Use para anotar.
- Recuo é de **quatro espaços**. O editor já está configurado para isso: a tecla Tab insere espaços nos arquivos `.yarn`. Não misture tab e espaços.

### Nó de entrada e nós internos

O **nó de entrada** é o que uma cena do jogo inicia. Quem monta a cena digita o nome dele em um campo de texto, então **o nome tem de ser exato**. Avise o Matheus do nome, no PR.

- **Nome:** `<quem_ou_onde>_<assunto>`, tudo em minúsculas, sem acento, com `_` entre as palavras. Exemplos: `gotica_primeiro_oi`, `sala_de_aula_chegada`.
- Os **nós internos** (os que a conversa chama) começam com o mesmo nome do nó de entrada: `gotica_primeiro_oi_resposta_sim`.
- O nome de um nó é único em **todo o projeto**, não só no arquivo. Repetir um nome é erro (`Duplicate node title`).
- **Nunca apague nem renomeie** um nó que uma cena já usa sem avisar o Matheus: a cena guarda o nome por texto e quebra sem aviso até alguém clicar no objeto.

### Ir para outro nó

| Comando | O que faz |
|---|---|
| `<<detour nome_do_no>>` | Vai para o outro nó e **volta** para a linha seguinte quando ele termina. |
| `<<jump nome_do_no>>` | Vai para o outro nó e **não volta**: a conversa continua lá e termina lá. Nada depois dele, no mesmo nível, roda. |
| `<<stop>>` | Termina a conversa na hora. |

Quando as linhas acabam, a conversa termina sozinha: não precisa de `<<stop>>` no último nó.

Um `<<jump>>` ou `<<detour>>` para um nó que não existe é só **aviso amarelo** no editor, mas é erro de conteúdo: a verificação do jogo reprova, e se chegar ao jogo a conversa é encerrada com erro. Copie o nome do nó, não o redigite.

`visited("nome_do_no")` responde se o jogador já passou por aquele nó nesta partida. Serve em condições: `<<if visited("gotica_primeiro_oi_resposta_sim")>>`.

### Um arquivo por conversa ou por sala

Um arquivo por conversa ou por sala, em `Assets/Roteiro/<sala ou área>/`. A pasta `Assets/Roteiro/Testes/` é do projeto: não mexa nela.

O arquivo [`exemplo_comentado.yarn`](../../Assets/Roteiro/exemplo_comentado.yarn) mostra cada recurso deste guia funcionando, com comentários. Copie-o para começar.

## Falas e narração

```
Uma sala de aula vazia. Dá para ouvir o relógio.
Gótica: Você é novo aqui?
Protagonista: Sou. Cheguei hoje.
Respiro fundo. Ela parece me estudar.
```

- **Fala:** `Nome: texto`. O nome de quem fala, dois-pontos, espaço e o texto. O jogo mostra o nome em uma placa acima da caixa.
- **Narração:** uma linha **sem** `Nome:` no começo. O jogo mostra o texto sem placa.
- **Uma linha do arquivo é uma caixa e um clique.** Fala longa se divide em várias linhas.
- Linhas em branco entre as falas não fazem diferença: use para organizar.

### Quem fala

**Escreva o nome sempre igual, com a mesma letra e o mesmo acento.** O jogo trata `Gótica`, `Gotica`, `gótica` e `Gótica ` (com espaço no fim) como quatro personagens diferentes, sem avisar. Nem o editor, nem o teste do jogo acusam.

| Nome | Quem é |
|---|---|
| `Gótica` | A Gótica |
| `Protagonista` | O protagonista (nome reservado, veja abaixo) |

Quando uma personagem nova entrar no jogo, o Matheus acrescenta uma linha a esta tabela. Se precisar de uma antes, peça com [`// PEDIDO`](#como-pedir-o-que-ainda-não-existe).

### O protagonista

Provisório: a #26 (nome escolhido pelo jogador) revê esta parte do guia.

- **Fala sem escolha.** Uma linha com o nome reservado `Protagonista`, como a de qualquer personagem:

  ```
  Gótica: Você é novo aqui?
  Protagonista: Sou. Cheguei hoje.
  ```

- **Fala com escolha.** Um bloco de [opções](#opções), **com duas ou mais**. Não crie bloco de uma opção só: se o protagonista só tem uma resposta, ela é uma linha `Protagonista:`.
- **Pensamento.** Narração em primeira pessoa, sem nome: `Peguei uma chave... O que será que ela abre?`

Hoje a placa mostra a palavra `Protagonista` literalmente, e ela aparece cortada (a placa é estreita; está registrado como a issue #41). Quando o nome escolhido pelo jogador entrar, você **não precisa mudar nada** nos roteiros.

### Dois-pontos na narração

**Tudo o que vem antes do primeiro dois-pontos de uma linha vira o nome de um personagem.** Isto vale em qualquer lugar da linha, não só no começo:

| Você escreveu | O jogo mostra |
|---|---|
| `Atenção: a porta fechou.` | personagem "Atenção" diz "a porta fechou." |
| `Eram 10:30 da manhã.` | personagem "Eram 10" diz "30 da manhã." |
| `Era uma vez... ou melhor: nunca foi.` | personagem "Era uma vez... ou melhor" diz "nunca foi." |

Nem o editor nem os testes acusam. **Em narração, não use dois-pontos.** Reescreva (`10h30`, `às dez e meia`) ou, quando não houver jeito, escape com barra: `Eram 10\:30 da manhã.` mostra "Eram 10:30 da manhã." sem personagem.

Dentro de uma fala o problema não existe: `Gótica: Dois: pontos: seguidos.` mostra a fala inteira.

### Quanto cabe

Medido no jogo em 1920×1080. O texto que não cabe **vaza para fora da caixa**: não há rolagem, nem corte, nem página nova.

| Onde | Cabe com folga | Limite | Acima disso |
|---|---|---|---|
| **Fala** (a caixa de texto) | até **200 caracteres** | cerca de 340 em texto comum (5 linhas) | o texto passa da caixa |
| **Opção** (o botão) | até **50 caracteres** | cerca de 55 em duas linhas; 85 em três | o texto passa do botão |
| **Nome** (a placa) | até **8 letras** (`Fernanda` cabe) | `Professora`, `Dona Marta` e `Protagonista` já são cortados | a placa mostra reticências ("…") |

Contagem de caracteres inclui espaços. Letras largas (`M`, `W`) ocupam mais: uma fala só de letras largas cabe em cerca de 210 caracteres, não 340. Na dúvida, divida em duas falas.

### Caracteres que pedem cuidado

Todos medidos. A coluna da direita é o que fazer para escrever o caractere de verdade.

| Caractere | O que acontece | Para escrever o caractere |
|---|---|---|
| `#` no meio da fala | **Erro de sintaxe.** | `\#` |
| `#palavra` no fim da linha | Vira uma etiqueta e **some** do texto, sem aviso. | `\#palavra` |
| `//` | O resto da linha vira comentário e **some**, sem aviso. | `\/\/` |
| `{` e `}` | Abrem uma expressão. `{$afinidade_gotica}` mostra o valor. Um `{` sem fechar é erro e quebra o arquivo inteiro. | `\{` e `\}` |
| `[` e `]` | Abrem marcação de texto. `[b]x[/b]` é **removida**, e o jogo mostra `x` sem negrito. `[x]` sozinho é aviso (`malformed or invalid markup`). Um **`[` sem fechar derruba a compilação do projeto inteiro**. | `\[` e `\]` |
| `<<` | `<<set ...>>` na mesma linha de uma fala é erro (`Commands should start on a new line`). | `\<\<` |
| `\` | Uma barra sozinha é erro. | `\\` |
| `->` no **começo** da linha | É uma opção. No meio da linha é texto comum. | |
| `:` na narração | Veja [Dois-pontos na narração](#dois-pontos-na-narração). | `\:` |
| `Nome :` (espaço antes dos dois-pontos) | O nome fica `Nome ` (com espaço): outro personagem. | Sem espaço antes. |

Aspas curvas `“ ”`, `‘ ’`, travessão `—`, meia-risca `–`, reticências `…`, `€` e todas as letras do português (`ã ç é õ Á É ª º`) funcionam. O `♥` também. **Fora disso nada é garantido**: `★`, `♪`, `→`, `✓` e emojis não existem na fonte do jogo e não aparecem direito. Por segurança, não use emoji nem símbolo.

## Opções

```
Gótica: O que você achou da escola?
-> Gostei bastante.
    <<set $afinidade_gotica to $afinidade_gotica + 1>>
    <<detour gotica_primeiro_oi_gostei>>
-> Achei estranha.
    <<detour gotica_primeiro_oi_estranha>>
-> Preciso ir.
    Gótica: Então vai. A gente se vê.
    <<stop>>
```

- Cada `->` é uma opção. **O texto da opção é a fala do protagonista.**
- O que acontece depois de escolher vai logo abaixo, com **quatro espaços** de recuo.
- **De duas a quatro opções por bloco.** Mais de quatro: o jogo mostra as quatro primeiras e descarta o resto, com erro no console. O editor **não acusa**. Opção bloqueada também conta (veja [Condicionar uma opção](#condicionar-uma-opção)).
- **Nunca uma opção só.** O jogo aceita, mas a convenção do projeto é que uma resposta única seja uma linha `Protagonista:`.
- A fala que vem logo antes do bloco aparece junto com as opções, na mesma tela. Por isso **não ponha `<<set>>` entre a fala e o bloco**: ponha dentro da opção. Entre os dois, o `<<set>>` tira a fala da tela das opções e o jogador precisa de um clique a mais.
- Depois de todas as opções, a conversa continua pela linha que vem logo abaixo do bloco, sem recuo. É onde os caminhos se encontram. Um caminho que não deve voltar termina com `<<stop>>` ou `<<jump>>`.

## Variáveis e afinidade

Uma variável guarda o que o jogador fez: se falou com alguém, o quanto uma personagem gosta dele. O nome começa com `$` e usa minúsculas sem acento, dígitos e `_` (`$afinidade_gotica`).

### Onde se declara

**Toda variável é declarada uma única vez, em `Assets/Roteiro/variaveis.yarn`**, com uma linha `///` de descrição logo acima:

```
/// Afinidade com a Gótica. Sobe 1 quando o protagonista concorda com ela.
<<declare $afinidade_gotica = 0>>
```

- O valor depois do `=` é o valor inicial, e o tipo vem dele: `false` ou `true` é sim/não, um número (`0`) é afinidade ou contador, um texto entre aspas é texto.
- Declarar a mesma variável em outro arquivo é erro, e usar uma variável que não está declarada é aviso: a verificação automática reprova os dois. Uma declaração sem descrição também é reprovada.
- O arquivo `variaveis.yarn` tem um nó chamado `variaveis`, só com declarações. Não aponte nenhuma cena para ele.
- Quem monta a cena escolhe a variável de um portão em uma lista feita a partir deste arquivo: o que você declara aqui aparece lá, com a descrição.
- **Acrescente as suas declarações no fim do nó, antes do `===`.** Todo roteirista mexe neste arquivo, e é o único lugar onde dois PRs podem se chocar. Se o PR avisar de conflito em `variaveis.yarn`, pare e chame o Matheus.
- Usar um valor do tipo errado é erro (`$afinidade_gotica (Number) cannot be assigned a String`).

### Mostrar uma variável no texto

`{$nome}` troca pelo valor atual: `Sua afinidade é {$afinidade_gotica}.` Serve para testar; num roteiro de verdade, não mostre número para o jogador.

### Alterar a afinidade em uma escolha

```
-> Sim
    <<set $afinidade_gotica to $afinidade_gotica + 1>>
    <<detour gotica_resposta_sim>>
```

Ponha o `<<set>>` **dentro da opção**, nunca entre a fala e o bloco de opções.

### Condicionar uma fala

```
<<if $afinidade_gotica >= 2>>
    Gótica: Ah, é você de novo. Que bom.
<<elseif $afinidade_gotica >= 1>>
    Gótica: Já nos vimos, não é?
<<else>>
    Gótica: Pode sentar onde quiser.
<<endif>>
```

- Todo `<<if>>` precisa do seu `<<endif>>`. O `<<elseif>>` e o `<<else>>` são opcionais.
- Compare a afinidade com `>=` ou `<=`, não com `==`: o valor é um número com vírgula.
- O recuo dentro da condição é de quatro espaços.
- Para sim/não: `<<if $falou_com_gotica>>` e `<<if not $falou_com_gotica>>`.

### Condicionar uma opção

```
-> Posso sentar perto de você? <<if $afinidade_gotica >= 1>>
    <<detour gotica_resposta_sentar>>
```

- **O jogador vê a opção bloqueada**, esmaecida e sem clique. Isso é de propósito: ele entende que as escolhas têm peso. Por isso o **texto de uma opção bloqueada não pode entregar o que ela esconde**.
- Uma opção bloqueada **conta** para o limite de **quatro opções** por bloco. Um bloco com quatro opções fixas e uma quinta condicional é erro de conteúdo, mesmo quando a quinta está bloqueada.
- Se **todas** as opções do bloco estiverem bloqueadas, o bloco não aparece e a conversa continua pela fala depois dele. Escreva essa fala pensando nisso.

## Comandos disponíveis

A lista cresce conforme as mecânicas entram. Cada issue que cria um comando o acrescenta aqui. **Nunca escreva um comando que não esteja nesta tabela**: veja [Como pedir o que ainda não existe](#como-pedir-o-que-ainda-não-existe).

| Comando | O que faz | Issue |
|---|---|---|
| `<<dar_item id>>` | Coloca um item no inventário | existe |
| `<<remover_item id>>` | Tira um item do inventário | existe |
| `tem_item("id")` | Em uma condição: o jogador tem o item? | existe |
| Expressão do personagem | *a definir* | #10 |
| `<<tocar_musica id>>` | Troca a música | #20 |
| `<<tocar_efeito id>>` | Toca um efeito sonoro | #20 |

Os `id` de item, personagem e áudio são definidos por quem monta o jogo no Unity. Uma verificação automática acusa um id que não existe.

Os comandos dos saltos (`<<detour>>`, `<<jump>>`, `<<stop>>`) e das variáveis (`<<set>>`, `<<declare>>`) são da linguagem e estão nas seções acima.

### Itens

```
Gótica: A escola está mais quieta do que eu imaginava.
<<if not tem_item("chave_teste")>>
    Gótica: Toma, achei esta chave no corredor.
    <<dar_item chave_teste>>
<<endif>>
```

- No comando o id vai **sem aspas** (`<<dar_item chave_teste>>`); na função, **com aspas** (`tem_item("chave_teste")`).
- O id é sempre escrito por extenso. Um id vindo de variável ou de expressão (`<<dar_item {$qual}>>`, `tem_item($qual)`) é recusado pela verificação automática.
- Cada comando leva exatamente um id. `<<dar_item>>` sem id, ou com dois, é recusado pela verificação automática; se chegar ao jogo, a conversa trava. **O editor não acusa isto.**
- Os itens são únicos. Dar um item que o jogador já tem não faz nada, e remover um item que ele não tem também não: nenhum dos dois é erro. Se a fala só faz sentido quando o item é entregue, proteja-a com `tem_item`, como no exemplo.
- Um id que não existe no jogo não trava a conversa: o comando é ignorado e `tem_item` responde falso. O erro aparece no console do Unity com o nome do nó. A verificação automática reprova antes disso.

**Itens que existem:**

| Id | Nome no jogo | O que é |
|---|---|---|
| `chave_teste` | Chave Teste | Uma chave que abre alguma porta |

Quando um item novo entrar no jogo, o Matheus acrescenta uma linha a esta tabela. Escreva o id sempre igual.

## Convenções de nome

| O quê | Formato | Exemplo |
|---|---|---|
| Arquivo | minúsculas, sem acento, `_` entre palavras | `gotica_primeiro_oi.yarn` |
| Pasta | `Assets/Roteiro/<sala ou área>/` | `Assets/Roteiro/sala_de_aula/` |
| Nó de entrada | `<quem_ou_onde>_<assunto>` | `gotica_primeiro_oi` |
| Nó interno | o nome do nó de entrada + `_` + o assunto | `gotica_primeiro_oi_resposta_sim` |
| Variável | `$` + minúsculas, sem acento, dígitos e `_` | `$afinidade_gotica` |
| Id de item | minúsculas, sem acento, dígitos e `_` | `chave_teste` |
| Branch | `roteiro/<seu-nome>-<assunto>` | `roteiro/ana-primeira-conversa-gotica` |
| Quem fala | como na tabela [Quem fala](#quem-fala), com acento | `Gótica` |

Um nome de nó com maiúscula ou acento **o editor não acusa**, mas o teste do jogo reprova.

## Como pedir o que ainda não existe

Um item novo, uma música, um som, uma expressão, uma personagem nova: o jogo ainda não tem. **Não invente o comando.** Um comando que não existe aparece como aviso no editor e é ignorado no jogo; um comando que existe com o número errado de parâmetros trava a conversa.

Deixe um comentário no ponto do roteiro e cite o pedido no texto do PR:

```
Gótica: Toma, achei esta chave no corredor.
// PEDIDO: dar o item "chave_da_biblioteca" aqui, quando ele existir.
```

O Matheus cria o que falta, acrescenta à tabela do guia e troca o comentário pelo comando.

## Deu erro, e agora?

Abra o painel *Problems*. As mensagens vêm em inglês, com um código (`YS0004`). **Vermelho é erro, amarelo é aviso, azul é informação.** O jogo trata aviso como erro: o PR só passa com o painel sem vermelho e sem amarelo.

| Mensagem | O que quer dizer | O que fazer |
|---|---|---|
| `YS0004 Missing node delimiter` | O nó não foi fechado, ou a linha `title:` está malformada. | Confira se o nó termina em `===` e se o `title:` não tem espaço. |
| `YS0007 Unclosed scope: expected an <<endif>> to match the <<if>> statement on line N` | Falta o `<<endif>>` do `<<if>>` da linha N. | Acrescente o `<<endif>>`. |
| `YS0005 Syntax error: Unexpected "endif" while reading a statement` (ou `"else"`) | Um `<<endif>>` ou `<<else>>` sobrando, ou fora do `<<if>>`. | Apague, ou ponha o `<<if>>` que falta. |
| `YS0005 Syntax error: extraneous input 'x' expecting {NEWLINE, '#'}` | Um `#` no meio da fala. | Escape: `\#`. |
| `YS0005 Syntax error: Unexpected "=" while reading an if clause` | `=` sozinho numa condição. | Compare com `>=` ou `<=`. |
| `YS0005 Syntax error: missing {OPERATOR_ASSIGNMENT, ...} at '1'` | Faltou o `to` no `<<set>>`. | `<<set $x to 1>>`. |
| `YS0005 Syntax error: missing NEWLINE at 'x'` | O nome do nó tem espaço ou caractere inválido. | Use só minúsculas, dígitos e `_`. |
| `YS0005 Syntax error: Unexpected "---" while reading a dialogue` | O nó está sem a linha `title:`. | Acrescente o `title:`. |
| `YS0005 Syntax error: token recognition error at: ...` | Um `{` ou um `<<` sem fechar. | Feche, ou escape com `\{` e `\<\<`. |
| `YS0011 Duplicate node title: 'x'` | Dois nós com o mesmo nome, no mesmo arquivo ou em arquivos diferentes. | Mude o nome de um. |
| `YS0012 Jump to undefined node: 'x'` (aviso) | O `<<jump>>` ou `<<detour>>` aponta para um nó que não existe. | Corrija o nome. |
| `YS0003 Variable '$x' is used but not declared` (aviso) | A variável não foi declarada. | Declare em `variaveis.yarn` ([Onde se declara](#onde-se-declara)). Também confira se não é erro de digitação. |
| `YS0029 Can't determine the type of the expression $x` | Consequência do anterior. | Declare a variável. |
| `YS0014 Unknown command 'x' in node 'y'` (aviso) | O comando não existe. | Confira a [tabela de comandos](#comandos-disponíveis). Se for um pedido, use [`// PEDIDO`](#como-pedir-o-que-ainda-não-existe). |
| `YS0020 Command "<<...>>" found following a line of dialogue` | Um comando na mesma linha de uma fala. | Ponha o comando em uma linha só dele. |
| `YS0050 $x (Number) cannot be assigned a String` | Tipo errado na variável (aqui, texto em uma variável numérica). | Use o tipo com que ela foi declarada. |
| `YS0063 Dialogue has malformed or invalid markup` (aviso) | Colchete na fala. | Escape: `\[` e `\]`. |
| `[Compilation Error] Index and length must refer to a location within the string`, em **todos** os arquivos | Um `[` sem fechar em alguma fala derrubou o projeto inteiro. | Procure `[` nas suas falas e escape com `\[`. |
| `YS0010 Variable '$x' is declared but never used` (azul) | A variável existe e nenhuma conversa a usa ainda. | Pode ignorar. |

Se a mensagem não está aqui, copie o texto para o PR e chame o Matheus.

## O que o editor não confere

Passar sem erro no painel *Problems* **não significa** que o jogo aceita o roteiro. O Matheus roda uma verificação no Unity antes do merge e devolve o que ela acusar. Estas são as coisas que **só o jogo** acusa (o editor deixa passar), para você evitar:

| O que | O que acontece |
|---|---|
| Id de item que não existe (`<<dar_item chave_que_nao_existe>>`) | A verificação reprova; no jogo o comando é ignorado. |
| `<<dar_item>>` sem id, com dois ids ou com id entre aspas | A verificação reprova; no jogo a conversa **trava**. |
| Nome de nó com maiúscula ou acento | A verificação reprova. A cena não conseguiria digitar o nome. |
| Nome de função errado (`tem_itm(...)`) | O jogo falha na hora da conversa. |
| Mais de quatro opções num bloco | A quinta some, com erro no console do jogo. |
| Nome de quem fala escrito de dois jeitos | Viram dois personagens, sem aviso. |
| Declaração de variável fora de `variaveis.yarn`, ou sem `///` | A verificação reprova. |
| Fala ou opção grande demais | O texto vaza da caixa ([Quanto cabe](#quanto-cabe)). |
| Dois-pontos na narração | Vira nome de personagem ([Dois-pontos na narração](#dois-pontos-na-narração)). |

A **pré-visualização** do editor também não é o jogo: ela não mostra a caixa de diálogo, o tamanho do texto, a placa de nome nem o efeito dos comandos do jogo (`dar_item`, `tem_item`), que são código do jogo.

## Do seu PR ao jogo

Isto é o que o Matheus faz com o seu PR, para você saber o que esperar:

1. Baixa a sua branch e abre o projeto no Unity. O Unity cria os arquivos `.meta` do seu `.yarn` novo; o Matheus os acrescenta ao seu PR.
2. Reimporta o `Roteiro.yarnproject` (um `.yarn` novo pode não entrar no projeto sem isso).
3. Roda a verificação (`unity command run_tests --mode EditMode`). Se algo falhar, ele explica no PR.
4. Abre a conversa no jogo e confere as falas, as opções e o tamanho do texto.
5. Se tudo estiver certo, junta ao `main` (*squash and merge*) e liga a conversa à cena.

## Exemplo de roteiro

Três conversas curtas que contam uma história só: o protagonista acha uma chave no corredor, a Gótica aparece procurando por ela, e o que ele faz com a chave muda o que ela aceita mais tarde. O exemplo mostra **coleta de item**, **entrega de item**, **ganho e perda de afinidade** e **uso da afinidade** em uma fala e em uma opção. Ele foi rodado no jogo como está escrito.

```
// CONVERSA 1: o protagonista clica no bebedouro do corredor e acha uma chave.
title: corredor_bebedouro
---
<<if tem_item("chave_teste")>>
    Só poeira e um chiclete seco. A chave já está comigo.
    <<stop>>
<<endif>>
Alguma coisa brilha embaixo do bebedouro.
Uma chave pequena, presa a um chaveiro de morcego.
// COLETA: o item entra no inventário do jogador.
<<dar_item chave_teste>>
Guardei no bolso. Alguém deve estar procurando por ela.
===

// CONVERSA 2: a Gótica procura a chave do armário dela.
title: gotica_chave_perdida
---
Gótica: Você viu uma chave por aí? Tem um morcego no chaveiro.

// Sem a chave, a conversa é curta e termina aqui.
<<if not tem_item("chave_teste")>>
    Protagonista: Não vi. Se eu achar, te aviso.
    Gótica: Tá. Ela não pode ter ido longe.
    <<stop>>
<<endif>>

Sinto o peso da chave no bolso.
Gótica: E aí? Viu ou não viu?
-> Vi. Estava embaixo do bebedouro.
    // ENTREGA: o item sai do inventário. GANHO de afinidade.
    <<remover_item chave_teste>>
    <<set $afinidade_gotica to $afinidade_gotica + 1>>
    Gótica: Sério? Já estava me vendo arrombar o meu próprio armário.
-> Depende. O que eu ganho com isso?
    // Entrega também, mas sem ganhar nada.
    <<remover_item chave_teste>>
    Gótica: Ganha eu não contar pra ninguém que você tentou.
-> Não vi nada.
    // PERDA de afinidade. O protagonista fica com a chave.
    <<set $afinidade_gotica to $afinidade_gotica - 1>>
    Gótica: Engraçado. O seu bolso está tilintando.
    Gótica: Quando lembrar onde não viu, me procura.
    <<stop>>

// Aqui chegam os dois caminhos em que a chave foi devolvida.
// USO da afinidade em uma fala.
<<if $afinidade_gotica >= 1>>
    Gótica: Fico te devendo uma. E eu pago o que devo.
<<else>>
    Gótica: Da próxima vez, devolve sem fazer graça.
<<endif>>
===

// CONVERSA 3: mais tarde, no fim da aula.
title: gotica_fim_da_aula
---
Gótica: Vou ensaiar no auditório. A banda toca na sexta.
-> Boa sorte no ensaio.
    Gótica: Sorte é pra quem não ensaia.
// USO da afinidade em uma opção: só quem ganhou a confiança dela pode pedir.
// Sem afinidade, o jogador vê a opção bloqueada.
-> Posso assistir? <<if $afinidade_gotica >= 1>>
    Gótica: Pode. Senta no fundo e não bate palma fora de hora.
===
```

Para escrever no texto um caractere que o Yarn usa:

| Para escrever | Digite |
|---|---|
| `#` | `\#` |
| `//` | `\/\/` |
| `{` `}` | `\{` `\}` |
| `[` `]` | `\[` `\]` |
| `<<` | `\<\<` |
| `\` | `\\` |
| `:` na narração | `\:` |

Limites: fala até 200 caracteres, opção até 50, nome até 8 letras, bloco de 2 a 4 opções. Recuo de quatro espaços. Nunca escreva um comando que não está na [tabela](#comandos-disponíveis).
