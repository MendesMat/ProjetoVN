using System;

namespace ProjetoVN.GameFlow.Persistence
{
    // O JsonUtility não serializa dicionários (D-20): o save guarda o estado da história como listas de pares.

    [Serializable]
    public struct StoryBoolEntry
    {
        public string name;
        public bool value;
    }

    [Serializable]
    public struct StoryNumberEntry
    {
        public string name;
        public float value;
    }

    [Serializable]
    public struct StoryTextEntry
    {
        public string name;
        public string value;
    }
}
