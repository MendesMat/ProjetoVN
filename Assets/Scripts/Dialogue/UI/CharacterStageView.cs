using ProjetoVN.Dialogue.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjetoVN.Dialogue.UI
{
    /// <summary>
    /// Desenha a tela de personagens da conversa: um sprite em cada lado, atrás da caixa de diálogo.
    /// Não decide nada; só mostra o que o <see cref="ConversationStage"/> informa. A escala e a linha de chão
    /// vêm do objeto que contém esta view, iguais para todos os personagens.
    /// </summary>
    public sealed class CharacterStageView : MonoBehaviour
    {
        [Tooltip("Imagem do lugar da esquerda. Vazio: o lado esquerdo não é desenhado e o console avisa.")]
        [SerializeField] private Image leftImage;

        [Tooltip("Imagem do lugar da direita. Vazio: o lado direito não é desenhado e o console avisa.")]
        [SerializeField] private Image rightImage;

        [Tooltip("Cor do sprite de quem não está em destaque; quem está em destaque é desenhado em branco. " +
                 "Quanto mais escura, mais apagado o personagem. Começa em 55% de brilho.")]
        [SerializeField] private Color dimmedColor = new(0.55f, 0.55f, 0.55f, 1f);

        public void Show(ConversationStage stage)
        {
            Draw(leftImage, stage.Occupant(StageSide.Left));
            Draw(rightImage, stage.Occupant(StageSide.Right));
        }

        // Um Image sem sprite desenha um quadrado branco, então o lugar vazio se desliga pelo objeto.
        private void Draw(Image image, StageOccupant occupant)
        {
            if (image == null) return;

            Sprite sprite = null;
            bool hasSprite = !occupant.IsEmpty && occupant.Character.TryGetSprite(occupant.Expression, out sprite) && sprite != null;
            image.gameObject.SetActive(hasSprite);
            if (!hasSprite) return;

            image.sprite = sprite;
            image.SetNativeSize();
            image.color = occupant.IsHighlighted ? Color.white : dimmedColor;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (leftImage == null || rightImage == null)
                Debug.LogWarning("[CharacterStageView] Left Image e Right Image precisam estar atribuídos: o lado vazio não é desenhado.", this);

            WarnIfBlocksClicks(leftImage);
            WarnIfBlocksClicks(rightImage);
        }

        private void WarnIfBlocksClicks(Image image)
        {
            if (image != null && image.raycastTarget)
                Debug.LogWarning($"[CharacterStageView] '{image.name}' recebe clique (Raycast Target ligado): desligue, ou o sprite bloqueia o avanço da fala e os botões.", this);
        }
#endif
    }
}
