using System;
using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-431 (owner's rule, UI-430): The Black Captain Commands' Move Nazgûl on the map. The forces of the player's Nazgûl
    // and the Witch-king glow and pulse where they stand, as armies a click may choose (eligible-army, UI-301: this map's
    // ability-owner only swaps a plinth), and say what a click does; a click on one (its force or its region) chooses
    // Move Nazgûl and arms that stack in the flight that follows, so the next click is its destination. The recruit stands
    // on the Reserve's Nazgûl (ReinforcementsInteraction).
    public sealed class CaptainOrderInteraction
    {
        private static readonly IReadOnlyDictionary<string, string> NoOffers = new Dictionary<string, string>();
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private readonly Func<string, bool> armFlight;
        private PlayerView view;
        private TurnDraft draft;
        // The region whose Nazgûl chose Move Nazgûl, armed when the flight opens; "" when none.
        private string carried = "";
        // The order whose lit Nazgûl the view was brought to, once.
        private string revealed = "";

        // What a click on each lit force does now, for its marker's tooltip.
        public IReadOnlyDictionary<string, string> Offers { get; private set; } = NoOffers;

        // armFlight: arms a region's Nazgûl in a card's flight (NazgulFlightInteraction.Region).
        public CaptainOrderInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy, Func<string, bool> armFlight)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            this.armFlight = armFlight;
        }

        // After the components and the armies' contexts, whose map sources this replaces while the order waits.
        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            StartCarried();
            var regions = BlackCaptainOrder.FlyRegions(view);
            Offers = busy() ? NoOffers : regions;
            if (regions.Count == 0)
                return;
            board.Map.HighlightSources(BlackCaptainOrder.FlyForces(view), view.You);
            Reveal(regions.Keys);
        }

        // The lit Nazgûl may stand off screen (under the console): the view moves to them once for the order.
        private void Reveal(IEnumerable<string> regions)
        {
            if (revealed == view.Decision.Id || board.Map.contentRect.width <= 0)
                return;
            revealed = view.Decision.Id;
            board.Map.Reveal(regions);
        }

        // A click on a lit Nazgûl's region chooses Move Nazgûl and carries the region into the flight.
        public bool Region(string region)
        {
            if (view == null || !BlackCaptainOrder.FlyRegions(view).ContainsKey(region))
                return false;
            if (busy())
                return true;
            var option = DraftOptions.For(view, draft);
            if (option == null)
                return false;
            carried = region;
            edit(() => CardDraft.Choose(draft, option, BlackCaptainOrder.Fly), true);
            return true;
        }

        // The flight's first step arms the carried region; any other decision drops the carry.
        private void StartCarried()
        {
            if (carried.Length == 0 || BlackCaptainOrder.FlyOffered(view))
                return;
            var region = carried;
            carried = "";
            if (NazgulFlightDraft.Active(view, draft) && NazgulFlightDraft.IsSource(view, draft, region))
                board.Root.schedule.Execute(() => armFlight(region));
        }
    }
}
