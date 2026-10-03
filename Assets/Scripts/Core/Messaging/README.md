# Sistema de Mensageria (Messaging System)

Este sistema provê comunicação entre componentes do projeto usando o padrão **Publisher/Subscriber (Pub/Sub)**, para que partes do código possam **avisar** que algo aconteceu sem conhecer quem está ouvindo.

> **Antes de mudar qualquer coisa aqui:** [regras de comunicação](../../../../docs/arquitetura/visao-geral.md#regras-de-comunicação) · [decisões](../../../../docs/arquitetura/decisoes.md) · [fluxo de trabalho](../../../../docs/agentes/fluxo-de-trabalho.md)

---

## O que passa por este barramento

**Só notificações:** "X aconteceu", com zero ou mais ouvintes, cruzando módulos. Comando, consulta e estado **não** passam por aqui (decisão D-05). A tabela completa, que vale para o projeto inteiro, está nas [regras de comunicação](../../../../docs/arquitetura/visao-geral.md#regras-de-comunicação).

O erro que essa regra existe para evitar: uma consulta em pub/sub falha em silêncio com 0 ouvintes e responde duas vezes com 2, e um estado enviado como evento nunca chega a quem ainda não existia.

---

## Arquitetura e Scripts

O sistema é composto por três pilares principais localizados nesta pasta:

### 1. [IMessage.cs]
- **Funcionalidade**: Interface de marcação (*marker interface*).
- **Detalhes**: Atua como um contrato. Qualquer classe ou struct que deseje ser enviada como uma mensagem através do sistema **deve** implementar esta interface. Ela não possui métodos, servindo apenas para tipagem forte e segurança no `MessageBroker`.

### 2. [MessageBroker.cs]
- **Funcionalidade**: Gerenciador central de mensagens.
- **Detalhes**: É uma classe estática que atua como o intermediário (broker). Os handlers de cada tipo de mensagem ficam guardados em um *holder* genérico próprio (`Handlers<T>`), fortemente tipado, sem conversões nem boxing.
- **Principais Funcionalidades**:
    - **Subscribe**: Permite que um objeto declare interesse em um tipo de mensagem. É **idempotente**: assinar o mesmo handler duas vezes registra uma única entrada, então um único `Unsubscribe` sempre desfaz a assinatura.
    - **Unsubscribe**: Remove o interesse de um objeto, evitando que funções sejam chamadas em objetos que já foram destruídos (importante para evitar *Memory Leaks* na Unity).
    - **Publish**: Dispara a mensagem para todos os ouvintes registrados, **isolando exceções**: se um handler estourar, o erro é logado com `Debug.LogException` e os demais handlers continuam recebendo a mensagem.
    - **Clear**: Limpa todos os registros. Também é chamado automaticamente no início de cada Play Mode (veja "Garantias" abaixo). `Clear<T>()` limpa apenas um tipo de mensagem.

### 3. Mensagens concretas
As mensagens vivem no módulo que as **publica**, não aqui. Exemplos em uso:
`Dialogue/Messaging/DialogueStartedMessage.cs`, `DialogueEndedMessage`, e
`Inventory/Messages/ItemCollectedMessage.cs`, `ItemUsedMessage`,
`InventoryReplacedMessage`. Todas são `readonly struct` implementando `IMessage`, e todas descrevem
algo que **já aconteceu**.

> **Uma mensagem sem assinante é um bug esperando acontecer.** A `DialogueTriggerMessage` foi publicada
> por meses com zero ouvintes: o roteiro "disparava" gatilhos que não faziam nada, sem erro nenhum.
> Ela foi removida e substituída por chamada direta, porque
> aquilo era um **comando com dono**, não uma notificação. Antes de criar uma mensagem nova, confirme
> que existe quem a escute — e que mais de um sistema pode legitimamente querer escutá-la.

---

## Fluxo de Funcionamento

O fluxo de comunicação segue uma ordem lógica de eventos:

### 1. Preparação (Definição)
Define-se uma `readonly struct` que implementa `IMessage`, no módulo que vai publicá-la. Ela deve conter todas as informações necessárias para quem for recebê-la, e o nome deve estar no passado ("aconteceu"), não no imperativo.
*Exemplo: `ItemCollectedMessage`.*

### 2. Contratação (Subscribe)
Um componente interessado (Ouvinte) se registra no `MessageBroker`. Geralmente isso é feito no `OnEnable` (Unity).
```csharp
void OnEnable() {
    MessageBroker.Subscribe<ItemCollectedMessage>(MinhaFuncaoDeResposta);
}
```

### 3. Gatilho (Publish)
Um evento acontece no jogo e o componente responsável (Emissor) "grita" para o sistema que algo mudou, sem saber quem está ouvindo.
```csharp
MessageBroker.Publish(new ItemCollectedMessage(item));
```

### 4. Reação
O `MessageBroker` recebe a mensagem e a entrega imediatamente (de forma síncrona, na mesma pilha de chamada de quem publicou) para todos que fizeram o "Subscribe" anteriormente, executando suas respectivas funções.

### 5. Finalização (Unsubscribe)
O ouvinte deve sempre se remover do registro quando não for mais necessário (geralmente no `OnDisable` ou `OnDestroy`).
```csharp
void OnDisable() {
    MessageBroker.Unsubscribe<ItemCollectedMessage>(MinhaFuncaoDeResposta);
}
```

---

## Garantias do Broker

Estas garantias existem para que um erro em um único assinante nunca trave o jogo inteiro. Conte com elas ao escrever handlers:

- **Isolamento de exceções**: um handler que estoura não interrompe os outros nem propaga a exceção para quem publicou. A exceção é sempre logada, nunca engolida em silêncio.
- **Snapshot durante o despacho**: assinar ou cancelar a assinatura *dentro* de um handler é seguro. A lista de handlers é substituída (copy-on-write) em vez de mutada, então o `Publish` em andamento termina de percorrer o conjunto que existia quando começou. Consequência: um handler removido durante o despacho ainda recebe a mensagem em voo.
- **Ordem não garantida**: na prática a ordem é a de inscrição, mas **nenhum handler deve assumir** que outro rodou antes ou depois dele.
- **Assinatura idempotente**: `Subscribe` duas vezes com o mesmo handler registra apenas uma entrada.
- **Reset automático**: um `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` chama `Clear()` no início de cada Play Mode, então o broker começa vazio mesmo com "Enter Play Mode Options → no domain reload" ligado. Isso não dispensa o `Unsubscribe`, que continua obrigatório durante a execução.
- **Rastreamento opcional**: com o define `VN_TRACE_MESSAGES`, cada `Publish` loga o tipo da mensagem e quantos handlers a receberam, e a `StateMachine` loga cada troca de estado.

### Por que o console fica quieto por padrão
Os módulos **não** logam o próprio fluxo. Não existe "→ entrando em diálogo", "← mensagem recebida" ou "nó 3 processado" em lugar nenhum: esse rastreamento inteiro mora atrás do define acima, em um único lugar. O console em Play Mode mostra então só o que exige atenção — avisos e erros — em vez de enterrá-los em ruído, e as chamadas de log não alocam strings em build.

Para ligar o rastreamento: **Project Settings → Player → Scripting Define Symbols**, adicione `VN_TRACE_MESSAGES`.

Avisos e erros continuam sendo logados normalmente e, quando quem loga é um componente ou asset, passam `this` como contexto (`Debug.LogWarning(msg, this)`), para que clicar na mensagem selecione o objeto culpado na Hierarchy ou no Project.

---

## Benefícios deste Fluxo
- **Desacoplamento**: O emissor da mensagem não precisa de uma referência para o ouvinte.
- **Escalabilidade**: Você pode adicionar novos ouvintes sem alterar o código de quem envia a mensagem.
- **Organização**: Facilita a comunicação entre sistemas complexos como UI, Áudio e Lógica de Jogo.

Esses benefícios valem **para notificações**. Para comandos e consultas, o barramento só troca uma chamada legível por um salto indireto, sem ganho de desacoplamento quando as duas pontas já compilam no mesmo assembly. Veja as [regras de comunicação](../../../../docs/arquitetura/visao-geral.md#regras-de-comunicação).
