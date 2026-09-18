using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Core.Messaging
{
    public static class MessageBroker
    {
        private static readonly List<Action> _clearActions = new();

        private static class Handlers<T> where T : IMessage
        {
            public static Action<T>[] Snapshot = Array.Empty<Action<T>>();

            static Handlers()
            {
                _clearActions.Add(() => Snapshot = Array.Empty<Action<T>>());
            }
        }

        public static void Subscribe<T>(Action<T> handler) where T : IMessage
        {
            if (handler == null) return;

            Action<T>[] current = Handlers<T>.Snapshot;
            if (Array.IndexOf(current, handler) >= 0) return;

            var updated = new Action<T>[current.Length + 1];
            Array.Copy(current, updated, current.Length);
            updated[current.Length] = handler;

            Handlers<T>.Snapshot = updated;
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IMessage
        {
            if (handler == null) return;

            Action<T>[] current = Handlers<T>.Snapshot;
            int index = Array.IndexOf(current, handler);
            if (index < 0) return;

            if (current.Length == 1)
            {
                Handlers<T>.Snapshot = Array.Empty<Action<T>>();
                return;
            }

            var updated = new Action<T>[current.Length - 1];
            Array.Copy(current, updated, index);
            Array.Copy(current, index + 1, updated, index, current.Length - index - 1);

            Handlers<T>.Snapshot = updated;
        }

        public static void Publish<T>(T message) where T : IMessage
        {
            Action<T>[] snapshot = Handlers<T>.Snapshot;
            TracePublish<T>(snapshot.Length);

            for (int i = 0; i < snapshot.Length; i++)
            {
                try { snapshot[i].Invoke(message); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }

        public static void Clear()
        {
            for (int i = 0; i < _clearActions.Count; i++)
                _clearActions[i].Invoke();
        }

        public static void Clear<T>() where T : IMessage => Handlers<T>.Snapshot = Array.Empty<Action<T>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart() => Clear();

        [System.Diagnostics.Conditional("VN_TRACE_MESSAGES")]
        private static void TracePublish<T>(int handlerCount) where T : IMessage
        {
            Debug.Log($"[MessageBroker] Publish<{typeof(T).Name}> → {handlerCount} handler(s).");
        }
    }
}
