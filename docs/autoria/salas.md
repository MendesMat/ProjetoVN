# Guia de autoria: salas e objetos

**Para quem:** quem monta cenas no Unity Editor. Você não precisa ler código para seguir este guia.

> **Este guia cobre o que existe hoje.** As seções marcadas com *a preencher* são escritas pela issue indicada, quando a mecânica existir. A cena de referência, com um exemplo de cada coisa descrita aqui, é `Assets/Scenes/[Teste] Mecanicas.unity`.

## Regras que valem sempre

- **Não coloque managers na cena.** `DialogueManager`, o `DialogueRunner` do Yarn Spinner, `StoryStateVariableStorage`, `InventoryManager`, `GameStateController`, `DialogueInputHandler`, `GameSaveManager` e `ItemScriptActions` são criados sozinhos quando o jogo começa. Um deles dentro da cena quebra o jogo de forma difícil de perceber.
- **Não coloque `Canvas` de jogo nem `EventSystem` na cena.** A caixa de diálogo, os botões de escolha, o painel de inventário e o `EventSystem` também são criados sozinhos quando o jogo começa, e são os mesmos em todas as salas. Se o console mostrar `There are 2 event systems in the scene` sem parar, há um `EventSystem` sobrando na cena: apague-o.
- **Não altere um asset durante o Play.** Itens são dados somente leitura.
- **Leia os avisos do console ao salvar.** Os componentes avisam quando um campo obrigatório ficou vazio.
- **Todo id é único.** O id de um item e o id persistente de um coletável são gravados no save.

## Objeto interativo

Qualquer objeto do cenário em que o jogador pode clicar.

1. Use o prefab `Assets/Prefabs/Gameplay/Interactable.prefab`, ou acrescente a um objeto um `Collider2D` e o componente **Interactable Item**.
2. Em **On Interact**, ligue o que deve acontecer no clique (veja as seções abaixo).
3. **Outline Object** é opcional: um objeto filho que aparece quando o mouse passa por cima.

O objeto cresce um pouco no hover (**Highlight Scale Multiplier**).

## Coletável

Um objeto que vira item do inventário ao ser clicado.

1. Crie o item: botão direito no Project → **Create → Items → Item**. Preencha **Id** (único, sem espaços), **Item Name**, **Description** e **Icon**.
2. Acrescente o item à lista do asset `Assets/Scripts/ScriptableObjects/Items/ItemRegistry.asset`. Sem isso, o item não é restaurado ao carregar um save. O registro avisa se dois itens têm o mesmo id.
3. No objeto interativo, acrescente o componente **Collectable Item Behaviour** e aponte **Item Data** para o item.
4. Em **On Interact**, ligue `CollectableItemBehaviour.Collect`.

O campo **Persistent Id** é gerado sozinho. **Se você duplicar um coletável, o id vem copiado:** clique com o botão direito no componente e escolha **Regenerate Persistent Id** na cópia.

## Diálogo ao clicar

