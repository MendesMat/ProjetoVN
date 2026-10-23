# State Machine

Máquina de estados que controla em qual **modo** o jogo está (explorando, em diálogo, trocando de sala ou em pausa, que é sobreposta). Fica em `Assets/Scripts/Core/StateMachine`.

> **Antes de mudar qualquer coisa aqui:** [regras de comunicação](../../../../docs/arquitetura/visao-geral.md#regras-de-comunicação) · [decisões](../../../../docs/arquitetura/decisoes.md) · [fluxo de trabalho](../../../../docs/agentes/fluxo-de-trabalho.md)

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

`Push`/`Pop` dão `Exit`/`Enter` completos no modo de baixo. Isso é intencional: como os efeitos colaterais moram no `Enter`/`Exit`, a simetria garante que abrir e fechar um overlay deixa o jogo exatamente como estava. A pausa (#18) é o primeiro overlay: o `PauseState` cobre a exploração ou a conversa.

**`ChangeState` não conhece a pilha de overlays:** trocar de modo com um overlay aberto sai do overlay e deixa o modo coberto empilhado. Quem usa um overlay garante que nada troca o modo de baixo enquanto ele está aberto (a pausa o faz congelando o tempo e bloqueando o diálogo). Mudar isso mudaria o contrato para todos os modos.

### Testes

`StateMachineTests` (EditMode, #18; **caracterização**, a máquina não tinha teste): `Push` dá `Exit` no coberto e `Enter` no overlay, nessa ordem; `Pop` dá `Exit` no overlay e `Enter` no coberto; `Pop` sem nada empilhado avisa e mantém o modo; `ChangeState` para o modo atual não reentra nele.

---

## Como adicionar um novo estado

São dois passos, e nenhum deles envolve registrar nada:

1. Crie a classe (o exemplo é uma versão enxuta do `PauseState` real, que também fecha o `PlayerInputGate`, bloqueia o diálogo e devolve o `timeScale` que **guardou**, e não um `1` fixo):
   ```csharp
   public sealed class PauseState : BaseState
   {
       private float _timeScaleBeforePause = 1f;

       public PauseState(StateMachine stateMachine) : base(stateMachine) { }

       public override void Enter()
       {
           _timeScaleBeforePause = Time.timeScale;
           Time.timeScale = 0f;
       }

       public override void Exit() => Time.timeScale = _timeScaleBeforePause;
   }
   ```
2. No dono da máquina (hoje o `GameStateController`, no módulo `GameFlow`), crie a instância no `Awake` e troque para ela quando for o caso:
   ```csharp
   _pauseState = new PauseState(_stateMachine);
   // ...
   _stateMachine.Push(_pauseState);   // ou ChangeState, se não for um overlay
   ```
