using NUnit.Framework;
using Game.Domain.Common;
using Game.Domain.Time;
using System.Collections.Generic;

public class TimeServiceTokimekiTests
{
    private sealed class Spy
    {
        public readonly List<(int y, int w, DOW d, Phase p)> started = new();
        public readonly List<(int y, int w, DOW d, Phase p)> ended = new();
        public readonly List<(int y, int w)> weekStarted = new();
        public readonly List<(int y, int w)> weekEnded = new();

        public void Wire(EventBus bus)
        {
            bus.Subscribe<PhaseStarted>(e => started.Add((e.Date.Year, e.Date.Week, e.Date.Day, e.Phase)));
            bus.Subscribe<PhaseEnded>(e => ended.Add((e.Date.Year, e.Date.Week, e.Date.Day, e.Phase)));
            bus.Subscribe<WeekStarted>(e => weekStarted.Add((e.Year, e.Week)));
            bus.Subscribe<WeekEnded>(e => weekEnded.Add((e.Year, e.Week)));
        }
    }

    [Test]
    public void Weekday_Ticks_Mon_To_Fri_Then_SaturdayMorning()
    {
        var bus = new EventBus();
        var spy = new Spy(); spy.Wire(bus);
        var cal = new SimpleCalendarService(42);
        var t = new TimeService(bus, cal);

        t.Reset(1, 1, DOW.Mon);
        // Mon -> Tue -> Wed -> Thu -> Fri -> SatMorning
        t.AdvancePhase(); // end Mon weekday, start Tue weekday
        t.AdvancePhase(); // Wed
        t.AdvancePhase(); // Thu
        t.AdvancePhase(); // Fri
        t.AdvancePhase(); // SatMorning

        Assert.AreEqual(DOW.Sat, t.Current.Day);
        Assert.AreEqual(Phase.SaturdayMorning, t.CurrentPhase);
        Assert.Contains((1, 1, DOW.Sat, Phase.SaturdayMorning), spy.started);
    }
    
    [Test]
    public void Weekend_Phases_Then_Week_Rollover()
    {
        var bus = new EventBus();
        var spy = new Spy(); spy.Wire(bus);
        var cal = new SimpleCalendarService(42);
        var t = new TimeService(bus, cal);

        t.Reset(1,1,DOW.Fri);
        // Fri -> SatMorning
        t.AdvancePhase();
        // SatMorning -> SatDay -> SunMorning -> SunDay -> WeekEnded -> next Week Mon
        t.AdvancePhase(); // SatDay
        t.AdvancePhase(); // SunMorning
        t.AdvancePhase(); // SunDay
        t.AdvancePhase(); // Week end -> Week 2 Mon Weekday

        Assert.AreEqual(1, t.Current.Year);
        Assert.AreEqual(2, t.Current.Week);
        Assert.AreEqual(DOW.Mon, t.Current.Day);
        Assert.AreEqual(Phase.Weekday, t.CurrentPhase);
        Assert.Contains((1,1), spy.weekEnded);
        Assert.Contains((1,2), spy.weekStarted);
    }
}
