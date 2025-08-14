using System.Collections.Generic;
using Game.Domain.Common;
using Game.Domain.Time;

namespace Game.Domain.Relationships
{
    public sealed class BombConfig
    {
        public int WeeksToArm = 8;
        public int FuseWeeks = 3;
        public int GlobalPenalty = 4;
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
        private readonly RelationshipState _rels;

        private readonly Dictionary<string, int> _weeksSinceLastDate = new();
        private readonly HashSet<string> _armed = new();
        private readonly Dictionary<string, int> _armedFuse = new();

        public BombService(EventBus bus, BombConfig cfg, RelationshipState rels)
        {
            _bus = bus;
            _cfg = cfg;
            _rels = rels;

            //_bus.Subscribe<WeekEnded>(OnWeekEnded);
            //_bus.Subscribe<DateOccurred>(OnDateOccurred);
        }

        public bool IsArmed(string npcId) => _armed.Contains(npcId);

        public void OnWeekEnded(WeekEnded e)
        {
            //1. tick the fuse for bombs that were armed before this week ended
            var armedSnapshot = new List<string>(_armed);
            foreach (var id in armedSnapshot)
            {
                var prev = _armedFuse.TryGetValue(id, out var f) ? f : 0;
                var now = prev + 1;
                _armedFuse[id] = now;

                if (now >= _cfg.FuseWeeks)
                {
                    Detonate(id);
                }
            }

            //2. Increment neglect counters for everyone
            foreach (var key in new List<string>(_weeksSinceLastDate.Keys))
            {
                _weeksSinceLastDate[key] = _weeksSinceLastDate[key] + 1;
            }

            //3. Arm any newly eligible npcs
            foreach (var id in new List<string>(_weeksSinceLastDate.Keys))
            {
                if (_weeksSinceLastDate[id] >= _cfg.WeeksToArm && !_armed.Contains(id))
                {
                    _armed.Add(id);
                    _armedFuse[id] = 0;
                    _bus.Publish(new BombArmed(id));
                }
            }
        }

        public void OnDateOccurred(DateOccurred e)
        {
            // defuse & reset counter for that NPC
            _weeksSinceLastDate[e.NpcId] = 0;
            _armed.Remove(e.NpcId);
            _armedFuse.Remove(e.NpcId);
        }

        private void Detonate(string sourceNpcId)
        {
            _bus.Publish(new BombDetonated(sourceNpcId, _cfg.GlobalPenalty));
            _bus.Publish(new RumorSpread(sourceNpcId, _cfg.GlobalPenalty, "Bomb detonation"));

            //Apply to all other tracked NPCs
            foreach (var id in _weeksSinceLastDate.Keys)
            {
                if (id == sourceNpcId) continue;
                var newAff = _rels.AddAffection(id, -_cfg.GlobalPenalty);
                _bus.Publish(new AffectionChanged(id, newAff));
            }

            //clear bomb for the source npc and reset counter
            _armed.Remove(sourceNpcId);
            _armedFuse.Remove(sourceNpcId);
            _weeksSinceLastDate[sourceNpcId] = 0;
        }

        public void EnsureTracked(string npcId)
        {
            if (!_weeksSinceLastDate.ContainsKey(npcId))
                _weeksSinceLastDate[npcId] = 0;
        }

        public (IReadOnlyDictionary<string,int> counters, IReadOnlyCollection<string> armed, IReadOnlyDictionary<string,int> fuse) Snapshot()
            => (_weeksSinceLastDate, _armed, _armedFuse);

        public void Restore(IReadOnlyDictionary<string, int> counters, IReadOnlyCollection<string> armed, IReadOnlyDictionary<string, int> fuse)
        {
            _weeksSinceLastDate.Clear();
            foreach (var kv in counters) _weeksSinceLastDate[kv.Key] = kv.Value;

            _armed.Clear();
            foreach (var id in armed) _armed.Add(id);

            _armedFuse.Clear();
            foreach (var kv in fuse) _armedFuse[kv.Key] = kv.Value;
        }
    }
}