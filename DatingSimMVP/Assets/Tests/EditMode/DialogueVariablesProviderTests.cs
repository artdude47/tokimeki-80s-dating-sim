using NUnit.Framework;
using Game.Infrastructure.Dialogue;
using Game.Domain.Stats;
using Game.Domain.Relationships;
using Game.Domain.Time;
using Game.Domain.Common;

public class DialogueVariablesProviderTests
{
    [Test]
    public void Snapshot_Contains_Time_Stats_And_Npc_Flags()
    {
        var bus = new EventBus();
        var cal = new SimpleCalendarService(42);
        var time = new TimeService(bus, cal);
        var stats = new StatBlock();
        stats.Add(Stat.Academics, 12);
        var rels = new RelationshipState();
        rels.SetInitial("npc_ash", 33);
        var bombs = new BombService(bus, new BombConfig(), rels);
        bombs.EnsureTracked("npc_ash");

        var prov = new DialogueVariablesProvider(time, stats, rels, bombs);
        var snap = prov.Snapshot();
        Assert.AreEqual(1, snap["YEAR"]);
        Assert.AreEqual(12, snap["ACAD"]);
        Assert.AreEqual(33, snap["AFF_npc_ash"]);
        Assert.IsFalse((bool)snap["BOMB_npc_ash"]);
    }
}
