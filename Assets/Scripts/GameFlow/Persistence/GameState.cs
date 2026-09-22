using System;
using System.Collections.Generic;

namespace ProjetoVN.GameFlow.Persistence
{
    [Serializable]
    public sealed class GameState
    {
        public List<string> ownedItemIds = new();
        public List<string> consumedWorldObjectIds = new();
        public List<string> storyFlagIds = new();
        public string currentScene = "";
    }
}
