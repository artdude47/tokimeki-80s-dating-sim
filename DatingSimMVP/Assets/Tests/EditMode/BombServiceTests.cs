using NUnit.Framework;
using Game.Domain.Common;
using Game.Domain.Relationships;
using Game.Domain.Time;
using Game.Application.Weekends;

public class BombServiceTests
{
    [Test]
    public void Arms_After_Threshold_And_Defuses_On_Date()
    {
        var bus = new EventBus();
        var cfg = new BombConfig{ WeeksToArm = 3, FuseWeeks = 2 };
        var bombs = new BombService(bus, cfg);

        bombs.EnsureTracked("npc_ash");
        // simulate weeks passing
        bus.Subscribe<WeekEnded>(e => bombs.OnWeekEnded(e));

        bus.Publish(new WeekEnded(1,1));
        Assert.IsFalse(bombs.IsArmed("npc_ash"));
        bus.Publish(new WeekEnded(1,2));
        Assert.IsFalse(bombs.IsArmed("npc_ash"));
        bus.Publish(new WeekEnded(1,3));
        Assert.IsTrue(bombs.IsArmed("npc_ash"));

        // date occurred → defuse
        var d = new GameDate(1,4,DOW.Sat);
        var occurred = new DateOccurred(d, "npc_ash", "arcade", DateOutcome.Success, 3);
        bombs.OnDateOccurred(occurred);
        Assert.IsFalse(bombs.IsArmed("npc_ash"));
    }
}
