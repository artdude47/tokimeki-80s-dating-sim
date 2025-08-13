using System.Collections.Generic;
using Game.Domain.Stats;
using Game.Domain.Time;

namespace Game.Domain.Save
{
    public sealed class GameState
    {
        //time
        public int Year, Week, Day;
        public int Phase;

        // stats
        public Dictionary<string, int> Stats;

        //command
        public string CurrentCommandId;
        public int SameCommandStreak;
        public string LastWeekCommandId;

        //relationships
        public Dictionary<string, int> Affection;

        //bombs
        public Dictionary<string, int> BombCounters;
        public List<string> BombArmed;

        //bookings
        public Dictionary<string, (string npc, string venue)> Bookings;
    }
}