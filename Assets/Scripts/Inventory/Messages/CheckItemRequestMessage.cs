using System;
using Assets.Scripts.Core.Messaging;

namespace ProjetoVN.Inventory.Messages
{
    public readonly struct CheckItemRequestMessage : IMessage
    {
        public string ItemId { get; }
        public Action<bool> Callback { get; }

        public CheckItemRequestMessage(string itemId, Action<bool> callback)
        {
            ItemId = itemId;
            Callback = callback;
        }
    }
}
