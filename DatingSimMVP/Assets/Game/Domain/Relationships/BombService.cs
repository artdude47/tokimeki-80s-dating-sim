using System.Collections.Generic;
using Game.Domain.Common;
using Game.Domain.Time;

namespace Game.Domain.Relationships
{
    public sealed class BombConfig
    {
        public int WeeksToArm = 8;
        public int FuseWeeks = 3;
    }

    public readonly struct BombArmed : IEvent
    {
        public readonly string NpcId;
        public BombArmed(string id)
        {
            NpcId = id;
        }
    }

    public sealed class BombService
    {
        private readonly EventBus _bus;
        private readonly BombConfig _cfg;
        private readonly Dictionary<string, int> _weeksSinceLastDate = new();
        private readonly HashSet<string> _armed = new();

        public BombService(EventBus bus, BombConfig cfg)
        {
            _bus = bus;
            _cfg = cfg;
        }

        public bool IsArmed(string npcId) => _armed.Contains(npcId);

        public void OnWeekEneded(WeekEnded e)
        {
            foreach (var key in _weeksSinceLastDate.Keys)
            {
                _weeksSinceLastDate[key] = _weeksSinceLastDate[key] + 1;
            }

            // arm any that cross threshhold
            foreach (var kv in new List<string>(_weeksSinceLastDate.Keys))
            {
                if (_weeksSinceLastDate[kv] >= _cfg.WeeksToArm && !_armed.Contains(kv))
                {
                    _armed.Add(kv);
                    _bus.Publish(new BombArmed(kv));
                }
            }
        }

        public void OnDateOccurred(DateOccurred e)
        {
            // defuse & reset counter for that NPC
            _weeksSinceLastDate[e.NpcId] = 0;
            _armed.Remove(e.NpcId);
        }

        public void EnsureTracked(string npcId)
        {
            if (!_weeksSinceLastDate.ContainsKey(npcId))
                _weeksSinceLastDate[npcId] = 0;
        }

        public (IReadOnlyDictionary<string,int> counters, IReadOnlyCollection<string> armed) Snapshot()
            => (_weeksSinceLastDate, _armed);

        public void Restore(IReadOnlyDictionary<string,int> counters, IReadOnlyCollection<string> armed)
        {
            _weeksSinceLastDate.Clear();
            foreach (var kv in counters) _weeksSinceLastDate[kv.Key] = kv.Value;
            _armed.Clear();
            foreach (var id in armed) _armed.Add(id);
        }
    }
}