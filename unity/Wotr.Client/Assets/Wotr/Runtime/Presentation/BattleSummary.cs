using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class BattleSummary
    {
        public static string Describe(TurnDraft draft, PlayerView view, ActionPreview preview)
        {
            if (draft.Kind == CommandKind.AttackArmy)
                return AttackDraft.Mode(draft, view) + (view.Options.First(o => o.Kind == draft.Kind).ArmyEvent != null
                    ? " · No extra die. " : " · One die pays for the battle. ") + "Attackers remain in their region until an advance.";
            if (draft.Kind == CommandKind.TakeBattleLosses && view.Battle.VoluntaryLosses)
                return Sacrifice(draft, view, preview);
            if (draft.Kind == CommandKind.ExtendSiege)
                return SiegeExtension(draft, view, preview);
            if (draft.Kind is CommandKind.TakeBattleLosses or CommandKind.TakeEventLosses)
            {
                var lostCommanders = view.Characters.Where(c => preview.RemovedIds.Contains(c.Id)).Select(c => CharacterLibrary.Name(c.DefinitionId))
                    .Concat(view.Units.Where(u => preview.RemovedIds.Contains(u.Id) && !ArmyDraft.IsCombat(u)).Select(u => u.Kind.ToString())).ToList();
                var reductions = draft.Payload.Losses.Count(l => l.Kind == BattleLossKind.Downgrade);
                var missing = preview.Losses?.Operations.Count(l => l.Operation == BattleLossKind.Downgrade && l.ReplacementId.Length == 0)
                    ?? reductions - preview.Placements.Count;
                var replacements = preview.Losses?.Operations.Count(l => l.ReplacementId.Length > 0) ?? preview.Placements.Count;
                return (missing > 0 ? "No regular available: elite removed for 1 hit. " : replacements > 0 ? "Elite replaced by a regular. " : "Selected units removed. ") +
                    (lostCommanders.Count > 1 ? "All " + lostCommanders.Count + " commanders also lost." :
                        lostCommanders.Count == 1 ? lostCommanders[0] + " also lost." :
                        "Confirm losses.");
            }
            if (draft.Kind == CommandKind.AdvanceBattle)
                return preview.Excess.Count > 0 ? "Enter the region · Then resolve the highlighted stacking excess." : "Enter the region with the selected survivors.";
            if (draft.Kind == CommandKind.RerollBattle)
                return "Rerolls " + draft.Payload.Ids.Count + " / " + view.Options.First(o => o.Kind == draft.Kind).Maximum +
                    (draft.Payload.Ids.Count == 0 ? " · Keep this roll." : " · Results after both players choose.");
            return BattleDraft.Hint(draft, view);
        }

        private static string SiegeExtension(TurnDraft draft, PlayerView view, ActionPreview preview)
        {
            var unit = view.Units.First(u => draft.Payload.Ids.Contains(u.Id));
            var cost = unit.Kind == UnitKind.Regular ? "Eliminate a Regular · Corrupted sovereign's weakness." :
                preview.Placements.Count > 0 ? "Elite → Regular." : "No regular available · Elite removed.";
            var survivors = view.Battle.Attacker.Units.Any(u => ArmyDraft.IsCombat(u) && !preview.RemovedIds.Contains(u.Id)) || preview.Placements.Count > 0;
            return cost + (survivors ? " Continue the siege." : " Last fighting unit lost · Battle ends.");
        }

        private static string Sacrifice(TurnDraft draft, PlayerView view, ActionPreview preview)
        {
            if (draft.Payload.Losses.Count == 0)
                return "Keep all units · No sacrifice bonus.";
            var hits = BattleDraft.Damage(draft, view);
            var figureChange = preview.Placements.Count > 0 ? "Elite → Regular" : "Remove chosen units";
            var combat = CardLibrary.Cards.TryGetValue(view.Battle.LossSource, out var card) ? card.CombatTitle : "";
            if (combat == "Onslaught")
                return figureChange + " · Roll " + hits + (hits == 1 ? " die" : " dice") + " · Hits on 4+.";
            var force = view.Battle.Attacker.Side == view.You ? view.Battle.Attacker : view.Battle.Defender;
            if (preview.Placements.Count == 0 && force.Units.Where(ArmyDraft.IsCombat).All(u => preview.RemovedIds.Contains(u.Id)))
                return "Army eliminated · No Combat roll.";
            return combat == "Relentless Assault" ? figureChange + " · Combat +" + hits + " · Re-roll unchanged."
                : figureChange + " · Confirm sacrifice.";
        }

        public static string LossDetails(PlayerView view, ActionPreview preview) => BattleLossText.Details(preview.Losses) + "\n" + string.Join("\n",
            view.Characters.Where(c => preview.RemovedIds.Contains(c.Id)).Select(c => CharacterLibrary.Name(c.DefinitionId) + " eliminated")
                .Concat(view.Units.Where(u => preview.RemovedIds.Contains(u.Id) && !ArmyDraft.IsCombat(u))
                    .GroupBy(u => (u.Nation, u.Kind)).Select(g => FigureGrid.Name(g.Key.Nation) + " " + g.Key.Kind + " × " + g.Count() + " eliminated")));
    }
}
