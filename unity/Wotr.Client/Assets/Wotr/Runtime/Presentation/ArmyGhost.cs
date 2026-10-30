using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-127: what a click on each legal target does with the unsent selection, from the server's options only (move, or
    // attack and its battle mode); never a server query. UI-403 (TABLETOP_FIGURES_UX, "The lifted figure"): nothing is drawn
    // faintly at the target any more — the map lays a landing mark on the hovered target's place (red for an attack) and
    // hangs the caption above the lifted figure — and a travelling figure has its captions too: a character's travel, the
    // Council token's chosen group and a card's flight ("Move Strider", "Fly a Nazgûl").
    public sealed class ArmyGhost
    {
        public Dictionary<string, string> Captions { get; } = new Dictionary<string, string>();
        // The targets a click attacks.
        public HashSet<string> Attacks { get; } = new HashSet<string>();

        // `context` is the selected die while its army context is active (null otherwise); `data` finds a walker's routes.
        public static ArmyGhost Of(PlayerView view, TurnDraft draft, ActionDieContext context, BoardPresentation data = null)
        {
            var ghost = new ArmyGhost();
            if (context != null)
                ghost.AddArmy(view, draft, context);
            if (ghost.Captions.Count == 0)
                ghost.AddTravel(view, draft, data);
            return ghost.Captions.Count > 0 ? ghost : null;
        }

        private void AddArmy(PlayerView view, TurnDraft draft, ActionDieContext context)
        {
            var move = ArmyDraft.Current(draft);
            if (move == null || move.To.Length > 0 || move.UnitIds.Count + move.CharacterIds.Count == 0)
                return;
            var count = move.UnitIds.Count + move.CharacterIds.Count;
            foreach (var region in ArmyActionTargets.For(draft, view, context, CommandKind.MoveArmies))
                Captions[region] = "Move " + count + (count == 1 ? " figure" : " figures");
            foreach (var target in ArmyActionTargets.Options(draft, view, context, CommandKind.AttackArmy))
            {
                Captions[target.RegionId] = "Attack · " + (target.BattleMode ?? BattleMode.Field);
                Attacks.Add(target.RegionId);
            }
        }

        // The same destinations the map lights for a travelling selection (MapDraftOverlay).
        private void AddTravel(PlayerView view, TurnDraft draft, BoardPresentation data)
        {
            if (NazgulFlightDraft.Active(view, draft))
            {
                if (NazgulFlightDraft.Next(view, draft) is { } next)
                    Add(NazgulFlightDraft.Targets(view, draft), (next.Flight ? "Fly " : "Move ") +
                        NazgulFlightDraft.Figures(view, new[] { next.Id }));
                return;
            }
            var group = LiftedFigures.Selection(view, draft).ToList();
            if (group.Count == 0)
                return;
            if (CouncilTravel.Active(view, draft))
                Add(CouncilTravel.Targets(view, draft), "Move " + NazgulFlightDraft.Figures(view, group));
            else if (data != null && TravelDraft.TargetRoute(draft.Kind))
                Add(TravelDraft.Reachable(draft, view, data), "Move " + Travellers(view, group));
        }

        private void Add(IEnumerable<string> targets, string caption)
        {
            foreach (var region in targets)
                Captions[region] = caption;
        }

        // UI-416's names: the Shadow's figures as the console names them ("the Witch-king and 2 Nazgûl"), the Free
        // Peoples' companions by name; a larger group by its count.
        private static string Travellers(PlayerView view, IReadOnlyList<string> ids)
        {
            if (view.You == Side.Shadow)
                return NazgulFlightDraft.Figures(view, ids);
            if (ids.Count > 3)
                return ids.Count + " figures";
            var names = ids.Select(id => CharacterLibrary.Name(view, id)).ToList();
            return names.Count == 1 ? names[0] : string.Join(", ", names.Take(names.Count - 1)) + " and " + names.Last();
        }
    }
}
