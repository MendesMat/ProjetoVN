using System;
using System.Collections.Generic;

namespace Assets.Scripts.Core.Messaging
{
    public static class MessageBroker
    {
        private static readonly Dictionary<Type, List<Action<IMessage>>> _handlers = new();
        private static readonly Dictionary<object, Action<IMessage>> _wrappers = new();

        public static void Subscribe<T>(Action<T> handler) where T : IMessage
        {
            Type messageType = typeof(T);

            if (!_handlers.ContainsKey(messageType))
                _handlers[messageType] = new List<Action<IMessage>>();

            Action<IMessage> wrapper = msg => handler((T)msg);
            _wrappers[handler] = wrapper;
            _handlers[messageType].Add(wrapper);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IMessage
        {
            Type messageType = typeof(T);

            if (!_handlers.TryGetValue(messageType, out List<Action<IMessage>> handlers))
                return;

            if (!_wrappers.TryGetValue(handler, out Action<IMessage> wrapper))
                return;

            handlers.Remove(wrapper);
            _wrappers.Remove(handler);
        }

        public static void Publish<T>(T message) where T : IMessage
        {
            Type messageType = typeof(T);

            if (!_handlers.TryGetValue(messageType, out List<Action<IMessage>> handlers))
                return;

            foreach (Action<IMessage> handler in handlers.ToArray())
                handler.Invoke(message);
        }

        public static void Clear()
        {
            _handlers.Clear();
            _wrappers.Clear();
        }

        public static void Clear<T>() where T : IMessage => _handlers.Remove(typeof(T));
    }
}
