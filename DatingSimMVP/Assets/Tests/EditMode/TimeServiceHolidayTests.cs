using NUnit.Framework;
using Game.Domain.Common;
using Game.Domain.Time;
using System.Collections.Generic;

public class TimeServiceHolidayTests
{
    private TimeService Make(int weeks = 10, params string[] holidays)
    {
        var bus = new EventBus();
        var calDef = new SchoolCalendarDef
        {
            years = new()
            {
                new YearDef {
                    year = 1,
                    weeks = weeks,
                    holidays = new System.Collections.Generic.List<string>(holidays)
                }
            }
        };
        var cal = new JsonCalendarService(calDef);
        return new TimeService(bus, cal);
    }

    [Test]
    public void Tuesday_Holiday_Acts_Like_Weekend_Day()
    {
        var t = Make(10, "1-2"); // week 1, day=2(Tue)
        t.Reset(1, 1, DOW.Mon); // Weekday(Mon) auto started
        t.AdvancePhase(); // end Mon-> start Tue
        Assert.AreEqual(DOW.Tue, t.Current.Day);
        // Because Tue is holiday, Phase should be HolidayMorning
        Assert.AreEqual(Phase.HolidayMorning, t.CurrentPhase);
        t.AdvancePhase(); // HolidayDay
        Assert.AreEqual(Phase.HolidayDay, t.CurrentPhase);
        t.AdvancePhase(); // Move to Wed Weekday
        Assert.AreEqual(DOW.Wed, t.Current.Day);
        Assert.AreEqual(Phase.Weekday, t.CurrentPhase);
    }
}