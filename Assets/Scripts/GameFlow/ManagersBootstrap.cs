using UnityEngine;

namespace ProjetoVN.GameFlow
{
    internal static class ManagersBootstrap
    {
        private const string ManagersResourcePath = "Managers";
        private static GameObject _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureManagersExist()
        {
            if (_instance != null) return;

            var prefab = Resources.Load<GameObject>(ManagersResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[ManagersBootstrap] Prefab 'Resources/{ManagersResourcePath}.prefab' não encontrado.");
                return;
            }

            _instance = Object.Instantiate(prefab);
            _instance.name = prefab.name;
            Object.DontDestroyOnLoad(_instance);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForDomainReload()
        {
            _instance = null;
        }
    }
}
