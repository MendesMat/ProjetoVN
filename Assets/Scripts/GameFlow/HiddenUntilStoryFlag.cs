using System.Collections.Generic;
using System.Linq;
using ProjetoVN.Core.Messaging;
using ProjetoVN.Core.State;
using ProjetoVN.Dialogue.Messaging;
using UnityEngine;

namespace ProjetoVN.GameFlow
{
    /// <summary>
    /// Esconde um objeto de cena (invisível e sem resposta ao mouse) até uma variável booleana de história ligar.
    /// A variável é lida no <c>Start()</c>, o que cobre voltar à sala, carregar um save e o Novo Jogo, e de novo
    /// quando uma conversa termina, o que cobre a variável ligada por uma conversa desta mesma sala. Só aparece:
    /// nunca esconde de novo no meio da sala.
    /// <para>
    /// Esconde desligando os <see cref="Renderer"/> e os <see cref="Collider2D"/> do objeto e dos filhos, e não o
    /// <see cref="GameObject"/>: um objeto desativado não ouviria a <see cref="DialogueEndedMessage"/> e some de
    /// <c>GameObject.Find</c>. Sem collider o <c>PointNClickSelector</c> não o acha, então não há destaque nem clique.
    /// </para>
    /// <para>
    /// Mora no <c>GameFlow</c> porque lê o <c>StoryState</c> (Core) e ouve uma mensagem do Dialogue (D-01).
    /// </para>
    /// </summary>
    public sealed class HiddenUntilStoryFlag : MonoBehaviour
    {
        [Tooltip("Variável de história que mostra este objeto, escolhida entre as booleanas declaradas em Assets/Roteiro/variaveis.yarn " +
                 "(ex.: $falou_com_motorista). Enquanto ela é falsa, o objeto fica invisível e não responde ao mouse; " +
                 "quando liga numa conversa, ele aparece ao fim dela e fica dali em diante. " +
                 "'(nenhuma)' = o objeto nunca aparece.")]
        [SerializeField, StoryFlag] private string requiredFlagId;

        private Renderer[] _renderers;
        private Collider2D[] _colliders;
        private bool _isShown = true;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.enabled).ToArray();
            _colliders = GetComponentsInChildren<Collider2D>(true).Where(collider => collider.enabled).ToArray();
        }

        private void OnEnable() => MessageBroker.Subscribe<DialogueEndedMessage>(OnDialogueEnded);

        private void OnDisable() => MessageBroker.Unsubscribe<DialogueEndedMessage>(OnDialogueEnded);

        private void Start() => SetShown(StoryState.IsTrue(requiredFlagId));

        private void OnDialogueEnded(DialogueEndedMessage message)
        {
            if (_isShown) return;
            if (!StoryState.IsTrue(requiredFlagId)) return;

            SetShown(true);
        }

        private void SetShown(bool shown)
        {
            _isShown = shown;
            Enable(_renderers, shown);
            Enable(_colliders, shown);
        }

        private static void Enable(IEnumerable<Renderer> renderers, bool enabled)
        {
            foreach (Renderer renderer in renderers)
                if (renderer != null) renderer.enabled = enabled;
        }

        private static void Enable(IEnumerable<Collider2D> colliders, bool enabled)
        {
            foreach (Collider2D collider in colliders)
                if (collider != null) collider.enabled = enabled;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(requiredFlagId))
                Debug.LogWarning("[HiddenUntilStoryFlag] O campo 'requiredFlagId' está vazio: este objeto nunca vai aparecer. Escolha a variável que o mostra (ex.: \"$falou_com_motorista\").", this);

            StoryVariableNameCheck.WarnIfOffConvention(nameof(HiddenUntilStoryFlag), nameof(requiredFlagId), requiredFlagId, this);
        }
#endif
    }
}
