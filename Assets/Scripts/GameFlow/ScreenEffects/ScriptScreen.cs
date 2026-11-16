using System;
using System.Threading;
using ProjetoVN.Core.Messaging;
using ProjetoVN.Dialogue.Messaging;
using UnityEngine;
using UnityEngine.UI;

namespace ProjetoVN.GameFlow.ScreenEffects
{
    /// <summary>
    /// A camada de tela que o roteiro comanda: uma imagem de tela cheia e, por cima dela, um escuro. Vive no
    /// <c>GameUI.prefab</c>, no <c>Canvas_Game</c>, entre o inventário e a tela de personagens. Não decide nada: quem
    /// manda são os comandos de roteiro (<see cref="ScreenScriptActions"/>) e o <c>RoomNavigator</c>. Anima com o tempo
    /// escalado, então a pausa congela o efeito. Quando a conversa termina, o que ficou na tela some.
    /// </summary>
    public sealed class ScriptScreen : MonoBehaviour
    {
        [Tooltip("A imagem de tela cheia (objeto Picture, inativo enquanto não há imagem). Vazio = <<mostrar_imagem>> não funciona.")]
        [SerializeField] private Image pictureImage;

        [Tooltip("O CanvasGroup do objeto Picture: o alfa da imagem. Vazio = <<mostrar_imagem>> e <<esconder_imagem>> não funcionam.")]
        [SerializeField] private CanvasGroup pictureGroup;

        [Tooltip("O CanvasGroup do objeto Darkness (imagem preta de tela cheia): o alfa do escuro. " +
                 "Vazio = <<escurecer>> e <<clarear>> não funcionam.")]
        [SerializeField] private CanvasGroup darknessGroup;

        [Tooltip("Segundos que o escuro e a imagem levam para sumir quando a conversa termina. 0 = somem na hora. " +
                 "0,4 é o tempo do clarear da troca de sala.")]
        [Min(0f)]
        [SerializeField] private float clearSecondsWhenDialogueEnds = 0.4f;

        private FadingLayer _pictureLayer;
        private FadingLayer _darknessLayer;

        private void Awake()
        {
            _pictureLayer = new FadingLayer(pictureGroup);
            _darknessLayer = new FadingLayer(darknessGroup);
        }

        private void OnEnable() => MessageBroker.Subscribe<DialogueEndedMessage>(OnDialogueEnded);

        private void OnDisable() => MessageBroker.Unsubscribe<DialogueEndedMessage>(OnDialogueEnded);

        public async Awaitable ShowPictureAsync(Sprite sprite, float seconds)
        {
            pictureImage.sprite = sprite;
            pictureImage.gameObject.SetActive(true);
            await _pictureLayer.FadeAsync(1f, seconds, destroyCancellationToken);
        }

        public async Awaitable HidePictureAsync(float seconds)
        {
            bool completed = await _pictureLayer.FadeAsync(0f, seconds, destroyCancellationToken);
            if (completed) pictureImage.gameObject.SetActive(false);
        }

        public async Awaitable DarkenAsync(float seconds) =>
            await _darknessLayer.FadeAsync(1f, seconds, destroyCancellationToken);

        public async Awaitable BrightenAsync(float seconds) =>
            await _darknessLayer.FadeAsync(0f, seconds, destroyCancellationToken);

        public void DarkenImmediately() => _darknessLayer.SetImmediately(1f);

        public void ClearImmediately()
        {
            _darknessLayer.SetImmediately(0f);
            _pictureLayer.SetImmediately(0f);
            pictureImage.gameObject.SetActive(false);
        }

        private void OnDialogueEnded(DialogueEndedMessage message)
        {
            _ = BrightenAsync(clearSecondsWhenDialogueEnds);
            _ = HidePictureAsync(clearSecondsWhenDialogueEnds);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (pictureImage == null)
                Debug.LogWarning("[ScriptScreen] O campo Picture Image está vazio: <<mostrar_imagem>> não vai funcionar.", this);
            if (pictureGroup == null)
                Debug.LogWarning("[ScriptScreen] O campo Picture Group está vazio: <<mostrar_imagem>> e <<esconder_imagem>> não vão funcionar.", this);
            if (darknessGroup == null)
                Debug.LogWarning("[ScriptScreen] O campo Darkness Group está vazio: <<escurecer>> e <<clarear>> não vão funcionar.", this);
        }
#endif

        /// <summary>
        /// O alfa de uma camada e a animação que o move. Uma animação nova cancela a anterior, e a cancelada termina
        /// sem lançar: o runner do Yarn Spinner descarta a tarefa do comando, e uma exceção ali viraria erro no console.
        /// </summary>
        private sealed class FadingLayer
        {
            private readonly CanvasGroup _group;
            private CancellationTokenSource _running;

            public FadingLayer(CanvasGroup group)
            {
                _group = group;
            }

            public void SetImmediately(float alpha)
            {
                _running?.Cancel();
                _group.alpha = alpha;
            }

            /// <returns><c>true</c> se chegou ao alvo; <c>false</c> se outra animação ou a destruição a cancelou.</returns>
            public async Awaitable<bool> FadeAsync(float targetAlpha, float seconds, CancellationToken ownerToken)
            {
                _running?.Cancel();
                using CancellationTokenSource mine = CancellationTokenSource.CreateLinkedTokenSource(ownerToken);
                _running = mine;

                float effectiveSeconds = Mathf.Approximately(_group.alpha, targetAlpha) ? 0f : seconds;
                var transition = new AlphaTransition(_group.alpha, targetAlpha, effectiveSeconds);

                try
                {
                    _group.alpha = transition.Alpha;
                    while (!transition.IsDone)
                    {
                        await Awaitable.NextFrameAsync(mine.Token);
                        _group.alpha = transition.Advance(Time.deltaTime);
                    }

                    return true;
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                finally
                {
                    if (_running == mine) _running = null;
                }
            }
        }
    }
}
