using Game.Domain.Time;

namespace Game.Domain.Common
{
    public readonly struct BombDetonated : IEvent
    {
        public readonly string NpcId;
        public readonly int GlobalPenalty;
        public BombDetonated(string npcId, int penalty)
        {
            NpcId = npcId;
            GlobalPenalty = penalty;
        }
    }
    
    public readonly struct RumorSpread : IEvent
    {
        public readonly string SourceNpcId;
        public readonly int GlobalPenalty;
        public readonly string Reason;
        public RumorSpread(string src, int penalty, string reason)
        {
            SourceNpcId = src;
            GlobalPenalty = penalty;
            Reason = reason;
        }
    }
}
