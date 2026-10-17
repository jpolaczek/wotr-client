using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Object gestures consume only the exact resources exposed by the current projection.
    public static class ExpansionActions
    {
        public static bool Immediate(CommandKind? kind) => kind is CommandKind.UseRing or CommandKind.UseDismay
            or CommandKind.UseNarya or CommandKind.UseVilya or CommandKind.AttemptCorruption
            or CommandKind.RemoveCorruptedSovereign or CommandKind.ChooseCorruptionTile
            or CommandKind.ChooseSpecialDie or CommandKind.DiscardRulerDie or CommandKind.ChooseCouncilToken;

        // What a ring click means now: the cost of the selected card, Vilya on a kept die, or changing a die.
        public static CommandKind RingKind(PlayerView view, TurnDraft draft) =>
            draft.Kind is CommandKind.DiscardPalantir or CommandKind.DiscardBaradPalantir or CommandKind.DiscardLore
                ? draft.Kind.Value : view.Decision?.Kind == DecisionKind.KeepActionDie ? CommandKind.UseVilya : CommandKind.UseRing;

        public static bool Ring(PlayerView view, TurnDraft draft, string id)
        {
            var kind = RingKind(view, draft);
            var option = DraftOptions.For(view, draft, kind);
            if (option?.Enabled != true || !option.EligibleIds.Contains(id))
                return false;
            var die = option.EligibleDice.Contains(draft.Payload.DieId) ? draft.Payload.DieId : "";
            if (draft.Kind != kind)
                draft.Choose(kind, die);
            draft.Change(p => p.RingId = id);
            return true;
        }

        public static bool RulerToken(PlayerView view, TurnDraft draft, string token, string die)
        {
            if (view.You != Side.FreePeoples || view.DecisionOwner != view.You ||
                !view.RulerTokenActions.Any(a => a.TokenId == token && a.Action.Enabled && a.Action.EligibleDice.Contains(die)))
                return false;
            DraftOptions.SelectToken(draft, view, token);
            draft.Change(p => p.DieId = die);
            return true;
        }

        public static bool Eye(PlayerView view, TurnDraft draft, string id)
        {
            var option = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.AttemptCorruption && o.EligibleDice.Contains(id));
            if (option == null || draft.Kind is not (null or CommandKind.AttemptCorruption))
                return false;
            if (draft.Kind == null)
                draft.Choose(CommandKind.AttemptCorruption, id);
            else
                draft.Change(p => p.DieId = id);
            return true;
        }

        public static ActionOption Sovereign(PlayerView view, TurnDraft draft, string id) => DraftOptions.All(view, draft)
            .FirstOrDefault(o => o.Enabled && o.Kind is CommandKind.AttemptCorruption or CommandKind.AwakenSovereign or CommandKind.RemoveCorruptedSovereign &&
                o.EligibleIds.Contains(id) && o.EligibleDice.Contains(draft.Payload.DieId));

        public static bool SelectSovereign(PlayerView view, TurnDraft draft, string id)
        {
            var option = Sovereign(view, draft, id);
            if (option == null)
                return false;
            var die = draft.Payload.DieId;
            draft.Choose(option.Kind, die);
            draft.Change(p => p.CharacterId = id);
            return true;
        }
    }
}
