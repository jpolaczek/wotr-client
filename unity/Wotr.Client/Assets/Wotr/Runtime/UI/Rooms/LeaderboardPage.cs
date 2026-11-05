using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-503 (R35, docs/ui/RANKED_UX.md): the Leaderboards page of the menu window: the top 25, the player's own row marked if
    // it is among them, and their place below. All 25 rows share the window's height, so nothing scrolls; a shorter list keeps
    // their size. The rows are not clickable. A failed read says so and keeps what was shown.
    public sealed class LeaderboardPage
    {
        public const int Size = 25;
        public const string OwnClass = "leaderboard-own";
        public const string Empty = "No ranked games yet. Choose Find game and finish one to get on the board.";
        public const string NoPlace = "You have no rating yet: finish a ranked game to get on the board.";
        public const string Failed = "The leaderboard could not be read. Use Refresh to try again.";

        public VisualElement Root { get; } = new VisualElement { name = "leaderboards" };
        private readonly Label empty = new Label(Empty) { name = "leaderboard-empty", pickingMode = PickingMode.Ignore };
        private readonly Label you = new Label { name = "leaderboard-you", pickingMode = PickingMode.Ignore };
        private readonly Label status = new Label { name = "leaderboard-status", pickingMode = PickingMode.Ignore };
        private readonly List<LeaderboardRowView> rows = new List<LeaderboardRowView>();

        public LeaderboardPage()
        {
            Root.AddToClassList("leaderboard");
            Root.Add(Header());
            var list = new VisualElement { name = "leaderboard-rows", pickingMode = PickingMode.Ignore };
            list.AddToClassList("leaderboard-rows");
            for (var index = 0; index < Size; index++)
            {
                var row = new LeaderboardRowView(index);
                rows.Add(row);
                list.Add(row.Root);
            }
            empty.AddToClassList("leaderboard-empty");
            empty.style.display = DisplayStyle.None;
            list.Add(empty);
            Root.Add(list);
            Root.Add(Footer());
        }

        public void Show(LeaderboardView view)
        {
            status.text = "";
            var ownRank = view.You?.Rank ?? 0;
            for (var index = 0; index < rows.Count; index++)
                rows[index].Show(index < view.Rows.Count ? view.Rows[index] : null, ownRank);
            empty.style.display = view.Rows.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            you.text = view.You == null ? NoPlace : "Your place: " + view.You.Rank + " · Rating " + view.You.Rating;
        }

        public void ShowFailed() => status.text = Failed;

        private static VisualElement Header()
        {
            var header = LeaderboardRowView.Line("leaderboard-head", PickingMode.Ignore);
            foreach (var (text, column) in new[]
            {
                ("#", "leaderboard-rank"), ("Player", "leaderboard-name"), ("Rating", "leaderboard-number"),
                ("Games", "leaderboard-number"), ("Wins", "leaderboard-number"), ("Losses", "leaderboard-number")
            })
                header.Add(LeaderboardRowView.Cell(text, column));
            return header;
        }

        private VisualElement Footer()
        {
            var footer = new VisualElement { pickingMode = PickingMode.Ignore };
            footer.AddToClassList("leaderboard-footer");
            you.AddToClassList("leaderboard-you");
            footer.Add(you);
            status.AddToClassList("leaderboard-status");
            footer.Add(status);
            return footer;
        }
    }

    // One row of the list; it writes only the text that changed, and an unused row keeps its room.
    public sealed class LeaderboardRowView
    {
        public VisualElement Root { get; }
        private readonly Label rank = Cell("", "leaderboard-rank");
        private readonly Label player = Cell("", "leaderboard-name");
        private readonly Label rating = Cell("", "leaderboard-number");
        private readonly Label games = Cell("", "leaderboard-number");
        private readonly Label wins = Cell("", "leaderboard-number");
        private readonly Label losses = Cell("", "leaderboard-number");

        public LeaderboardRowView(int index)
        {
            Root = Line("leaderboard-row", PickingMode.Ignore);
            Root.name = "leaderboard-row-" + (index + 1);
            foreach (var cell in new[] { rank, player, rating, games, wins, losses })
                Root.Add(cell);
            Show(null, 0);
        }

        public void Show(LeaderboardRow row, int ownRank)
        {
            Root.style.visibility = row == null ? Visibility.Hidden : Visibility.Visible;
            Root.EnableInClassList(LeaderboardPage.OwnClass, row != null && row.Rank == ownRank);
            if (row == null)
                return;
            Set(rank, row.Rank.ToString());
            Set(player, row.Name);
            Set(rating, row.Rating.ToString());
            Set(games, row.Games.ToString());
            Set(wins, row.Wins.ToString());
            Set(losses, row.Losses.ToString());
        }

        public static VisualElement Line(string style, PickingMode picking)
        {
            var line = new VisualElement { pickingMode = picking };
            line.AddToClassList(style);
            return line;
        }

        public static Label Cell(string text, string column)
        {
            var cell = new Label(text) { pickingMode = PickingMode.Ignore };
            cell.AddToClassList("leaderboard-cell");
            cell.AddToClassList(column);
            return cell;
        }

        private static void Set(Label label, string text)
        {
            if (label.text != text)
                label.text = text;
        }
    }
}
