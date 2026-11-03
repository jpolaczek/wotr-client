using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-454 (owner, 2026-11-03: "when recruiting units we want animation; if the user just picks with a click, the unit
    // should glide smoothly from its Reserve slot to its proper place on the board, like drawn cards"): every refresh hands
    // over the recruits the draft's recruitment places — a card's or an ability's plan, whose recruits land on their ghost's
    // place, or the Muster die's step, which is sent at once — and a recruit placed since the last look flies from its
    // Reserve slot (the Reserve tab while the panel is closed) to the map (ReserveFlights); one dropped there by a carry
    // flies on from where it was dropped (Carried). A planned recruit taken back ends its flight. Nothing flies under Reduced
    // motion.
    public sealed class RecruitFlightInteraction
    {
        private readonly BoardScreen board;
        private readonly RecruitPlacementWatch watch = new RecruitPlacementWatch();
        private string carriedUnit = "";
        private ReserveFlightStart carriedStart;

        public RecruitFlightInteraction(BoardScreen board)
        {
            this.board = board;
        }

        // `recruits`: the units placed now; `planned`: they belong to a card's or an ability's plan, not a Muster step.
        public void Refresh(PlayerView view, IReadOnlyList<PlannedReinforcement> recruits, bool planned)
        {
            var change = watch.Observe(Scope(view, planned), recruits.Select(recruit => recruit.UnitId));
            foreach (var unit in change.Removed)
                board.Map.ReserveFlights.End(unit);
            foreach (var unit in change.Added)
                Launch(recruits.First(recruit => recruit.UnitId == unit), view.You, planned);
        }

        private static string Scope(PlayerView view, bool planned) =>
            view.MatchId + "/" + view.You + "/" + view.Decision?.Id + "/" + (planned ? "plan" : "muster");

        // UI-454: while a carried recruit is dropped, its placement flies on from where it shows (map-local, held up).
        public void Carried(string unit, ReserveFlightStart start)
        {
            carriedUnit = unit ?? "";
            carriedStart = start;
        }

        public void EndCarried() => carriedUnit = "";

        private void Launch(PlannedReinforcement recruit, Side side, bool planned)
        {
            var start = recruit.UnitId == carriedUnit ? carriedStart : Slot(recruit, side);
            board.Map.ReserveFlights.Launch(recruit, side, planned, start);
        }

        // The recruit's miniature first stands exactly on its slot's picture of it, or on the Reserve tab while the panel
        // does not show the slot.
        private ReserveFlightStart Slot(PlannedReinforcement recruit, Side side)
        {
            var figure = MapArmyArtwork.LoneFigure(FigureAppearances.Unit(recruit.Nation, recruit.Kind, side));
            var art = board.Reinforcements.Art(recruit.Nation, recruit.Kind);
            var shown = art != null && art.worldBound.width > 0 && art.worldBound.height > 0;
            var picture = shown ? art.worldBound : board.Reinforcements.Tab.worldBound;
            var aspect = shown && art.sprite != null && art.sprite.rect.height > 0
                ? art.sprite.rect.width / art.sprite.rect.height
                : figure.width / Mathf.Max(1, figure.height);
            return ReserveFlightStart.OnSlot(board.Map, picture, aspect, figure, MapArmyArtwork.Feet(false, true));
        }
    }
}
