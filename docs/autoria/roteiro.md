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

*A preencher pela issue #7:* onde as variáveis são declaradas, como alterar a afinidade em uma escolha, como condicionar uma fala ou uma opção.

## Comandos disponíveis

A lista cresce conforme as mecânicas entram. Cada issue que cria um comando o acrescenta aqui.

| Comando | O que faz | Issue |
|---|---|---|
| `<<dar_item id>>` | Coloca um item no inventário | #6 |
| `<<remover_item id>>` | Tira um item do inventário | #6 |
| `tem_item("id")` | Em uma condição: o jogador tem o item? | #6 |
| Expressão do personagem | *a definir* | #10 |
| `<<tocar_musica id>>` | Troca a música | #20 |
| `<<tocar_efeito id>>` | Toca um efeito sonoro | #20 |

Os `id` de item, personagem e áudio são definidos por quem monta o jogo no Unity. Uma verificação automática acusa um id que não existe.

## Convenções de nome

*A preencher pela issue #8.* Já vale: minúsculas, sem acento, palavras separadas por sublinhado (`afinidade_gotica`, `sala_de_aula_primeira_conversa`).

## Como testar e entregar

*A preencher pela issue #8.*
