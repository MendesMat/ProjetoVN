# ScriptableObjects

Este diretório funciona como o banco de dados principal do jogo, mas com uma peculiaridade: aqui estão guardados as **instâncias** (os arquivos `.asset`) e não os códigos (C#) dos ScriptableObjects.

As definições das classes e a lógica desses objetos (como `ItemDataSO` ou `DialogueData`) residem dentro de seus respectivos módulos lógicos (`Inventory`, `Dialogue`, etc). Esta pasta centraliza as instâncias criadas pelos Game Designers para facilitar a organização.

> **Antes de mudar qualquer coisa aqui:** [regras de comunicação](../../../docs/arquitetura/visao-geral.md#regras-de-comunicação) · [decisões](../../../docs/arquitetura/decisoes.md) · [guia de autoria](../../../docs/autoria/salas.md)

> A pasta `Dialogues/` e a pasta `DialogueEffects/` deixam de existir com a migração para o Yarn Spinner (issue #5): os roteiros passam a ser arquivos `.yarn` em `Assets/Roteiro/`. Não crie conteúdo definitivo nelas.

---

## Estrutura de Pastas de Dados

- **`Dialogues/`**: Contém todos os nós e roteiros do jogo. Cada arquivo representa uma conversa, cena ou capítulo da Visual Novel.
- **`Items/`**: Contém os metadados de cada item coletável no jogo (Ícone, nome, ID, propriedades de uso).
- **`DialogueEffects/`**: Contém os efeitos que um nó ou escolha pode disparar (`GiveItemEffect`, `SetFlagEffect`, ...). Arraste-os na lista `Effects` do nó ou da escolha no Inspector. As classes vivem em `GameFlow/DialogueEffects/`; ver o [README do Dialogue](../Dialogue/README.md#5-efeitos-effects).

---

## Fluxo e Boas Práticas (Para IAs e Game Designers)

A regra de uso de ScriptableObjects neste projeto é focada no princípio de ser uma **base de dados somente leitura (Read-Only) em Runtime**.

1. Os objetos nesta pasta nunca devem sofrer mutação (alteração de seus dados) durante a execução do jogo. Eles servem como plantas (blueprints).
2. Se um módulo precisar modificar o estado de um item (ex: reduzir quantidade, marcar diálogo como lido), o módulo deve copiar os dados necessários para a memória volátil (`Model`) ou em um sistema de *Save Data* persistente (JSON/PlayerPrefs).
3. Essa divisão garante que ao jogar no Editor da Unity, você não sobrescreva acidentalmente os dados bases do jogo salvando as modificações sujas daquele Play Mode de volta nos arquivos `.asset`.
4. **Isso vale em dobro para `DialogueEffects/`.** Um mesmo asset de efeito costuma estar ligado a vários nós, então ele precisa ser totalmente **sem estado**: só campos de configuração preenchidos no Inspector, nada escrito em runtime.
