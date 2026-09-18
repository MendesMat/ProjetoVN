# Tests

Este módulo é reservado para os testes automatizados do projeto (Unity Test Framework).

> Antes de escrever testes, leia as [Regras de comunicação](../Core/Messaging/README.md#regras-de-comunicação) e o [`ARCHITECTURE_ROADMAP.md`](../../../ARCHITECTURE_ROADMAP.md).

Os testes vivem em `EditMode/` e rodam pela janela **Window → General → Test Runner**, aba *EditMode*.

- **`MessageBrokerTests`**: entrega, cancelamento de assinatura, assinatura duplicada, isolamento de exceções, `Clear`, e o comportamento de snapshot quando alguém assina durante um despacho.
- **`DialogueControllerTests`**: avanço sequencial, escolhas (com e sem diálogo-alvo), encadeamento publicando Started e Ended uma única vez, e dados inválidos sendo rejeitados sem travar o jogo.

---

## Estrutura e Práticas

- **Teste a lógica em C# puro, direto.** `DialogueController`, `InventoryModel` e `InventoryService` não são `MonoBehaviour`: instancie com `new` e chame os métodos. É por isso que o projeto mantém essa separação, e é por isso que ele **não** precisa de interfaces nem de um container de injeção de dependência para ser testável.
- **Comandos e consultas se testam chamando o método**, não publicando mensagens. Publicar uma mensagem para provocar um comando testa o barramento, não o sistema.
- **Use o `MessageBroker` para verificar as notificações**: assine a mensagem que o sistema deveria publicar, execute a ação e confira o que chegou. Chame `MessageBroker.Clear()` no `SetUp` para que um teste não herde assinaturas de outro.
- O `.asmdef` deste módulo é restrito ao ambiente de testes, então ele não entra na build final do jogo.
