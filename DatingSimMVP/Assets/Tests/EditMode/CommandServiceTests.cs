using NUnit.Framework;
using System.Collections.Generic;
using Game.Domain.Common;
using Game.Domain.CommonEvents;
using Game.Domain.Commands;
using Game.Domain.Stats;
using Game.Domain.Time;
using Game.Application.Commands;
using JetBrains.Annotations;

public class CommandServiceTests
{
    private class FixedRng : IRandomizer
    {
        private readonly int _v;
        public FixedRng(int v) { _v = v; }
        public int Range(int min, int max) => _v;
    }

    private Dictionary<string, CommandDef> Catalog()
    {
        return new Dictionary<string, CommandDef>
        {
            ["Study"] = new CommandDef
            {
                inc = new() { new StringRange { stat = "Academics", min = 2, max = 4 } },
                dec = new() { new StringRange { stat = "Athletics", min = 0, max = 1 } },
                stress = new IntRange { min = 1, max = 2 },
                staminaCost = new IntRange { min = 1, max = 2 },
                repeatDecay = 0.9f
            }
        };
    }

    [Test]
    public void Applies_One_Day_Effects_On_Weekday_Phase()
    {
        var bus = new EventBus();
        var stats = new StatBlock();
        var cmd = new CommandService(bus, stats, new FixedRng(3));
        cmd.SetCatalog(Catalog());
        cmd.SelectWeekdayCommand("Study");

        //Simulate monday weekday start
        var date = new GameDate(1, 1, DOW.Mon);
        bus.Publish(new WeekStarted(1, 1));
        bus.Publish(new PhaseStarted(date, Phase.Weekday));

        Assert.GreaterOrEqual(stats[Stat.Academics], 2);
        Assert.LessOrEqual(stats[Stat.Athletics], 0);
    }

    [Test]
    public void Repeat_Decay_Applies_Second_Week()
    {
        var bus = new EventBus();
        var stats = new StatBlock();
        var cmd = new CommandService(bus, stats, new FixedRng(4)); // choose max value each time
        cmd.SetCatalog(Catalog());
        cmd.SelectWeekdayCommand("Study");

        // Week 1 Monday
        bus.Publish(new WeekStarted(1,1));
        bus.Publish(new PhaseStarted(new GameDate(1,1,DOW.Mon), Phase.Weekday));
        int week1Acad = stats[Stat.Academics];

        // Week 2 Monday (same command)
        bus.Publish(new WeekStarted(1,2)); // streak increments
        bus.Publish(new PhaseStarted(new GameDate(1,2,DOW.Mon), Phase.Weekday));
        int week2Delta = stats[Stat.Academics] - week1Acad;

        Assert.Less(week2Delta, 4); // decay reduced the gain from raw 4
    }
}