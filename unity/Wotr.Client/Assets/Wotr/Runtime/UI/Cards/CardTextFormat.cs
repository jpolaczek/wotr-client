namespace Wotr.Client
{
    // One wording for a card's kind line and Combat heading, wherever its full text is shown.
    public static class CardTextFormat
    {
        public static string Kind(CardText card)
        {
            var deck = card.Deck == "special" ? "Special card" : card.Deck == "character" ? "Character card" : "Strategy card";
            var type = card.Deck == "special" ? "Special event" : card.EventType + " event";
            return (deck + " · " + type).ToUpperInvariant();
        }

        public static string CombatHeading(CardText card) => card.HasCombat
            ? ("Combat · Initiative " + string.Join("/", card.Initiative) + " · " + card.CombatTitle).ToUpperInvariant()
            : "";
    }
}
