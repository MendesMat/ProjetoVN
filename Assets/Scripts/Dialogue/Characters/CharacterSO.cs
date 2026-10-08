using System;
using System.Collections.Generic;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;

namespace ProjetoVN.Dialogue.Characters
{
    /// <summary>
    /// Um personagem do jogo (D-21). Somente leitura em runtime (D-02): não guarda a expressão atual nem o lugar
    /// na tela, que são estado da conversa em curso (<see cref="ConversationStage"/>).
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterSO", menuName = "Characters/Character")]
    public sealed class CharacterSO : ScriptableObject
    {
        [Tooltip("Identificador único do personagem para o código: minúsculas sem acento, dígitos e _ (ex.: luna). " +
                 "Vazio ou fora do formato: o console avisa.")]
        [SerializeField] private string id = "";

        [Tooltip("Nome como o roteiro o escreve antes dos dois-pontos e como aparece na placa de nome (ex.: Luna). " +
                 "Vazio: o roteiro não consegue citar o personagem e o console avisa.")]
        [SerializeField] private string displayName = "";

        [Tooltip("Cor do nome na placa de nome.")]
        [SerializeField] private Color nameColor = Color.white;

        [Tooltip("Sprites por expressão. O primeiro é o padrão, mostrado nas falas sem etiqueta de expressão. " +
                 "Vazio: o personagem fala sem aparecer na tela.")]
        [SerializeField] private List<CharacterExpression> expressions = new();

        public string Id => id;
        public string DisplayName => displayName;
        public Color NameColor => nameColor;
        public IReadOnlyList<CharacterExpression> Expressions => expressions;

        public string DefaultExpression => expressions.Count > 0 ? expressions[0].Expression : null;

        public bool HasExpression(string expression) => Find(expression) != null;

        public bool TryGetSprite(string expression, out Sprite sprite)
        {
            CharacterExpression found = Find(expression);
            sprite = found?.Sprite;
            return found != null;
        }

        private CharacterExpression Find(string expression) =>
            expressions.Find(candidate => string.Equals(candidate.Expression, expression, StringComparison.Ordinal));

#if UNITY_EDITOR
        private void OnValidate()
        {
            WarnAboutIdentity();
            WarnAboutExpressions();
        }

        private void WarnAboutIdentity()
        {
            if (!ScriptNodeName.FollowsConvention(id))
                Warn("O 'Id' está vazio ou fora do formato: use minúsculas sem acento, dígitos e _ (ex.: luna).");

            if (string.IsNullOrEmpty(displayName) || displayName != displayName.Trim())
                Warn("O 'Display Name' está vazio ou tem espaço no começo ou no fim: o roteiro não vai achar o personagem.");
        }

        private void WarnAboutExpressions()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterExpression expression in expressions)
            {
                if (!ScriptNodeName.FollowsConvention(expression.Expression))
                    Warn($"A expressão '{expression.Expression}' está vazia ou fora do formato: use minúsculas sem acento, dígitos e _ (ex.: raiva).");
                else if (!seen.Add(expression.Expression))
                    Warn($"A expressão '{expression.Expression}' aparece mais de uma vez.");

                if (expression.Sprite == null)
                    Warn($"A expressão '{expression.Expression}' está sem Sprite.");
            }
        }

        private void Warn(string message) => Debug.LogWarning($"[CharacterSO] {name}: {message}", this);
#endif
    }
}
