# Inventory (Inventário)

O módulo **Inventory** gerencia os itens coletáveis do jogo, mantendo a lógica de dados separada da interface visual (UI) e da interação na cena.

> Antes de mudar qualquer coisa aqui, leia as [Regras de comunicação](../Core/Messaging/README.md#regras-de-comunicação) e o [`ARCHITECTURE_ROADMAP.md`](../../../ARCHITECTURE_ROADMAP.md).

---

## Estrutura Principal

- **`ItemDataSO` (ScriptableObject)**: A representação autorada de um item (id, nome, ícone, descrição). Criado por Game Designers no Editor da Unity, e **somente leitura em runtime**.
- **`InventoryModel`**: Guarda a lista de `ItemDataSO` que o jogador possui. Não copia os dados do item: guarda a referência ao próprio asset, então não existe uma segunda fonte de verdade para divergir.
- **`InventoryService`**: As regras. Só publica uma notificação quando o estado realmente mudou.
- **`InventoryManager` (MonoBehaviour)**: A API do inventário para o resto do jogo.
- **`CollectableItemBehaviour`** e **`LockedActionBehaviour`**: Utilitários de cena, ligados aos objetos pelo `UnityEvent` `OnInteract` do `InteractableItem`.

---

## A API (comandos e consultas são chamadas diretas)

Coletar, usar e consultar têm **um único dono**, o `InventoryManager`. Por isso são chamadas de método, não mensagens. O `MessageBroker` entra só depois, para **notificar** que algo mudou.

```csharp
InventoryManager.Instance.Items;             // IReadOnlyList<ItemDataSO>, a fonte de verdade
InventoryManager.Instance.HasItem(item);     // bool
InventoryManager.Instance.Collect(item);     // bool: false se nulo ou já possuído
InventoryManager.Instance.TryUse(item);      // bool: false se o jogador não tem o item
```

Todos recebem `ItemDataSO`, nunca uma string de id. Quem chama deve tratar o `null` do singleton (logando um erro), como fazem os dois behaviours da cena.

**Duplicatas:** o mesmo item não entra duas vezes. `Collect` retorna `false` e não publica nada. Se um dia existirem quantidades, o lugar de modelá-las é um tipo de entrada (item + quantidade) dentro do `InventoryModel`, e não uma lista com repetições.

---

## Fluxo de Interações e Mensagens

### 1. Coletando um Item
1. O jogador clica no objeto (módulo `PointNClick`), cujo `OnInteract` chama `CollectableItemBehaviour.Collect()`.
2. O behaviour chama `InventoryManager.Instance.Collect(itemData)` e desativa o GameObject.
3. Se o item entrou de fato, o `InventoryService` publica **`ItemCollectedMessage`** (carregando o `ItemDataSO`).
   - *Consequência:* o `InventoryPresenter` (módulo `UI`) adiciona o slot correspondente.

### 2. Usando um Item (porta trancada)
1. O jogador clica na porta, cujo `OnInteract` chama `LockedActionBehaviour.Interact()`.
2. O behaviour chama `InventoryManager.Instance.TryUse(requiredItem)`.
3. **Se retornou `true`**, o item foi consumido e o `InventoryService` publicou **`ItemUsedMessage`**; o behaviour dispara `OnUnlocked`.
   **Se retornou `false`**, o jogador não tinha o item: nada foi consumido, nada foi publicado, e o behaviour dispara `OnLocked`.

Consultar antes com `HasItem` seria um segundo lookup sem nada a ganhar: `TryUse` falha exatamente quando o jogador não tem o item, então ele já decide os dois caminhos. `OnUnlocked` nunca dispara sem o consumo ter acontecido.

### 3. Consultas de Estado
Sistemas da cena que precisem bifurcar conforme os itens do jogador chamam `InventoryManager.Instance.HasItem(item)` **diretamente**. Nunca faça pergunta pelo `MessageBroker`: com 0 ouvintes ela falha em silêncio, e com 2 ela responde duas vezes.

O módulo `Dialogue` é o único caso que não pode fazer essa chamada, porque o seu assembly não referencia `Inventory` (e não deve passar a referenciar). A solução planejada para isso é uma interface pequena de posse do próprio `Dialogue` — ver ARCH-17 no roadmap, ainda não implementado.
