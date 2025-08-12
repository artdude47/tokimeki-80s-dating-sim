using System.Collections.Generic;
using Game.Domain.Stats;

namespace Game.Domain.Commands
{
    [System.Serializable]
    public class CommandDef
    {
        public string id;
        public List<StringRange> inc;
        public List<StringRange> dec;
        public IntRange stress;
        public IntRange staminaCost;
        public float repeatDecay;
    }

    [System.Serializable]
    public class StringRange
    {
        public string stat;
        public int min;
        public int max;
    }

    [System.Serializable]
    public class IntRange
    {
        public int min; public int max;
    }

    public static class CommandDefExtensions
    {
        public static Stat ParseStat(string s)
        {
            return (Stat)System.Enum.Parse(typeof(Stat), s);
        }
    }
}
