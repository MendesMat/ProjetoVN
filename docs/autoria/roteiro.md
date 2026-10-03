# Guia de autoria: roteiro

**Para quem:** roteiristas. Você não precisa do Unity, nem de saber programar, nem de ter usado Git antes.

Este guia ensina três coisas, nesta ordem:

1. **[Como organizar a história](#como-organizar-a-história)**: em que pasta fica cada texto e como intitulá-lo.
2. **Como escrever**: de [Estrutura de um arquivo](#estrutura-de-um-arquivo) até [Como pedir o que ainda não existe](#como-pedir-o-que-ainda-não-existe). Cada regra vem com um exemplo.
3. **[Como conferir](#como-conferir)** o que você escreveu e **[como enviar](#como-enviar)** para o jogo.

No fim há um [exemplo de roteiro](#exemplo-de-roteiro) completo, com uma história curta do começo ao fim.

Se é a sua primeira vez, comece instalando os programas: [Na primeira vez: prepare o computador](#na-primeira-vez-prepare-o-computador).

## O que você precisa saber antes

- **O roteiro é texto puro.** Cada conversa é escrita em um arquivo que termina em `.yarn`, dentro da pasta `Assets/Roteiro/` do projeto.
- **Você escreve no VS Code**, um editor de texto gratuito, com a extensão **Yarn Spinner**. Ela colore o texto e aponta erros enquanto você digita.
- **Você envia pelo GitHub Desktop**, um programa que manda os seus arquivos para o projeto. O programador confere e junta ao jogo.
- **Nomes técnicos são em minúsculas, sem acento, com `_` no lugar do espaço.** Isso vale para nome de arquivo, de pasta, de conversa e de variável: `gotica_chave_perdida`, nunca `Gótica Chave Perdida`. **O texto que o jogador lê é português normal**, com acento, maiúscula e pontuação.
- **Uma conversa não é salva pela metade.** Se o jogador fechar o jogo no meio de um diálogo, ele volta ao começo da sala.
- **O editor não é o jogo.** Um texto sem erro no editor ainda pode ser recusado pela verificação do jogo. A seção [O que o editor não confere](#o-que-o-editor-não-confere) lista esses casos.

## Como organizar a história

A história é dividida em **atos**, cada ato em **capítulos**, cada capítulo em **episódios**. O episódio é a menor unidade: uma cena ou uma sequência curta de cenas. As pastas seguem essa divisão, e todo arquivo começa com uma linha de título que diz onde ele se encaixa.

Organizar assim serve para qualquer pessoa da equipe achar um texto sabendo só em que ponto da história ele acontece.

### As pastas

```
Assets/Roteiro/
  ato_1/
    capitulo_1/
      episodio_1/
        gotica_primeiro_oi.yarn
      episodio_2/
        corredor_bebedouro.yarn
        gotica_chave_perdida.yarn
      episodio_3/
        gotica_fim_da_aula.yarn
    capitulo_2/
      episodio_1/
        ...
```

- As pastas se chamam `ato_1`, `capitulo_1`, `episodio_1`: a palavra, `_` e o número. Sem acento e sem título no nome da pasta.
- **Um arquivo por conversa.** O nome do arquivo é o nome da conversa: `gotica_chave_perdida.yarn`.
- Se a pasta do ato, do capítulo ou do episódio ainda não existe, crie.
- Uma conversa que vale para vários episódios (a descrição de um objeto que fica sempre na sala, por exemplo) fica na pasta do **primeiro** episódio em que ela aparece.

Três coisas em `Assets/Roteiro/` não são suas, e você não mexe nelas: a pasta `Testes/`, o arquivo `Roteiro.yarnproject` e o arquivo `exemplo_comentado.yarn` (pode ler e copiar trechos dele, mas não alterar).

### A linha de título de cada arquivo

Todo arquivo começa com **uma linha de título**, que diz o ato, o capítulo, o episódio e o nome do episódio:

```
// ATO 1 · CAPÍTULO 1 · EPISÓDIO 2 · A chave perdida
```

A linha começa com `//`, que marca um comentário: o jogador nunca vê e o jogo ignora. Ela existe para quem abre o arquivo saber, de cara, em que ponto da história ele está.

**Dê nome a todo episódio.** "Episódio 2" não diz nada a quem procura um texto; "A chave perdida" diz. Mais exemplos:

```
// ATO 1 · CAPÍTULO 1 · EPISÓDIO 1 · O primeiro dia
// ATO 1 · CAPÍTULO 1 · EPISÓDIO 3 · O ensaio
// ATO 1 · CAPÍTULO 2 · EPISÓDIO 1 · A biblioteca fechada
```

Arquivos do mesmo episódio repetem a mesma linha:

```
// ATO 1 · CAPÍTULO 1 · EPISÓDIO 2 · A chave perdida     ← em corredor_bebedouro.yarn
// ATO 1 · CAPÍTULO 1 · EPISÓDIO 2 · A chave perdida     ← em gotica_chave_perdida.yarn
```

## Estrutura de um arquivo

Depois da linha de título vem a conversa. No Yarn, uma conversa (ou um pedaço de conversa) se chama **nó**. Um nó tem sempre três partes:

```
title: gotica_primeiro_oi
---
Gótica: Oi. Você é novo aqui?
Protagonista: Sou. Cheguei hoje.
Gótica: Então senta, que a aula já vai começar.
===
```

| Parte | O que é |
|---|---|
| `title: gotica_primeiro_oi` | O **nome do nó**. É por esse nome que o jogo encontra a conversa. |
| `---` (três hífens) | Separa o nome do texto. |
| as linhas do meio | A conversa. |
| `===` (três sinais de igual) | Fecha o nó. Sem ele, o editor acusa `Missing node delimiter`. |

Um arquivo completo, então, fica assim:

```
// ATO 1 · CAPÍTULO 1 · EPISÓDIO 1 · O primeiro dia

title: gotica_primeiro_oi
---
Gótica: Oi. Você é novo aqui?
Protagonista: Sou. Cheguei hoje.
Gótica: Então senta, que a aula já vai começar.
===
```

Duas regras de digitação que valem para o arquivo inteiro:

- **Comentário:** tudo o que vem depois de `//` em uma linha é comentário. Use para deixar recados para você e para o programador.
- **Recuo:** algumas linhas ficam "para dentro" (você vai ver em [Opções](#opções) e em [Condicionar uma fala](#condicionar-uma-fala)). O recuo é sempre de **quatro espaços**. O editor já está configurado: a tecla Tab insere os quatro espaços.

### O nome do nó

Quem monta a cena no jogo digita o nome do nó em um campo de texto, letra por letra. Por isso o nome precisa ser simples e **exato**.

- **Formato:** `<quem_ou_onde>_<assunto>`, em minúsculas, sem acento, com `_` entre as palavras.
- **O nome é único no projeto inteiro**, não só no arquivo. Dois nós com o mesmo nome dão o erro `Duplicate node title`.

| Certo | Errado | Por quê |
|---|---|---|
| `gotica_primeiro_oi` | `Gótica primeiro oi` | Tem maiúscula, acento e espaço. |
| `corredor_bebedouro` | `corredor-bebedouro` | Hífen não vale; use `_`. |
| `gotica_chave_perdida` | `conversa2` | Não diz quem fala nem sobre o quê. |

O nome do nó **não** leva ato, capítulo nem episódio: isso já está na pasta e na linha de título.

**Nunca apague nem renomeie um nó que já foi entregue** sem avisar o programador. A cena guarda o nome como texto; se o nó mudar de nome, o objeto para de responder ao clique, sem aviso.

### Vários nós na mesma conversa

Uma conversa longa pode ser dividida em vários nós, no mesmo arquivo, um depois do outro. O primeiro é o **nó de entrada** (o que a cena chama). Os outros começam com o nome dele:

```
title: gotica_primeiro_oi
---
Gótica: Oi. Você é novo aqui?
<<detour gotica_primeiro_oi_apresentacao>>
Gótica: Então senta, que a aula já vai começar.
===

title: gotica_primeiro_oi_apresentacao
---
Protagonista: Sou. Cheguei hoje.
Gótica: Dá pra ver. Você ainda está sorrindo.
===
```

Há três comandos para andar entre nós:

| Comando | O que faz | Quando usar |
|---|---|---|
| `<<detour nome_do_no>>` | Vai para o outro nó e **volta** para a linha seguinte quando ele termina. | Um trecho que se encaixa no meio da conversa. |
| `<<jump nome_do_no>>` | Vai para o outro nó e **não volta**. A conversa termina lá. | Um caminho que leva a um final diferente. |
| `<<stop>>` | Termina a conversa na hora. | Um caminho que acaba antes dos outros. |

No exemplo acima, o jogador lê: "Oi. Você é novo aqui?" → "Sou. Cheguei hoje." → "Dá pra ver. Você ainda está sorrindo." → "Então senta, que a aula já vai começar." Com `<<jump>>` no lugar de `<<detour>>`, a última fala nunca apareceria.

Quando as linhas acabam, a conversa termina sozinha: o último nó não precisa de `<<stop>>`.

**Copie o nome do nó, não redigite.** Um `<<jump>>` ou `<<detour>>` para um nó que não existe aparece só como aviso amarelo no editor, mas a verificação do jogo reprova.

## Falas e narração

Cada linha do arquivo é **uma caixa de texto e um clique** do jogador.

```
Uma sala de aula vazia. Dá para ouvir o relógio.
Gótica: Você é novo aqui?
Protagonista: Sou. Cheguei hoje.
Respiro fundo. Ela parece me estudar.
```

| Tipo | Como se escreve | O que o jogador vê |
|---|---|---|
| **Fala** | `Nome: texto` (o nome, dois-pontos, um espaço, o texto) | O texto, com o nome em uma placa acima da caixa. |
| **Narração** | Só o texto, sem nome | O texto, sem placa. |

No exemplo, a primeira e a última linha são narração; as do meio são falas.

**Fala longa se divide em várias linhas.** Em vez de:

```
Gótica: Eu cheguei cedo porque o ônibus passa às seis e depois só às oito, e às oito já não dá tempo, então eu fico aqui esperando e desenhando no caderno até alguém aparecer.
```

escreva:

```
Gótica: Eu cheguei cedo porque o ônibus passa às seis.
Gótica: Depois, só às oito. E às oito já não dá tempo.
Gótica: Então eu fico aqui, desenhando, até alguém aparecer.
```

Linhas em branco entre as falas não fazem diferença para o jogo: use para organizar.

### Quem fala

**Escreva o nome sempre igual, com a mesma letra e o mesmo acento.** Para o jogo, cada grafia é uma personagem diferente, e nada avisa do engano:

| Você escreveu | O jogo entende |
|---|---|
| `Gótica: Oi.` | A Gótica. |
| `Gotica: Oi.` | Outra personagem, chamada "Gotica". |
| `gótica: Oi.` | Outra personagem, chamada "gótica". |
| `Gótica : Oi.` | Outra personagem, chamada "Gótica " (com um espaço no fim). |

Os nomes que existem hoje:

| Nome | Quem é |
|---|---|
| `Gótica` | A Gótica |
| `Protagonista` | O protagonista (nome reservado, veja abaixo) |

Quando uma personagem nova entrar no jogo, o programador acrescenta uma linha a esta tabela. Se precisar de uma antes, peça com [`// PEDIDO`](#como-pedir-o-que-ainda-não-existe).

### O protagonista

Provisório: a issue #26 (nome escolhido pelo jogador) revê esta parte do guia.

O protagonista aparece no texto de três jeitos:

| Situação | Como escrever | Exemplo |
|---|---|---|
| Ele **fala**, e o jogador não escolhe nada | Uma linha com o nome reservado `Protagonista` | `Protagonista: Sou. Cheguei hoje.` |
| Ele **fala**, e o jogador escolhe o que dizer | Um bloco de [opções](#opções), com duas ou mais | `-> Vi. Estava embaixo do bebedouro.` |
| Ele **pensa** | Narração em primeira pessoa, sem nome | `Sinto o peso da chave no bolso.` |

Os três juntos:

```
Gótica: Você viu uma chave por aí?
Sinto o peso da chave no bolso.
Protagonista: Que chave?
Gótica: Uma com um morcego no chaveiro. E aí?
-> Vi. Estava embaixo do bebedouro.
    Gótica: Sério?
-> Não vi nada.
    Gótica: Droga. Se você achar, me avisa?
```

Hoje a placa mostra a palavra `Protagonista`, e ela aparece cortada (a placa é estreita; está registrado na issue #41). Quando o nome escolhido pelo jogador entrar, você **não precisa mudar nada** nos roteiros.

### Dois-pontos na narração

**Em uma linha de narração, tudo o que vem antes do primeiro dois-pontos vira o nome de uma personagem.** Vale em qualquer lugar da linha, não só no começo, e nada avisa do engano:

| Você escreveu | O jogo mostra |
|---|---|
| `Atenção: a porta fechou.` | Uma personagem "Atenção" dizendo "a porta fechou." |
| `Eram 10:30 da manhã.` | Uma personagem "Eram 10" dizendo "30 da manhã." |
| `Era uma vez... ou melhor: nunca foi.` | Uma personagem "Era uma vez... ou melhor" dizendo "nunca foi." |

**Em narração, não use dois-pontos.** Reescreva a frase:

| Em vez de | Escreva |
|---|---|
| `Atenção: a porta fechou.` | `A porta fechou. Preciso prestar atenção.` |
| `Eram 10:30 da manhã.` | `Eram dez e meia da manhã.` |

Quando não houver jeito, ponha uma barra invertida antes dos dois-pontos: `Eram 10\:30 da manhã.` mostra "Eram 10:30 da manhã.", sem personagem.

Dentro de uma fala o problema não existe: em `Gótica: São 10:30, anda logo.`, só o primeiro dois-pontos conta, e a Gótica diz a frase inteira.

### Quanto cabe

O texto que não cabe **vaza para fora da caixa**: não há rolagem, corte nem página nova. Medido no jogo em 1920×1080:

| Onde | Escreva até | O limite real | Acima disso |
|---|---|---|---|
| **Fala** (a caixa de texto) | **200 caracteres** | cerca de 340 (5 linhas na tela) | o texto passa da caixa |
| **Opção** (o botão) | **50 caracteres** | cerca de 55 em duas linhas; 85 em três | o texto passa do botão |
| **Nome** (a placa) | **8 letras** | `Fernanda` cabe; `Professora` e `Dona Marta` não | a placa corta e mostra "…" |

A contagem inclui os espaços. Para ter uma ideia do tamanho:

- Esta fala tem 66 caracteres: `Eu cheguei cedo porque o ônibus passa às seis. Depois, só às oito.`
- Esta opção tem 33: `Depende. O que eu ganho com isso?`

Letras largas (`M`, `W`) ocupam mais espaço. Na dúvida, divida em duas falas.

### Caracteres que pedem cuidado

Alguns caracteres têm um significado para o Yarn. Para escrevê-los como texto comum, ponha uma barra invertida (`\`) antes.

| Caractere | O que acontece se você só digitar | Para aparecer no texto | Exemplo |
|---|---|---|---|
| `#` no meio da fala | Erro de sintaxe. | `\#` | `Gótica: Sala \#12, no fim do corredor.` |
| `#palavra` no fim da linha | A palavra **some** do texto, sem aviso. | `\#palavra` | `Gótica: Ela postou com \#saudade` |
| `//` | O resto da linha **some**, sem aviso. | `\/\/` | `Gótica: O site é escola.com\/\/alunos` |
| `{` e `}` | Abrem uma expressão. Um `{` sem fechar é erro e quebra o arquivo. | `\{` e `\}` | `Rabiscado na carteira, um \{ torto.` |
| `[` e `]` | Abrem marcação. `[b]x[/b]` vira só `x`. Um **`[` sem fechar derruba o projeto inteiro.** | `\[` e `\]` | `Gótica: Escreve \[urgente\] no bilhete.` |
| `<<` | É o começo de um comando; no meio de uma fala é erro. | `\<\<` | `Na lousa, alguém rabiscou \<\< três vezes.` |
| `\` | Uma barra sozinha é erro. | `\\` | `Gótica: É barra assim \\ ou assim /?` |
| `:` na narração | Vira nome de personagem ([veja acima](#dois-pontos-na-narração)). | `\:` | `Eram 10\:30 da manhã.` |
| `->` no **começo** da linha | É uma opção. No meio da linha é texto comum. | | |

**Funcionam sem cuidado nenhum:** todas as letras do português (`ã ç é õ Á É ª º`), aspas curvas `“ ”` e `‘ ’`, travessão `—`, meia-risca `–`, reticências `…`, `€` e `♥`.

**Não funcionam:** emojis e símbolos como `★`, `♪`, `→`, `✓`. Eles não existem na fonte do jogo. Não use.

## Opções

Um bloco de opções é uma pergunta ao jogador. Cada opção começa com `->`, e o texto da opção é **a fala do protagonista**.

```
Gótica: O que você achou da escola?
-> Gostei bastante.
    Gótica: Que bom. A maioria reclama.
-> Achei estranha.
    Gótica: Estranha é pouco. Você se acostuma.
-> Preciso ir.
    Gótica: Então vai. A gente se vê.
    <<stop>>
Gótica: Bom, a aula já vai começar.
```

Como ler este exemplo:

- **O que acontece depois de cada escolha** fica logo abaixo da opção, com quatro espaços de recuo. Quem escolhe "Gostei bastante." lê "Que bom. A maioria reclama."
- **Depois da resposta, a conversa continua na primeira linha sem recuo** abaixo do bloco. Quem escolheu a primeira ou a segunda opção lê em seguida "Bom, a aula já vai começar."
- **Um caminho que não deve continuar termina com `<<stop>>`** (ou com `<<jump>>`). Quem escolhe "Preciso ir." lê a despedida e a conversa acaba ali.
- **A fala logo antes do bloco aparece junto com as opções**, na mesma tela. O jogador vê "O que você achou da escola?" e os três botões ao mesmo tempo.

As regras:

| Regra | Por quê |
|---|---|
| **De duas a quatro opções por bloco.** | O jogo tem quatro botões. Com cinco opções, a quinta some, e o editor não avisa. |
| **Nunca uma opção só.** | Se o protagonista só tem uma resposta, ela é uma linha `Protagonista:`. |
| **Nada entre a pergunta e a primeira opção.** | Um comando ali separa a pergunta dos botões, e o jogador precisa de um clique a mais. |

Um bloco de opções pode ficar dentro de outro: basta recuar mais quatro espaços.

```
Gótica: Quer saber um segredo?
-> Quero.
    Gótica: É sobre a professora ou sobre mim?
    -> Sobre a professora.
        Gótica: Ela dorme na sala dos professores.
    -> Sobre você.
        Gótica: Aí já é pedir demais.
-> Melhor não.
    Gótica: Sábia decisão.
```

## Variáveis e afinidade

Uma **variável** é uma anotação que o jogo guarda sobre o que o jogador fez. Há dois tipos em uso:

| Tipo | Guarda | Exemplo |
|---|---|---|
| Sim ou não | Se uma coisa já aconteceu | `$falou_com_gotica` |
| Número | A **afinidade**: o quanto uma personagem gosta do protagonista | `$afinidade_gotica` |

O nome de uma variável começa com `$` e segue a regra de sempre: minúsculas, sem acento, com `_`.

### Onde se declara

Antes de usar uma variável, ela precisa ser **declarada**, isto é, apresentada ao jogo com um nome, um valor inicial e uma descrição. **Todas as declarações ficam em um único arquivo, `Assets/Roteiro/variaveis.yarn`.** Ele é assim:

```
title: variaveis
---
/// A Gótica já conversou com o protagonista e ele respondeu "Sim".
<<declare $falou_com_gotica = false>>
/// Afinidade com a Gótica. Sobe 1 quando o protagonista concorda com ela.
<<declare $afinidade_gotica = 0>>
===
```

Para criar uma variável, acrescente duas linhas **no fim, antes do `===`**:

```
/// O protagonista devolveu a chave do armário para a Gótica.
<<declare $devolveu_chave_gotica = false>>
```

- A linha com `///` (três barras) é a descrição. Ela é **obrigatória** e aparece para quem monta as cenas.
- O valor depois do `=` é o valor inicial e define o tipo: `false` para sim/não, `0` para número.
- Não declare uma variável em outro arquivo, e não use uma que não foi declarada: a verificação reprova os dois.
- Este é o único arquivo em que duas pessoas mexem ao mesmo tempo. Acrescentar sempre no fim evita que as mudanças se choquem. Se mesmo assim aparecer um aviso de **conflito**, pare e chame o programador.

### Mudar o valor

O comando é `<<set>>`. Para sim/não:

```
<<set $falou_com_gotica to true>>
```

Para a afinidade, some ou subtraia do valor atual:

```
<<set $afinidade_gotica to $afinidade_gotica + 1>>
<<set $afinidade_gotica to $afinidade_gotica - 1>>
```

O `<<set>>` fica **dentro da opção** que causa a mudança:

```
Gótica: Gostou do meu desenho?
-> Gostei. É a sua cara.
    <<set $afinidade_gotica to $afinidade_gotica + 1>>
    Gótica: Sombrio e mal acabado. Obrigada.
-> Parece um borrão.
    <<set $afinidade_gotica to $afinidade_gotica - 1>>
    Gótica: É um corvo. Mas tudo bem.
-> Não entendo de arte.
    Gótica: Ninguém entende. Por isso é bom.
```

Aqui a primeira resposta sobe a afinidade, a segunda desce e a terceira não muda nada.

### Condicionar uma fala

Para uma fala só aparecer em certa situação, ponha-a entre `<<if>>` e `<<endif>>`, com recuo:

```
<<if $falou_com_gotica>>
    Gótica: Você de novo.
<<endif>>
```

Para escolher entre várias falas, use `<<elseif>>` e `<<else>>`:

```
<<if $afinidade_gotica >= 2>>
    Gótica: Ah, é você. Guardei um lugar.
<<elseif $afinidade_gotica >= 1>>
    Gótica: Já nos vimos, não é?
<<else>>
    Gótica: Pode sentar onde quiser. Só não faça barulho.
<<endif>>
```

O jogo lê de cima para baixo e mostra **só a primeira** que for verdadeira: com afinidade 2 ou mais, a primeira fala; com 1, a segunda; com 0 ou menos, a terceira.

| Para dizer | Escreva |
|---|---|
| "se já falou com a Gótica" | `<<if $falou_com_gotica>>` |
| "se ainda não falou com a Gótica" | `<<if not $falou_com_gotica>>` |
| "se a afinidade é 1 ou mais" | `<<if $afinidade_gotica >= 1>>` |
| "se a afinidade é 0 ou menos" | `<<if $afinidade_gotica <= 0>>` |
| "se o jogador já passou por aquele nó" | `<<if visited("gotica_primeiro_oi")>>` |

- Todo `<<if>>` precisa do seu `<<endif>>`.
- Compare a afinidade com `>=` ou `<=`, nunca com `==`.
- `visited("nome_do_no")` vale para a partida inteira: o jogo lembra mesmo depois de salvar e carregar. O nome do nó vai entre aspas.

### Condicionar uma opção

Ponha o `<<if>>` no **fim da linha da opção**:

```
Gótica: Vou ensaiar no auditório. A banda toca na sexta.
-> Boa sorte no ensaio.
    Gótica: Sorte é pra quem não ensaia.
-> Posso assistir? <<if $afinidade_gotica >= 1>>
    Gótica: Pode. Senta no fundo e não bate palma fora de hora.
```

- **O jogador vê a opção bloqueada**, esmaecida e sem clique. É de propósito: ele entende que existe um caminho fechado e que as escolhas têm peso.
- Por isso **o texto de uma opção bloqueada não pode entregar o que ela esconde**. "Posso assistir?" funciona; "Contar que eu sei quem roubou a chave" estraga a surpresa.
- Uma opção bloqueada **conta** para o limite de quatro.
- Se **todas** as opções do bloco estiverem bloqueadas, o bloco não aparece e a conversa segue pela linha depois dele.

### Mostrar um valor no texto

`{$nome}` é trocado pelo valor atual: `Sua afinidade com a Gótica é {$afinidade_gotica}.` Serve para testar. Num roteiro de verdade, não mostre número para o jogador.

## Comandos disponíveis

Um **comando** é uma ordem para o jogo, escrita entre `<<` e `>>`, em uma linha só dele. A lista cresce conforme as mecânicas entram: cada issue que cria um comando o acrescenta aqui.

**Nunca escreva um comando que não esteja nestas tabelas.** Se precisar de um, veja [Como pedir o que ainda não existe](#como-pedir-o-que-ainda-não-existe).

As tabelas abaixo são a lista completa. Cada linha aponta para a seção que explica o comando com exemplo.

**Andar entre nós**

| Comando | O que faz | Explicado em |
|---|---|---|
| `<<detour nome_do_no>>` | Vai para o outro nó e volta para a linha seguinte quando ele termina | [Vários nós na mesma conversa](#vários-nós-na-mesma-conversa) |
| `<<jump nome_do_no>>` | Vai para o outro nó e não volta | [Vários nós na mesma conversa](#vários-nós-na-mesma-conversa) |
| `<<stop>>` | Termina a conversa na hora | [Vários nós na mesma conversa](#vários-nós-na-mesma-conversa) |

**Variáveis**

| Comando | O que faz | Explicado em |
|---|---|---|
| `<<declare $nome = valor>>` | Cria uma variável, com o valor inicial. Só em `variaveis.yarn` | [Onde se declara](#onde-se-declara) |
| `<<set $nome to valor>>` | Muda o valor de uma variável | [Mudar o valor](#mudar-o-valor) |

**Condições**

| Comando | O que faz | Explicado em |
|---|---|---|
| `<<if condição>>` | Abre um trecho que só aparece se a condição for verdadeira | [Condicionar uma fala](#condicionar-uma-fala) |
| `<<elseif condição>>` | Dentro de um `<<if>>`: outro trecho, testado só se os anteriores falharam | [Condicionar uma fala](#condicionar-uma-fala) |
| `<<else>>` | Dentro de um `<<if>>`: o trecho que aparece se nenhuma condição valeu | [Condicionar uma fala](#condicionar-uma-fala) |
| `<<endif>>` | Fecha o `<<if>>` | [Condicionar uma fala](#condicionar-uma-fala) |
| `-> texto <<if condição>>` | No fim de uma opção: ela aparece bloqueada se a condição for falsa | [Condicionar uma opção](#condicionar-uma-opção) |

**Itens**

| Comando | O que faz | Explicado em |
|---|---|---|
| `<<dar_item id>>` | Coloca um item no inventário do jogador | [Itens](#itens) |
| `<<remover_item id>>` | Tira um item do inventário | [Itens](#itens) |

**Perguntas para usar em uma condição**

Estas não são comandos: não levam `<<` `>>` e só valem dentro de um `<<if>>` ou `<<elseif>>`.

| Pergunta | O que responde | Explicado em |
|---|---|---|
| `tem_item("id")` | O jogador tem o item? | [Itens](#itens) |
| `visited("nome_do_no")` | O jogador já passou por aquele nó? | [Condicionar uma fala](#condicionar-uma-fala) |

**Ainda não existem**

Não escreva estes; enquanto a issue não entrar, use [`// PEDIDO`](#como-pedir-o-que-ainda-não-existe).

| Comando | O que vai fazer | Entra com |
|---|---|---|
| Expressão do personagem | *a definir* | issue #10 |
| `<<tocar_musica id>>` | Trocar a música | issue #20 |
| `<<tocar_efeito id>>` | Tocar um efeito sonoro | issue #20 |

### Itens

**O jogador recebe um item** (ele acha, ou alguém dá):

```
Uma chave pequena, presa a um chaveiro de morcego.
<<dar_item chave_teste>>
Guardei no bolso.
```

**O jogador entrega um item** (ele devolve, usa ou perde):

```
Protagonista: Toma. Estava embaixo do bebedouro.
<<remover_item chave_teste>>
Gótica: Sério? Obrigada.
```

**A conversa muda conforme o jogador tem ou não o item:**

```
<<if tem_item("chave_teste")>>
    Gótica: Essa chave no seu bolso é minha.
<<else>>
    Gótica: Você viu uma chave por aí?
<<endif>>
```

As regras:

| Regra | Certo | Errado | O que acontece com o errado |
|---|---|---|---|
| No comando, o id vai **sem aspas** | `<<dar_item chave_teste>>` | `<<dar_item "chave_teste">>` | Funciona, mas escreva sempre sem aspas, para o roteiro ficar igual do começo ao fim. |
| Em `tem_item`, o id vai **com aspas** | `tem_item("chave_teste")` | `tem_item(chave_teste)` | O painel mostra um erro vermelho. |
| Cada comando leva **um** id | `<<dar_item chave_teste>>` | `<<dar_item>>` ou `<<dar_item chave_teste mapa>>` | O editor não acusa. A verificação do jogo reprova, e a conversa **trava** se ele chegar ao jogo. |
| O id é escrito por extenso | `<<dar_item chave_teste>>` | `<<dar_item {$qual}>>` | A verificação do jogo reprova. |

- **Os itens são únicos.** Dar um item que o jogador já tem não faz nada; tirar um que ele não tem também não. Nenhum dos dois é erro.
- Por isso, **proteja a fala que acompanha a entrega**, para ela não se repetir quando o jogador já tem o item:

```
<<if not tem_item("chave_teste")>>
    Gótica: Toma, achei esta chave no corredor.
    <<dar_item chave_teste>>
<<endif>>
```

**Itens que existem:**

| Id | Nome no jogo | O que é |
|---|---|---|
| `chave_teste` | Chave Teste | Uma chave que abre alguma porta |

Quando um item novo entrar no jogo, o programador acrescenta uma linha a esta tabela. Copie o id daqui, sempre igual.

## Convenções de nome

| O quê | Formato | Exemplo |
|---|---|---|
| Pasta | `ato_N/capitulo_N/episodio_N/` | `Assets/Roteiro/ato_1/capitulo_1/episodio_2/` |
| Arquivo | o nome do nó de entrada + `.yarn` | `gotica_chave_perdida.yarn` |
| Nome do episódio (na linha de título) | português normal, com acento | `A chave perdida` |
| Nó de entrada | `<quem_ou_onde>_<assunto>` | `gotica_chave_perdida` |
| Nó interno | o nome do nó de entrada + `_` + o assunto | `gotica_chave_perdida_devolveu` |
| Variável | `$` + minúsculas, sem acento, dígitos e `_` | `$afinidade_gotica` |
| Id de item | minúsculas, sem acento, dígitos e `_` | `chave_teste` |
| Quem fala | como na tabela [Quem fala](#quem-fala), com acento | `Gótica` |
| Branch | `roteiro/<seu-nome>-<assunto>` | `roteiro/ana-chave-perdida` |

## Como pedir o que ainda não existe

Um item novo, uma música, um som, uma expressão, uma personagem nova: enquanto o jogo não tiver, **não invente o comando**. Um comando que não existe é ignorado pelo jogo, e um comando que existe mas foi escrito errado trava a conversa.

Deixe um comentário `// PEDIDO:` no ponto exato do roteiro, dizendo o que deve acontecer ali:

```
Gótica: Toma. É a chave da biblioteca.
// PEDIDO: dar o item "chave_da_biblioteca" aqui. É uma chave grande e antiga.
Gótica: Não perde, que eu não tenho outra.
```

```
Gótica: Você não devia ter visto isso.
// PEDIDO: a Gótica fica com expressão de raiva nesta fala.
// PEDIDO: tocar uma música tensa a partir daqui.
```

Repita os pedidos no texto do envio (veja [A cada entrega](#a-cada-entrega)). O programador cria o que falta, acrescenta à tabela do guia e troca o comentário pelo comando.

## Como conferir

Antes de enviar, confira o texto em dois passos.

**1. O painel de problemas.** No VS Code, salve o arquivo (Ctrl+S) e abra o painel *Problems* (*View → Problems*, ou Ctrl+Shift+M). As mensagens vêm em inglês, com um código e uma cor:

| Cor | Significa | O que fazer |
|---|---|---|
| Vermelho | Erro | Corrigir. |
| Amarelo | Aviso | Corrigir também: o jogo trata aviso como erro. |
| Azul | Informação | Pode ignorar. |

O texto só está pronto com o painel **sem vermelho e sem amarelo**.

**2. A pré-visualização (opcional).** Aperte Ctrl+Shift+P (Cmd+Shift+P no Mac), digite `Preview Dialogue` e escolha o comando: o editor mostra a conversa andando, para você ler o fluxo. `Show Project Graph` mostra o mapa de todas as conversas. A pré-visualização **não** mostra a caixa de diálogo, o tamanho do texto nem o efeito dos comandos de item.

### Deu erro, e agora?

Procure a mensagem nesta tabela.

| Mensagem | O que quer dizer | O que fazer |
|---|---|---|
| `YS0004 Missing node delimiter` | O nó não foi fechado, ou a linha `title:` está malformada. | Confira se o nó termina em `===` e se o nome não tem espaço. |
| `YS0007 Unclosed scope: expected an <<endif>> to match the <<if>> statement on line N` | Falta o `<<endif>>` do `<<if>>` da linha N. | Acrescente o `<<endif>>`. |
| `YS0005 Syntax error: Unexpected "endif" while reading a statement` (ou `"else"`) | Um `<<endif>>` ou `<<else>>` sobrando, ou fora do `<<if>>`. | Apague, ou ponha o `<<if>>` que falta. |
| `YS0005 Syntax error: extraneous input 'x' expecting {NEWLINE, '#'}` | Um `#` no meio da fala. | Escreva `\#`. |
| `YS0005 Syntax error: Unexpected "=" while reading an if clause` | `=` sozinho numa condição. | Compare com `>=` ou `<=`. |
| `YS0005 Syntax error: missing {OPERATOR_ASSIGNMENT, ...} at '1'` | Faltou o `to` no `<<set>>`. | `<<set $x to 1>>`. |
| `YS0005 Syntax error: missing NEWLINE at 'x'` | O nome do nó tem espaço ou caractere inválido. | Use só minúsculas, dígitos e `_`. |
| `YS0005 Syntax error: Unexpected "---" while reading a dialogue` | O nó está sem a linha `title:`. | Acrescente o `title:`. |
| `YS0005 Syntax error: token recognition error at: ...` | Um `{` ou um `<<` sem fechar. | Feche, ou escreva `\{` e `\<\<`. |
| `YS0011 Duplicate node title: 'x'` | Dois nós com o mesmo nome, no mesmo arquivo ou em arquivos diferentes. | Mude o nome de um. |
| `YS0012 Jump to undefined node: 'x'` (aviso) | O `<<jump>>` ou `<<detour>>` aponta para um nó que não existe. | Corrija o nome. |
| `YS0003 Variable '$x' is used but not declared` (aviso) | A variável não foi declarada, ou o nome foi digitado errado. | Declare em `variaveis.yarn` ([Onde se declara](#onde-se-declara)) ou corrija o nome. |
| `YS0029 Can't determine the type of the expression $x` | Consequência do anterior. | Declare a variável. |
| `YS0014 Unknown command 'x' in node 'y'` (aviso) | O comando não existe. | Confira a [tabela de comandos](#comandos-disponíveis). Se for um pedido, use [`// PEDIDO`](#como-pedir-o-que-ainda-não-existe). |
| `YS0020 Command "<<...>>" found following a line of dialogue` | Um comando na mesma linha de uma fala. | Ponha o comando em uma linha só dele. |
| `YS0050 $x (Number) cannot be assigned a String` | Tipo errado (aqui, texto em uma variável de número). | Use o tipo com que ela foi declarada. |
| `YS0063 Dialogue has malformed or invalid markup` (aviso) | Colchete na fala. | Escreva `\[` e `\]`. |
| `[Compilation Error] Index and length must refer to a location within the string`, em **todos** os arquivos | Um `[` sem fechar em alguma fala derrubou o projeto inteiro. | Procure `[` nas suas falas e escreva `\[`. |
| `YS0010 Variable '$x' is declared but never used` (azul) | A variável existe e nenhuma conversa a usa ainda. | Pode ignorar. |

Se a mensagem não está aqui, copie o texto dela e mande ao programador.

### O que o editor não confere

O painel sem erro **não garante** que o jogo aceita o roteiro. Estas coisas o editor deixa passar; releia o texto procurando por elas antes de enviar:

| O que conferir | Exemplo do erro | O que acontece |
|---|---|---|
| O id do item existe na [tabela de itens](#itens)? | `<<dar_item chave_da_sala>>` | A verificação do jogo reprova. |
| O comando de item tem um id, e só um? | `<<dar_item>>`, `<<dar_item chave_teste mapa>>` | A verificação reprova; no jogo, a conversa **trava**. |
| `tem_item` está escrito certo? | `tem_itm("chave_teste")` | O jogo falha na hora da conversa. |
| O nome do nó é minúsculo e sem acento? | `title: Gótica_Oi` | A verificação reprova. |
| O bloco tem no máximo quatro opções? | cinco linhas `->` seguidas | A quinta some. |
| O nome de quem fala está igual ao da tabela? | `Gotica: Oi.` | Vira outra personagem, sem aviso. |
| As variáveis novas estão em `variaveis.yarn`, com `///`? | `<<declare>>` dentro da conversa | A verificação reprova. |
| As falas cabem na caixa? | uma fala de 400 caracteres | O texto vaza ([Quanto cabe](#quanto-cabe)). |
| Há dois-pontos em alguma narração? | `Eram 10:30 da manhã.` | Vira nome de personagem ([Dois-pontos na narração](#dois-pontos-na-narração)). |

O programador roda a verificação do jogo em todo envio e devolve o que ela acusar.

## Como enviar

Os textos chegam ao jogo pelo **GitHub**, o lugar na internet onde o projeto fica guardado. Quatro palavras aparecem o tempo todo:

| Palavra | O que é |
|---|---|
| **Repositório** | A pasta do projeto inteiro, guardada no GitHub. Você tem uma cópia no seu computador. |
| **Branch** | Uma cópia de trabalho só sua, com nome. Você escreve nela sem mexer no jogo de verdade. A versão oficial do jogo se chama `main`. |
| **Commit** | Um "salvar" com descrição: registra o que você mudou e por quê. |
| **PR** (*pull request*) | O pedido para juntar a sua branch à `main`. É o seu envio. |

Você nunca envia direto para a `main`: ela é protegida. Se o GitHub Desktop recusar um envio para a `main`, isso é esperado.

### Na primeira vez: prepare o computador

Faça uma vez só.

1. **Conta no GitHub.** Crie em [github.com](https://github.com) e mande o seu nome de usuário ao programador. Ele te convida para o repositório `MendesMat/ProjetoVN`. Aceite o convite pelo e-mail que o GitHub enviar.
2. **GitHub Desktop.** Baixe e instale em [desktop.github.com](https://desktop.github.com) e entre com a sua conta (*Sign in to GitHub.com*).
3. **VS Code.** Baixe e instale em [code.visualstudio.com](https://code.visualstudio.com).
4. **Copie o projeto para o seu computador.** No GitHub Desktop: *File → Clone repository*, aba *GitHub.com*, escolha `MendesMat/ProjetoVN`, escolha uma pasta e clique em *Clone*. Demora um pouco: é o projeto inteiro do jogo.
5. **Abra o projeto no VS Code.** *File → Open Folder* e escolha a pasta **raiz** do projeto: a que se chama `ProjetoVN` e tem a pasta `Assets` dentro. Abra sempre a raiz, não só `Assets/Roteiro`: é isso que faz o editor conhecer os comandos do jogo.
6. **Instale a extensão.** Ao abrir a pasta, o VS Code oferece instalar as extensões recomendadas: clique em *Install*. Se não oferecer, abra *Extensions* (Ctrl+Shift+X; Cmd+Shift+X no Mac), procure **Yarn Spinner** e instale. Na primeira vez, a extensão pode baixar uma ferramenta chamada .NET: espere terminar. Precisa de internet.
7. **Confira que funcionou.** Abra `Assets/Roteiro/exemplo_comentado.yarn`. O texto deve aparecer colorido, e o painel *Problems* (*View → Problems*) deve estar sem vermelho e sem amarelo. Se aparecer `Unknown command 'dar_item'`, confira que você abriu a raiz (passo 5) e que o download do passo 6 terminou; se continuar, avise o programador.

Na lista de arquivos à esquerda do VS Code você vai ver várias pastas do jogo (`Assets`, `Packages` e outras). A única que importa para você é `Assets/Roteiro`.

### A cada entrega

O exemplo acompanha a Ana, que vai escrever a conversa da chave perdida.

1. **Pegue a versão mais nova do jogo.** No GitHub Desktop, confira que a branch atual é `main` e clique em *Fetch origin*; se aparecer *Pull origin*, clique também.
2. **Crie a sua branch.** *Current Branch → New Branch*. O nome é `roteiro/<seu-nome>-<assunto>`:

   ```
   roteiro/ana-chave-perdida
   ```

   Uma branch nova para cada entrega, sempre criada a partir da `main`.
3. **Escreva.** No VS Code, crie o arquivo na pasta do episódio (botão direito na pasta → *New File*):

   ```
   Assets/Roteiro/ato_1/capitulo_1/episodio_2/gotica_chave_perdida.yarn
   ```

   Comece pela [linha de título](#a-linha-de-título-de-cada-arquivo).
4. **Confira.** Painel *Problems* sem vermelho e sem amarelo, e a lista de [O que o editor não confere](#o-que-o-editor-não-confere).
5. **Faça o commit.** No GitHub Desktop, a lista à esquerda mostra os arquivos que mudaram. **Deixe marcados só os que estão em `Assets/Roteiro/`**; desmarque qualquer outro. No campo de resumo, escreva o ato, o capítulo, o episódio e o título:

   ```
   Ato 1, Cap. 1, Ep. 2: A chave perdida
   ```

   Clique em *Commit to roteiro/ana-chave-perdida*.
6. **Publique a branch.** Clique em *Publish branch*.
7. **Abra o PR.** Clique em *Create Pull Request*; ele abre o navegador. O título é o mesmo do commit. O GitHub traz um texto pronto, escrito para programadores: **apague tudo** e escreva o seu, neste formato:

   ```
   Arquivos: Assets/Roteiro/ato_1/capitulo_1/episodio_2/gotica_chave_perdida.yarn
   Nó de entrada: gotica_chave_perdida
   Onde entra: corredor, no intervalo. O jogador clica na Gótica.
   Variáveis novas: nenhuma
   Pedidos: uma expressão de surpresa para a Gótica quando recebe a chave
   ```

   Clique em *Create pull request*.
8. **Espere a resposta.** O programador roda a verificação do jogo e responde no PR. Se ele pedir uma mudança: edite o arquivo, faça um novo commit (passo 5) e clique em *Push origin*. O PR se atualiza sozinho.
9. **Depois que o programador juntar ao jogo.** Volte para a `main` no GitHub Desktop, clique em *Pull origin* e apague a sua branch (*Branch → Delete*).

Outro exemplo de texto de PR, para uma entrega com dois arquivos e uma variável nova:

```
Arquivos:
  Assets/Roteiro/ato_1/capitulo_2/episodio_1/biblioteca_porta.yarn
  Assets/Roteiro/ato_1/capitulo_2/episodio_1/gotica_biblioteca.yarn
  Assets/Roteiro/variaveis.yarn
Nós de entrada: biblioteca_porta, gotica_biblioteca
Onde entra: porta da biblioteca (clique na porta) e Gótica ao lado dela.
Variáveis novas: $gotica_contou_da_biblioteca
Pedidos: o item "chave_da_biblioteca"
```

### Regras do envio

- **Mexa só em `Assets/Roteiro/`.** Nunca edite `Roteiro.yarnproject` nem a pasta `Testes/`.
- **Não use estes comandos da extensão**, que alteram vários arquivos de uma vez: `Add Line Tags`, `Create New Yarn Project`, `Create New Yarn File`, `Set as Start Node`, `Open in Project Editor`. O jogo não precisa de nenhum deles.
- **Conflito você não resolve.** Se o GitHub Desktop ou o PR avisarem de *conflict*, pare e chame o programador.
- **Se o GitHub Desktop disser que não pode publicar na `main`,** você está na branch errada: crie a sua (passo 2).

### O que acontece com o seu envio

Para você saber o que esperar:

1. O programador baixa a sua branch e abre o projeto no Unity. O Unity cria arquivos `.meta` para cada `.yarn` novo; o programador os acrescenta ao seu PR.
2. Ele roda a verificação do jogo. Se algo falhar, ele explica no PR o que corrigir.
3. Ele abre a conversa no jogo e confere as falas, as opções e o tamanho do texto.
4. Com tudo certo, ele junta a sua branch à `main` e liga a conversa à cena.

## Exemplo de roteiro

Três conversas que contam uma história só: o protagonista acha uma chave no corredor, a Gótica aparece procurando por ela, e o que ele faz com a chave muda o que ela aceita mais tarde. O exemplo mostra **coleta de item**, **entrega de item**, **ganho e perda de afinidade** e **uso da afinidade** em uma fala e em uma opção. Os três arquivos foram rodados no jogo como estão.

**`Assets/Roteiro/ato_1/capitulo_1/episodio_2/corredor_bebedouro.yarn`**

```
// ATO 1 · CAPÍTULO 1 · EPISÓDIO 2 · A chave perdida

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
```

**`Assets/Roteiro/ato_1/capitulo_1/episodio_2/gotica_chave_perdida.yarn`**

```
// ATO 1 · CAPÍTULO 1 · EPISÓDIO 2 · A chave perdida

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
    // PERDA de afinidade. Ele entrega a chave mesmo assim.
    <<set $afinidade_gotica to $afinidade_gotica - 1>>
    <<remover_item chave_teste>>
    Gótica: Não ganha nada. Ser uma pessoa decente é o mínimo.
-> Não vi nada.
    // Sem ganho nem perda de afinidade. O protagonista fica com a chave.
    Gótica: Droga. Deve ter caído em outro lugar.
    Gótica: Se você achar, me avisa?
    <<stop>>

// Aqui chegam os dois caminhos em que a chave foi devolvida.
// USO da afinidade em uma fala.
<<if $afinidade_gotica >= 1>>
    Gótica: Fico te devendo uma. E eu pago o que devo.
<<else>>
    Gótica: Da próxima vez, devolve sem fazer graça.
<<endif>>
===
```

**`Assets/Roteiro/ato_1/capitulo_1/episodio_3/gotica_fim_da_aula.yarn`**

```
// ATO 1 · CAPÍTULO 1 · EPISÓDIO 3 · O ensaio

title: gotica_fim_da_aula
---
Gótica: Vou ensaiar no auditório. A banda toca na sexta.
-> Boa sorte no ensaio.
    Gótica: Sorte é pra quem não ensaia.
// USO da afinidade em uma opção. Sem afinidade, o jogador vê a opção
// bloqueada.
-> Posso assistir? <<if $afinidade_gotica >= 1>>
    Gótica: Pode. Senta no fundo e não bate palma fora de hora.
===
```

O que cada escolha da conversa da chave causa:

| O protagonista responde | A chave | A afinidade | A Gótica fecha com | No episódio 3, "Posso assistir?" |
|---|---|---|---|---|
| "Vi. Estava embaixo do bebedouro." | é devolvida | sobe 1 | "Fico te devendo uma. E eu pago o que devo." | liberada |
| "Depende. O que eu ganho com isso?" | é devolvida | desce 1 | "Da próxima vez, devolve sem fazer graça." | bloqueada |
| "Não vi nada." | fica com ele | não muda | "Se você achar, me avisa?" | bloqueada |
