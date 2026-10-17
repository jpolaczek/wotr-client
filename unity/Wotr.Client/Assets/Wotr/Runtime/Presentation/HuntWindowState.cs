using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-162: what the Hunt window shows and when it opens by itself, from the projection only. A new Hunt opens the
    // window for both players; the stage ribbon follows HuntView.Stage, and a Hunt in Mordor has no roll (BASE p. 43).
    public static class HuntWindowState
    {
        private static readonly (string Key, string Title)[] Roll =
        {
            ("roll", "Roll"), ("reroll", "Re-roll"), ("tile", "Tile"), ("damage", "Damage"), ("result", "Result")
        };

        private static readonly (string Key, string Title)[] Mordor =
        {
            ("tile", "Tile"), ("damage", "Damage"), ("step", "Step")
        };

        // One key per Hunt: the report that records it (id and starting revision), or its source without a report.
        public static string Key(PlayerView view)
        {
            if (view?.Hunt == null)
                return "";
            var report = view.HuntReport;
            return view.MatchId + "/" + (report != null && !report.Completed
                ? report.Id + "/" + report.StartedRevision
                : view.Hunt.SourceId);
        }

        public static IReadOnlyList<(string Key, string Title)> Stages(HuntView hunt) => hunt?.Mordor == true ? Mordor : Roll;

        // -1 when no Hunt is shown; the last stage once the Hunt is over and its report complete.
        public static int Index(PlayerView view, HuntView hunt)
        {
            if (view?.Hunt == null && view?.HuntReport?.Completed == true)
                return Stages(hunt).Count - 1;
            if (hunt == null)
                return -1;
            if (hunt.Mordor)
                return MordorIndex(hunt.Stage);
            if (view.Decision?.Kind == DecisionKind.HuntReroll)
                return 1;
            return hunt.Stage switch
            {
                "BeforeRoll" or "Roll" => hunt.Dice.Count > 0 && hunt.RerollSources.Count > 0 ? 1 : 0,
                "BeforeDraw" or "Draw" or "Table" => 2,
                "DetermineDamage" or "Damage" or "Guide" or "Corruption" or "Replacement" or "ShadowReplacement" => 3,
                "Reveal" or "Done" => 4,
                _ => hunt.TileId.Length > 0 ? 3 : hunt.Dice.Count > 0 ? 0 : -1
            };
        }

        private static int MordorIndex(string stage) => stage switch
        {
            "BeforeDraw" or "Draw" or "Table" => 0,
            "DetermineDamage" or "Damage" or "Guide" or "Corruption" or "Replacement" or "ShadowReplacement" => 1,
            "Reveal" or "Done" => 2,
            _ => 0
        };
    }

    // UI-162: the Shadow's allocation in the turn console: the count on the Hunt plaque and the console hint.
    public static class HuntAllocationText
    {
        public static string Count(TurnDraft draft, ActionOption option) => draft.Payload.Ids.Count + "/" + option.Maximum;

        public static string Hint(TurnDraft draft, ActionOption option)
        {
            if (option == null)
                return "";
            var limit = HuntActions.AtLimit(draft, option) && option.Maximum > 0 ? " · Limit reached"
                : option.Minimum > 0 ? " · At least " + option.Minimum : " · Zero allowed";
            return "Click dice to put them in the Hunt Box · " + draft.Payload.Ids.Count + " of " + option.Maximum + limit +
                " · The Hunt plaque allocates and rolls";
        }

        // UI-172: the plaque's tooltip while it is the allocation's confirmation.
        public static string Plaque(TurnDraft draft, ActionOption option, bool ready) => ready
            ? Confirm(draft) + " · Click the plaque to allocate and roll"
            : Hint(draft, option);

        public static string Confirm(TurnDraft draft) => "Allocate & roll · " +
            (draft.Payload.Ids.Count == 0 ? "No dice in the Hunt" : draft.Payload.Ids.Count + (draft.Payload.Ids.Count == 1 ? " die" : " dice"));
    }
}
