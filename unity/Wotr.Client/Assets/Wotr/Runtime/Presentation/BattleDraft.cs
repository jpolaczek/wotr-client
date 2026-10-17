using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class BattleDraft
    {
        public static bool UsesBattle(CommandKind? kind) => kind is CommandKind.CommitCombatCard or CommandKind.RerollBattle or
            CommandKind.TakeBattleLosses or CommandKind.TakeEventLosses or CommandKind.AdvanceBattle or CommandKind.RetreatBattle or CommandKind.ExtendSiege;

        public static bool IsImmediate(CommandKind kind) => kind is CommandKind.ContinueBattle or CommandKind.StopBattle or
            CommandKind.HoldBattle or CommandKind.FightFieldBattle or CommandKind.RetreatIntoSiege or CommandKind.FinishArmyEvent or CommandKind.UseEvilKnowledge;

        public static bool SelectCard(TurnDraft draft, PlayerView view, string card)
        {
            if (!view.Options.Any(o => o.Enabled && o.Kind == CommandKind.CommitCombatCard && o.EligibleIds.Contains(card)))
                return false;
            if (draft.Kind != CommandKind.CommitCombatCard)
                draft.Choose(CommandKind.CommitCombatCard);
            draft.Change(p => p.CardId = card);
            return true;
        }

        public static void Loss(TurnDraft draft, string id, BattleLossKind kind)
        {
            draft.Change(p =>
            {
                var old = p.Losses.FirstOrDefault(l => l.UnitId == id);
                if (old != null)
                    p.Losses.Remove(old);
                if (old == null || old.Kind != kind)
                    p.Losses.Add(new BattleLossChoice { UnitId = id, Kind = kind });
            });
        }

        public static int Damage(TurnDraft draft, PlayerView view) => draft.Payload.Losses.Sum(l =>
            l.Kind == BattleLossKind.Remove && view.Units.Any(u => u.Id == l.UnitId && u.Kind == UnitKind.Elite) ? 2 : 1);

        public static void CycleLoss(TurnDraft draft, UnitView unit)
        {
            if (unit.Kind is not (UnitKind.Regular or UnitKind.Elite))
                return;
            draft.Change(p =>
            {
                var old = p.Losses.FirstOrDefault(l => l.UnitId == unit.Id);
                if (old == null)
                    p.Losses.Add(new BattleLossChoice { UnitId = unit.Id,
                        Kind = unit.Kind == UnitKind.Elite ? BattleLossKind.Downgrade : BattleLossKind.Remove });
                else if (old.Kind == BattleLossKind.Downgrade)
                    old.Kind = BattleLossKind.Remove;
                else
                    p.Losses.Remove(old);
            });
        }

        public static bool Ready(TurnDraft draft, ActionOption option) => option.Kind switch
        {
            CommandKind.CommitCombatCard => option.EligibleIds.Contains(draft.Payload.CardId),
            CommandKind.RerollBattle => draft.Payload.Ids.Count <= option.Maximum,
            // A depleted army can absorb fewer hits. The authoritative preview validates this.
            CommandKind.TakeBattleLosses or CommandKind.TakeEventLosses => option.Minimum == 0 || draft.Payload.Losses.Count > 0,
            CommandKind.AdvanceBattle => draft.Payload.Ids.Count > 0,
            CommandKind.ExtendSiege => draft.Payload.Ids.Count == 1,
            CommandKind.RetreatBattle => option.Targets.Contains(draft.Payload.RegionId),
            _ => false
        };

        public static void Reconcile(TurnDraft draft, ActionOption option, PlayerView view = null)
        {
            if (option.Kind == CommandKind.CommitCombatCard && !option.EligibleIds.Contains(draft.Payload.CardId))
                draft.Payload.CardId = "";
            draft.Payload.Losses.RemoveAll(l => !option.EligibleIds.Contains(l.UnitId));
            if (view != null)
                draft.Payload.Losses.RemoveAll(loss => loss.Kind == BattleLossKind.Downgrade &&
                    view.Units.Any(unit => unit.Id == loss.UnitId && unit.Kind != UnitKind.Elite));
            if (!option.Targets.Contains(draft.Payload.RegionId))
                draft.Payload.RegionId = "";
        }

        public static string Hint(TurnDraft draft, PlayerView view)
        {
            var option = view.Options.FirstOrDefault(o => o.Kind == draft.Kind);
            return draft.Kind switch
            {
                CommandKind.CommitCombatCard => draft.Payload.CardId.Length == 0 ? "Choose a highlighted card, or No card." : "Your card stays secret until both players have chosen.",
                CommandKind.RerollBattle => "Rerolls " + draft.Payload.Ids.Count + " / " + option.Maximum + " · Select failed dice above.",
                CommandKind.TakeBattleLosses when view.Battle.VoluntaryLosses => "Sacrifice up to " + new string('●', option.Maximum) + " · Chosen " + new string('●', Damage(draft, view)),
                CommandKind.TakeBattleLosses or CommandKind.TakeEventLosses => "Resolve " + (view.EventLoss?.Hits ?? view.Battle.PendingHits) + " hits · Selected " + Damage(draft, view) + ".",
                CommandKind.AdvanceBattle => "Choose figures, then click the destination · Or Stay.",
                CommandKind.RetreatBattle => draft.Payload.RegionId.Length == 0 ? "Choose a highlighted retreat region." : "The surviving defenders retreat together.",
                CommandKind.ExtendSiege => "Click a highlighted figure to extend the siege with it.",
                _ => ""
            };
        }
    }
}
