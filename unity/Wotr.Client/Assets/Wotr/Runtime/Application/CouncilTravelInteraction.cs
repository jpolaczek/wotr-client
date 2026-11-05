using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-377/UI-379: the Council token's travel in its popover above the token pool (CouncilTravelPicker) and on the map.
    // A figure's icon, its figure in the region panel or a region holding only it chooses it for the group, a second click
    // releases it; a click on a lit destination plans the group there (a faint marker with its count) and the next group
    // can be chosen. The console's ✓ sends the planned moves (UI-381); planning every figure sends them at once.
    public sealed class CouncilTravelInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private PlayerView view;
        private TurnDraft draft;

        public CouncilTravelInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            board.CouncilPicker.FigureChosen += Choose;
            board.Root.RegisterCallback<GeometryChangedEvent>(Follow);
        }

        private CouncilTravelPicker Picker => board.CouncilPicker;

        public bool Active => view != null && CouncilTravel.Active(view, draft);

        // The board's new size or scale moves the token; the popover follows it.
        private void Follow(GeometryChangedEvent _) => Picker.Position();

        public void Refresh(PlayerView latest, TurnDraft selection, bool enabled)
        {
            view = latest;
            draft = selection;
            var token = Active ? board.OwnDice.Q<Button>("pool-" + ExpansionDraft.CouncilTravel) : null;
            if (token == null)
            {
                Picker.Hide();
                return;
            }
            var ready = enabled && !busy();
            Picker.Show(token, board.OwnDice, view, CouncilTravel.Figures(view, draft));
            Picker.Apply(id => CouncilTravel.IsChosen(draft, id), id => CouncilTravel.Planned(draft, id), _ => true, ready);
            ShowOnMap();
        }

        // The unmoved figures glow where they stand and in the region panel; the planned groups stand faintly at their
        // destinations with their count.
        private void ShowOnMap()
        {
            var unmoved = CouncilTravel.Unmoved(view, draft).ToList();
            board.Map.HighlightSources(unmoved.Select(t => new ArmyOption { RegionId = t.Origin, Garrison = Garrison(t.Id) }), view.You);
            board.Map.PlannedFlights.Apply(NazgulFlightDraft.Planned(view, draft), view.You, false);
            PanelFigureHighlight.Add(board.Hover.Panel, CouncilTravel.Figures(view, draft).ToHashSet(),
                CouncilTravel.Chosen(draft).ToHashSet(), busy);
        }

        // A figure clicked in the region panel.
        public bool Figure(string id)
        {
            if (!Active || busy() || !CouncilTravel.Figures(view, draft).Contains(id))
                return false;
            Choose(id);
            return true;
        }

        // A lit destination plans the chosen group there; a region holding one movable figure chooses it, one holding
        // several opens its panel, where they are chosen.
        public bool Region(string region)
        {
            if (!Active || busy())
                return false;
            if (CouncilTravel.Targets(view, draft).Contains(region))
            {
                var next = draft.Fork();
                if (!CouncilTravel.Move(view, next, region, board.Presentation))
                    return false;
                edit(() => draft.Adopt(next), CouncilTravel.Complete(view, next));
                return true;
            }
            var here = CouncilTravel.Figures(view, draft).Where(id => Location(id) == region).ToList();
            if (here.Count == 0)
                return false;
            if (here.Count == 1)
                Choose(here[0]);
            else
                board.Hover.OpenDecision(region, view.Decision.Id);
            return true;
        }

        private void Choose(string id)
        {
            if (!Active || busy())
                return;
            edit(() => CouncilTravel.Choose(view, draft, id), false);
        }

        private string Location(string id) => view.Units.FirstOrDefault(u => u.Id == id)?.Location ??
            view.Characters.FirstOrDefault(c => c.Id == id)?.Location ?? "";

        private bool Garrison(string id) => view.Units.FirstOrDefault(u => u.Id == id)?.Garrison ??
            view.Characters.FirstOrDefault(c => c.Id == id)?.Garrison ?? false;

        public void Dispose()
        {
            board.CouncilPicker.FigureChosen -= Choose;
            board.Root.UnregisterCallback<GeometryChangedEvent>(Follow);
            Picker.Hide();
        }
    }
}
