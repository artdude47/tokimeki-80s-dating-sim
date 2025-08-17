using System;
using Game.Domain.Common;
using Game.Domain.Relationships;
using Game.Domain.Time;

namespace Game.Application.Weekends
{
    public enum DateResponse { Accept, Reject, Busy }

    public interface IPhoneService
    {
        DateResponse ProposeDate(string npcId, GameDate target, string venueId);
    }

    public sealed class PhoneService : IPhoneService
    {
        private readonly EventBus _bus;
        private readonly RelationshipState _rels;
        private readonly IBookingCalendar _bookings;
        private readonly Func<int, int, int> _rng01to100;

        public PhoneService(EventBus bus, RelationshipState rels, IBookingCalendar bookings, Func<int, int, int> rng = null)
        {
            _bus = bus;
            _rels = rels;
            _bookings = bookings;
            if (rng != null)
                _rng01to100 = rng;
            else
                _rng01to100 = (min, max) => new Random().Next(min, max + 1);
        }

        public (int chance, string reason) PreviewAcceptance(string npcId, GameDate target)
        {
            if (!_bookings.IsBookable(target)) return (0, "Not a weekend/holiday");
            if (!_bookings.IsFreeForDate(npcId, target)) return (0, "Already booked");

            var aff = _rels.GetAffection(npcId);
            int baseChance =
                aff >= 60 ? 90 :
                aff >= 40 ? 70 :
                aff >= 20 ? 50 : 30;

            string reason = $"Affection {aff} ⇒ base {baseChance}%";
            return (baseChance, reason);
        }

        public DateResponse ProposeDate(string npcId, GameDate target, string venueId)
        {
            if (!_bookings.IsBookable(target)) return DateResponse.Reject;
            if (!_bookings.IsFreeForDate(npcId, target)) return DateResponse.Busy;

            //Simple acceptance heuristic: affection gates + small RNG
            var aff = _rels.GetAffection(npcId);
            int baseChance =
                aff >= 60 ? 90 :
                aff >= 40 ? 70 :
                aff >= 20 ? 50 : 30;

            int roll = _rng01to100(1, 100);
            if (roll <= baseChance)
            {
                if (_bookings.BookDate(npcId, target, venueId))
                    return DateResponse.Accept;
                return DateResponse.Busy;
            }
            return DateResponse.Reject;
        }
    }
}
