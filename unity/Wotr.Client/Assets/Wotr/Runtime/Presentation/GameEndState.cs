using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-169 (SIGNALS_UX D): the end of the game from one player's view — the title of their result, who won and
    // why (BASE p. 44, LOGIC B20), the scene of the reason and the game in a few lines.
    public sealed class GameEndState
    {
        public const int MaxCorruption = 12;
        public Side Winner { get; private set; }
        public bool Won { get; private set; }
        // OBS-03: an observer is told who won, not a personal Victory or Defeat.
        public bool Observed { get; private set; }
        public string Title => Observed ? (Winner == Side.FreePeoples ? "FREE PEOPLES WIN" : "SHADOW WINS") : Won ? "VICTORY" : "DEFEAT";
        public string Subtitle { get; private set; } = "";
        // UI-233: the painting from this view: the viewer's side and result, or the winner's victory for an observer.
        public string Scene { get; private set; } = "";
        public IReadOnlyList<string> Summary { get; private set; } = new string[0];

        public static GameEndState For(PlayerView view, bool observed = false)
        {
            if (view?.Winner == null)
                return null;
            var winner = view.Winner.Value;
            return new GameEndState
            {
                Winner = winner,
                Won = !observed && winner == view.You,
                Observed = observed,
                Subtitle = BoardScreen.SideName(winner) + " win · " + Reason(view.Victory),
                Scene = SceneOf(observed ? winner : view.You, observed || winner == view.You),
                Summary = Lines(view)
            };
        }

        public static string Reason(VictoryCondition? victory) => victory switch
        {
            VictoryCondition.RingDestroyed => "The Ring is destroyed",
            VictoryCondition.RingCorrupted => "The Ring-bearers are corrupted",
            VictoryCondition.ShadowMilitary or VictoryCondition.FreePeoplesMilitary => "Military victory",
            _ => "The game is over"
        };

        private static string SceneOf(Side side, bool won) =>
            (side == Side.FreePeoples ? "free" : "shadow") + (won ? "-victory" : "-defeat");

        private static IReadOnlyList<string> Lines(PlayerView view) => new[]
        {
            "Turn " + view.Turn,
            "Victory points · Free Peoples " + VictoryPresentation.ScoreText(view, Side.FreePeoples) +
                " · Shadow " + VictoryPresentation.ScoreText(view, Side.Shadow),
            Fellowship(view.Fellowship),
            "Companions in the Fellowship · " + view.Fellowship.CompanionCount
        };

        private static string Fellowship(FellowshipView fellowship)
        {
            var place = fellowship.MordorStep switch
            {
                >= 5 => "Crack of Doom",
                int step => "Mordor step " + step,
                null => "Progress " + fellowship.Progress
            };
            return "Fellowship · " + place + " · Corruption " + fellowship.Corruption + "/" + MaxCorruption;
        }
    }
}
