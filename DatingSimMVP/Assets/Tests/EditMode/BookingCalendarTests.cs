using NUnit.Framework;
using System.Collections.Generic;
using Game.Domain.Time;
using static Game.Domain.Time.IBookingCalendar;

public sealed class BookingCalendarTests
{
    // Minimal fake calendar: defaults to Sat/Sun => Weekend, others => Weekday,
    // with an override map for specific dates (e.g., make Tue a Holiday).
    private sealed class FakeCalendar : ICalendarService
    {
        private readonly Dictionary<(int year, int week, DOW day), DayType> _overrides = new();

        public void SetDayType(int year, int week, DOW day, DayType type)
            => _overrides[(year, week, day)] = type;

        public DayType GetDayType(int year, int week, DOW day)
        {
            if (_overrides.TryGetValue((year, week, day), out var t)) return t;
            return (day == DOW.Sat || day == DOW.Sun) ? DayType.Weekend : DayType.Weekday;
        }

        // Not used by BookingCalendar but part of the interface elsewhere
        public int WeeksInYear(int year) => 10;
    }

    private FakeCalendar cal = null!;
    private BookingCalendar sut = null!;
    private static GameDate Date(int y, int w, DOW d) => new GameDate(y, w, d);

    [SetUp]
    public void Setup()
    {
        cal = new FakeCalendar();
        sut = new BookingCalendar(cal);
    }

    [Test]
    public void IsBookable_ReturnsTrue_OnWeekend()
    {
        Assert.IsTrue(sut.IsBookable(Date(1, 1, DOW.Sat)));
        Assert.IsTrue(sut.IsBookable(Date(1, 1, DOW.Sun)));
    }

    [Test]
    public void IsBookable_ReturnsTrue_OnHoliday()
    {
        cal.SetDayType(1, 1, DOW.Tue, DayType.Holiday);
        Assert.IsTrue(sut.IsBookable(Date(1, 1, DOW.Tue)));
    }

    [Test]
    public void IsBookable_ReturnsFalse_OnWeekday()
    {
        Assert.IsFalse(sut.IsBookable(Date(1, 1, DOW.Wed)));
    }

    [Test]
    public void BookDate_Fails_OnWeekday()
    {
        var d = Date(1, 1, DOW.Wed);
        Assert.IsFalse(sut.BookDate("npcA", d, "park"));
        Assert.IsTrue(sut.IsFreeForDate("npcA", d)); // still free (nothing booked)
    }

    [Test]
    public void BookDate_Succeeds_OnWeekend_ThenNotFree()
    {
        var d = Date(1, 1, DOW.Sat);
        Assert.IsTrue(sut.BookDate("npcA", d, "arcade"));
        Assert.IsFalse(sut.IsFreeForDate("npcA", d)); // booked now
    }

    [Test]
    public void BookDate_Succeeds_OnHoliday_ThenNotFree()
    {
        var d = Date(1, 1, DOW.Tue);
        cal.SetDayType(1, 1, DOW.Tue, DayType.Holiday);
        Assert.IsTrue(sut.BookDate("npcA", d, "cafe"));
        Assert.IsFalse(sut.IsFreeForDate("npcA", d));
    }

    [Test]
    public void DoubleBooking_SameDate_IsBlocked()
    {
        var d = Date(1, 1, DOW.Sat);
        Assert.IsTrue(sut.BookDate("npcA", d, "arcade"));
        Assert.IsFalse(sut.BookDate("npcB", d, "mall")); // cannot double-book
    }

    [Test]
    public void IsFreeForDate_BlockedForAllNpcs_WhenAnyBookingExists()
    {
        var d = Date(1, 1, DOW.Sat);
        Assert.IsTrue(sut.BookDate("npcA", d, "arcade"));
        // Different NPC still cannot book same date
        Assert.IsFalse(sut.IsFreeForDate("npcB", d));
    }

    [Test]
    public void TryGetBooking_ReturnsStoredNpcAndVenue()
    {
        var d = Date(1, 1, DOW.Sun);
        sut.BookDate("npcZ", d, "boardwalk");

        Assert.IsTrue(sut.TryGetBooking(d, out var b));
        Assert.AreEqual(("npcZ", "boardwalk"), (b.npcId, b.venueId));
    }

    [Test]
    public void TryGetBooking_ReturnsFalse_WhenNoBooking()
    {
        var d = Date(1, 1, DOW.Sun);
        Assert.IsFalse(sut.TryGetBooking(d, out _));
    }

    [Test]
    public void Snapshot_And_Restore_RoundTrip()
    {
        var a = Date(1, 1, DOW.Sat);
        var b = Date(1, 1, DOW.Sun);
        sut.BookDate("npcA", a, "arcade");
        sut.BookDate("npcB", b, "beach");

        var snap = sut.Snapshot();
        var sut2 = new BookingCalendar(cal);
        sut2.Restore(snap);

        Assert.IsTrue(sut2.TryGetBooking(a, out var ba));
        Assert.IsTrue(sut2.TryGetBooking(b, out var bb));
        Assert.AreEqual(("npcA", "arcade"), (ba.npcId, ba.venueId));
        Assert.AreEqual(("npcB", "beach"), (bb.npcId, bb.venueId));
    }
}
