using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Time
{
    public sealed class JsonCalendarService : ICalendarService
    {
        private readonly Dictionary<int, YearDef> _years = new();
        private readonly HashSet<(int week, int day)> _holidaySet = new();

        public JsonCalendarService(SchoolCalendarDef def)
        {
            if (def?.years == null) return;
            foreach (var y in def.years)
            {
                _years[y.year] = y;
                if (y.holidays != null)
                {
                    foreach (var hd in y.holidays)
                    {
                        var parts = hd.Split('-');
                        if (parts.Length == 2 && int.TryParse(parts[0], out var w) &&
                            int.TryParse(parts[1], out var d))
                            _holidaySet.Add((w, d));
                    }
                }
            }
        }

        public DayType GetDayType(int year, int week, DOW day)
        {
            // holiday should override weekday
            if (_holidaySet.Contains((week, (int)day))) return DayType.Holiday;
            return (day == DOW.Sat || day == DOW.Sun) ? DayType.Weekend : DayType.Weekday;
        }

        public int WeeksInYear(int year)
        {
            if (_years.TryGetValue(year, out var y)) return y.weeks;
            // default
            return 42;
        }
    }
}