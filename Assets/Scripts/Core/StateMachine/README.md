# State Machine

Máquina de estados que controla em qual **modo** o jogo está (explorando, em diálogo e, no futuro, inventário ou pausa). Fica em `Assets/Scripts/Core/StateMachine`.

> Antes de mudar qualquer coisa aqui, leia as [Regras de comunicação](../Messaging/README.md#regras-de-comunicação) e o [`ARCHITECTURE_ROADMAP.md`](../../../../ARCHITECTURE_ROADMAP.md).

---

## O que este sistema deliberadamente **não** tem

Isto é tão importante quanto o que ele tem, porque a versão anterior tinha tudo isso para servir dois estados vazios:

- **Sem fábricas e sem `Activator`.** Estados são criados com `new`. Construtores alcançados só por reflexão podem ser removidos pelo *managed stripping* do IL2CPP, e isso quebra **apenas na build**, nunca no Editor.
- **Sem `IStateMachine`.** Há uma implementação só; a interface só servia para o Inspector guardar um `MonoBehaviour` e fazer cast.
- **Sem registro prévio.** Não existe `RegisterState`: quem cria o estado é quem o usa.
- **Sem `MonoBehaviour`.** A máquina é C# puro; o dono repassa o `Update`.
- **Sem `StateChangedMessage`.** Troca de estado não é notificação de módulo cruzado; quem precisa saber é o próprio dono.
- **Sem estados hierárquicos nem tabelas de transição.** Uma Visual Novel não precisa disso.

---

## Scripts

### `BaseState.cs`
Classe base de qualquer modo. Expõe `Enter()`, `Update()` e `Exit()`, e dá acesso protegido à `StateMachine` para que um estado possa pedir uma transição.

**O estado é dono dos seus efeitos colaterais.** O que precisa valer enquanto ele estiver ativo é ligado no `Enter()` e desligado no `Exit()`. É por isso que `GameplayState.Enter()` libera o `PlayerInputGate` e `DialogueState.Enter()` o bloqueia: quem troca de estado não precisa lembrar de mexer no input, isso vem junto com o modo.

### `StateMachine.cs`
C# puro. API completa:

| Membro | O que faz |
|---|---|
| `CurrentState` | O modo ativo. |
| `ChangeState(next)` | Troca de modo: `Exit()` no atual, `Enter()` no novo. Ignora a chamada se `next` já for o estado atual. |
| `Push(overlay)` | Empilha o modo atual e entra em um modo sobreposto (inventário, pausa). |
| `Pop()` | Volta para o modo que estava embaixo. |
| `Tick()` | Repassa o `Update` para o estado ativo. O dono chama isto do seu próprio `Update`. |

`Push`/`Pop` dão `Exit`/`Enter` completos no modo de baixo. Isso é intencional: como os efeitos colaterais moram no `Enter`/`Exit`, a simetria garante que abrir e fechar um overlay deixa o jogo exatamente como estava.

---

## Como adicionar um novo estado

São dois passos, e nenhum deles envolve registrar nada:

1. Crie a classe:
   ```csharp
   public sealed class PauseState : BaseState
   {
       public PauseState(StateMachine stateMachine) : base(stateMachine) { }

       public override void Enter() => Time.timeScale = 0f;
       public override void Exit()  => Time.timeScale = 1f;
   }
   ```
2. No dono da máquina (hoje o `GameStateController`, no módulo `GameFlow`), crie a instância no `Awake` e troque para ela quando for o caso:
   ```csharp
   _pauseState = new PauseState(_stateMachine);
   // ...
   _stateMachine.Push(_pauseState);   // ou ChangeState, se não for um overlay
   ```
