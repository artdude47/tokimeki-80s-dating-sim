using System.Collections.Generic;
using Game.Domain.Save;
using Game.Domain.Stats;
using Game.Domain.Time;
using Game.Domain.Relationships;
using Game.Application.Commands;

namespace Game.Application.Save
{
    public static class GameStateRestorer
    {
        public static void Restore(
            GameState s,
            TimeService time,
            StatBlock stats,
            ICommandService commands,
            RelationshipState rels,
            BombService bombs,
            IBookingCalendar bookings)
        {
            time.RestoreTo(s.Year, s.Week, (DOW)s.Day, (Phase)s.Phase);

            void SetStat(Stat st, int v)
            {
                var current = stats[st];
                stats.Add(st, v - current);
            }

            SetStat(Stat.Academics, s.Stats["Academics"]);
            SetStat(Stat.Art, s.Stats["Art"]);
            SetStat(Stat.Athletics, s.Stats["Athletics"]);
            SetStat(Stat.Stamina, s.Stats["Stamina"]);
            SetStat(Stat.Charm, s.Stats["Charm"]);
            SetStat(Stat.Guts, s.Stats["Guts"]);
            SetStat(Stat.Stress, s.Stats["Stress"]);
            SetStat(Stat.GenKnowledge, s.Stats["GenKnowledge"]);

            if (commands is CommandService cs) cs.RestoreState(s.CurrentCommandId, s.LastWeekCommandId, s.SameCommandStreak);

            rels.Restore(s.Affection);

            var dict = new Dictionary<(int, int, int), (string, string)>();
            if (s.Bookings != null)
            {
                foreach (var kv in s.Bookings)
                {
                    var parts = kv.Key.Split('-');
                    dict[(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]))] = kv.Value;
                }
            }
            bookings.Restore(dict);

            var armedSet = new HashSet<string>(s.BombArmed ?? new List<string>());
            bombs.Restore(s.BombCounters ?? new Dictionary<string, int>(), armedSet, new Dictionary<string, int>()); //Fuse not saved in Day 3; will rebuild
        }
    }
}
