using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-377: the Council token's travel in its popover above the token pool (CouncilTravelPicker). A lit Nazgûl's icon
    // adds it to the group being planned, as its figure on the map does; its destination is clicked on the map; the ✓
    // above the token sends the move as the console's ✓ does.
    public sealed class CouncilTravelInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private readonly Action confirm;
        private readonly Func<bool> legal;
        private PlayerView view;
        private TurnDraft draft;

        // legal: the server's preview accepts the planned move.
        public CouncilTravelInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy, Action confirm, Func<bool> legal)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            this.confirm = confirm;
            this.legal = legal;
            board.CouncilPicker.FigureChosen += Choose;
            board.CouncilPicker.Confirmed += Confirm;
            board.Root.RegisterCallback<GeometryChangedEvent>(Follow);
        }

        // The board's new size or scale moves the token; the popover follows it.
        private void Follow(GeometryChangedEvent _) => Picker.Position();

        private CouncilTravelPicker Picker => board.CouncilPicker;

        public void Refresh(PlayerView latest, TurnDraft selection, bool enabled)
        {
            view = latest;
            draft = selection;
            var option = CouncilTravel.Option(view, draft);
            var token = option == null ? null : board.OwnDice.Q<Button>("pool-" + ExpansionDraft.CouncilTravel);
            if (token == null)
            {
                Picker.Hide();
                return;
            }
            Picker.Show(token, board.OwnDice, view, CouncilTravel.Figures(view, draft));
            Picker.Apply(id => CouncilTravel.Chosen(draft, id), id => CouncilTravel.Planned(draft, id),
                id => TravelDraft.Selectable(draft, view, option, id), enabled && !busy());
            Picker.SetConfirm(enabled && !busy() && legal(), CouncilTravel.ConfirmTooltip(view, draft));
        }

        private void Choose(string id)
        {
            var option = CouncilTravel.Option(view, draft);
            if (option == null || busy() || !TravelDraft.Selectable(draft, view, option, id))
                return;
            edit(() => TravelDraft.ToggleCompanion(draft, view, option, id), false);
        }

        private void Confirm()
        {
            if (CouncilTravel.Active(view, draft) && !busy() && legal())
                confirm();
        }

        public void Dispose()
        {
            board.CouncilPicker.FigureChosen -= Choose;
            board.CouncilPicker.Confirmed -= Confirm;
            board.Root.UnregisterCallback<GeometryChangedEvent>(Follow);
            Picker.Hide();
        }
    }
}
