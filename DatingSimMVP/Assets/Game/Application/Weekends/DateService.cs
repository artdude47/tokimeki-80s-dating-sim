using System;
using Game.Domain.Common;        // DateOccurred, DateOutcome, IEvent, EventBus
using Game.Domain.Relationships; // RelationshipState
using Game.Domain.Time;          // GameDate, IBookingCalendar

namespace Game.Application.Weekends
{
    public interface IDateService
    {
        /// <summary>
        /// If a booking exists for <paramref name="today"/>, runs the date,
        /// updates affection, and publishes DateOccurred & AffectionChanged.
        /// Returns true if a date ran; false if no booking.
        /// </summary>
        bool TryRunTodayDate(GameDate today);
    }

    public sealed class DateService : IDateService
    {
        private readonly EventBus _bus;
        private readonly RelationshipState _rels;
        private readonly IBookingCalendar _bookings;

        public DateService(EventBus bus, RelationshipState rels, IBookingCalendar bookings)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _rels = rels ?? throw new ArgumentNullException(nameof(rels));
            _bookings = bookings ?? throw new ArgumentNullException(nameof(bookings));
        }

        public bool TryRunTodayDate(GameDate today)
        {
            if (!_bookings.TryGetBooking(today, out var b)) return false;

            // Minimal scoring: base + small venue bonus; awkward if affection low
            int baseDelta  = (_rels.GetAffection(b.npcId) >= 40) ? 3 : 1;
            int venueBonus = 1; // TODO: per-NPC venue prefs
            int delta = baseDelta + venueBonus;

            var outcome = (_rels.GetAffection(b.npcId) >= 20) ? DateOutcome.Success : DateOutcome.Awkward;
            var newAff  = _rels.AddAffection(b.npcId, delta);

            _bus.Publish(new AffectionChanged(b.npcId, newAff));
            _bus.Publish(new DateOccurred(today, b.npcId, b.venueId, outcome, delta));
            return true;
        }
    }
}
