using ProjetoVN.Dialogue.Logic;
using UnityEngine;

namespace ProjetoVN.Dialogue.UI
{
    public class DialogueChoiceButton : MonoBehaviour
    {
        [Tooltip("A posição deste botão no vetor Choice Button Objects do Dialogue UI Controller, de 0 a 3 (são quatro botões de escolha). " +
                 "Cada botão tem um número diferente; fora da faixa, o clique é ignorado. Não aceita negativo.")]
        [Min(0)]
        [SerializeField] private int choiceIndex;

        public void OnClicked()
        {
            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[DialogueChoiceButton] Não há DialogueManager na cena. Clique ignorado.", this);
                return;
            }

            DialogueManager.Instance.MakeChoice(choiceIndex);
        }
    }
}
