# Tests

Este módulo é reservado para os testes automatizados do projeto (Unity Test Framework).

> Antes de escrever testes, leia as [Regras de comunicação](../Core/Messaging/README.md#regras-de-comunicação) e o [`ARCHITECTURE_ROADMAP.md`](../../../ARCHITECTURE_ROADMAP.md).

Os testes vivem em `EditMode/` e rodam pela janela **Window → General → Test Runner**, aba *EditMode*.

- **`MessageBrokerTests`**: entrega, cancelamento de assinatura, assinatura duplicada, isolamento de exceções, `Clear`, e o comportamento de snapshot quando alguém assina durante um despacho.
- **`DialogueControllerTests`**: avanço sequencial, escolhas (com e sem diálogo-alvo), encadeamento publicando Started e Ended uma única vez, dados inválidos sendo rejeitados sem travar o jogo, e os **efeitos** (ordem de execução, efeito nulo, efeito que estoura, efeito reentrante).
- **`StoryFlagsTests`**: leitura/escrita, entradas inválidas e restauração a partir de um save.
- **`StoryFlagEffectsTests`**: `SetFlagEffect` e `ClearFlagEffect`.
- **`GameStateTests`**: round-trip do JSON, incluindo um save gravado antes do campo `storyFlagIds` existir.

> **Lacuna conhecida e deliberada:** `GiveItemEffect`, `RemoveItemEffect` e `LockedActionBehaviour`
> não têm teste de EditMode. Todos exigiriam um `InventoryManager` num `GameObject`, o que quebraria
> a regra "C# puro, sem GameObjects" desta suíte; o `LockedActionBehaviour` ainda por cima é um
> `MonoBehaviour` cujas asserções interessantes são sobre `UnityEvent` disparando, e a `Tests.asmdef`
> nem referencia `ProjetoVN.Inventory`. São verificados em Play Mode na cena `[Teste] Mecanicas`,
> como ARCH-04/08/09 também foram.
>
> A parte do `LockedActionBehaviour` que **mais** mereceria um teste automatizado é a ordem dos
> guardas: a flag é checada antes do item porque `IsSet` não consome nada e `TryUse` consome.
> Extrair isso para uma classe pura só para testar criaria mais superfície do que as quatro linhas
> que ela embrulharia, então a proteção é o comentário no código mais a asserção em Play Mode
> ("portão de flag não come a chave").

---

## Estrutura e Práticas

- **Teste a lógica em C# puro, direto.** `DialogueController`, `InventoryModel` e `InventoryService` não são `MonoBehaviour`: instancie com `new` e chame os métodos. É por isso que o projeto mantém essa separação, e é por isso que ele **não** precisa de interfaces nem de um container de injeção de dependência para ser testável.
- **Comandos e consultas se testam chamando o método**, não publicando mensagens. Publicar uma mensagem para provocar um comando testa o barramento, não o sistema.
- **Use o `MessageBroker` para verificar as notificações**: assine a mensagem que o sistema deveria publicar, execute a ação e confira o que chegou. Chame `MessageBroker.Clear()` no `SetUp` para que um teste não herde assinaturas de outro.
- **Todo estado estático precisa ser limpo no `SetUp` *e* no `TearDown`.** Vale para `MessageBroker.Clear()` e para `StoryFlags.ClearAll()`: sem isso, a ordem dos testes passa a importar e a suíte fica intermitente.
- **Para testar efeitos, faça um dublê herdando de `DialogueEffectSO`** (ver `SpyEffect` em `DialogueControllerTests`) em vez de montar o efeito real. Campos `[SerializeField] private` de um efeito real podem ser preenchidos com `SerializedObject`, já que este assembly é Editor-only.
- O `.asmdef` deste módulo é restrito ao ambiente de testes, então ele não entra na build final do jogo.

---

## Verificação em Play Mode (armadilha que já custou tempo)

O que não dá para cobrir em EditMode é verificado rodando o jogo pelo Editor conectado. Uma pegadinha
importante, descoberta na ARCH-22:

> **Se o Editor estiver sem foco — o caso normal quando ele é dirigido pela CLI — o Play Mode
> congela**, porque o Player Settings tem `runInBackground: 0`. Nenhum frame avança, então a Unity
> **nunca chama `Start()`, `Update()` nem continua corrotinas**. O sintoma engana: `SceneManager.LoadScene`
> ainda conclui e os objetos são recriados de verdade, então parece que o código de restauração é que
> está quebrado.
>
> Comece todo teste que dependa de frames com:
> ```csharp
> UnityEngine.Application.runInBackground = true;
> ```
> É uma mudança só de runtime: não suja o `ProjetoSettings.asset`. Para confirmar que está rodando,
> leia `UnityEngine.Time.frameCount` duas vezes com alguns segundos de intervalo — se o número não
> mudar, o jogo está parado e qualquer asserção sobre `Start()` é falso negativo.

Asserções feitas só com chamadas diretas de método (`door.Interact()`, `manager.Save()`) não dependem
de frames e funcionam mesmo com o jogo parado.
