using NUnit.Framework;
using Game.Domain.Common;
using Game.Domain.Relationships;
using Game.Domain.Time;
using Game.Application.Weekends;
using static Game.Domain.Time.IBookingCalendar;

public class PhoneServiceTests
{
    private (PhoneService phone, RelationshipState rels, BookingCalendar bookings, TimeService time) Make(int affAsh = 50)
    {
        var bus = new EventBus();
        var calDef = new SchoolCalendarDef{ years = new(){ new YearDef{ year=1, weeks=10, holidays=new() } } };
        var cal = new JsonCalendarService(calDef);
        var bookings = new BookingCalendar(cal);
        var rels = new RelationshipState();
        rels.SetInitial("npc_ash", affAsh);
        var phone = new PhoneService(bus, rels, bookings, rng: (a,b)=>1); // force best roll
        var time = new TimeService(bus, cal);
        return (phone, rels, bookings, time);
    }

    [Test]
    public void High_Affection_Accepts_And_Books()
    {
        var (phone, rels, bookings, time) = Make(affAsh:60);
        var target = new GameDate(1,1,DOW.Sat);
        var resp = phone.ProposeDate("npc_ash", target, "arcade");
        Assert.AreEqual(DateResponse.Accept, resp);
        Assert.IsTrue(bookings.TryGetBooking(target, out var b));
        Assert.AreEqual("npc_ash", b.npcId);
    }

    [Test]
    public void Busy_When_Slot_Already_Booked()
    {
        var (phone, rels, bookings, time) = Make(affAsh:60);
        var target = new GameDate(1,1,DOW.Sun);
        Assert.AreEqual(DateResponse.Accept, phone.ProposeDate("npc_ash", target, "park"));
        Assert.AreEqual(DateResponse.Busy,   phone.ProposeDate("npc_jen", target, "diner"));
    }

    [Test]
    public void Rejects_On_Weekday()
    {
        var (phone, rels, bookings, time) = Make(affAsh:60);
        var target = new GameDate(1,1,DOW.Tue);
        Assert.AreEqual(DateResponse.Reject, phone.ProposeDate("npc_ash", target, "arcade"));
    }
}
