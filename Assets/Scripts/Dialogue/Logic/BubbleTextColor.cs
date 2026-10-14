using UnityEngine;

namespace ProjetoVN.Dialogue.Logic
{
    public static class BubbleTextColor
    {
        public static readonly Color Dark = new Color32(0x24, 0x11, 0x1F, 0xFF);
        public static readonly Color Light = new Color32(0xF7, 0xEB, 0xF5, 0xFF);

        public const float MinimumContrast = 4.5f;

        public static float ContrastRatio(Color first, Color second) => 0f;

        public static Color For(Color bubble) => Color.clear;

        public static bool IsReadable(Color bubble) => true;
    }
}
