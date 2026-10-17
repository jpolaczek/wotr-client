using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-115: the informational stage ribbon; the current stage carries a gilt marker behind its icon.
    public static class BattleStages
    {
        private static readonly (string Key, string Title)[] Stages =
        {
            ("cards", "Cards"), ("roll", "Combat roll"), ("reroll", "Leader re-roll"), ("losses", "Losses"), ("next", "Next round / end")
        };

        public static VisualElement Create(BattleView battle)
        {
            var row = BoardScreen.Row("battle-stages");
            row.name = "battle-stages";
            var current = Index(battle.Stage);
            for (var i = 0; i < Stages.Length; i++)
            {
                var stage = new VisualElement { name = "battle-stage-" + Stages[i].Key, pickingMode = PickingMode.Ignore };
                stage.AddToClassList("battle-stage");
                stage.EnableInClassList("current-stage", i == current);
                var marker = new VisualElement { name = "battle-stage-marker", pickingMode = PickingMode.Ignore };
                marker.AddToClassList("battle-stage-marker");
                stage.Add(marker);
                var icon = new VisualElement { name = "battle-stage-icon", pickingMode = PickingMode.Ignore };
                icon.AddToClassList("battle-stage-icon");
                icon.AddToClassList("battle-stage-icon-" + Stages[i].Key);
                stage.Add(icon);
                stage.Add(new Label(Stages[i].Title) { pickingMode = PickingMode.Ignore, enableRichText = false });
                row.Add(stage);
            }
            row.tooltip = "Current stage: " + ActionCardText.Words(battle.Stage) + " · Card effects can open their own decisions between these steps.";
            return row;
        }

        public static int Index(string stage) => stage switch
        {
            "AttackerCard" or "DefenderCard" or "BeforeReveal" or "BeforeRoll" or "StrongholdDefense" => 0,
            "CombatRoll" => 1,
            "BeforeReroll" or "AttackerReroll" or "DefenderReroll" or "LeaderReroll" or "AfterReroll" => 2,
            "BeforeLosses" or "AttackerLosses" or "DefenderLosses" or "AfterLosses" => 3,
            "RoundEnd" or "Continue" or "Retreat" or "Finish" or "Advance" or "Complete" or "SiegeContinuation" or "NextRound" => 4,
            _ => -1
        };
    }
}
