using ProjetoVN.Dialogue.Logic;
using UnityEngine;

namespace ProjetoVN.Dialogue.UI
{
    public class DialogueChoiceButton : MonoBehaviour
    {
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
