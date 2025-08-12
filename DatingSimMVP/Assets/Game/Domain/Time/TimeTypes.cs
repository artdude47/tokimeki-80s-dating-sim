using System;
using Game.Domain.Common;


namespace Game.Domain.Time
{
    public enum DOW { Mon = 1, Tue, Wed, Thu, Fri, Sat, Sun }
    public enum DayType { Weekday, Weekend, Holiday }
    public enum Phase { Weekday, SaturdayMorning, SaturdayDay, SundayMorning, SundayDay, HolidayMorning, HolidayDay }

    public sealed class GameDate
    {
        public int Year { get; private set; }
        public int Week { get; private set; }
        public DOW Day { get; private set; }

        public GameDate(int year, int week, DOW day)
        {
            Year = year;
            Week = week;
            Day = day;
        }

        public void Set(int year, int week, DOW day)
        {
            Year = year;
            Week = week;
            Day = day;
        }
    }

    //Events
    public readonly struct WeekStarted : IEvent
    {
        public readonly int Year, Week;
        public WeekStarted(int y, int w)
        {
            Year = y;
            Week = w;
        }
    }

    public readonly struct PhaseStarted : IEvent
    {
        public readonly GameDate Date;
        public readonly Phase Phase;
        public PhaseStarted(GameDate d, Phase p)
        {
            Date = d;
            Phase = p;
        }
    }

    public readonly struct PhaseEnded : IEvent
    {
        public readonly GameDate Date;
        public readonly Phase Phase;
        public PhaseEnded(GameDate d, Phase p)
        {
            Date = d;
            Phase = p;
        }
    }
    public readonly struct WeekEnded : IEvent
    {
        public readonly int Year, Week;
        public WeekEnded(int y, int w)
        {
            Year = y;
            Week = w;
        }
    }

}
