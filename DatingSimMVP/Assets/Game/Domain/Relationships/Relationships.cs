using System.Collections.Generic;
using Game.Domain.Common;

namespace Game.Domain.Relationships
{
    public sealed class RelationshipState
    {
        private readonly Dictionary<string, int> _affection = new();
        public int GetAffection(string npcId) => _affection.TryGetValue(npcId, out var v) ? v : 0;
        public void SetInitial(string npcId, int value) => _affection[npcId] = value;
        public int AddAffection(string npcId, int delta)
        {
            var v = GetAffection(npcId) + delta;
            _affection[npcId] = v;
            return v;
        }

        public IReadOnlyDictionary<string, int> Snapshot() => _affection;
        public void Restore(IReadOnlyDictionary<string, int> data)
        {
            _affection.Clear();
            foreach (var kv in data)
            {
                _affection[kv.Key] = kv.Value;
            }
        }
    }

    //Events
    public readonly struct AffectionChanged : IEvent
    {
        public readonly string NpcId;
        public readonly int NewValue;
        public AffectionChanged(string npcId, int newValue)
        {
            NpcId = npcId;
            NewValue = newValue;
        }
    }
}
