using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // The step a decision starts with while the draft is empty: an action sequence's next step, or a forced choice's
    // command (the Hunt allocation, a discard, a reveal, a Guide, losses...). TurnInteraction applies it after every edit
    // that empties the draft; UI-455's Cancel (DraftCancel) applies it to tell whether a cancel would change anything.
    public static class MandatoryDraft
    {
        public static void Ensure(PlayerView view, TurnDraft draft)
        {
            if (view?.Decision == null || draft.Kind.HasValue)
                return;
            if (view.Decision.Kind == DecisionKind.ActionSequence && view.ActionSequence is { } sequence)
            {
                ContinueSequence(draft, sequence);
                return;
            }
            var simple = Simple(view.Decision.Kind);
            if (simple.HasValue)
            {
                draft.Choose(simple.Value);
                return;
            }
            var mandatory = Forced(view);
            if (!mandatory.HasValue)
                return;
            DraftGesture.Choose(draft, view, mandatory.Value);
            CouncilDraft.Defaults(draft, DraftOptions.For(view, draft));
        }

        private static void ContinueSequence(TurnDraft draft, ActionSequenceView sequence)
        {
            draft.Choose(sequence.Kind, sequence.DieId);
            draft.Change(p =>
            {
                p.Stepwise = true;
                p.MovementCost = sequence.Face;
                p.AbilityId = sequence.AbilityId;
            });
        }

        // The decisions whose one command is chosen without its option.
        private static CommandKind? Simple(DecisionKind kind) => kind switch
        {
            DecisionKind.AllocateHunt => CommandKind.AllocateHunt,
            DecisionKind.DiscardCards => CommandKind.DiscardCards,
            DecisionKind.RemoveExcess => CommandKind.RemoveExcess,
            DecisionKind.EffectChoice => CommandKind.ChooseEffect,
            _ => null
        };

        private static CommandKind? Forced(PlayerView view) => view.Decision.Kind switch
        {
            DecisionKind.FellowshipRoute => CommandKind.RevealFellowship,
            DecisionKind.Guide => CommandKind.SelectGuide,
            DecisionKind.CompanionSeparation => view.Options.FirstOrDefault(o => o.Kind is CommandKind.SeparateCompanions or CommandKind.MoveCompanions)?.Kind,
            DecisionKind.HuntReroll => CommandKind.RerollHunt,
            DecisionKind.HuntDamage => CommandKind.ResolveHuntDamage,
            DecisionKind.CorruptionTile => view.Options.FirstOrDefault(o => o.Enabled)?.Kind,
            DecisionKind.CouncilVersions => CommandKind.ChooseCompanionVersions,
            DecisionKind.CouncilHomelands => CommandKind.ChooseCouncilHomelands,
            DecisionKind.CouncilToken => CommandKind.ChooseCouncilToken,
            DecisionKind.SpecialDieChoice => CommandKind.ChooseSpecialDie,
            DecisionKind.Dismay when view.Options.Any(o => o.Enabled && o.Kind == CommandKind.UseDismay) => CommandKind.UseDismay,
            DecisionKind.KeepActionDie when view.Options.Any(o => o.Enabled && o.Kind == CommandKind.UseVilya) => CommandKind.UseVilya,
            DecisionKind.Reaction when view.Options.Any(o => o.Enabled && o.Kind == CommandKind.RespondEffect) => CommandKind.RespondEffect,
            DecisionKind.RulerDiscard => CommandKind.DiscardRulerDie,
            DecisionKind.CombatCard => CommandKind.CommitCombatCard,
            DecisionKind.Losses => view.EventLoss != null ? CommandKind.TakeEventLosses : CommandKind.TakeBattleLosses,
            DecisionKind.BattleReroll => CommandKind.RerollBattle,
            DecisionKind.BattleAdvance => CommandKind.AdvanceBattle,
            DecisionKind.BattleRetreat when view.Options.Any(o => o.Kind == CommandKind.RetreatBattle) => CommandKind.RetreatBattle,
            DecisionKind.SiegeContinuation => CommandKind.ExtendSiege,
            DecisionKind.ArmyEvent => view.Options.FirstOrDefault(o => o.Enabled && o.ArmyEvent != null)?.Kind,
            DecisionKind.CharacterEvent => CommandKind.MoveShadowCharacters,
            _ => null
        };
    }
}
