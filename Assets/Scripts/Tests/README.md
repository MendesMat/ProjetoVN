# Tests

Este módulo é reservado para os testes automatizados do projeto (Testes de Unidade e Integração utilizando o Unity Test Framework).

---

## Estrutura e Práticas

- Por convenção, toda a arquitetura focada em desacoplamento (usando `MessageBroker`, `IStateMachine` e injeção leve) facilita muito os testes.
- **Mockando Mensagens:** Ao testar módulos isolados (ex: `Inventory`), você não precisa instanciar a `UI` ou o `GameFlow`. Basta emitir mensagens diretamente via `MessageBroker.Publish(...)` nos seus testes de unidade e verificar como o modelo responde (ex: `InventoryModel.Contains(...)`).
- A compilação deste módulo (`.asmdef`) normalmente restringe-se apenas ao ambiente de testes para que não suba junto com a build final do jogo.
