# Audio

O som do jogo: a música de cada sala, com troca suave (#19); o som de ambiente em loop, os efeitos e os sons de interface, que o roteiro, os botões, a coleta e as portas pedem (#20).

**Depende de:** nada. O asmdef `ProjetoVN.Audio` não referencia outro módulo (D-01): quem precisa tocar som o enxerga, e não o contrário. Enxergam o `Audio` o `GameFlow` (comandos de roteiro e som da coleta), o `Dialogue` (som de avançar fala) e a `UI` (`ButtonSounds`), desde a #20.

## Os componentes

| Tipo | O que é | Onde mora |
|---|---|---|
| `AudioManager` | `MonoBehaviour` com `Instance`, o dono do som: música, ambiente, efeitos e sons de interface | `Managers.prefab` (raiz), com cinco `AudioSource` em três filhos: `Music` (2), `Ambience` (2) e `Effects` (1) |
| `CrossfadeChannel` | Classe interna, criada com `new`: duas `AudioSource`, uma sai enquanto a outra entra. É a regra de `PlayMusic`, usada duas vezes (música e ambiente) | Criada pelo `AudioManager` |
| `MusicCrossfade` | C# puro: os volumes de entrada e de saída de uma troca, por tempo decorrido | Criado pelo `CrossfadeChannel` |
| `RoomMusic` | Declaração da sala: o campo `music` com o `AudioClip` dela. Sem lógica | Um objeto `Room Music` em cada cena de sala |
| `SoundRegistry` | `ScriptableObject` somente leitura (D-02): três listas de `SoundEntry` (`id` + `AudioClip`), `music`, `ambience` e `effects`. É onde o roteiro acha o som pelo id | `Assets/Scripts/ScriptableObjects/Audio/SoundRegistry.asset` |
| `SoundId` | C# puro, `static`: `FollowsConvention(id)` (minúsculas sem acento, dígitos e `_`, D-22) | Usado pelo `SoundRegistry` e pelos testes |
| `SoundEffect` | Componente de cena: o campo `clip` e `Play()`, ligado por `UnityEvent` (D-07) a qualquer evento de qualquer objeto (`OnLocked`, `OnUnlocked`, `OnInteract`) | Um filho do objeto que soa |
| `InterfaceSound` | `enum`: `ButtonHover`, `ButtonClick`, `LineAdvance`, `ItemCollected`. Cada um é um campo de clipe do `AudioManager` | — |

O mixer do projeto é `Assets/Audio/ProjetoVN.mixer`, com os grupos `Music` e `SFX` sob o `Master`. As fontes da música saem no `Music`; as do ambiente e a dos efeitos, no `SFX` (nenhum grupo novo). O mixer e os grupos são criados à mão na janela Audio Mixer: não há API pública para isso.

Os arquivos de som ficam em `Assets/Audio/`: `Musica/`, `Ambiente/` e `Efeitos/`. Importação: música e ambiente com `Load Type` = `Streaming`, `Vorbis`, `Preload Audio Data` desligado; efeitos com `Decompress On Load`, `Vorbis` e `Preload Audio Data` ligado. Os sons de hoje (`Efeitos/` e `Ambiente/`) são **provisórios**, gerados por script; trocar pelo definitivo é substituir o arquivo mantendo o nome e o `.meta`.

## Como a música de uma sala é escolhida

1. O `AudioManager` assina o `SceneManager.sceneLoaded`. A cena que carrega é a sala nova, e ele procura o `RoomMusic` **nas raízes dessa cena** (não com `FindFirstObjectByType`, para não depender da ordem em que a sala antiga é descarregada).
2. Na primeira cena do jogo o `sceneLoaded` já disparou antes de o `Managers.prefab` existir; por isso o `Start` do manager aplica a música da cena ativa. É assim que o menu na abertura e o Play direto numa sala funcionam.
3. Sem `RoomMusic` na cena, com o campo vazio ou com o objeto desativado: **silêncio**, sem aviso e sem erro. Vazio é uma declaração válida.
4. A mesma passagem silencia o **ambiente** (`PlayAmbience(null)`): o som de fundo pedido pelo roteiro vale até a troca de sala, inclusive a volta ao menu e o carregar de um save.

Não há mensagem no `MessageBroker` (D-05): ninguém precisa ser avisado de que a música trocou, e a música atual é a propriedade `AudioManager.Instance.CurrentMusic`. Música e ambiente **não entram no save** (D-20): ao carregar, a sala carregada declara a sua música e o ambiente começa em silêncio.

## `PlayMusic(AudioClip clip)` e `PlayAmbience(AudioClip clip)`

São os comandos que o roteiro chama (por `SoundScriptActions`, no `GameFlow`). Os dois seguem as mesmas regras, porque são o mesmo `CrossfadeChannel`, na ordem:

1. `clip` igual ao atual (`CurrentMusic` ou `CurrentAmbience`): nada acontece. A mesma música entre duas salas (ou ao recarregar a sala), e o mesmo ambiente pedido duas vezes, **não reiniciam**.
2. Senão, a fonte que estava entrando passa a sair, a partir do volume em que está.
3. A outra fonte: se já toca o `clip` pedido (o jogador voltou no meio de uma troca), ela volta a entrar do volume e do ponto em que está, sem reiniciar. Senão recebe o `clip` e começa em volume 0. Com `clip` nulo, ela é parada e nada entra.
4. Começa um `MusicCrossfade` com os volumes de partida de cada fonte. Uma troca interrompida parte de onde a anterior parou, sem salto.

O `Update` avança os dois canais com `Time.unscaledDeltaTime` (D-06: sem `async`) e, no fim, para a fonte que saiu. A duração é o campo `crossfadeSeconds` da música (1 s) e `ambienceFadeSeconds` do ambiente (1 s); 0 = troca seca. As fontes do ambiente têm `Loop` ligado.

`PlayAmbience(null)` é o `<<parar_ambiente>>`: o som sai aos poucos e a fonte para; sem ambiente tocando, nada acontece.

### A curva

Potência constante: com `p` o progresso de 0 a 1, a entrada vale `de + (1 − de) · sin(p·π/2)` e a saída `de · cos(p·π/2)`. A soma das potências fica em 1; um crossfade linear entre duas faixas abre um "buraco" de volume no meio. A conta é independente do FPS, e um quadro longo termina a troca de uma vez, sem passar de 1 nem ficar abaixo de 0.

## Efeitos e sons de interface

- `PlayEffect(AudioClip clip)`: `PlayOneShot` na fonte `Effects`, então sons sobrepostos não se cortam. `clip == null`: nada, sem log. Grava `LastEffect` e soma 1 a `EffectCount`.
- `PlayInterfaceSound(InterfaceSound sound)`: escolhe o campo do manager (`buttonHoverSound`, `buttonClickSound`, `lineAdvanceSound`, `itemCollectedSound`) e chama `PlayEffect`. Campo vazio = silêncio.
- **`LastEffect` e `EffectCount` existem para a verificação e para o `SoundTests`**, porque o `isPlaying` não prova um `PlayOneShot` (medido na #20: um clipe de 2 s dá `isPlaying = True` no mesmo quadro e `False` cerca de 1 s depois, com o clipe ainda no meio). Estado consultável, não notificação (D-05).

Quem toca cada som:

| Som | Quem pede |
|---|---|
| Passar o mouse e clicar em um botão | `ButtonSounds` (`UI`), em todo `Button` |
| Completar ou passar uma fala | `DialogueManager.AdvanceDialogue` (`Dialogue`), só quando a fala é completada ou passa; não toca com a pausa, o histórico ou as opções na tela |
| Coletar item (inclusive o `<<dar_item>>`) | `ItemCollectedSound` (`GameFlow`), ouvinte do `ItemCollectedMessage` |
| Porta trancada, porta abrindo e qualquer objeto | `SoundEffect`, ligado por `UnityEvent` na cena |
| `<<tocar_musica>>`, `<<tocar_efeito>>`, `<<tocar_ambiente>>`, `<<parar_ambiente>>` | `SoundScriptActions` (`GameFlow`), com o `SoundRegistry` |

Nenhum som passa pelo `PlayerInputGate` (D-28) e nenhum entra no histórico de falas (D-30).

## A pausa

A pausa zera o `Time.timeScale`, que **não** para uma `AudioSource`: com a pausa aberta a música, o ambiente e os efeitos continuam tocando (decisão do programador na #19, mantida na #20), e o `PauseState` não sabe do `Audio`. Por isso os botões da pausa soam com o jogo congelado sem nada especial, e **não se usa `AudioListener.pause`**. Os fades usam `Time.unscaledDeltaTime`, como o `ScreenFader`, então uma troca em curso não fica parada no meio de uma pausa. O som de avançar fala é o que a pausa recusa: `DialogueManager.IsPaused` barra o avanço antes de qualquer som.

## Como estender

- **Pôr música numa sala:** veja [docs/autoria/salas.md](../../../docs/autoria/salas.md#música-e-sons). Sem código.
- **Um som novo para o roteiro:** uma linha no `SoundRegistry` (lista certa) e uma linha na tabela "Sons que existem" de [roteiro.md](../../../docs/autoria/roteiro.md#música-e-sons). O teste de conteúdo confere o resto.
- **Um som em uma porta ou objeto:** um filho com `SoundEffect`, ligado ao evento (veja [salas.md](../../../docs/autoria/salas.md#som-em-um-objeto-ou-em-uma-porta-20)).
- **Um botão novo:** leva o `ButtonSounds` (o teste reprova o que esquecer).
- **Tocar por código:** `AudioManager.Instance.PlayMusic/PlayAmbience/PlayEffect(clip)`. Quem chama confere o `Instance` e loga um erro com contexto (D-04).

## Armadilhas

- **O nome `Audio` colide com o namespace da Unity.** Dentro de `ProjetoVN.*`, `Audio.AudioMixerGroup` resolve para `ProjetoVN.Audio`. Use `using UnityEngine.Audio;` com o nome curto.
- **O `AudioManager` não entra em cena** (regra 6): ele mora só no `Managers.prefab`, com as fontes dele. O `RoomMusic` e o `SoundEffect` entram, são declarações da cena. Não ponha um `AudioListener` no `Managers`: cada cena já tem o seu, na câmera.
- **No máximo um `RoomMusic` por cena** (um teste do `Tests` reprova duas).
- **Um `UnityEvent` guarda o tipo por nome:** não renomeie nem mova o `SoundEffect` depois de ligar (`ProjetoVN.Audio.SoundEffect, ProjetoVN.Audio`).
- **Loop sem estalo:** as fontes da música e do ambiente têm `loop` ligado; a faixa precisa fechar num ciclo completo (sem corte no meio de uma onda) ou a emenda estala. Num WAV gerado por script, use duração e frequências inteiras (o ruído do ônibus fecha o ciclo por filtro circular).
- **Os testes de música leem só o filho `Music`:** o `MusicTests.MusicSources()` restringe as fontes ao filho `Music`, senão um ambiente tocando quebraria o `PlayingSources().Single()`.
- **Um id de som que não existe** loga erro com o comando, o id e o nó (`SoundScriptActions`) e a conversa segue. Um comando com o número errado de parâmetros trava a conversa; o `ScriptSoundReferences` (testes) o reprova.
- **Com a janela sem foco** o som continua (D-31).
