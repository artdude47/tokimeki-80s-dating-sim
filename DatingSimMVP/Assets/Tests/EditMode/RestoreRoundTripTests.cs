using NUnit.Framework;
using System.Collections.Generic;
using Game.Domain.Common;
using Game.Domain.Stats;
using Game.Domain.Time;
using Game.Domain.Save;
using Game.Domain.Relationships;
using Game.Application.Commands;
using Game.Application.Save;
using static Game.Domain.Time.IBookingCalendar;

public class RestoreRoundTripTests
{
    [Test]
    public void Restore_Puts_Systems_Back_And_Bookings_Work()
    {
        var bus = new EventBus();
        var cal = new SimpleCalendarService(42);
        var time = new TimeService(bus, cal);
        var stats = new StatBlock();
        var rels = new RelationshipState();
        var bookings = new BookingCalendar(cal);
        var bombs = new BombService(bus, new BombConfig(), rels);
        var cmd = new CommandService(bus, stats);
        cmd.SetCatalog(new System.Collections.Generic.Dictionary<string,Game.Domain.Commands.CommandDef>()); // empty ok

        // set some state
        time.RestoreTo(1, 5, DOW.Sat, Phase.SaturdayMorning);
        stats.Add(Stat.Academics, 25);
        rels.SetInitial("npc_jen", 30);
        bookings.BookDate("npc_jen", new GameDate(1,5,DOW.Sun), "diner");
        bombs.EnsureTracked("npc_jen");

        // snapshot (reuse Day 3 assembler)
        var snap = Game.Application.Save.GameStateAssembler.BuildSnapshot(
            time, time.CurrentPhase, stats,
            cmd.CurrentCommandId, cmd.SameCommandStreak, cmd.LastWeekCommandId,
            rels, bombs, bookings);

        // wipe and restore to fresh instances
        var time2 = new TimeService(bus, cal);
        var stats2 = new StatBlock();
        var rels2 = new RelationshipState();
        var bookings2 = new BookingCalendar(cal);
        var bombs2 = new BombService(bus, new BombConfig(), rels2);
        var cmd2 = new CommandService(bus, stats2);
        cmd2.SetCatalog(new System.Collections.Generic.Dictionary<string,Game.Domain.Commands.CommandDef>());

        GameStateRestorer.Restore(snap, time2, stats2, cmd2, rels2, bombs2, bookings2);

        Assert.AreEqual(1, time2.Current.Year);
        Assert.AreEqual(5, time2.Current.Week);
        Assert.AreEqual(DOW.Sat, time2.Current.Day);
        Assert.AreEqual(25, stats2[Stat.Academics]);
        Assert.AreEqual(30, rels2.GetAffection("npc_jen"));
        Assert.IsTrue(bookings2.TryGetBooking(new GameDate(1,5,DOW.Sun), out var b));
        Assert.AreEqual("npc_jen", b.npcId);
    }
}
