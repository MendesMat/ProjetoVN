using System;
using System.Collections.Generic;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;

namespace ProjetoVN.Dialogue.Characters
{
    /// <summary>
    /// Um personagem do jogo (D-21). Somente leitura em runtime (D-02): não guarda a expressão atual,
    /// porque a etiqueta do roteiro vale só para a fala em que aparece.
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

        [Tooltip("Retratos por expressão. O primeiro é o padrão, mostrado nas falas sem etiqueta de expressão. " +
                 "Vazio: o personagem fala sem retrato.")]
        [SerializeField] private List<CharacterPortrait> portraits = new();

        public string Id => id;
        public string DisplayName => displayName;
        public Color NameColor => nameColor;
        public IReadOnlyList<CharacterPortrait> Portraits => portraits;

        public Sprite DefaultPortrait => portraits.Count > 0 ? portraits[0].Sprite : null;

        public bool HasExpression(string expression) => FindPortrait(expression) != null;

        public bool TryGetPortrait(string expression, out Sprite portrait)
        {
            CharacterPortrait found = FindPortrait(expression);
            portrait = found?.Sprite;
            return found != null;
        }

        private CharacterPortrait FindPortrait(string expression) =>
            portraits.Find(portrait => string.Equals(portrait.Expression, expression, StringComparison.Ordinal));

#if UNITY_EDITOR
        private void OnValidate()
        {
            WarnAboutIdentity();
            WarnAboutPortraits();
        }

        private void WarnAboutIdentity()
        {
            if (!ScriptNodeName.FollowsConvention(id))
                Warn("O 'Id' está vazio ou fora do formato: use minúsculas sem acento, dígitos e _ (ex.: luna).");

            if (string.IsNullOrEmpty(displayName) || displayName != displayName.Trim())
                Warn("O 'Display Name' está vazio ou tem espaço no começo ou no fim: o roteiro não vai achar o personagem.");
        }

        private void WarnAboutPortraits()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterPortrait portrait in portraits)
            {
                if (!ScriptNodeName.FollowsConvention(portrait.Expression))
                    Warn($"A expressão '{portrait.Expression}' está vazia ou fora do formato: use minúsculas sem acento, dígitos e _ (ex.: raiva).");
                else if (!seen.Add(portrait.Expression))
                    Warn($"A expressão '{portrait.Expression}' aparece mais de uma vez.");

                if (portrait.Sprite == null)
                    Warn($"A expressão '{portrait.Expression}' está sem Sprite.");
            }
        }

        private void Warn(string message) => Debug.LogWarning($"[CharacterSO] {name}: {message}", this);
#endif
    }
}
