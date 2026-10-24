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

        // UI-320: the side a card belongs to, from its id ("base.sh.strategy.20"); null for one of neither side.
        public static Wotr.Contracts.Side? SideOf(string id) => id.Contains(".sh.") ? Wotr.Contracts.Side.Shadow
            : id.Contains(".fp.") ? Wotr.Contracts.Side.FreePeoples : (Wotr.Contracts.Side?)null;

        // UI-320: the line under a card's title: its deck and side ("Strategy · Shadow").
        public static string DeckLine(CardText card)
        {
            var deck = card.Deck == "special" ? "Special" : card.Deck == "character" ? "Character" : "Strategy";
            var side = SideOf(card.Id);
            return side.HasValue ? deck + " · " + (side == Wotr.Contracts.Side.Shadow ? "Shadow" : "Free Peoples") : deck;
        }
    }
}
