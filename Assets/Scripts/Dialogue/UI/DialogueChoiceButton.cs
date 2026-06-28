using Assets.Scripts.Dialogue.Logic;
using UnityEngine;

namespace Assets.Scripts.Dialogue.UI
{
    public class DialogueChoiceButton : MonoBehaviour
    {
        [SerializeField] private int choiceIndex;

        public void OnClicked()
        {
            DialogueManager.Instance.MakeChoice(choiceIndex);
        }
    }
}
