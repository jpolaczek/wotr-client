using System;
using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-431 (owner's rule, UI-430): Monsters Roused's halves on their regions. A lit region's click chooses its half and
    // carries the region into the card's next step (BranchCarry), where it is that step's region click: East's Elite
    // destination (sent at once, as its own click sent it) or the start of West's recruitment plan.
    public sealed class MonstersHalfInteraction
    {
        private static readonly IReadOnlyDictionary<string, string> NoOffers = new Dictionary<string, string>();
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private readonly BranchCarry carry;
        private readonly Action<string> regionClick;
        private PlayerView view;
        private TurnDraft draft;
        // The decision whose lit regions the view was brought to, once.
        private string revealed = "";

        // What a click on each lit region's forces does now, for their markers' tooltips.
        public IReadOnlyDictionary<string, string> Offers { get; private set; } = NoOffers;

        // regionClick: the map's region click (TurnInteraction.ChooseRegion), which the carried region makes in the next step.
        public MonstersHalfInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy, BranchCarry carry, Action<string> regionClick)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            this.carry = carry;
            this.regionClick = regionClick;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            Offers = busy() ? NoOffers : MonstersHalves.Offers(view);
            if (busy())
                return;
            Reveal();
            var region = carry.Take(view, MonstersHalves.EastStep);
            if (region.Length == 0)
                region = carry.Take(view, MonstersHalves.WestStep);
            if (region.Length > 0)
                board.Root.schedule.Execute(() => regionClick(region));
        }

        // The halves' regions lie in the north, often off screen: the view moves to them once for the decision.
        private void Reveal()
        {
            var regions = MonstersHalves.Halves(view);
            if (regions.Count == 0 || revealed == view.Decision.Id || board.Map.contentRect.width <= 0)
                return;
            revealed = view.Decision.Id;
            board.Map.Reveal(regions.Keys);
        }

        // A lit region's click chooses its half and carries the region.
        public bool Region(string region)
        {
            var half = view == null ? "" : MonstersHalves.Half(view, region);
            if (half.Length == 0)
                return false;
            if (busy())
                return true;
            var option = DraftOptions.For(view, draft);
            if (option == null)
                return false;
            carry.Set(view, MonstersHalves.NextStep(half), region);
            edit(() => CardDraft.Choose(draft, option, half), true);
            return true;
        }
    }
}
