using NUnit.Framework;
using System.IO;
using Game.Domain.Stats;
using Game.Domain.Time;
using Game.Domain.Save;
using Game.Domain.Relationships;
using Game.Infrastructure.Save;
using Game.Application.Save;
using Game.Domain.Common;
using static Game.Domain.Time.IBookingCalendar;

public class SaveBasicsTests
{
    [Test]
    public void BuildSnapshot_Serializes_And_Deserializes()
    {
        var bus = new EventBus();
        var cal = new SimpleCalendarService(42);
        var time = new TimeService(bus, cal);
        time.Reset(1, 5, DOW.Fri);

        var stats = new StatBlock();
        stats.Add(Stat.Academics, 12);
        stats.Add(Stat.Stress, 3);

        var rels = new RelationshipState();
        rels.SetInitial("npc_jen", 25);

        var booking = new BookingCalendar(cal);
        booking.BookDate("npc_jen", new GameDate(1,5,DOW.Sun), "diner");

        var bombs = new BombService(bus, new BombConfig{ WeeksToArm=8, FuseWeeks=3 });
        bombs.EnsureTracked("npc_jen");

        var snapshot = GameStateAssembler.BuildSnapshot(time, time.CurrentPhase, stats,
            currentCmd: "Study", sameCmdStreak: 1, lastCmd: "Study",
            rels, bombs, booking);

        var svc = new FileSaveService();
        var path = Path.Combine(UnityEngine.Application.persistentDataPath, "test_save.json");
        File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(snapshot, Newtonsoft.Json.Formatting.Indented));
        var loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<GameState>(File.ReadAllText(path));

        Assert.AreEqual(1, loaded.Year);
        Assert.AreEqual(5, loaded.Week);
        Assert.AreEqual((int)DOW.Fri, loaded.Day);
        Assert.AreEqual("Study", loaded.CurrentCommandId);
        Assert.IsTrue(loaded.Affection.ContainsKey("npc_jen"));
    }
}
