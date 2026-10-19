using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-212 (CARD_CHOICES_UX): a card's Nazgûl flights on the map. Movable Nazgûl glow where they stand and in the
    // region panel; a click on a lit region or figure arms it, and every click on a lit destination flies one armed
    // Nazgûl there. The console's ✓ sends the flights.
    public sealed class NazgulFlightInteraction
    {
        private static readonly IReadOnlyList<PlannedReinforcement> Nothing = new List<PlannedReinforcement>();
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private PlayerView view;
        private TurnDraft draft;

        public bool Active => view != null && NazgulFlightDraft.Active(view, draft);

        public NazgulFlightInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy)
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
                board.Map.MarkArmed(null, Side.Shadow);
                board.Map.PlannedFlights.Apply(Nothing, Side.Shadow, false);
                return;
            }
            HighlightFigures();
            board.Map.HighlightSources(NazgulFlightDraft.Sources(view, draft), view.You);
            board.Map.MarkArmed(NazgulFlightDraft.Armed(view, draft), view.You);
            board.Map.PlannedFlights.Apply(NazgulFlightDraft.Planned(view, draft), view.You, false);
        }

        // The region panel lights every movable Nazgûl and marks the armed one.
        private void HighlightFigures()
        {
            var armed = NazgulFlightDraft.Next(view, draft)?.Id;
            var selected = armed == null ? new HashSet<string>() : new HashSet<string> { armed };
            PanelFigureHighlight.Add(board.Hover.Panel, NazgulFlightDraft.Movable(view, draft).ToHashSet(), selected, busy);
        }

        // A Nazgûl clicked in the region panel.
        public bool Figure(string id)
        {
            if (!Active || busy() || !NazgulFlightDraft.Movable(view, draft).Contains(id))
                return false;
            edit(() => NazgulFlightDraft.Arm(view, draft, id), false);
            return true;
        }

        // A map click releases the armed source, flies to a lit destination or arms a lit source.
        public bool Region(string region)
        {
            if (!Active || busy())
                return false;
            if (NazgulFlightDraft.ArmedRegion(view, draft) == region)
            {
                edit(() => NazgulFlightDraft.Release(draft), false);
                return true;
            }
            if (NazgulFlightDraft.Targets(view, draft).Contains(region))
            {
                edit(() => NazgulFlightDraft.Fly(view, draft, region, board.Presentation), false);
                return true;
            }
            if (!NazgulFlightDraft.IsSource(view, draft, region))
                return false;
            // As with an army, the armed region's panel opens: it marks the armed Nazgûl and lets the player pick another.
            edit(() => NazgulFlightDraft.Arm(view, draft, region), false);
            board.Hover.Open(region);
            return true;
        }
    }
}
