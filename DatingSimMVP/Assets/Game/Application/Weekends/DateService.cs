using Game.Domain.Common;
using Game.Domain.Relationships;
using Game.Domain.Time;

namespace Game.Application.Weekends
{
    public enum DateOutcome { Success, Awkward, NoShow }

    public readonly struct DateOccurred : IEvent
    {
        public readonly GameDate Date;
        public readonly string NpcId;
        public readonly string VenueId;
        public readonly DateOutcome Outcome;
        public readonly int AffectionDelta;
        public DateOccurred(GameDate d, string npc, string venue, DateOutcome outcome, int delta)
        {
            Date = d;
            NpcId = npc;
            VenueId = venue;
            Outcome = outcome;
            AffectionDelta = delta;
        }

        public interface IDateService
        {
            bool TryRunTodayDate(GameDate today);
        }

        public sealed class DateService : IDateService
        {
            private readonly EventBus _bus;
            private readonly RelationshipState _rels;
            private readonly IBookingCalendar _bookings;

            public DateService(EventBus bus, RelationshipState rels, IBookingCalendar bookings)
            {
                _bus = bus;
                _rels = rels;
                _bookings = bookings;
            }

            public bool TryRunTodayDate(GameDate today)
            {
                if (!_bookings.TryGetBooking(today, out var b)) return false;

                // Minimal scoring: base + small venue bonus; awkward if affection low
                int baseDelta = (_rels.GetAffection(b.npcId) >= 40) ? 3 : 1;
                int venueBonus = 1; //TODO: Implement per-npc venue prefs
                int delta = baseDelta + venueBonus;

                var outcome = (_rels.GetAffection(b.npcId) >= 20) ? DateOutcome.Success : DateOutcome.Awkward;
                var newAff = _rels.AddAffection(b.npcId, delta);
                _bus.Publish(new AffectionChanged(b.npcId, newAff));
                _bus.Publish(new DateOccurred(today, b.npcId, b.venueId, outcome, delta));
                return true;
            }
        }
    }
}