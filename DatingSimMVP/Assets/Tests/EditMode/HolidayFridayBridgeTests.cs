using NUnit.Framework;
using Game.Domain.Common;
using Game.Domain.Time;

public class HolidayFridayBridgeTests
{
    [Test]
    public void Friday_Holiday_Leads_To_SaturdayMorning()
    {
        var bus = new EventBus();
        var def = new SchoolCalendarDef
        {
            years = new()
            {
                new YearDef
                { year=1, weeks=20, holidays=new()
                    {
                        "1-5"
                    }
                }
            }
        };
        var cal = new JsonCalendarService(def);
        var t = new TimeService(bus, cal);

        t.Reset(1, 1, DOW.Fri);
        t.AdvancePhase();
        Assert.AreEqual(Phase.HolidayMorning, t.CurrentPhase);
        t.AdvancePhase();
        t.AdvancePhase();
        Assert.AreEqual(DOW.Sat, t.Current.Day);
        Assert.AreEqual(Phase.SaturdayMorning, t.CurrentPhase);
    }
}