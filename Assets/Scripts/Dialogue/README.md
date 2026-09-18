# Sistema de Diálogos

O sistema foi desenhado visando ser de fácil uso por Game Designers diretamente via Unity Editor (usando ScriptableObjects), possuir total desacoplamento da interface de usuário (UI) e integrar-se de forma nativa com o `MessageBroker` do projeto para disparos de eventos.

> Antes de mudar qualquer coisa aqui, leia as [Regras de comunicação](../Core/Messaging/README.md#regras-de-comunicação) e o [`ARCHITECTURE_ROADMAP.md`](../../../ARCHITECTURE_ROADMAP.md).

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

- **`DialogueStartedMessage`**: Publicada na transição **inativo → ativo**, logo antes do primeiro nó ser processado. **Não** é publicada ao encadear diálogos (`NextDialogueData` ou `TargetDialogue` de uma escolha): uma conversa, por mais encadeada que seja, dispara um único Started e um único Ended.
- **`DialogueEndedMessage`**: Publicada quando a conversa termina de verdade, ou seja, quando não há mais nós nem encadeamento pendente.
- **`DialogueLineMessage`**: Publicada sempre que uma nova fala deve ser apresentada na tela. Carrega quem está falando e o texto. A UI deve assinar essa mensagem para atualizar seus Textos/TextMeshPro.
- **`DialogueChoicesMessage`**: Publicada quando um nó exige uma decisão do jogador. Carrega uma lista em modo somente-leitura das escolhas possíveis.
- **`DialogueTriggerMessage`**: Publicada sempre que um gatilho é encontrado em um nó de diálogo ou ao selecionar uma escolha. Sistemas externos devem assinar para reagir (`ShakeScreen`, `PlayBGM`, etc.).

### 3. Camada de Lógica (Logic)
Localizada em `Assets/Scripts/Dialogue/Logic`, responsável pela máquina que processa e avança na história.

- **`DialogueController`**: Motor C# puro que navega na árvore de dados. Dispara mensagens no momento certo e decide se aguarda um input de escolha ou pode avançar sequencialmente.
- **`DialogueManager` (MonoBehaviour)**: Vive no prefab persistente `Assets/Prefabs/Resources/Managers.prefab`, criado uma única vez pelo `ManagersBootstrap` (módulo `GameFlow`) — **não** coloque um `DialogueManager` em cenas. Gerencia o `DialogueController` e expõe a API (`AdvanceDialogue`, `MakeChoice`, `StartDialogue`) para que a mecânica de Input do jogo consiga interagir.

### 4. Camada de Apresentação (UI)
Localizada em `Assets/Scripts/Dialogue/UI`.

- **`DialogueUIController`**: assina `DialogueLineMessage`, `DialogueChoicesMessage` e `DialogueEndedMessage` e liga/desliga a caixa, as escolhas e os textos. Dois campos opcionais (nulos em cenas antigas, sem erro):
  - `speakerNameplate`: escondido quando o `SpeakerName` da fala está vazio. Deixe o `SpeakerName` vazio para **narração**.
  - `continueIndicator`: o `>>`. Aparece a cada fala e some quando as escolhas abrem. É só um indicador, **não é clicável**: o clique esquerdo já avança o diálogo em qualquer lugar, e um `>>` clicável avançaria duas falas por clique (uma no press, pelo input, outra no release, pelo botão).
- **`DialogueChoiceButton`**: um por botão de escolha, com o `choiceIndex` e o `OnClick` do `Button` ligado a `OnClicked()`.

A arte fica em `Assets/UI/` (SVGs importados como **Textured Sprite** e desenhados com `Image` comum do uGUI). `Exemplo.svg` é o mockup de layout, não é usado em cena.

Não use o tipo "UI SVGImage": ele desenha a arte como malha de triângulos sem anti-aliasing, e as bordas ficam serrilhadas (nem o canvas Overlay nem o URP deste projeto suavizam). Como Textured Sprite, o importador rasteriza o SVG com **8 amostras por pixel** (com 4, aparecia uma emenda diagonal fina no preenchimento semitransparente da caixa).

**Cada textura tem exatamente o tamanho que o elemento ocupa na tela em 1080p** (caixa 1500×368, nameplate 230×72, escolha 560×120, `>>` 168×150). O importador de SVG não gera mipmaps, então uma textura maior que o elemento é *reduzida* na tela sem filtragem adequada e as bordas finas voltam a serrilhar — maior não é melhor. Por isso as escolhas usam `ChoicePill.svg`, uma cópia de `Nameplate.svg` rasterizada em 560×120: a mesma textura não fica nítida em 230 px e em 560 px ao mesmo tempo. **Se mudar o tamanho de um elemento na tela, mude o `Texture Size` do SVG para o mesmo valor.**

O pacote `com.unity.vectorgraphics` continua instalado mesmo sem usar `SVGImage`: é ele que fornece o Inspector de configurações de importação dos SVGs. Sem ele, os arquivos ainda importam, mas o Inspector mostra só um aviso e não dá para editar nada.

Para julgar nitidez, use o Game view em **Full HD (1920×1080)**, não "16:9 Aspect": este último renderiza no tamanho da janela, abaixo da resolução-alvo, e reduz toda a UI. A montagem de referência está em `Assets/Scenes/[Teste] Mecanicas.unity`, em `UI/Canvas_Game/DialogueUI`.

#### Dados inválidos nunca travam o jogo
`StartDialogue` retorna `bool`. Um `DialogueData` nulo ou sem nós loga um aviso (com o nome do asset) e retorna `false`, **sem publicar `DialogueStartedMessage`**. Como o `GameFlow` só entra em `DialogueState` ao ouvir o Started, o jogo continua em `GameplayState` com o input liberado em vez de ficar preso esperando um `DialogueEndedMessage` que nunca viria.

A mesma proteção vale no meio de uma conversa: se o `NextDialogueData` encadeado ou o `TargetDialogue` de uma escolha for inválido, o aviso é logado e o diálogo **encerra normalmente** (publicando `DialogueEndedMessage`), em vez de deixar o jogador travado.

---

## Fluxo de Uso e Integração

O funcionamento diário na Unity segue este roteiro:

### 1. Criando um Diálogo
1. Na janela Project da Unity, clique com botão direito: `Create -> Dialogue -> Dialogue Data`.
2. Adicione "Nodes" no Inspector e preencha os nomes, textos e opções de resposta.

### 2. Iniciando um Diálogo
Iniciar um diálogo é um **comando**, e comando vai por chamada direta ao dono, nunca pelo `MessageBroker`. Use o `DialogueManager.Instance` e conecte-o aos seus scripts de Interação ou de *Eventos de Cena*:
```csharp
// Retorna false (e loga um aviso) se o DialogueData for nulo ou não tiver nós.
DialogueManager.Instance.StartDialogue(meuDialogueData);
```
Em objetos de cena, o caminho pronto é o componente `InteractableDialogueTrigger` (módulo `GameFlow`), cujo método `TriggerDialogue()` é ligado ao `UnityEvent` `OnInteract` do `InteractableItem` no Inspector.

### 3. Conectando a UI (Desacoplada)
As falas são **notificações** ("há uma nova linha para mostrar"), então a UI se inscreve no `MessageBroker` em seu `OnEnable` em vez de ficar consultando o `DialogueManager` a cada frame. Se um dia a view precisar saber o *estado* do diálogo (e não só reagir a uma fala nova), aí sim ela lê o `DialogueManager` diretamente — ler estado de um manager é permitido pelas regras de comunicação.
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
Para arquitetura geral, é crucial entender que o Diálogo **é dono do seu próprio ciclo de vida** e **não dita regras de input**. Ele apenas anuncia o que aconteceu; quem decide o estado do jogo é o `GameFlow` (via `GameStateController`), que assina as duas notificações:
- O início publica a **`DialogueStartedMessage`**, *antes* da primeira fala aparecer. O `GameFlow` troca para o `DialogueState` e trava as interações do `PointNClick` pelo `PlayerInputGate`.
- O término (após o último nó, sem encadeamento pendente) publica a **`DialogueEndedMessage`**. O `GameFlow` detecta, volta pro `GameplayState` e destrava o input.

O módulo `Dialogue` não conhece o `GameFlow` nem o `PointNClick`, e a direção das dependências entre assemblies continua sendo `Core ← Dialogue ← GameFlow`.
