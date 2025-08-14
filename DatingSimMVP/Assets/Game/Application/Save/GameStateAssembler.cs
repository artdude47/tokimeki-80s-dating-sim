using System.Collections.Generic;
using Game.Domain.Relationships;
using Game.Domain.Save;
using Game.Domain.Stats;
using Game.Domain.Time;

namespace Game.Application.Save
{
    public static class GameStateAssembler
    {
        public static GameState BuildSnapshot(
            ITimeService time, Phase currentPhase,
            StatBlock stats, string currentCmd, int sameCmdStreak, string lastCmd,
            RelationshipState rels,
            BombService bombs,
            IBookingCalendar bookings)
        {
            var s = new GameState
            {
                Year = time.Current.Year,
                Week = time.Current.Week,
                Day = (int)time.Current.Day,
                Phase = (int)currentPhase,
                Stats = new Dictionary<string, int>
                {
                    ["Academics"] = stats[Stat.Academics],
                    ["Art"] = stats[Stat.Art],
                    ["Athletics"] = stats[Stat.Athletics],
                    ["Stamina"] = stats[Stat.Stamina],
                    ["Charm"] = stats[Stat.Charm],
                    ["Guts"] = stats[Stat.Guts],
                    ["Stress"] = stats[Stat.Stress],
                    ["GenKnowledge"] = stats[Stat.GenKnowledge],
                },
                CurrentCommandId = currentCmd,
                SameCommandStreak = sameCmdStreak,
                LastWeekCommandId = lastCmd,
                Affection = new Dictionary<string, int>(rels.Snapshot())
            };

            //bombs
            var (counters, armed, fuse) = bombs.Snapshot();
            s.BombCounters = new Dictionary<string, int>(counters);
            s.BombArmed = new List<string>(armed);

            //bookings
            s.Bookings = new Dictionary<string, (string, string)>();
            foreach (var kv in bookings.Snapshot())
            {
                var (y, w, d) = kv.Key; var (npc, venue) = kv.Value;
                s.Bookings[$"{y}-{w}-{d}"] = (npc, venue);
            }

            return s;
        }
    }
}
