using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class MusterActions
    {
        public static bool Active(PlayerView view, TurnDraft draft, ActionDieContext context) =>
            view?.DecisionOwner == view?.You && view?.Decision != null && context.Supports(DieFace.Muster) &&
            (view.Decision.Kind == DecisionKind.Action || view.Decision.Kind == DecisionKind.ActionSequence &&
                view.ActionSequence?.Kind == CommandKind.Recruit) &&
            ActionDieContext.DirectIntent(draft) &&
            draft.Payload.RulerTokenId.Length == 0 && draft.Payload.AbilityId.Length == 0;

        // UI-317 (owner report): the Muster die backs the Reserve only while it can recruit — the server's Recruit is
        // enabled for it — or the player's own Recruit sequence is open (its ✓ Finish). Otherwise only Politics can act.
        public static bool CanRecruit(PlayerView view, TurnDraft draft, ActionDieContext context) =>
            Active(view, draft, context) && (Option(view, context, CommandKind.Recruit) != null ||
                view.Decision.Kind == DecisionKind.ActionSequence && view.ActionSequence?.Kind == CommandKind.Recruit);

        public static ActionOption Option(PlayerView view, ActionDieContext context, CommandKind kind) =>
            view.Options.FirstOrDefault(o => o.Kind == kind && context.Allows(o));

        public static IEnumerable<string> Targets(PlayerView view, TurnDraft draft, ActionDieContext context) =>
            Option(view, context, CommandKind.Recruit)?.Recruitment
                .Where(g => draft.ReserveId.Length == 0 || g.ReserveIds.Contains(draft.ReserveId))
                .SelectMany(g => g.Targets).Distinct() ?? Enumerable.Empty<string>();

        public static bool Recruit(TurnDraft draft, PlayerView view, ActionDieContext context, string figure, string region)
        {
            if (!Active(view, draft, context) || Option(view, context, CommandKind.Recruit)?.Recruitment
                .Any(g => g.ReserveIds.Contains(figure) && g.Targets.Contains(region)) != true)
                return false;
            draft.Choose(CommandKind.Recruit, context.DieId);
            draft.Change(p =>
            {
                p.Stepwise = true;
                p.Placements.Add(new UnitPlacementChoice { UnitId = figure, RegionId = region });
                draft.ReserveId = figure;
            });
            return true;
        }

        // UI-130: a reserve chosen in the Reinforcements panel prepares the step without a settlement, so the map
        // lights only the settlements that figure can take; a click on one of them completes the step.
        public static bool Select(TurnDraft draft, PlayerView view, ActionDieContext context, string figure)
        {
            if (!Active(view, draft, context) || Option(view, context, CommandKind.Recruit)?.Recruitment
                .Any(g => g.ReserveIds.Contains(figure)) != true)
                return false;
            draft.Gesture(() =>
            {
                draft.Choose(CommandKind.Recruit, context.DieId);
                draft.Change(p => p.Stepwise = true);
                draft.ReserveId = figure;
            });
            return true;
        }

        public static List<string> FigureTargets(PlayerView view, ActionDieContext context, string figure) =>
            Option(view, context, CommandKind.Recruit)?.Recruitment.Where(g => g.ReserveIds.Contains(figure))
                .SelectMany(g => g.Targets).Distinct().ToList() ?? new List<string>();

        public static bool Politics(TurnDraft draft, PlayerView view, ActionDieContext context, string nation)
        {
            if (!Active(view, draft, context) || Option(view, context, CommandKind.AdvancePolitics)?.Targets.Contains(nation) != true)
                return false;
            draft.Choose(CommandKind.AdvancePolitics, context.DieId);
            draft.Change(p => p.NationId = nation);
            return true;
        }

        public static bool OrphanLeader(PlayerView view, UnitKind kind, string region) =>
            view.You == Side.FreePeoples && kind == UnitKind.Leader &&
            !view.Units.Any(u => u.Side == view.You && u.Location == region && u.Kind is UnitKind.Regular or UnitKind.Elite);
    }
}
