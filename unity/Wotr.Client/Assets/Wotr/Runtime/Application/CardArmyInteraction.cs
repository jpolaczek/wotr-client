using System;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-216 (CARD_CHOICES_UX): a card's army event on the map and in the region panel, as with an Army die. A lit army
    // starts its move and opens its panel; the panel's lit figures toggle; destinations keep their map click.
    public sealed class CardArmyInteraction
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private PlayerView view;
        private TurnDraft draft;
        private string carried = "";

        public bool Active => view != null && CardArmyEvent.Active(view, draft);

        public CardArmyInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy)
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
            {
                if (!BalrogCardOrder.Active(view))
                    carried = "";
                return;
            }
            StartCarried();
            HighlightFigures();
            board.Map.HighlightSources(CardArmyEvent.Sources(view, draft), view.You);
        }

        // UI-347 (D11): the army whose figure chose "with the army" on A Balrog is Come! starts its move once the card's army
        // step opens; the player picked it already.
        public void Carry(string region) => carried = region;

        private void StartCarried()
        {
            if (carried.Length == 0)
                return;
            var region = carried;
            carried = "";
            if (CardArmyEvent.ArmyRegion(draft).Length == 0)
                board.Root.schedule.Execute(() => Region(region));
        }

        private void HighlightFigures() => PanelFigureHighlight.Add(board.Hover.Panel,
            CardArmyEvent.Movable(view, draft).ToHashSet(), CardArmyEvent.Moving(draft).ToHashSet(), busy);

        // A lit figure of the planned army clicked in the region panel.
        public bool Figure(string id)
        {
            if (!Active || busy() || !CardArmyEvent.Movable(view, draft).Contains(id))
                return false;
            edit(() => CardArmyEvent.Toggle(view, draft, id), false);
            return true;
        }

        // A lit army starts (or restarts) its move and opens its panel; a destination keeps its usual map click.
        public bool Region(string region)
        {
            if (!Active || busy() || !CardArmyEvent.IsSource(view, draft, region) || CardArmyEvent.IsTarget(view, draft, region))
                return false;
            edit(() => BoardDraft.SelectRegion(draft, view, region), false);
            if (CardArmyEvent.ArmyRegion(draft) == region)
                board.Hover.Open(region);
            return true;
        }
    }
}
