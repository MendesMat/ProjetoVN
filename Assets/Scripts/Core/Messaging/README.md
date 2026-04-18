# Sistema de Mensageria (Messaging System)

Este sistema provê uma forma desacoplada de comunicação entre diferentes componentes do projeto, utilizando o padrão **Publisher/Subscriber (Pub/Sub)**. Isso permite que partes do código interajam sem precisar conhecer diretamente a existência uma da outra.

## Arquitetura e Scripts

O sistema é composto por três pilares principais localizados nesta pasta:

### 1. [IMessage.cs]
- **Funcionalidade**: Interface de marcação (*marker interface*).
- **Detalhes**: Atua como um contrato. Qualquer classe ou struct que deseje ser enviada como uma mensagem através do sistema **deve** implementar esta interface. Ela não possui métodos, servindo apenas para tipagem forte e segurança no `MessageBroker`.

### 2. [MessageBroker.cs]
- **Funcionalidade**: Gerenciador central de mensagens.
- **Detalhes**: É uma classe estática que atua como o intermediário (broker). Ela mantém dicionários internos para mapear quais funções (*handlers*) estão interessadas em quais tipos de mensagens.
- **Principais Funcionalidades**:
    - **Subscribe**: Permite que um objeto declare interesse em um tipo de mensagem. Internamente, ele cria um *wrapper* para lidar com a conversão de tipos de forma segura.
    - **Unsubscribe**: Remove o interesse de um objeto, evitando que funções sejam chamadas em objetos que já foram destruídos (importante para evitar *Memory Leaks* na Unity).
    - **Publish**: Dispara a mensagem para todos os ouvintes registrados.
    - **Clear**: Limpa todos os registros, útil para limpeza de memória entre transições de cenas.

### 3. [Messages/StateChangedMessage.cs]
- **Funcionalidade**: Exemplo de implementação de mensagem.
- **Detalhes**: Uma `readonly struct` que transporta dados sobre mudanças de estado.
- **Dados**:
    - `Previous`: O estado do qual a máquina está saindo.
    - `Next`: O estado para o qual a máquina está indo.

---

## Fluxo de Funcionamento

O fluxo de comunicação segue uma ordem lógica de eventos:

### 1. Preparação (Definição)
Define-se uma estrutura de dados que implementa `IMessage`. Ela deve conter todas as informações necessárias para quem for recebê-la.
*Exemplo: `StateChangedMessage`.*

### 2. Contratação (Subscribe)
Um componente interessado (Ouvinte) se registra no `MessageBroker`. Geralmente isso é feito no `OnEnable` (Unity).
```csharp
void OnEnable() {
    MessageBroker.Subscribe<StateChangedMessage>(MinhaFuncaoDeResposta);
}
```

### 3. Gatilho (Publish)
Um evento acontece no jogo e o componente responsável (Emissor) "grita" para o sistema que algo mudou, sem saber quem está ouvindo.
```csharp
MessageBroker.Publish(new StateChangedMessage(estadoAntigo, novoEstado));
```

### 4. Reação
O `MessageBroker` recebe a mensagem e a entrega imediatamente para todos que fizeram o "Subscribe" anteriormente, executando suas respectivas funções.

### 5. Finalização (Unsubscribe)
O ouvinte deve sempre se remover do registro quando não for mais necessário (geralmente no `OnDisable` ou `OnDestroy`).
```csharp
void OnDisable() {
    MessageBroker.Unsubscribe<StateChangedMessage>(MinhaFuncaoDeResposta);
}
```

---

## Benefícios deste Fluxo
- **Desacoplamento**: O emissor da mensagem não precisa de uma referência para o ouvinte.
- **Escalabilidade**: Você pode adicionar novos ouvintes sem alterar o código de quem envia a mensagem.
- **Organização**: Facilita a comunicação entre sistemas complexos como UI, Áudio e Lógica de Jogo.
