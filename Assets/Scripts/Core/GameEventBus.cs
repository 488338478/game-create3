using System;
using System.Collections.Generic;

namespace GameCreate3.Core
{
    public static class GameEventBus
    {
        private static readonly Dictionary<string, List<Action<object>>> Subscribers =
            new Dictionary<string, List<Action<object>>>(StringComparer.Ordinal);

        public static void Subscribe(string eventId, Action<object> callback)
        {
            if (string.IsNullOrEmpty(eventId) || callback == null)
            {
                return;
            }

            if (!Subscribers.TryGetValue(eventId, out var list))
            {
                list = new List<Action<object>>();
                Subscribers[eventId] = list;
            }

            if (!list.Contains(callback))
            {
                list.Add(callback);
            }
        }

        public static void Unsubscribe(string eventId, Action<object> callback)
        {
            if (string.IsNullOrEmpty(eventId) || callback == null)
            {
                return;
            }

            if (!Subscribers.TryGetValue(eventId, out var list))
            {
                return;
            }

            list.Remove(callback);
            if (list.Count == 0)
            {
                Subscribers.Remove(eventId);
            }
        }

        public static void Publish(string eventId, object payload = null)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return;
            }

            if (!Subscribers.TryGetValue(eventId, out var list) || list.Count == 0)
            {
                return;
            }

            var snapshot = new Action<object>[list.Count];
            list.CopyTo(snapshot);
            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i]?.Invoke(payload);
            }
        }

        public static void Clear()
        {
            Subscribers.Clear();
        }

        public static void ClearEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return;
            }

            Subscribers.Remove(eventId);
        }
    }
}
