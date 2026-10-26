using ProjetoVN.Dialogue.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yarn.Unity;

namespace ProjetoVN.Dialogue.UI
{
    /// <summary>
    /// A folha da lista de passageiros (#26): o jogador escreve o nome do protagonista e assina. Fica inativa no
    /// <c>GameUI.prefab</c> e só abre por <see cref="AskAsync"/>. O campo recusa o caractere que a regra do nome não
    /// aceita, que a fonte não tem ou com que o nome deixaria de caber na placa de nome. Quem grava o nome é o
    /// <see cref="DialogueManager"/>; a folha só devolve o texto.
    /// </summary>
    public sealed class ProtagonistNameSheet : MonoBehaviour
    {
        private const char RefusedCharacter = '\0';
        private const string EmptyNameMessage = "Escreva um nome.";

        [Tooltip("O campo em que o jogador escreve o nome. Vazio: a folha não funciona e o console avisa.")]
        [SerializeField] private TMP_InputField nameField;

        [Tooltip("Botão que assina a lista; fica desabilitado enquanto o nome está vazio. Vazio: a folha não funciona e o console avisa.")]
        [SerializeField] private Button signButton;

        [Tooltip("Texto do aviso de nome vazio. Vazio: o botão ASSINAR desabilitado continua indicando o problema, sem mensagem.")]
        [SerializeField] private TMP_Text messageText;

        [Tooltip("O texto da placa de nome da caixa de diálogo: a largura dele é o limite do nome. Vazio: o nome não tem limite de largura.")]
        [SerializeField] private TMP_Text nameplateText;

        [Tooltip("O nome que aparece no campo quando o jogador ainda não assinou. Precisa ser aceito pela regra do nome (letras, espaço, hífen e apóstrofo) e caber na placa.")]
        [SerializeField] private string defaultName = "Lucas";

        private YarnTaskCompletionSource<string> _signing;

        private static bool IsGamePaused => DialogueManager.Instance != null && DialogueManager.Instance.IsPaused;

        private bool HasRequiredReferences => nameField != null && signButton != null;

        private void OnEnable()
        {
            if (!HasRequiredReferences) return;

            nameField.onValidateInput = ValidateCharacter;
            nameField.onValueChanged.AddListener(RefreshState);
            nameField.onSubmit.AddListener(Sign);
            signButton.onClick.AddListener(Sign);
        }

        private void OnDisable()
        {
            if (!HasRequiredReferences) return;

            nameField.onValidateInput = null;
            nameField.onValueChanged.RemoveListener(RefreshState);
            nameField.onSubmit.RemoveListener(Sign);
            signButton.onClick.RemoveListener(Sign);
        }

        // A pausa leva a seleção para os botões dela e a deixa vazia ao fechar: sem devolver o foco, o teclado não digita mais.
        private void Update()
        {
            if (_signing == null || IsGamePaused) return;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null) FocusNameField();
        }

        /// <returns>O nome normalizado, ou <c>null</c> se a folha foi cancelada (a conversa terminou).</returns>
        internal YarnTask<string> AskAsync(string currentName)
        {
            Cancel();
            if (!HasRequiredReferences)
            {
                Debug.LogError("[ProtagonistNameSheet] Name Field e Sign Button não estão atribuídos: a folha não abriu e o nome continua como estava.", this);
                return YarnTask<string>.FromResult(null);
            }

            _signing = new YarnTaskCompletionSource<string>();
            gameObject.SetActive(true);
            nameField.text = ProtagonistName.TryNormalize(currentName, out string normalizedName) ? normalizedName : defaultName;
            FocusNameField();
            return _signing.Task;
        }

        /// <summary>Fecha a folha devolvendo <c>null</c> a quem espera. Sem folha aberta, não faz nada.</summary>
        internal void Cancel()
        {
            if (_signing != null) Finish(null);
        }

        private void Sign(string ignored) => Sign();

        private void Sign()
        {
            if (_signing == null) return;

            if (!ProtagonistName.TryNormalize(nameField.text, out string normalizedName))
            {
                FocusNameField();
                return;
            }

            Finish(normalizedName);
        }

        private void Finish(string result)
        {
            YarnTaskCompletionSource<string> signing = _signing;
            _signing = null;
            ReleaseSelectionInsideSheet();
            gameObject.SetActive(false);
            signing.TrySetResult(result);
        }

        private void RefreshState(string typed)
        {
            bool isValid = ProtagonistName.TryNormalize(typed, out _);
            signButton.interactable = isValid;
            if (messageText != null) messageText.text = isValid ? "" : EmptyNameMessage;
        }

        private char ValidateCharacter(string text, int position, char character)
        {
            if (!ProtagonistName.IsAllowedCharacter(character)) return RefusedCharacter;
            if (!FontHasGlyph(character)) return RefusedCharacter;

            string withCharacter = text.Insert(Mathf.Clamp(position, 0, text.Length), character.ToString());
            return FitsOnNameplate(withCharacter) ? character : RefusedCharacter;
        }

        private bool FontHasGlyph(char character) => nameField.textComponent.font.HasCharacter(character, true, true);

        private bool FitsOnNameplate(string typed)
        {
            if (nameplateText == null) return true;

            float width = nameplateText.GetPreferredValues(ProtagonistName.Normalize(typed)).x;
            return width <= nameplateText.rectTransform.rect.width;
        }

        private void FocusNameField()
        {
            if (EventSystem.current == null) return;

            EventSystem.current.SetSelectedGameObject(nameField.gameObject);
            nameField.ActivateInputField();
        }

        private void ReleaseSelectionInsideSheet()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return;

            GameObject selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(transform)) eventSystem.SetSelectedGameObject(null);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (nameField == null || signButton == null)
                Debug.LogWarning("[ProtagonistNameSheet] Name Field e Sign Button precisam estar atribuídos: a folha não funciona sem eles.", this);

            if (!ProtagonistName.TryNormalize(defaultName, out _))
                Debug.LogWarning($"[ProtagonistNameSheet] Default Name ('{defaultName}') não é aceito pela regra do nome: use letras, espaço, hífen e apóstrofo.", this);

            if (DefaultNameExceedsNameplate())
                Debug.LogWarning($"[ProtagonistNameSheet] Default Name ('{defaultName}') não cabe na placa de nome: ele apareceria cortado com \"…\". Use um nome mais curto.", this);
        }

        private bool DefaultNameExceedsNameplate() =>
            nameplateText != null && !FitsOnNameplate(defaultName);
#endif
    }
}
