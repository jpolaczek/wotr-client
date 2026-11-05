using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-456 (owner, 2026-11-03, kings-far-sighted): Far-sighted's "Move companions" moves companions outside the
    // Fellowship one region further. While none stands outside it but Thranduil himself, the king who uses the ability,
    // the variant is greyed in its slot and says why, instead of starting a move whose only lit objects are the
    // Fellowship's portraits (their click separates). UI-465 (owner, 2026-11-03): "both" — the variant stays greyed, and
    // Thranduil's own figure on the map moves with Far-sighted instead (FarSightedFigure), so the reason points at it.
    public static class FarSightedMoves
    {
        public const string Owner = "kings.sovereign.thranduil";
        public const string Ability = "kings.far-sighted";
        public const string NoCompanion = "No companion is outside the Fellowship · Thranduil moves from his figure";

        // Whether Far-sighted's variant of this option has a figure to move.
        public static bool Offered(ActionOption option) => option.Kind != CommandKind.MoveCompanions ||
            option.RulerTravel != null && option.RulerTravel.Travel.Any(travel => travel.Id != Owner);

        public static string Reason(ActionOption option) => Offered(option) ? "" : NoCompanion;
    }
}
