using Game.Domain.Time;

namespace Game.Domain.Common
{
    // One canonical outcome enum used everywhere
    public enum DateOutcome { Success, Awkward, NoShow }

    /// <summary>
    /// Raised when a date actually takes place.
    /// Published by Application's DateService, consumed by Domain/Presentation.
    /// </summary>
    public readonly struct DateOccurred : IEvent
    {
        public readonly GameDate Date;
        public readonly string NpcId;
        public readonly string VenueId;
        public readonly DateOutcome Outcome;
        public readonly int AffectionDelta;

        public DateOccurred(GameDate date, string npcId, string venueId, DateOutcome outcome, int affectionDelta)
        {
            Date = date;
            NpcId = npcId;
            VenueId = venueId;
            Outcome = outcome;
            AffectionDelta = affectionDelta;
        }
    }
}
