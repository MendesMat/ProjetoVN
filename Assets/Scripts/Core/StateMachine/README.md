# State Machine System

Este documento descreve o funcionamento e a arquitetura do sistema de máquina de estados (State Machine) localizado em `Assets/Scripts/Core/StateMachine`.

O sistema foi projetado para ser extensível, desacoplado e integrado ao sistema de mensagens do projeto.

---

## Fluxo de Funcionamento

O funcionamento da State Machine segue uma lógica de registro, criação sob demanda e execução de ciclo de vida:

1.  **Definição**: Novos estados são criados herdando de `BaseState`.
2.  **Registro**: As fábricas de estados são registradas no `StateMachine` via `RegisterState`.
3.  **Requisição**: Quando uma troca de estado é solicitada, o sistema usa `GetOrCreateState` para recuperar uma instância existente ou criar uma nova usando a fábrica registrada.
4.  **Transição**: O `ChangeState` encerra o estado atual (`Exit`), atualiza para o novo e inicia o novo estado (`Enter`).
5.  **Ciclo de Vida**: O `StateMachine` (como `MonoBehaviour`) repassa os eventos de `Update` e `FixedUpdate` da Unity para o estado ativo.
6.  **Comunicação**: Mudanças de estado notificam ouvintes através de eventos C# tradicionais e via `MessageBroker`.

---

## Scripts e Funcionalidades

### 1. BaseState.cs
**Papel**: Classe base abstrata para todos os estados do sistema.

-   **Funcionalidades**:
    -   Define os métodos virtuais de ciclo de vida: `Enter()`, `Update()`, `FixedUpdate()` e `Exit()`.
    -   Fornece acesso à `IStateMachine` para que os estados possam solicitar transições.
    -   Possui o evento `OnStateExit` disparado quando o estado termina.

### 2. IStateMachine.cs & StateMachine.cs
**Papel**: O núcleo do sistema que gerencia qual estado está ativo.

-   **IStateMachine**: Interface que define o contrato público (mudar estado, registrar fábricas, histórico).
-   **StateMachine (Implementação)**:
    -   **Cache de Estados**: Mantém um dicionário (`stateCache`) para reaproveitar instâncias de estados, evitando alocações desnecessárias.
    -   **Fábricas**: Gerencia um dicionário de `IStateFactory` para saber como instanciar cada tipo de estado.
    -   **Histórico (Stack)**: Permite salvar o estado anterior em uma pilha para suportar operações de "Voltar" (`PopState`).
    -   **Integração Unity**: Como herda de `MonoBehaviour`, ele é o responsável por chamar o `Update` e `FixedUpdate` do estado atual.
    -   **Notificação**: Dispara eventos e publica mensagens (`StateChangedMessage`) no `MessageBroker`.

### 3. IStateFactory.cs & StateFactory.cs
**Papel**: Define como os estados são instanciados.

-   **IStateFactory**: Interface simples para o padrão Factory.
-   **StateFactory<T>**: Implementação genérica que pode instanciar estados automaticamente via `Activator` ou usar um método customizado passado por parâmetro. Isso permite injeção de dependências nos estados se necessário.

### 4. StateController.cs
**Papel**: Componente de alto nível que orquestra o uso da State Machine em um contexto específico.

-   **Funcionalidades**:
    -   Atua como o "ponto de entrada" para configurar a State Machine no Inspector.
    -   **Registro de Fábricas**: No `Awake`, ele registra quais estados (`MenuState`, `InventoryState`, etc.) a State Machine deve conhecer.
    -   **Comandos de Transição**: Oferece métodos públicos como `OnOpenMenu()` ou `OnOpenInventory()` que facilitam a chamada de `ChangeState`.
    -   **Logs e Debug**: Escuta as mudanças de estado para logar a transição no Console da Unity, tanto via eventos diretos quanto via `MessageBroker`.

---

## Como Adicionar um Novo Estado

1.  Crie uma nova classe herdando de `BaseState`.
2.  No `StateController` (ou classe equivalente), registre a fábrica para esse novo tipo no método `RegisterFactories()`:
    ```csharp
    stateMachine.RegisterState<MeuNovoEstado>(new StateFactory<MeuNovoEstado>());
    ```
3.  Solicite a mudança para o novo estado quando necessário:
    ```csharp
    stateMachine.ChangeState(stateMachine.GetOrCreateState<MeuNovoEstado>());
    ```
