# Audio

A música do jogo: cada sala declara a sua, e a troca de uma para outra é suave. Efeitos sonoros e comandos de roteiro (`<<tocar_musica>>`) são da #20.

**Depende de:** nada. O asmdef `ProjetoVN.Audio` não referencia outro módulo (D-01): quem precisar tocar som (a #20 pelo `GameFlow`, a interface) enxerga o `Audio`, e não o contrário.

## Os componentes

| Tipo | O que é | Onde mora |
|---|---|---|
| `AudioManager` | `MonoBehaviour` com `Instance`, o dono da música: toca a da sala e faz a troca suave | `Managers.prefab` (raiz), com duas `AudioSource` no filho `Music` |
| `RoomMusic` | Declaração da sala: o campo `music` com o `AudioClip` dela. Sem lógica | Um objeto `Room Music` em cada cena de sala |
| `MusicCrossfade` | C# puro: os volumes de entrada e de saída de uma troca, por tempo decorrido | Criado pelo `AudioManager` |

O mixer do projeto é `Assets/Audio/ProjetoVN.mixer`, com os grupos `Music` e `SFX` sob o `Master`. As duas `AudioSource` do `Managers.prefab` saem no grupo `Music`; o `SFX` fica sem uso até a #20. O mixer e os grupos são criados à mão na janela Audio Mixer: não há API pública para isso.

## Como a música de uma sala é escolhida

1. O `AudioManager` assina o `SceneManager.sceneLoaded`. A cena que carrega é a sala nova, e ele procura o `RoomMusic` **nas raízes dessa cena** (não com `FindFirstObjectByType`, para não depender da ordem em que a sala antiga é descarregada).
2. Na primeira cena do jogo o `sceneLoaded` já disparou antes de o `Managers.prefab` existir; por isso o `Start` do manager aplica a música da cena ativa. É assim que o menu na abertura e o Play direto numa sala funcionam.
3. Sem `RoomMusic` na cena, com o campo vazio ou com o objeto desativado: **silêncio**, sem aviso e sem erro. Vazio é uma declaração válida.

Não há mensagem no `MessageBroker` (D-05): ninguém precisa ser avisado de que a música trocou, e a música atual é a propriedade `AudioManager.Instance.CurrentMusic`. A música **não entra no save** (D-20): ao carregar, a sala carregada declara a sua.

## `PlayMusic(AudioClip clip)`

É o comando que a #20 vai chamar. As regras, na ordem:

1. `clip` igual ao `CurrentMusic`: nada acontece. A mesma música entre duas salas (ou ao recarregar a sala) **não reinicia**.
2. Senão, a fonte que estava entrando passa a sair, a partir do volume em que está.
3. A outra fonte: se já toca o `clip` pedido (o jogador voltou no meio de uma troca), ela volta a entrar do volume e do ponto em que está, sem reiniciar. Senão recebe o `clip` e começa em volume 0. Com `clip` nulo, ela é parada e nada entra.
4. Começa um `MusicCrossfade` com os volumes de partida de cada fonte. Uma troca interrompida parte de onde a anterior parou, sem salto.

O `Update` avança o crossfade com `Time.unscaledDeltaTime` e, no fim, para a fonte que saiu.

### A curva

Potência constante: com `p` o progresso de 0 a 1, a entrada vale `de + (1 − de) · sin(p·π/2)` e a saída `de · cos(p·π/2)`. A soma das potências fica em 1; um crossfade linear entre duas músicas abre um "buraco" de volume no meio. A duração é o campo `crossfadeSeconds` do manager (1 s de partida; 0 = troca seca). A conta é independente do FPS, e um quadro longo termina a troca de uma vez, sem passar de 1 nem ficar abaixo de 0.

## A pausa

A pausa zera o `Time.timeScale`, que **não** para uma `AudioSource`: com a pausa aberta a música continua tocando (decisão do programador no levantamento da #19), e o `PauseState` não sabe do `Audio`. O crossfade usa `Time.unscaledDeltaTime`, como o `ScreenFader`, então uma troca em curso não fica parada no meio de uma pausa. Quem quiser calar os sons com a pausa (a #20, para os sons de interface da pausa) usa `AudioListener.pause` com `ignoreListenerPause` nas fontes que precisam soar.

## Como estender

- **Pôr música numa sala:** veja [docs/autoria/salas.md](../../../docs/autoria/salas.md#música-e-sons). Sem código.
- **Tocar uma música por código ou roteiro (#20):** `AudioManager.Instance.PlayMusic(clip)`. Quem chama confere o `Instance` e loga um erro com contexto (D-04).
- **Um efeito sonoro novo (#20):** o grupo `SFX` do mixer está pronto para receber as fontes.

## Armadilhas

- **O nome `Audio` colide com o namespace da Unity.** Dentro de `ProjetoVN.*`, `Audio.AudioMixerGroup` resolve para `ProjetoVN.Audio`. Use `using UnityEngine.Audio;` com o nome curto.
- **O `AudioManager` não entra em cena** (regra 6): ele mora só no `Managers.prefab`. O `RoomMusic` entra, é a declaração da sala. Não ponha um `AudioListener` no `Managers`: cada cena já tem o seu, na câmera.
- **No máximo um `RoomMusic` por cena** (um teste do `Tests` reprova duas).
- **Importação da música:** `Load Type` = `Streaming`, `Compression Format` = `Vorbis`, `Preload Audio Data` desligado. A cena guarda só a referência, e a faixa inteira não vai para a memória.
- **Loop sem estalo:** o `AudioSource` da música tem `loop` ligado; a faixa precisa fechar num ciclo completo (sem corte no meio de uma onda) ou a emenda estala.
- **Com a janela sem foco** a música continua (D-31).
