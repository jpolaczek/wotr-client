using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-568 (owner, 2026-11-22, after a player's game: Faramir's Rangers recruited nothing and the game did not say why): what
    // a card leaves out when it is played now, read from the public board. Faramir's Rangers recruits a Gondor unit and a
    // Gondor Leader in Osgiliath only while a Free Peoples Army stands there (its text; Almanac, "Faramir's Rangers"); without
    // one it is the roll alone. The hand's hint says so before the play, and the card's dice window after the roll.
    public static class CardPlayNotes
    {
        public const string FaramirsRangers = "base.fp.strategy.06";
        public const string Osgiliath = "osgiliath";
        public const string BeforePlay = "No Free Peoples Army in Osgiliath: the roll only, no recruitment";
        public const string AfterRoll = "No Free Peoples Army in Osgiliath · no recruitment";

        // The hand's note for a card about to be played, or "".
        public static string Play(PlayerView view, string card) => Misses(view, card) ? BeforePlay : "";

        // The dice window's note for the card that rolled, or "".
        public static string Roll(PlayerView view, string card) => Misses(view, card) ? AfterRoll : "";

        private static bool Misses(PlayerView view, string card) =>
            card == FaramirsRangers && view != null && !FreeArmyIn(view, Osgiliath);

        public static bool FreeArmyIn(PlayerView view, string region) => view.Units.Any(unit => unit.Location == region &&
            unit.Side == Side.FreePeoples && unit.Kind is UnitKind.Regular or UnitKind.Elite or UnitKind.Leader);
    }
}
