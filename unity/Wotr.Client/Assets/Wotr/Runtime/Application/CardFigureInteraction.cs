using System;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-215 (CARD_CHOICES_UX): a card's figure choice in the region panel. The regions holding offered figures glow on
    // the map; a click opens the region's panel, whose lit figures toggle on click. A single figure choice sends at
    // once; otherwise the console counts the choice and its ✓ confirms.
    public sealed class CardFigureInteraction
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private PlayerView view;
        private TurnDraft draft;

        public bool Active => view != null && CardFigureDraft.Active(view, draft);

        public CardFigureInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            if (!Active)
                return;
            HighlightFigures();
            board.Map.HighlightSources(CardFigureDraft.Sources(view, draft), CardFigureDraft.FigureSide(view, draft));
        }

        private void HighlightFigures() => PanelFigureHighlight.Add(board.Hover.Panel,
            CardFigureDraft.Eligible(view, draft).ToHashSet(), draft.Payload.Ids.ToHashSet(), busy);

        // A lit figure clicked in the region panel.
        public bool Figure(string id)
        {
            if (!Active || busy() || !CardFigureDraft.Eligible(view, draft).Contains(id))
                return false;
            edit(() => CardFigureDraft.Toggle(view, draft, id), CardFigureDraft.CompletesOnClick(view, draft));
            return true;
        }

        // A lit region opens its panel; any other click keeps its usual meaning (a Dead Men destination). UI-254: the
        // panel hosts the decision, so it stays while the pointer goes to the console's ✓.
        public bool Region(string region)
        {
            if (!Active || busy() || !CardFigureDraft.IsSource(view, draft, region))
                return false;
            board.Hover.OpenDecision(region, view.Decision.Id);
            return true;
        }
    }
}
