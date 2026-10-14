using UnityEngine;

namespace ProjetoVN.Dialogue.Logic
{
    /// <summary>
    /// A cor do texto de um balão do histórico (D-30): a escura ou a clara, a que tem mais contraste com o balão
    /// (WCAG 2.x, alfa ignorado). C# puro.
    /// </summary>
    public static class BubbleTextColor
    {
        // O fundo da caixa de diálogo (DialogueBox.svg) e a cor do texto dela (DialogueText).
        public static readonly Color Dark = new Color32(0x24, 0x11, 0x1F, 0xFF);
        public static readonly Color Light = new Color32(0xF7, 0xEB, 0xF5, 0xFF);

        public const float MinimumContrast = 4.5f;

        private const float LuminanceOffset = 0.05f;

        public static float ContrastRatio(Color first, Color second)
        {
            float firstLuminance = RelativeLuminance(first);
            float secondLuminance = RelativeLuminance(second);
            float lighter = Mathf.Max(firstLuminance, secondLuminance);
            float darker = Mathf.Min(firstLuminance, secondLuminance);

            return (lighter + LuminanceOffset) / (darker + LuminanceOffset);
        }

        public static Color For(Color bubble) =>
            ContrastRatio(Dark, bubble) >= ContrastRatio(Light, bubble) ? Dark : Light;

        public static bool IsReadable(Color bubble) => ContrastRatio(For(bubble), bubble) >= MinimumContrast;

        private static float RelativeLuminance(Color color) =>
            0.2126f * Linearize(color.r) + 0.7152f * Linearize(color.g) + 0.0722f * Linearize(color.b);

        private static float Linearize(float channel) =>
            channel <= 0.03928f ? channel / 12.92f : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
    }
}
