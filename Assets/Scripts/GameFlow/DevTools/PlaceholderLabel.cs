using ProjetoVN.GameFlow.Rooms;
using ProjetoVN.Inventory;
using TMPro;
using UnityEngine;

namespace ProjetoVN.GameFlow.DevTools
{
    /// <summary>
    /// Rótulo provisório de um objeto interativo que ainda é o quadrado do prefab <c>Interactable</c>: mostra sobre
    /// ele o tipo (deduzido dos componentes que o objeto já tem) e o nome escrito à mão, e pinta o quadrado na cor
    /// do tipo. A regra mora em <see cref="PlaceholderRoles"/>; este componente só lê o objeto e aplica o resultado.
    /// <para>
    /// O texto é um filho serializado do prefab, não criado em tempo de execução: o
    /// <see cref="HiddenUntilStoryFlag"/> guarda os <see cref="Renderer"/> no <c>Awake</c> e escondê-lo junto
    /// depende de o texto já existir nesse momento. O <c>Awake</c> daqui roda antes do <c>Start</c> do portão, então
    /// o <see cref="PlaceholderTint"/> guarda a cor do tipo como original e a devolve ao restaurar.
    /// </para>
    /// Ferramenta de desenvolvimento: quando a arte definitiva chegar, remova o componente e o filho <c>Label</c>.
    /// </summary>
    [ExecuteAlways]
    public sealed class PlaceholderLabel : MonoBehaviour
    {
        [Tooltip("O nome legível que o rótulo mostra sob o tipo, em português (ex.: Motorista; numa saída, para onde ela leva: Loja). " +
                 "Vazio = o rótulo mostra '(sem nome)', com aviso no console, e o teste de conteúdo reprova a sala.")]
        [SerializeField] private string displayName;

        [Tooltip("O filho Label do prefab Interactable, o texto do rótulo. Vazio = o objeto fica sem rótulo, com erro no console.")]
        [SerializeField] private TextMeshPro label;

        // ExecuteAlways: ao abrir a sala no Editor o OnValidate não aplica o rótulo, e a Scene view ficaria com o
        // texto e a cor do prefab (OBJETO, quadrado preto) até o Play. Em Edit Mode o aviso de nome vazio é do
        // OnValidate. O próprio prefab, no asset ou aberto no Prefab Mode, fica como foi gravado: repintá-lo
        // levaria o cinza do tipo OBJETO para o arquivo no primeiro save.
        private void Awake()
        {
            if (Application.isPlaying)
            {
                WarnIfNameMissing();
                Refresh();
                return;
            }

            if (IsThePrefabItself()) return;

            Refresh();
        }

        private void Refresh()
        {
            if (label == null)
            {
                Debug.LogError("[PlaceholderLabel] O campo Label está vazio: o objeto fica sem rótulo.", this);
                return;
            }

            PlaceholderRole role = RoleOfThisObject();
            label.text = PlaceholderRoles.LabelText(role, displayName);
            PaintSquare(role);
        }

        private PlaceholderRole RoleOfThisObject()
        {
            InteractableDialogueTrigger trigger = GetComponent<InteractableDialogueTrigger>();

            return PlaceholderRoles.Pick(
                hasLockedAction: GetComponent<LockedActionBehaviour>() != null,
                hasExit: GetComponent<RoomExit>() != null,
                isCollectable: GetComponent<CollectableItemBehaviour>() != null,
                startsConversation: trigger != null && trigger.HasNode);
        }

        private void PaintSquare(PlaceholderRole role)
        {
            SpriteRenderer square = GetComponent<SpriteRenderer>();
            if (square == null)
            {
                Debug.LogError("[PlaceholderLabel] O objeto não tem SpriteRenderer para pintar.", this);
                return;
            }

            square.color = PlaceholderRoles.ColorOf(role);
        }

        private void WarnIfNameMissing()
        {
            if (!PlaceholderRoles.IsNameMissing(displayName)) return;

            Debug.LogWarning("[PlaceholderLabel] O campo Display Name está vazio: o rótulo mostra '(sem nome)'.", this);
        }

        private bool IsThePrefabItself()
        {
#if UNITY_EDITOR
            return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)
                   || UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null;
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        // O prefab nasce com o nome vazio de propósito; quem preenche é cada instância na sala. No Prefab Mode o
        // objeto mora numa cena de pré-visualização, e IsPartOfPrefabAsset não o reconhece.
        private void OnValidate()
        {
            if (IsThePrefabItself()) return;

            WarnIfNameMissing();

            // Mexer em outro componente dentro do OnValidate dispara SendMessage proibido; adiar resolve.
            UnityEditor.EditorApplication.delayCall += RefreshInEditor;
        }

        // Em Play o Awake já aplicou o rótulo; repintar depois do Start apagaria o verde do PlaceholderTint.
        private void RefreshInEditor()
        {
            if (this == null || Application.isPlaying || IsThePrefabItself()) return;

            Refresh();
        }
#endif
    }
}
