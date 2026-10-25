using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-115: the informational stage ribbon; the current stage carries a gilt marker behind its icon. UI-327: a window
    // keeps one ribbon (StageRibbon) so the marker glides from stage to stage.
    public static class BattleStages
    {
        private static readonly (string Key, string Title)[] Stages =
        {
            ("cards", "Cards"), ("roll", "Combat roll"), ("reroll", "Leader re-roll"), ("losses", "Losses"), ("next", "Next round / end")
        };

        public static StageRibbon Ribbon() => new StageRibbon("battle-stages", "battle-stage-");

        public static void Apply(StageRibbon ribbon, BattleView battle) =>
            ribbon.Apply(Stages, Index(battle.Stage), "battle-stage-icon-",
                "Current stage: " + ActionCardText.Words(battle.Stage) + " · Card effects can open their own decisions between these steps.");

        // A ribbon of its own, for a view drawn once.
        public static VisualElement Create(BattleView battle)
        {
            var ribbon = Ribbon();
            Apply(ribbon, battle);
            return ribbon.Root;
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
