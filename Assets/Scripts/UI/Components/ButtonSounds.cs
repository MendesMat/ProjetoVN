using ProjetoVN.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjetoVN.UI.Components
{
    /// <summary>
    /// Os sons de um botão: passar o mouse sobre ele habilitado e acioná-lo (clique ou Enter). Todo <c>Button</c> do jogo
    /// leva este componente, e um teste reprova o que esquecer. O som do clique é um ouvinte do <c>onClick</c>, separado
    /// da ação do botão, então não quebra a regra de que a ação não passa por <c>AddListener</c>.
    /// </summary>
    [RequireComponent(typeof(Button)), DisallowMultipleComponent]
    public sealed class ButtonSounds : MonoBehaviour, IPointerEnterHandler
    {
        private Button _button;

        // No Awake e no OnDestroy, e não no OnEnable e no OnDisable: as janelas ligam e desligam os botões a cada abertura.
        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(PlayClick);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(PlayClick);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button.IsInteractable()) Play(InterfaceSound.ButtonHover);
        }

        private void PlayClick() => Play(InterfaceSound.ButtonClick);

        private void Play(InterfaceSound sound)
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogError("[ButtonSounds] Não há AudioManager (o prefab Managers não foi criado). O botão ficou sem som.", this);
                return;
            }

            AudioManager.Instance.PlayInterfaceSound(sound);
        }
    }
}
