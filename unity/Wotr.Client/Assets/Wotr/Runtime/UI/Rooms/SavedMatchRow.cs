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
        private readonly VisualElement headingAvatar = AvatarImage.Create(AvatarImage.Small);
        private readonly VisualElement detailAvatar = AvatarImage.Create(AvatarImage.Tiny);
        // UI-263: the game's expansion crests in the row's upper right corner.
        private readonly VisualElement crests;
        // UI-537 (owner, 2026-11-14): an x beside the row deletes the game for both players (MENU-21's removal); a ranked game
        // is never removed (RANK-04), so its row has none.
        private readonly Button delete;
        private MatchSummary current;
        private string version = "";

        public SavedMatchRow(MatchSummary match, Action<MatchSummary> open, Action<MatchSummary> remove = null)
        {
            Root.name = "saved-match-" + match.MatchId;
            Root.AddToClassList("saved-match");
            crests = ExpansionCrests.Row("saved-match-crests-" + match.MatchId);
            var resume = BoardScreen.Button("", () => open(current));
            resume.name = "resume-match-" + match.MatchId;
            resume.AddToClassList("resume-match");
            heading.AddToClassList("saved-match-heading");
            detail.AddToClassList("saved-match-detail");
            resume.Add(AvatarImage.Line(heading, headingAvatar));
            resume.Add(AvatarImage.Line(detail, detailAvatar));
            resume.Add(state);
            resume.Add(crests);
            Root.Add(resume);
            delete = BoardScreen.Button("×", () => remove?.Invoke(current));
            delete.name = "delete-match-" + match.MatchId;
            delete.AddToClassList("delete-match");
            delete.tooltip = "Delete this game for both players";
            Root.Add(delete);
            Apply(match);
        }

        public Button Delete => delete;

        public void Apply(MatchSummary match)
        {
            current = match;
            var key = $"{match.Revision}/{match.LastSeenRevision}/{match.Status}/{match.Opponent}/{match.OpponentAvatar}/{match.Name}/{match.Ranked}/{match.IsPve}/{match.Bot?.Status}";
            if (key == version)
                return;
            version = key;
            var opponent = match.IsPve ? "CPU" : match.Opponent;
            heading.text = string.IsNullOrEmpty(match.Name) ? "With " + opponent : match.Name;
            detail.text = (string.IsNullOrEmpty(match.Name) ? "" : "With " + opponent + " · ") + BoardScreen.SideName(match.You) + " · Turn " + match.Turn + " · " + Modules(match.Configuration);
            // UI-505: the picture stands before the line that names the opponent.
            var unnamed = string.IsNullOrEmpty(match.Name);
            AvatarImage.Show(headingAvatar, unnamed ? match.OpponentAvatar : "");
            AvatarImage.Show(detailAvatar, unnamed ? "" : match.OpponentAvatar);
            state.text = (match.Ranked ? "Ranked · " : "") + (match.HasUnseenChanges ? "● New changes · " : "") + Status(match);
            Root.EnableInClassList("your-decision", match.Status == MatchStatus.YourDecision);
            ExpansionCrests.Show(crests, match.Configuration);
            delete.style.display = match.Ranked ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public static string Status(MatchSummary match)
        {
            if (match.Status == MatchStatus.Finished)
                return (match.Winner == match.You ? "Victory" : "Defeat") + " · " + (GameEndState.Personal(match.Victory, match.Winner == match.You) ?? Victory(match.Victory));
            if (match.Status != MatchStatus.Waiting)
                return "Your decision · " + Decision(match.YourDecision);
            if (!match.IsPve)
                return "Waiting for opponent";
            return match.Bot?.Status == BotSeatStatus.Unavailable ? "CPU unavailable" : "Waiting for CPU";
        }

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
