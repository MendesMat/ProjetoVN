# Editor

Código que **só existe no Editor do Unity** e não entra na build do jogo. Hoje tem um só assunto: o seletor de variável de história do Inspector. O asmdef `ProjetoVN.Editor` é restrito à plataforma Editor e referencia `ProjetoVN.Core` e o Yarn Spinner (`YarnSpinner.Unity` e `YarnSpinner.Unity.Editor`). Nenhum módulo de runtime o referencia, nem pode: um asmdef de runtime não pode depender de um de Editor sem quebrar a build.

> **Antes de mudar qualquer coisa aqui:** [decisões](../../../docs/arquitetura/decisoes.md) (D-01, D-18) · [fluxo de trabalho](../../../docs/agentes/fluxo-de-trabalho.md)

---

## O que há aqui

| Classe | Papel |
|---|---|
| `StoryFlagDrawer` | `PropertyDrawer` do `[StoryFlag]` (do `Core`). Desenha o campo `string` como um dropdown de UI Toolkit |
| `DeclaredStoryFlags` | Lê do Yarn Spinner as variáveis booleanas declaradas. Devolve a lista, ou o motivo de não ter conseguido |

## O seletor de variável de história

`[StoryFlag]` em um campo `string` o transforma, no Inspector, em uma lista:

- as opções são **(nenhuma)** (grava texto vazio, que os campos aceitam) e as variáveis `Bool` **explícitas** de `Assets/Roteiro/variaveis.yarn`, em ordem alfabética. Numéricas, de texto e as internas do Yarn (`$Yarn.Internal.*`) não aparecem;
- a **descrição** (`///`) da variável escolhida aparece abaixo do campo, e o `[Tooltip]` do campo é preservado;
- um valor que **não está declarado** continua no campo, entra na lista e ganha um `HelpBox` de aviso;
- se o projeto não tem exatamente um `YarnProject`, ou o roteiro não compila, o campo vira um texto comum com um `HelpBox` dizendo o motivo, para não travar quem monta a cena.

Como funciona:

- `DeclaredStoryFlags` acha o único `YarnProject` por `AssetDatabase.FindAssets("t:YarnProject")` e lê `YarnProjectImporter.ImportData.serializedDeclarations` **a cada vez que o Inspector é montado**. Não usa `YarnProject.InitialValues` nem `Program`, que guardam cache até o próximo `Awake`.
- Depois de editar `variaveis.yarn`, o Unity reimporta o `YarnProject`; a lista se atualiza na próxima vez que o Inspector é montado (selecionar outro objeto e voltar basta).
- `CreatePropertyGUI` só é usado quando o Inspector do componente é de UI Toolkit, que é o caso do `LockedActionBehaviour` (sem editor customizado). Não crie um `Editor` IMGUI para um componente que usa `[StoryFlag]`.

## Limitações

- Só cobre variáveis **booleanas**. Um seletor para número ou texto fica para quando uma issue pedir (D-27).
- Renomear uma variável em `variaveis.yarn` deixa órfão o campo de uma cena que a usava, e o aviso só aparece com o objeto selecionado.
- É código de Editor: não tem teste automatizado (D-24). Foi verificado lendo o Inspector por `eval`; o clique no dropdown e a aparência ficam para o Matheus.
