using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class SavedMatchRow
    {
        public VisualElement Root { get; } = new VisualElement();
        private readonly Label heading = new Label();
        private readonly Label detail = new Label();
        private readonly Label state = new Label();
        private MatchSummary current;
        private string version = "";

        public SavedMatchRow(MatchSummary match, Action<MatchSummary> open)
        {
            Root.name = "saved-match-" + match.MatchId;
            Root.AddToClassList("saved-match");
            var resume = BoardScreen.Button("", () => open(current));
            resume.name = "resume-match-" + match.MatchId;
            resume.AddToClassList("resume-match");
            heading.AddToClassList("saved-match-heading");
            detail.AddToClassList("saved-match-detail");
            resume.Add(heading);
            resume.Add(detail);
            resume.Add(state);
            Root.Add(resume);
            Apply(match);
        }

        public void Apply(MatchSummary match)
        {
            current = match;
            var key = $"{match.Revision}/{match.LastSeenRevision}/{match.Status}/{match.Opponent}/{match.Name}";
            if (key == version)
                return;
            version = key;
            heading.text = string.IsNullOrEmpty(match.Name) ? "With " + match.Opponent : match.Name;
            detail.text = (string.IsNullOrEmpty(match.Name) ? "" : "With " + match.Opponent + " · ") + BoardScreen.SideName(match.You) + " · Turn " + match.Turn + " · " + Modules(match.Configuration);
            state.text = (match.HasUnseenChanges ? "● New changes · " : "") + Status(match);
            Root.EnableInClassList("your-decision", match.Status == MatchStatus.YourDecision);
        }

        public static string Status(MatchSummary match) => match.Status == MatchStatus.Finished
            ? (match.Winner == match.You ? "Victory" : "Defeat") + " · " + Victory(match.Victory)
            : match.Status == MatchStatus.Waiting ? "Waiting for opponent"
            : "Your decision · " + Decision(match.YourDecision);

        private static string Decision(DecisionKind? kind) => kind switch
        {
            DecisionKind.CombatCard => "Combat card",
            DecisionKind.Losses => "Losses",
            DecisionKind.BattleReroll => "Battle reroll",
            DecisionKind.BattleContinue or DecisionKind.SiegeContinuation => "Continue battle",
            DecisionKind.BattleRetreat => "Retreat",
            DecisionKind.BattleAdvance => "Advance",
            DecisionKind.StrongholdDefense => "Stronghold defense",
            DecisionKind.HuntDamage or DecisionKind.HuntReroll => "Hunt",
            DecisionKind.FellowshipRoute => "Fellowship route",
            DecisionKind.Guide => "Guide",
            DecisionKind.CompanionSeparation => "Companions",
            DecisionKind.FellowshipPhase => "Fellowship",
            DecisionKind.AllocateHunt => "Hunt allocation",
            DecisionKind.CouncilVersions or DecisionKind.CouncilHomelands or DecisionKind.CouncilToken => "Council",
            DecisionKind.CorruptionTile => "Corruption tile",
            DecisionKind.SpecialDieChoice or DecisionKind.RulerDiscard or DecisionKind.KeepActionDie => "Action dice",
            DecisionKind.Dismay => "Dismay",
            DecisionKind.DiscardCards => "Discard cards",
            DecisionKind.RemoveExcess => "Army limit",
            DecisionKind.Reaction => "Response",
            DecisionKind.EffectChoice or DecisionKind.ArmyEvent or DecisionKind.CharacterEvent => "Card effect",
            _ => "Action"
        };

        private static string Victory(VictoryCondition? victory) => victory switch
        {
            VictoryCondition.RingCorrupted => "Ring-bearers corrupted",
            VictoryCondition.RingDestroyed => "Ring destroyed",
            _ => "Military victory"
        };

        private static string Modules(MatchConfiguration configuration)
        {
            var names = new List<string>();
            if (configuration.Lords) names.Add("Lords");
            if (configuration.Kings) names.Add("Kings");
            if (configuration.Fate) names.Add("Fate");
            return names.Count == 0 ? "Base" : string.Join(" + ", names);
        }
    }
}
