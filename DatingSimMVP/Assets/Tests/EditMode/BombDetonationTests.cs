using NUnit.Framework;
using Game.Domain.Common;
using Game.Domain.Relationships;
using Game.Domain.Time;
using System.Collections.Generic;
using System;

public class BombDetonationTests
{
    [Test]
    public void Detonation_Applies_Global_Penalty_And_Clears_Bomb()
    {
        var bus = new EventBus();
        var rels = new RelationshipState();
        rels.SetInitial("npc_ash", 10);
        rels.SetInitial("npc_jen", 20);
        rels.SetInitial("npc_max", 30);

        var cfg   = new BombConfig { WeeksToArm = 2, FuseWeeks = 1, GlobalPenalty = 4 };
        var bombs = new BombService(bus, cfg, rels);

        // IMPORTANT: track EVERY NPC that should participate
        bombs.EnsureTracked("npc_ash");
        bombs.EnsureTracked("npc_jen");
        bombs.EnsureTracked("npc_max");

        //Seed neglet to arm only ash
        bombs.Restore(
            new Dictionary<string, int>
            {
                ["npc_ash"] = 1,
                ["npc_jen"] = 0,
                ["npc_max"] = 0,
            },
            Array.Empty<string>(),
            new Dictionary<string, int>()
        );

        bus.Subscribe<WeekEnded>(e => bombs.OnWeekEnded(e));

        bool detonated = false;
        bus.Subscribe<BombDetonated>(_ => detonated = true);

        // Week 1 end → ash arms (neglect goes 0→1, then arming happens)
        bus.Publish(new WeekEnded(1, 1));
        Assert.IsTrue(bombs.IsArmed("npc_ash"), "Expected ash to be armed after first WeekEnded");

        // Week 2 end → ash fuse ticks 0→1 (>= FuseWeeks) → detonate
        bus.Publish(new WeekEnded(1, 2));
        Assert.IsTrue(detonated, "Expected BombDetonated event after second WeekEnded");
        Assert.IsFalse(bombs.IsArmed("npc_ash"), "Bomb should be cleared after detonation");

        // Global penalty should apply to OTHER tracked NPCs only
        Assert.AreEqual(20 - 4, rels.GetAffection("npc_jen"));
        Assert.AreEqual(30 - 4, rels.GetAffection("npc_max"));
        Assert.AreEqual(10,      rels.GetAffection("npc_ash")); // unchanged
    }
}
