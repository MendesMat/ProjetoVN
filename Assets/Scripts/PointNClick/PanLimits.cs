using System;

namespace ProjetoVN.PointNClick
{
    /// <summary>
    /// A conta dos limites do pan: onde o cenário pode ficar para que a borda da imagem de fundo
    /// nunca passe da borda da tela. C# puro; quem mede o fundo e lê a câmera é o <see cref="ScreenPanController"/>.
    /// </summary>
    public static class PanLimits
    {
        /// <param name="backgroundLeft">Borda esquerda do fundo, com o cenário em x 0.</param>
        /// <param name="backgroundRight">Borda direita do fundo, com o cenário em x 0.</param>
        /// <param name="cameraX">Posição x da câmera no mundo.</param>
        /// <param name="halfViewWidth">Metade da largura que a câmera mostra, em unidades do mundo.</param>
        public static PanRange For(float backgroundLeft, float backgroundRight, float cameraX, float halfViewWidth)
        {
            if (backgroundLeft > backgroundRight)
                throw new ArgumentException($"A borda esquerda do fundo ({backgroundLeft}) é maior que a direita ({backgroundRight}).");
            if (halfViewWidth < 0f)
                throw new ArgumentException($"A metade da largura visível ({halfViewWidth}) não pode ser negativa.", nameof(halfViewWidth));

            float backgroundWidth = backgroundRight - backgroundLeft;
            if (backgroundWidth <= halfViewWidth * 2f)
                return CenterOnBackground(backgroundLeft, backgroundRight, cameraX);

            float min = cameraX + halfViewWidth - backgroundRight;
            float max = cameraX - halfViewWidth - backgroundLeft;
            return new PanRange(min, max);
        }

        private static PanRange CenterOnBackground(float backgroundLeft, float backgroundRight, float cameraX)
        {
            float centered = cameraX - (backgroundLeft + backgroundRight) / 2f;
            return new PanRange(centered, centered);
        }
    }
}
