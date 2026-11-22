using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-566 (owner, 2026-11-22: "unmarking every unit by hand should let the Nazgûl move alone with the Character die"):
    // with a plain Character die a Shadow army's force is one selection in its region's panel, as the Balrog's is (UI-459).
    // Left with no army unit marked — only Nazgûl, the Witch-king or another Minion — the selection becomes their move
    // (Move Nazgûl and Minions, the die's own action) in the same gesture: the marked figures that travel together (same
    // region, same side of the walls, all flying or all walking; flyers first) form its group, and the next click on a lit
    // destination moves them. Marking a unit again turns the group back into the army (ActionContextInteraction).
    //
    // UI-567 (owner, 2026-11-22: "a Move Nazgûl button in the army window, active when the Shadow has the Character die
    // selected"): the button of a Shadow force with flying figures selects all of them (its Nazgûl and the Witch-king) as
    // that move's group in one click; the map's lit regions are then their destinations.
    public static class ShadowCharacterSelection
    {
        public static bool Normalize(TurnDraft draft, PlayerView view, ActionDieContext context)
        {
            if (!PlainCharacterDie(view, context) || draft.Kind is not (CommandKind.MoveArmies or CommandKind.AttackArmy))
                return false;
            var move = ArmyDraft.Current(draft);
            if (move == null || move.To.Length > 0 || move.UnitIds.Count + move.CharacterIds.Count == 0 ||
                move.UnitIds.Any(id => view.Units.FirstOrDefault(u => u.Id == id)?.Kind != UnitKind.Nazgul))
                return false;
            var option = TravelActions.Option(view, draft, context);
            if (option == null)
                return false;
            var figures = move.UnitIds.Concat(move.CharacterIds)
                .Select(id => option.Travel.FirstOrDefault(t => t.Id == id))
                .Where(travel => travel != null)
                .OrderByDescending(travel => travel.Flight)
                .Select(travel => travel.Id)
                .ToList();
            return Group(draft, view, context, figures);
        }

        // UI-567: the force's flying figures the die can move now (none for another die, a used one or the Free Peoples).
        public static IReadOnlyList<string> Flyers(PlayerView view, TurnDraft draft, ActionDieContext context, string region,
            bool garrison)
        {
            if (view == null || draft == null || !PlainCharacterDie(view, context))
                return Array.Empty<string>();
            var probe = draft.Fork();
            probe.Unchoose();
            var option = TravelActions.Option(view, probe, context);
            if (option == null)
                return Array.Empty<string>();
            return option.Travel
                .Where(t => t.Flight && t.Origin == region && Garrison(view, t.Id) == garrison &&
                    TravelActions.CanSelect(view, probe, context, t.Id))
                .Select(t => t.Id)
                .ToList();
        }

        // UI-567: the button's click; whatever was chosen before gives way to this force's flight.
        public static bool SelectFlyers(TurnDraft draft, PlayerView view, ActionDieContext context, string region, bool garrison)
        {
            var flyers = Flyers(view, draft, context, region, garrison);
            if (flyers.Count == 0)
                return false;
            draft.Unchoose();
            return Group(draft, view, context, flyers);
        }

        private static bool Group(TurnDraft draft, PlayerView view, ActionDieContext context, IReadOnlyList<string> figures)
        {
            if (figures.Count == 0 || !TravelActions.Select(view, draft, context, figures[0]))
                return false;
            foreach (var id in figures.Skip(1))
            {
                // A figure that cannot join the group (a walker beside flyers) stays behind rather than restart it.
                var travel = TravelActions.Option(view, draft, context);
                if (travel != null && TravelDraft.CanToggle(draft, view, travel, id))
                    TravelActions.Select(view, draft, context, id);
            }
            return true;
        }

        // The Shadow's own Character die (or the paid sequence of its Move Nazgûl and Minions), not an ability's die.
        private static bool PlainCharacterDie(PlayerView view, ActionDieContext context) =>
            view.You == Side.Shadow && !context.Implied && context.AbilityId.Length == 0 && context.Supports(DieFace.Character);

        private static bool? Garrison(PlayerView view, string id) =>
            view.Units.FirstOrDefault(u => u.Id == id)?.Garrison ?? view.Characters.FirstOrDefault(c => c.Id == id)?.Garrison;
    }
}
