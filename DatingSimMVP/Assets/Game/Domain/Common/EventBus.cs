using System;
using System.Collections.Generic;

namespace Game.Domain.Common
{
    public interface IEvent { }

    public sealed class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subs = new();

        public void Publish<T>(T evt) where T : IEvent
        {
            if (_subs.TryGetValue(typeof(T), out var list))
                foreach (var d in list.ToArray()) ((Action<T>)d)?.Invoke(evt);
        }

        public Action Subscribe<T>(Action<T> handler) where T : IEvent 
        {
            var t = typeof(T);
            if (!_subs.TryGetValue(t, out var list))
                _subs[t] = list = new List<Delegate>();
            list.Add(handler);
            return () => list.Remove(handler);
        }
    }
}