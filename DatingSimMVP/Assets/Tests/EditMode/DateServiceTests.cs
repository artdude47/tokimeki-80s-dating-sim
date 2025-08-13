using NUnit.Framework;
using Game.Domain.Common;
using Game.Domain.Relationships;
using Game.Domain.Time;
using Game.Application.Weekends;
using static Game.Domain.Time.IBookingCalendar;
using static Game.Application.Weekends.DateOccurred;

public class DateServiceTests
{
    [Test]
    public void RunDate_Increases_Affection_And_Publishes_Events()
    {
        var bus = new EventBus();
        var calDef = new SchoolCalendarDef{ years = new(){ new YearDef{ year=1, weeks=10, holidays=new() } } };
        var cal = new JsonCalendarService(calDef);
        var bookings = new BookingCalendar(cal);
        var rels = new RelationshipState();
        rels.SetInitial("npc_ash", 10);

        var service = new DateService(bus, rels, bookings);

        var today = new GameDate(1,1,DOW.Sat);
        bookings.BookDate("npc_ash", today, "arcade");

        bool dateEventSeen = false;
        bus.Subscribe<DateOccurred>(e => dateEventSeen = true);

        var ok = service.TryRunTodayDate(today);
        Assert.IsTrue(ok);
        Assert.Greater(rels.GetAffection("npc_ash"), 10);
        Assert.IsTrue(dateEventSeen);
    }

    [Test]
    public void No_Booking_Returns_False()
    {
        var bus = new EventBus();
        var calDef = new SchoolCalendarDef{ years = new(){ new YearDef{ year=1, weeks=10, holidays=new() } } };
        var cal = new JsonCalendarService(calDef);
        var bookings = new BookingCalendar(cal);
        var rels = new RelationshipState();
        var service = new DateService(bus, rels, bookings);

        var today = new GameDate(1,1,DOW.Sun);
        Assert.IsFalse(service.TryRunTodayDate(today));
    }
}