1. No objeto interativo, acrescente o componente **Interactable Dialogue Trigger** e digite em **Node Name** o nome do nó do roteiro que ele inicia (por exemplo `porta_trancada`). O nome é de um nó dos arquivos `.yarn` em `Assets/Roteiro/`; o nome do nó de entrada vem no PR do roteirista (formato `<quem_ou_onde>_<assunto>`, ver [as convenções do guia](roteiro.md#convenções-de-nome)); na dúvida, procure a linha `title:` no arquivo.
2. Em **On Interact**, ligue `InteractableDialogueTrigger.TriggerDialogue`.

O nome é minúsculas sem acento, dígitos e `_`; o componente avisa no console se ele fugir desse formato. **O nome não é conferido ao montar a cena:** um nome errado só aparece ao clicar no objeto, com um aviso no console ("o nó 'x' não existe no roteiro") e sem iniciar conversa. **Node Name** vazio também não inicia conversa e avisa ao clicar.

Os diálogos não são mais assets do Inspector: quem escreve a conversa é o roteirista, em arquivos de texto; ver [roteiro.md](roteiro.md).

## Portão (porta trancada)

Um objeto que só libera uma ação com um item, uma flag ou os dois, e que lembra que foi aberto.

1. No objeto interativo, acrescente o componente **Locked Action Behaviour**.
2. Preencha pelo menos um requisito:
   - **Required Item:** o item exigido. Ele é **consumido** ao abrir.
   - **Required Flag Id:** uma variável de história que precisa estar ligada. Ela **não** é consumida.
3. Preencha **Unlocked Flag Id** com uma variável **única para este portão** (por exemplo `$porta_biblioteca_destrancada`). É o que mantém o portão aberto depois. Dois portões com a mesma variável abrem juntos.

**Os dois campos de variável são listas.** Eles mostram as variáveis **booleanas** declaradas em `Assets/Roteiro/variaveis.yarn`, em ordem alfabética, mais **(nenhuma)** para deixar o campo vazio. Abaixo do campo aparece a descrição da variável escolhida. As variáveis numéricas (afinidade) e de texto não aparecem.

- **Uma variável nova** (a do seu portão) entra primeiro em `variaveis.yarn`, com uma linha `///` de descrição e `<<declare $nome = false>>`; veja [roteiro.md](roteiro.md#variáveis-e-afinidade). Depois do Unity importar o arquivo, ela aparece na lista.
- **Um valor que não está declarado** (a variável foi renomeada ou nunca existiu) continua no campo e aparece com um aviso amarelo logo abaixo. Escolha outra na lista ou declare a variável.
- Se o roteiro não compila, ou o projeto não tem exatamente um `YarnProject`, a lista dá lugar a um aviso e a um campo de texto comum.
- O formato do nome é `$` seguido de minúsculas sem acento, dígitos e `_`. O componente ainda avisa no console se um valor digitado por fora da lista fugir desse formato.
4. Em **On Interact**, ligue `LockedActionBehaviour.Interact`.
5. Ligue os eventos:

| Evento | Quando dispara | Use para |
|---|---|---|
| **On Locked** | O jogador clicou e faltou o requisito | A fala de "está trancada" |
| **On Unlocked** | O instante em que destrancou, uma vez só | A fala de "a chave serviu" |
| **On Opened** | Ao destrancar **e** toda vez que a cena carrega com o portão já aberto | Aparência: trocar o sprite, desligar um collider |
| **On Already Unlocked** | O jogador clicou em um portão já aberto | Ação: atravessar, ir para outra sala |

**Cuidado:** nunca ligue em **On Opened** algo que o jogador deveria iniciar. Ele dispara sozinho quando a cena carrega; uma troca de sala ali levaria o jogador embora sem ele clicar. Isso pertence a **On Already Unlocked**.

O componente `PlaceholderTint`, que pinta a porta de verde, é provisório: troque pela arte de porta aberta.

## Pan do cenário

O componente **Screen Pan Controller** desloca o objeto apontado em **Environment Container** quando o mouse encosta na borda. **Min X** e **Max X** são os limites; ajuste conforme a largura do fundo.

## Criar uma sala nova

Uma cena de sala contém **só o mundo**:

- **Uma câmera** com a tag `MainCamera`, ortográfica, com o componente **Point N Click Selector** (é ele que transforma o clique em interação).
- **O fundo** (um ou mais sprites).
- **Os objetos interativos**, montados como descrito em [Objeto interativo](#objeto-interativo).
- Opcional: o **Screen Pan Controller**, se o fundo for mais largo que a tela.

E **não** contém: managers, `Canvas` de jogo (diálogo, inventário) nem `EventSystem`. Dê Play direto na cena nova: a interface aparece sozinha e um objeto com **Interactable Dialogue Trigger** já abre a conversa.

O `Canvas_Debug` (botões Save, Load, Reset Session e Reload Scene) é ferramenta da cena de teste e não vai para uma sala.

*A preencher pelas issues #12 e #13:* como registrar a sala no jogo, como definir pontos de entrada e saídas.

## Personagens

*A preencher pela issue #10:* como criar um personagem, cadastrar retratos e expressões.

## Música e sons

*A preencher pelas issues #19 e #20.*
