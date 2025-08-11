namespace Game.Domain.Time
{
    public interface ICalendarService
    {
        // For now, sat/sun is weekend; holidays none
        DayType GetDayType(int year, int week, DOW day);
        int WeeksInYear(int year);
    }

    // Simple default for day 1
    public sealed class SimpleCalendarService : ICalendarService
    {
        private readonly int _weeksPerYear;
        public SimpleCalendarService(int weeksPerYear = 42) => _weeksPerYear = weeksPerYear;

        public DayType GetDayType(int year, int week, DOW day)
            => (day == DOW.Sat || day == DOW.Sun) ? DayType.Weekend : DayType.Weekday;

        public int WeeksInYear(int year) => _weeksPerYear;
    }
}