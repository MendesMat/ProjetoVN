# Guia de autoria: roteiro

**Para quem:** roteiristas. Você não precisa do Unity para seguir este guia.

> **Este guia ainda é um esqueleto.** O diálogo do jogo já roda no Yarn Spinner (issue #5): os roteiros de teste estão em `Assets/Roteiro/Testes/` e servem de exemplo de nó, escolha, `<<detour>>` e `<<jump>>`. As seções marcadas com *a preencher* são escritas pela issue indicada, quando a mecânica existir.

## O que já está decidido

- O roteiro é escrito em arquivos de texto `.yarn`, que ficam em `Assets/Roteiro/`.
- A ferramenta é o **VS Code** com a extensão do Yarn Spinner. Ela marca erros enquanto você escreve, mostra o grafo das conversas e deixa testar o diálogo sem abrir o jogo.
- Você entrega os arquivos pelo **GitHub Desktop**, em uma branch própria. O Matheus junta ao projeto.
- **Tudo o que você digita é em português:** nomes de nó, variáveis e comandos.
- Uma conversa não é salva pela metade. Se o jogador fechar o jogo no meio de um diálogo, ele volta ao começo da sala.

## Ambiente

*A preencher pela issue #8:* instalar o VS Code e a extensão, clonar o repositório pelo GitHub Desktop, criar a sua branch, abrir a pasta de roteiro.

## Estrutura de um arquivo

*A preencher pela issue #8:* como um arquivo se divide em nós, como nomear um nó, como uma cena do jogo inicia um nó.

## Falas e narração

*A preencher pela issue #8.* O formato básico de uma fala é o nome do personagem, dois-pontos e o texto. Uma linha sem nome é narração.

## Opções

*A preencher pela issue #8:* como oferecer escolhas e o que acontece depois de cada uma.

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

### Alterar a afinidade em uma escolha

```
-> Sim
    <<set $afinidade_gotica to $afinidade_gotica + 1>>
    <<detour gotica_resposta_sim>>
```

Ponha o `<<set>>` **dentro da opção**, nunca entre a fala e o bloco de opções: isso tira a fala da mesma tela das opções e o jogador precisa de um clique a mais.

### Condicionar uma fala

```
<<if $afinidade_gotica >= 1>>
    Gótica: Gostei de você.
<<endif>>
```

Compare a afinidade com `>=` ou `<=`, não com `==`: o valor é um número com vírgula.

### Condicionar uma opção

```
-> Posso sentar perto de você? <<if $afinidade_gotica >= 1>>
    <<detour gotica_resposta_sentar>>
```

- **O jogador vê a opção bloqueada**, esmaecida e sem clique. Isso é de propósito: ele entende que as escolhas têm peso. Por isso o **texto de uma opção bloqueada não pode entregar o que ela esconde**.
- Uma opção bloqueada **conta** para o limite de **quatro opções** por bloco. Um bloco com quatro opções fixas e uma quinta condicional é erro de conteúdo, mesmo quando a quinta está bloqueada.
- Se **todas** as opções do bloco estiverem bloqueadas, o bloco não aparece e a conversa continua pela fala depois dele. Escreva essa fala pensando nisso.

## Comandos disponíveis

A lista cresce conforme as mecânicas entram. Cada issue que cria um comando o acrescenta aqui.

| Comando | O que faz | Issue |
|---|---|---|
| `<<dar_item id>>` | Coloca um item no inventário | existe |
| `<<remover_item id>>` | Tira um item do inventário | existe |
| `tem_item("id")` | Em uma condição: o jogador tem o item? | existe |
| Expressão do personagem | *a definir* | #10 |
| `<<tocar_musica id>>` | Troca a música | #20 |
| `<<tocar_efeito id>>` | Toca um efeito sonoro | #20 |

Os `id` de item, personagem e áudio são definidos por quem monta o jogo no Unity. Uma verificação automática acusa um id que não existe.

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
- Cada comando leva exatamente um id. `<<dar_item>>` sem id, ou com dois, é recusado pela verificação automática; se chegar ao jogo, a conversa trava.
- Os itens são únicos. Dar um item que o jogador já tem não faz nada, e remover um item que ele não tem também não: nenhum dos dois é erro. Se a fala só faz sentido quando o item é entregue, proteja-a com `tem_item`, como no exemplo.
- Um id que não existe no jogo não trava a conversa: o comando é ignorado e `tem_item` responde falso. O erro aparece no console do Unity com o nome do nó.

## Convenções de nome

*A preencher pela issue #8.* Já vale: minúsculas, sem acento, palavras separadas por sublinhado (`afinidade_gotica`, `sala_de_aula_primeira_conversa`). O id de um item segue a mesma regra (`chave_teste`).

## Como testar e entregar

*A preencher pela issue #8.*
