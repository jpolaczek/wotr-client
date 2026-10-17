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

        public static string ScoreText(PlayerView view, Side side)
        {
            var score = Score(view, side);
            var points = score?.Points ?? view?.Players.FirstOrDefault(player => player.Side == side)?.VictoryPoints;
            var threshold = score?.Threshold > 0 ? score.Threshold : side == Side.FreePeoples ? 4 : 10;
            return (points?.ToString() ?? "—") + "/" + threshold;
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
            _ => "Game finished"
        };

        public static string SettlementSymbol(string settlement) => settlement == "Stronghold" ? "▣" : "○";
        public static string SideCode(Side side) => side == Side.FreePeoples ? "FP" : "SH";
    }
}
