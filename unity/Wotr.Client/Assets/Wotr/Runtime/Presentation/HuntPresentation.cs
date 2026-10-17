using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public enum HuntFocus { None, Allocation, Roll, Tile, Guide }

    // Presentation of public facts only. Options and the existing sender own all choices.
    public static class HuntPresentation
    {
        public static bool Current(PlayerView view) => view.Hunt != null || view.Phase == GamePhase.HuntAllocation;

        public static string Title(PlayerView view) => view.Phase == GamePhase.HuntAllocation ? "Hunt allocation" :
            view.Hunt != null ? "Current Hunt" : view.HuntReport != null ? "Last Hunt" : "Hunt";

        public static string Owner(PlayerView view)
        {
            if (!Current(view) || !view.DecisionOwner.HasValue)
                return "";
            var side = BoardScreen.SideName(view.DecisionOwner.Value);
            // OBS-03: an observer's view carries no decision, so it always waits for the side that decides.
            return view.DecisionOwner == view.You && view.Decision != null ? "Your decision · " + side : "Waiting for " + side;
        }

        public static string Activity(PlayerView view)
        {
            if (view.Phase == GamePhase.HuntAllocation)
                return "Allocate dice";
            if (view.Hunt == null)
                return view.HuntReport == null ? "No Hunt report yet" :
                    view.HuntReport.Completed ? "Complete" : "Awaiting continuation";
            if (view.DecisionOwner == view.You && view.Decision != null)
                return HuntActions.DecisionText(view);
            // Stage is a continuation point, not the other player's current private decision.
            return view.Hunt.TileId.Length > 0 ? "Resolve the tile" :
                view.Hunt.Dice.Count > 0 ? "Hunt roll" : "Resolving the Hunt";
        }

        public static HuntFocus Focus(PlayerView view)
        {
            if (view.Phase == GamePhase.HuntAllocation)
                return HuntFocus.Allocation;
            if (view.Hunt != null && view.DecisionOwner == view.You && view.Decision?.Kind == DecisionKind.Guide)
                return HuntFocus.Guide;
            var hunt = HuntActions.Displayed(view);
            if (hunt?.TileId.Length > 0)
                return HuntFocus.Tile;
            if (hunt == null || hunt.Mordor)
                return HuntFocus.None;
            if (hunt.Dice.Count > 0)
                return HuntFocus.Roll;
            // An empty rolled result is a known fact; a pre-roll window is not a zero result.
            var latest = view.HuntReport?.Entries.LastOrDefault(e => e.Hunt != null);
            return latest != null && view.HuntReport.Entries.Any(e => e.HuntId == latest.HuntId && e.Kind == HuntReportKind.Rolled)
                ? HuntFocus.Roll : HuntFocus.None;
        }
    }
}
