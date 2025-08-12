using System.Collections.Generic;

namespace Game.Domain.Time
{
    [System.Serializable]
    public class YearDef
    {
        public int year;
        public int weeks;
        public List<string> holidays;
    }

    [System.Serializable]
    public class SchoolCalendarDef
    {
        public List<YearDef> years;
    }
}