using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjetoVN.PointNClick
{
    public static class PlayerInputGate
    {
        private const int NoFrame = -1;

        private static int _enabledFrame = NoFrame;

        public static bool IsEnabled { get; private set; } = true;

        public static bool CanClickThisFrame =>
            IsEnabled && Time.frameCount != _enabledFrame && !IsPointerOverUI();

        public static void SetEnabled(bool isEnabled)
        {
            if (isEnabled && !IsEnabled) _enabledFrame = Time.frameCount;

            IsEnabled = isEnabled;
        }

        private static bool IsPointerOverUI()
        {
            EventSystem eventSystem = EventSystem.current;
            return eventSystem != null && eventSystem.IsPointerOverGameObject();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            IsEnabled = true;
            _enabledFrame = NoFrame;
        }
    }
}
