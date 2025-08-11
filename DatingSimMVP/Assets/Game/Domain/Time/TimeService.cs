using System;
using Game.Domain.Common;

namespace Game.Domain.Time
{
    public interface ITimeService
    {
        GameDate Current { get; } // Year, week, day
        Phase CurrentPhase { get; }
        void Reset(int year = 1, int week = 1, DOW day = DOW.Mon);
        void StartNewWeek();
        void AdvancePhase();
    }

    public sealed class TimeService : ITimeService
    {
        private readonly EventBus _bus;
        private readonly ICalendarService _cal;
        public GameDate Current { get; }
        public Phase CurrentPhase { get; private set; } = Phase.Weekday;

        public TimeService(EventBus bus, ICalendarService calendar)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _cal = calendar ?? throw new ArgumentNullException(nameof(calendar));
            Current = new GameDate(1, 1, DOW.Mon);
        }

        public void Reset(int year = 1, int week = 1, DOW day = DOW.Mon)
        {
            Current.Set(year, week, day);
            CurrentPhase = Phase.Weekday;
            _bus.Publish(new WeekStarted(Current.Year, Current.Week));
            _bus.Publish(new PhaseStarted(Current, Phase.Weekday));
        }

        public void StartNewWeek()
        {
            Current.Set(Current.Year, Current.Week, DOW.Mon);
            CurrentPhase = Phase.Weekday;
            _bus.Publish(new WeekStarted(Current.Year, Current.Week));
            _bus.Publish(new PhaseStarted(Current, Phase.Weekday));
        }

        public void AdvancePhase()
        {
            // End the current phase
            _bus.Publish(new PhaseEnded(Current, CurrentPhase));

            if (CurrentPhase == Phase.Weekday)
            {
                if (Current.Day >= DOW.Mon && Current.Day <= DOW.Thu)
                {
                    //Next Weekday
                    Current.Set(Current.Year, Current.Week, Current.Day + 1);
                    CurrentPhase = Phase.Weekday;
                    _bus.Publish(new PhaseStarted(Current, CurrentPhase));
                    return;
                }
                if (Current.Day == DOW.Fri)
                {
                    // Weekend Starts
                    Current.Set(Current.Year, Current.Week, DOW.Sat);
                    CurrentPhase = Phase.SaturdayMorning;
                    _bus.Publish(new PhaseStarted(Current, CurrentPhase));
                    return;
                }
            }

            // Weekend phase flow
            switch (CurrentPhase)
            {
                case Phase.SaturdayMorning:
                    CurrentPhase = Phase.SaturdayDay;
                    _bus.Publish(new PhaseStarted(Current, CurrentPhase));
                    return;
                    case Phase.SaturdayDay:
                    Current.Set(Current.Year, Current.Week, DOW.Sun);
                    CurrentPhase = Phase.SundayMorning;
                    _bus.Publish(new PhaseStarted(Current, CurrentPhase));
                    return;
                case Phase.SundayMorning:
                    CurrentPhase = Phase.SundayDay;
                    _bus.Publish(new PhaseStarted(Current, CurrentPhase));
                    return;
                case Phase.SundayDay:
                    // Week ends -> roll to next week (and possibly next year)
                    _bus.Publish(new WeekEnded(Current.Year, Current.Week));
                    int nextWeek = Current.Week + 1;
                    int weeksInYear = _cal.WeeksInYear(Current.Year);
                    int nextYear = Current.Year;
                    if (nextWeek > weeksInYear)
                    {
                        nextWeek = 1;
                        nextYear = Math.Min(Current.Year + 1, 4); // cap at 4 for MVP
                    }
                    Current.Set(nextYear, nextWeek, DOW.Mon);
                    CurrentPhase = Phase.Weekday;
                    _bus.Publish(new WeekStarted(Current.Year, Current.Week));
                    _bus.Publish(new PhaseStarted(Current, CurrentPhase));
                    return;
            }

            throw new InvalidOperationException($"Unhandled phase {CurrentPhase}");
        }
    }
}
