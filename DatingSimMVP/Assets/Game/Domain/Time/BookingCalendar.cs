using System.Collections.Generic;

namespace Game.Domain.Time
{
    public interface IBookingCalendar
    {
        bool IsBookable(GameDate date);
        bool IsFreeForDate(string npcId, GameDate date);
        bool BookDate(string npcId, GameDate date, string venueId);
        bool TryGetBooking(GameDate date, out (string npcId, string venueId) booking);
        IReadOnlyDictionary<(int year, int week, int day), (string npc, string venue)> Snapshot();
        void Restore(IReadOnlyDictionary<(int, int, int), (string, string)> data);

        public sealed class BookingCalendar : IBookingCalendar
        {
            private readonly ICalendarService _cal;
            private readonly Dictionary<(int, int, int), (string, string)> _bookings = new();
            public BookingCalendar(ICalendarService cal)
            {
                _cal = cal;
            }

            public bool IsBookable(GameDate date)
            {
                var t = _cal.GetDayType(date.Year, date.Week, date.Day);
                return t == DayType.Weekend || t == DayType.Holiday;
            }

            public bool IsFreeForDate(string npcId, GameDate date)
            {
                return !_bookings.ContainsKey((date.Year, date.Week, (int)date.Day));
            }

            public bool BookDate(string npcId, GameDate date, string venueId)
            {
                if (!IsBookable(date)) return false;
                var key = (date.Year, date.Week, (int)date.Day);
                if (_bookings.ContainsKey(key)) return false;
                _bookings[key] = (npcId, venueId);
                return true;
            }

            public bool TryGetBooking(GameDate date, out (string npcId, string venueId) booking)
            {
                var key = (date.Year, date.Week, (int)date.Day);
                if (_bookings.TryGetValue(key, out var b))
                {
                    booking = b;
                    return true;
                }
                booking = default;
                return false;
            }

            public IReadOnlyDictionary<(int, int, int), (string, string)> Snapshot() => _bookings;

            public void Restore(IReadOnlyDictionary<(int, int, int), (string, string)> data)
            {
                _bookings.Clear();
                foreach (var kv in data)
                {
                    _bookings[kv.Key] = kv.Value;
                }
            }
        }
    }
}
