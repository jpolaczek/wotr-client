using System;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-274 (UX_SPEC): the kings' decisions on their objects in the Characters panel: a portrait's choice in a card's
    // step or the cleanse, the attempt button beside an Initial king, the tiles beside a king. Nothing is sent without a
    // click (UI-279): a draw that is the only legal step waits for the console's Draw tiles, a last tile for its click.
    public sealed class SovereignInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private readonly Func<string, bool> previousActivate;
        private readonly BranchCarry carry;
        private PlayerView view;
        private TurnDraft draft;

        public SovereignInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy, BranchCarry carry)
        {
            this.board = board;
            this.carry = carry;
            this.edit = edit;
            this.busy = busy;
            previousActivate = board.Sovereigns.Activate;
            board.Sovereigns.Activate = id => previousActivate?.Invoke(id) == true || Portrait(id);
            board.Sovereigns.Choice = Choice;
            board.Sovereigns.AttemptChosen += Attempt;
            board.Sovereigns.TileChosen += Tile;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            // UI-278 (D15): the king whose portrait chose Wisdom's inspection is the inspection's king.
            if (!busy() && carry.Take(view, SovereignChoices.Sovereign) is { Length: > 0 } king)
                board.Root.schedule.Execute(() => Portrait(king));
        }

        private SovereignChoice Choice(string id) =>
            view != null && !busy() && SovereignChoices.Portraits(view).TryGetValue(id, out var choice) ? choice : null;

        private bool Portrait(string id)
        {
            var choice = Choice(id);
            var option = EffectOption();
            if (choice == null || option == null)
                return false;
            if (choice.EffectId == KingsCardChoices.Inspect)
                carry.Set(view, SovereignChoices.Sovereign, id);
            edit(() => EffectChoiceSelection.Choose(view, draft, option, choice.EffectId), true);
            return true;
        }

        private void Attempt(string id)
        {
            if (view == null || busy() || !SovereignAttempt.Enabled(view, draft, id))
                return;
            var eye = SovereignAttempt.Eye(view, draft);
            edit(() =>
            {
                draft.Choose(CommandKind.AttemptCorruption, eye);
                draft.Change(p => p.CharacterId = id);
            }, true);
        }

        private void Tile(string king, string tile)
        {
            if (view == null || busy())
                return;
            if (SovereignAttempt.OwnAttempt(view))
                AttachTile(tile);
            else if (SovereignChoices.InspectedTiles(view).TryGetValue(tile, out var choice) && EffectOption() is { } option)
                edit(() => CardDraft.Choose(draft, option, choice.EffectId), true);
        }

        private void AttachTile(string tile)
        {
            var option = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseCorruptionTile && o.EligibleIds.Contains(tile));
            if (option == null)
                return;
            edit(() =>
            {
                if (draft.Kind != CommandKind.ChooseCorruptionTile)
                    draft.Choose(CommandKind.ChooseCorruptionTile);
                CardDraft.Choose(draft, option, tile);
            }, true);
        }

        private ActionOption EffectOption() => view?.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect);

        public void Dispose()
        {
            board.Sovereigns.Activate = previousActivate;
            board.Sovereigns.Choice = _ => null;
            board.Sovereigns.AttemptChosen -= Attempt;
            board.Sovereigns.TileChosen -= Tile;
        }
    }
}
