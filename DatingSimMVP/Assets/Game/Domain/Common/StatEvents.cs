using System.Collections.Generic;
using Game.Domain.Common;
using Game.Domain.Stats;

namespace Game.Domain.CommonEvents
{
    public readonly struct StatsChanged : IEvent
    {
        public readonly Dictionary<Stat, int> Deltas;
        public StatsChanged(Dictionary<Stat, int> deltas)
        {
            Deltas = deltas;
        }
    }
}
