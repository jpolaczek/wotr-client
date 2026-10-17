using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class DecisionNotice
    {
        public Button Button { get; }
        private readonly BoardScreen board;

        public DecisionNotice(BoardScreen board)
        {
            this.board = board;
            Button = BoardScreen.Button("◎ Your decision", Open);
            Button.name = "decision-notice";
            Button.AddToClassList("decision-notice");
            Button.style.display = DisplayStyle.None;
        }

        public void Apply(PlayerView view)
        {
            var attention = DecisionAttention.For(view);
            Button.style.display = attention == null ? DisplayStyle.None : DisplayStyle.Flex;
            board.Map.SetAttention(attention?.Region);
            if (attention == null)
                return;
            Button.text = "◎ " + DecisionNoticeText.Describe(view);
            Button.tooltip = attention.Region.Length > 0 && board.Presentation.Regions.TryGetValue(attention.Region, out var region)
                ? DecisionNoticeText.Describe(view) + " · " + region.Name : DecisionNoticeText.Describe(view);
        }

        public void Open()
        {
            var attention = DecisionAttention.For(board.View);
            if (attention == null)
                return;
            board.CloseInformation();
            if (attention.Context is not (DecisionContext.Fellowship or DecisionContext.Hunt or DecisionContext.Sovereign))
                board.Map.Focus(attention.Region);
            if (!DecisionHostNavigation.TryOpen(board))
                OpenContext(attention);
            if (board.View.Decision.Kind == DecisionKind.DiscardCards ||
                board.View.Options.Any(o => o.Kind == CommandKind.DiscardProtection && o.Enabled) ||
                board.View.Decision.Presentation == "Deck")
                board.HandDrawer.Open();
        }

        private void OpenContext(DecisionAttention attention)
        {
            switch (attention.Context)
            {
                case DecisionContext.Hunt:
                    board.Hunt.Open();
                    break;
                case DecisionContext.Battle:
                    board.Battles.Open();
                    break;
                case DecisionContext.Fellowship:
                    board.Fellowship.Open();
                    break;
                case DecisionContext.Sovereign:
                    board.Sovereigns.Show(attention.Subject);
                    break;
                case DecisionContext.Region:
                    if (board.Presentation.Regions.ContainsKey(attention.Region))
                        board.Hover.Open(attention.Region);
                    break;
                default:
                    board.Tray.Focus();
                    break;
            }
        }
    }
}
