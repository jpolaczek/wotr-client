using System;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class DecisionNoticeText
    {
        public static string Describe(PlayerView view)
        {
            var choice = view.Decision;
            if (choice == null) return "";
            return choice.Kind switch
            {
                DecisionKind.Action => "Choose an action",
                DecisionKind.ActionSequence => "Complete your action",
                DecisionKind.FellowshipPhase => "Prepare the Fellowship",
                DecisionKind.AllocateHunt => "Allocate Hunt dice",
                DecisionKind.DiscardCards => "Discard " + choice.RequiredCount + " excess cards",
                DecisionKind.Reaction => view.Hunt != null ? HuntActions.DecisionText(view) : "Choose your reaction",
                DecisionKind.CombatCard => "Choose a Combat card",
                DecisionKind.Losses => "Assign " + choice.RequiredCount + " hits",
                DecisionKind.HuntDamage => view.Hunt == null ? "Resolve Hunt damage" : "Resolve " + view.Hunt.Damage + " Hunt damage",
                DecisionKind.CouncilVersions => "Choose Companion versions",
                DecisionKind.CorruptionTile => "Choose a Corruption tile",
                DecisionKind.RemoveExcess => "Return " + choice.RequiredCount + " excess units",
                DecisionKind.EffectChoice => choice.Presentation == "CorruptionRemoval" ? "Return a Corruption tile?"
                    : choice.SourceId.Length > 0 ? "Resolve " + ActionCardText.Source(choice.SourceId) : "Resolve the Event choice",
                DecisionKind.FellowshipRoute => "Choose the Fellowship route",
                DecisionKind.Guide => "Choose the next Guide",
                DecisionKind.CompanionSeparation => "Place separated Companions",
                DecisionKind.HuntReroll => "Choose Hunt re-rolls",
                DecisionKind.BattleReroll => "Choose Leader re-rolls",
                DecisionKind.BattleContinue => "Continue or stop the attack",
                DecisionKind.BattleRetreat => "Retreat or hold",
                DecisionKind.BattleAdvance => "Advance or stay",
                DecisionKind.StrongholdDefense => "Fight or shelter",
                DecisionKind.SiegeContinuation => "Extend or end the siege battle",
                DecisionKind.ArmyEvent => "Move the Event armies",
                DecisionKind.CharacterEvent => "Move the Event characters",
                DecisionKind.SpecialDieChoice => "Choose a special die",
                DecisionKind.KeepActionDie => "Keep or spend the action die",
                DecisionKind.CouncilHomelands => "Place Companions in homelands",
                DecisionKind.CouncilToken => "Choose a Council token",
                DecisionKind.RulerDiscard => "Discard a die after the roll",
                DecisionKind.Dismay => "Choose a Ruler re-roll",
                _ => throw new ArgumentOutOfRangeException(nameof(choice.Kind))
            };
        }
    }
}
