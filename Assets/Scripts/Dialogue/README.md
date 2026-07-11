# Sistema de Diálogos

O sistema foi desenhado visando ser de fácil uso por Game Designers diretamente via Unity Editor (usando ScriptableObjects), possuir total desacoplamento da interface de usuário (UI) e integrar-se de forma nativa com o `MessageBroker` do projeto para disparos de eventos.

---

## Arquitetura e Camadas

O sistema é dividido em três camadas lógicas: **Data**, **Messaging** e **Logic**.

### 1. Camada de Dados (Data)
Localizada em `Assets/Scripts/Dialogue/Data`, responsável por como a história é escrita e salva.

- **`DialogueData` (ScriptableObject)**: Representa uma cena, arquivo ou fase. Contém uma lista sequencial de diálogos (`DialogueNode`). Você pode encadear diálogos apontando para um próximo `DialogueData`.
- **`DialogueNode`**: Representa a fala de um personagem, armazenando o nome (`SpeakerName`), o texto (`Text`), e opcionalmente uma lista de escolhas para o jogador e/ou gatilhos de eventos.
- **`DialogueChoice`**: Representa uma opção que o jogador pode tomar. Pode carregar um novo `DialogueData` como resposta, e disparar eventos.
- **`DialogueTrigger`**: Define eventos (Triggers) que podem ocorrer tanto no início de um nó (Node) quanto na seleção de uma escolha. Consiste em uma string de Tipo (`TriggerType`) e um parâmetro opcional (`Parameter`).

### 2. Camada de Mensageria (Messaging)
Localizada em `Assets/Scripts/Dialogue/Messaging`, utiliza a interface genérica `IMessage` do `Core` para estabelecer a ponte de comunicação com o resto do jogo.

- **`DialogueLineMessage`**: Publicada sempre que uma nova fala deve ser apresentada na tela. Carrega quem está falando e o texto. A UI deve assinar essa mensagem para atualizar seus Textos/TextMeshPro.
- **`DialogueChoicesMessage`**: Publicada quando um nó exige uma decisão do jogador. Carrega uma lista em modo somente-leitura das escolhas possíveis.
- **`DialogueTriggerMessage`**: Publicada sempre que um gatilho é encontrado em um nó de diálogo ou ao selecionar uma escolha. Sistemas externos devem assinar para reagir (`ShakeScreen`, `PlayBGM`, etc.).

### 3. Camada de Lógica (Logic)
Localizada em `Assets/Scripts/Dialogue/Logic`, responsável pela máquina que processa e avança na história.

- **`DialogueController`**: Motor C# puro que navega na árvore de dados. Dispara mensagens no momento certo e decide se aguarda um input de escolha ou pode avançar sequencialmente.
- **`DialogueManager` (MonoBehaviour)**: Componente que deve viver na cena ou de forma global (`DontDestroyOnLoad`). Gerencia o `DialogueController` e expõe a API (`AdvanceDialogue`, `MakeChoice`, `StartDialogue`) para que a mecânica de Input do jogo consiga interagir.

---

## Fluxo de Uso e Integração

O funcionamento diário na Unity segue este roteiro:

### 1. Criando um Diálogo
1. Na janela Project da Unity, clique com botão direito: `Create -> Dialogue -> Dialogue Data`.
2. Adicione "Nodes" no Inspector e preencha os nomes, textos e opções de resposta.

### 2. Iniciando um Diálogo
Use o `DialogueManager.Instance` para dar início a uma conversa e conecte-o aos seus scripts de Interação ou de *Eventos de Cena*:
```csharp
DialogueManager.Instance.StartDialogue(meuDialogueData);
```

### 3. Conectando a UI (Desacoplada)
Seu script de Interface Gráfica não deve acessar o `DialogueManager` para puxar os textos. Ao invés disso, deve se inscrever no `MessageBroker` em seu `OnEnable`:
```csharp
void OnEnable() {
    MessageBroker.Subscribe<DialogueLineMessage>(OnNewLine);
    MessageBroker.Subscribe<DialogueChoicesMessage>(OnChoicesAvailable);
}

void OnNewLine(DialogueLineMessage msg) {
    nomeTexto.text = msg.SpeakerName;
    falaTexto.text = msg.Text;
}
```

### 4. Avançando o Diálogo (Inputs)
A passagem de diálogos pode ser vinculada ao Unity Input System chamando a API do Manager publicamente:
```csharp
// Quando o jogador apertar a tecla Espaço ou clicar com o mouse:
DialogueManager.Instance.AdvanceDialogue();

// Quando o jogador clicar no Botão 0 da UI de opções:
DialogueManager.Instance.MakeChoice(0);
```

### 5. Respondendo a Gatilhos (Triggers)
Qualquer sistema do jogo pode reagir a um evento do roteiro ouvindo os Triggers.
```csharp
void OnDialogueTrigger(DialogueTriggerMessage msg) {
    if (msg.TriggerType == "PlayBGM") {
        audioManager.PlayMusic(msg.Parameter);
    }
}
```

### 6. Integração com o GameFlow (Orquestrador)
Para arquitetura geral, é crucial entender que o Diálogo **não dita regras de input**. Quando o `DialogueManager` inicia ou encerra um diálogo, o módulo `GameFlow` (via `GameStateController`) está escutando:
- O início dispara a troca para o `DialogueState` e publica a `TogglePlayerInputMessage(false)`, que automaticamente trava interações do `PointNClick`.
- O término (após o último nó) envia a `DialogueEndedMessage`. O `GameFlow` detecta, volta pro `GameplayState` e destrava os inputs enviando `TogglePlayerInputMessage(true)`.
