using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // The player's own decision at the top of the board; a click opens where it is made. UI-254: a decision the game
    // waits on (RequiredDecisions) wears the required variant — red enamel with a "!" that pulses in the plaque's
    // must-act rhythm — and goes when the decision is resolved. Its click opens the decision's own window, never a new one.
    public sealed class DecisionNotice
    {
        public const string RequiredClass = "required-decision";
        public Button Button { get; }
        public bool Required => Button.ClassListContains(RequiredClass);
        private readonly BoardScreen board;
        private readonly ChoicePulse pulse;

        public DecisionNotice(BoardScreen board)
        {
            this.board = board;
            Button = BoardScreen.Button("◎ Your decision", Open);
            Button.name = "decision-notice";
            Button.AddToClassList("decision-notice");
            Button.style.display = DisplayStyle.None;
            pulse = new ChoicePulse(Button);
        }

        public void Apply(PlayerView view)
        {
            var attention = DecisionAttention.For(view);
            var required = attention != null && RequiredDecisions.Waits(view);
            Button.style.display = attention == null ? DisplayStyle.None : DisplayStyle.Flex;
            Button.EnableInClassList(RequiredClass, required);
            pulse.Run(required);
            board.Map.SetAttention(attention?.Region);
            if (attention == null)
                return;
            Button.text = (required ? "! " : "◎ ") + DecisionNoticeText.Describe(view);
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
            if (UsesHand(board.View))
                board.HandDrawer.Open();
        }

        // UI-292: a click shows something: the panel holding the tray, the decision's panel or region (the camera moves
        // to it), or the hand. Otherwise Open only focuses the tray, which nobody sees.
        public bool Opens
        {
            get
            {
                var attention = DecisionAttention.For(board.View);
                if (attention == null)
                    return false;
                if (DecisionHostNavigation.Hosted(board) || UsesHand(board.View) || attention.Region.Length > 0)
                    return true;
                return attention.Context is DecisionContext.Hunt or DecisionContext.Battle or DecisionContext.Fellowship or
                    DecisionContext.Sovereign;
            }
        }

        private static bool UsesHand(PlayerView view) => view.Decision.Kind == DecisionKind.DiscardCards ||
            view.Options.Any(o => o.Kind == CommandKind.DiscardProtection && o.Enabled) || view.Decision.Presentation == "Deck";

        // UI-254: the window the notice opened for the current decision is open: its region panel, or the hand for
        // excess cards. The console's plaque closes it again.
        public bool IsOpen
        {
            get
            {
                var view = board.View;
                if (DecisionAttention.For(view) == null)
                    return false;
                return board.Hover.HoldsDecisionOf(view.Decision.Id) ||
                    view.Decision.Kind == DecisionKind.DiscardCards && board.HandDrawer.IsOpen;
            }
        }

        public void Close()
        {
            if (board.Hover.HoldsDecision)
                board.Hover.Close();
            if (board.View?.Decision?.Kind == DecisionKind.DiscardCards)
                board.HandDrawer.Close();
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
                // UI-274: the kings' decisions are made on their portraits in the Characters panel.
                case DecisionContext.Sovereign:
                    board.Characters.Open();
                    break;
                case DecisionContext.Region:
                    if (board.Presentation.Regions.ContainsKey(attention.Region))
                        board.Hover.OpenDecision(attention.Region, board.View.Decision.Id);
                    break;
                default:
                    board.Tray.Focus();
                    break;
            }
        }
    }
}
