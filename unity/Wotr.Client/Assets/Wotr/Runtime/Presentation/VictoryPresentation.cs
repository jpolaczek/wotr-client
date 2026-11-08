using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class VictoryPresentation
    {
        public static MilitaryVictoryView Score(PlayerView view, Side side) =>
            view?.MilitaryVictory.FirstOrDefault(score => score.Side == side);

        public static string Summary(PlayerView view) =>
            "FP " + ScoreText(view, Side.FreePeoples) + " · SH " + ScoreText(view, Side.Shadow);

        public static string ScoreText(PlayerView view, Side side) => Points(view, side) + "/" + Threshold(view, side);

        // UI-518: the left rail's Victory tab — each side's points in its colour — and its tooltip with the thresholds.
        public static string RailText(PlayerView view) =>
            "VP <color=#92C4EC>" + Points(view, Side.FreePeoples) + "</color> · <color=#EC8070>" + Points(view, Side.Shadow) + "</color>";

        public static string RailTooltip(PlayerView view) => TooltipText.Lines(
            "Victory points · Free Peoples " + ScoreText(view, Side.FreePeoples) + " · Shadow " + ScoreText(view, Side.Shadow),
            "Click for the captured settlements and the victory conditions");

        private static string Points(PlayerView view, Side side) =>
            (Score(view, side)?.Points ?? view?.Players.FirstOrDefault(player => player.Side == side)?.VictoryPoints)?.ToString() ?? "—";

        private static int Threshold(PlayerView view, Side side)
        {
            var score = Score(view, side);
            return score?.Threshold > 0 ? score.Threshold : side == Side.FreePeoples ? 4 : 10;
        }

        public static string MilitaryTiming(PlayerView view)
        {
            if (view?.Winner.HasValue == true)
                return "Final military score";
            return view?.MilitaryVictory.Any(score => score.ThresholdReached) == true
                ? "Threshold reached · Checked at turn end"
                : "Checked at turn end · Shadow first";
        }

        public static string Result(PlayerView view) => view?.Winner.HasValue == true
            ? (view.Winner == Side.Shadow ? "Shadow wins" : "Free Peoples win") + " · " + Reason(view.Victory) : "";

        public static string Reason(VictoryCondition? reason) => reason switch
        {
            VictoryCondition.RingCorrupted => "Ring-bearers corrupted",
            VictoryCondition.RingDestroyed => "Ring destroyed",
            VictoryCondition.ShadowMilitary or VictoryCondition.FreePeoplesMilitary => "Military victory",
            VictoryCondition.Conceded => "Conceded",
            VictoryCondition.TimedOut => "Out of time",
            _ => "Game finished"
        };

        public static string SettlementSymbol(string settlement) => settlement == "Stronghold" ? "▣" : "○";
        public static string SideCode(Side side) => side == Side.FreePeoples ? "FP" : "SH";
    }
}
