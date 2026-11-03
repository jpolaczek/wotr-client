using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-465 (owner, 2026-11-03, kings-far-sighted, deciding UI-456's open question): "both". While Awakened Thranduil is
    // the only companion outside the Fellowship, Far-sighted's Move companions stays greyed (FarSightedMoves) and his own
    // figure on the map is Far-sighted's mover. Far-sighted is paid by the Free Peoples' Ruler die showing a Character
    // (the server's KingsFarSighted.Valid); with that die it gives Thranduil his plain move plus one region (RangeBonus 1,
    // the option's RulerTravel), for the same die, so with that die his figure's click — or its drag past the threshold,
    // UI-405 — starts MoveCompanions with Far-sighted (AbilityId "kings.far-sighted") and lights his Far-sighted range.
    // With any other Character die his move stays plain; with another companion outside, the variant works as before UI-465
    // and every figure's move, his included, is plain. A chosen Ruler token keeps its own travel (UI-389).
    public static class FarSightedFigure
    {
        public const string Offer = "Far-sighted · Range +1 · Click to move Thranduil one region further";

        // Whether a figure's move started now from the map uses Far-sighted: no companions' move or ability is drafted yet,
        // no Ruler token is chosen, the chosen die pays Far-sighted and Thranduil alone stands outside the Fellowship.
        public static bool Moves(PlayerView view, TurnDraft draft, ActionDieContext context) =>
            view?.Decision?.Kind == DecisionKind.Action && draft.Kind != CommandKind.MoveCompanions &&
            draft.Payload.AbilityId.Length == 0 && draft.Payload.RulerTokenId.Length == 0 &&
            AloneOutside(view, draft) is { } travel && travel.Dice.Contains(context.DieId);

        // The option Thranduil's figure moves with: Far-sighted's dice and his Far-sighted travel.
        public static ActionOption Option(ActionOption plain) => DraftOptions.WithRulerTravel(plain);

        // The ability a figure's move started now takes: Far-sighted's while his figure is its mover, else the die's own.
        public static string Ability(PlayerView view, TurnDraft draft, ActionDieContext context) =>
            Moves(view, draft, context) ? FarSightedMoves.Ability : context.AbilityId;

        // The figures whose tooltip names Far-sighted's move (TOOLTIP_AUDIT rule 1: what the click does, and why): his, while
        // his click starts it or joins the Far-sighted move his figure started.
        public static IReadOnlyDictionary<string, string> Offers(PlayerView view, TurnDraft draft, ActionDieContext context) =>
            Moves(view, draft, context) || Drafted(view, draft)
                ? new Dictionary<string, string> { [FarSightedMoves.Owner] = Offer }
                : new Dictionary<string, string>();

        private static bool Drafted(PlayerView view, TurnDraft draft) => view?.Decision?.Kind == DecisionKind.Action &&
            draft.Kind == CommandKind.MoveCompanions && draft.Payload.AbilityId == FarSightedMoves.Ability &&
            draft.Payload.RulerTokenId.Length == 0 && AloneOutside(view, draft) != null;

        // Far-sighted's travel of the companions' move while it moves no companion but Thranduil; null otherwise.
        private static RulerTravelOption AloneOutside(PlayerView view, TurnDraft draft)
        {
            var plain = DraftOptions.For(view, draft, CommandKind.MoveCompanions);
            return plain?.RulerTravel is { } travel && travel.AbilityId == FarSightedMoves.Ability && !FarSightedMoves.Offered(plain) &&
                travel.Travel.Any(t => t.Id == FarSightedMoves.Owner) ? travel : null;
        }
    }
}
