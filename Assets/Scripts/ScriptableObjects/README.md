# ScriptableObjects

Este diretório funciona como o banco de dados principal do jogo, mas com uma peculiaridade: aqui estão guardados as **instâncias** (os arquivos `.asset`) e não os códigos (C#) dos ScriptableObjects.

As definições das classes e a lógica desses objetos (como `ItemDataSO`) residem dentro de seus respectivos módulos lógicos (`Inventory`, `Dialogue`, etc). Esta pasta centraliza as instâncias criadas pelos Game Designers para facilitar a organização.

> **Antes de mudar qualquer coisa aqui:** [regras de comunicação](../../../docs/arquitetura/visao-geral.md#regras-de-comunicação) · [decisões](../../../docs/arquitetura/decisoes.md) · [guia de autoria](../../../docs/autoria/salas.md)

> Os diálogos não são mais ScriptableObjects: com a migração para o Yarn Spinner (issue #5), os roteiros são arquivos `.yarn` em `Assets/Roteiro/`, e as pastas `Dialogues/` e `DialogueEffects/` deixaram de existir.

---

## Estrutura de Pastas de Dados

- **`Items/`**: Contém os metadados de cada item coletável no jogo (Ícone, nome, ID, propriedades de uso).
  - O **id** de um item é em minúsculas sem acento, com dígitos e `_` (`chave_teste`), igual aos nomes de nó e de variável do roteiro (D-22). Ele é chave do save e é o que o roteiro cita em `<<dar_item id>>`, `<<remover_item id>>` e `tem_item("id")`.
  - Um item que o roteiro cita precisa estar no `ItemRegistry.asset`. Um teste EditMode acusa o id que não está lá.
  - **Quem cria um item acrescenta a linha dele na tabela "Itens que existem" de [roteiro.md](../../../docs/autoria/roteiro.md#comandos-disponíveis)** (id, nome no jogo e uma frase). É dali que o roteirista copia o id.

---

## Fluxo e Boas Práticas (Para IAs e Game Designers)

A regra de uso de ScriptableObjects neste projeto é focada no princípio de ser uma **base de dados somente leitura (Read-Only) em Runtime**.

1. Os objetos nesta pasta nunca devem sofrer mutação (alteração de seus dados) durante a execução do jogo. Eles servem como plantas (blueprints).
2. Se um módulo precisar modificar o estado de um item (ex: reduzir quantidade, marcar diálogo como lido), o módulo deve copiar os dados necessários para a memória volátil (`Model`) ou em um sistema de *Save Data* persistente (JSON/PlayerPrefs).
3. Essa divisão garante que ao jogar no Editor da Unity, você não sobrescreva acidentalmente os dados bases do jogo salvando as modificações sujas daquele Play Mode de volta nos arquivos `.asset`.
