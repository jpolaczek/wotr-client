using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-274 (UX_SPEC): the kings' decisions on their objects in the Characters panel: a portrait's choice in a card's
    // step or the cleanse, the attempt button beside an Initial king, the tiles beside a king. A draw that is the only
    // legal step, and a choice with one tile left, are sent without a click, once per step, when they arise during play;
    // one already waiting when the game is opened waits for the console's Draw tiles or the tile's click.
    public sealed class SovereignInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private readonly Func<bool> online;
        private readonly Func<string, bool> previousActivate;
        private PlayerView view;
        private TurnDraft draft;
        private const long ForcedRetryMilliseconds = 150;
        private string forced = "";
        private string match = "";
        private IVisualElementScheduledItem forcedStep;

        public SovereignInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy, Func<bool> online)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            this.online = online;
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
            // UI-12: opening a game never submits; a step already waiting when the game is shown waits for its click.
            if (view.MatchId != match)
            {
                match = view.MatchId;
                forced = Key();
            }
            SendForcedStep();
        }

        private SovereignChoice Choice(string id) =>
            view != null && !busy() && SovereignChoices.Portraits(view).TryGetValue(id, out var choice) ? choice : null;

        private bool Portrait(string id)
        {
            var choice = Choice(id);
            var option = EffectOption();
            if (choice == null || option == null)
                return false;
            edit(() => CardDraft.Choose(draft, option, choice.EffectId), true);
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

        // After the refresh that showed it, so the player sees the step being taken; while the board is busy (a pending
        // send) or not yet online (the first view after connecting) it waits and tries again, once per decision and
        // revision.
        private void SendForcedStep()
        {
            var key = Key();
            if (key.Length == 0 || key == forced)
                return;
            var tile = SovereignAttempt.ForcedTile(view);
            if (!SovereignAttempt.ForcedDraw(view) && tile.Length == 0)
                return;
            forced = key;
            forcedStep?.Pause();
            forcedStep = board.Root.schedule.Execute(() => TryForcedStep(key, tile)).Every(ForcedRetryMilliseconds);
        }

        private void TryForcedStep(string key, string tile)
        {
            if (Key() != key)
            {
                forcedStep.Pause();
                return;
            }
            if (busy() || !online())
                return;
            forcedStep.Pause();
            if (tile.Length > 0)
                AttachTile(tile);
            else
                edit(() =>
                {
                    if (draft.Kind != CommandKind.DrawCorruptionTile)
                        draft.Choose(CommandKind.DrawCorruptionTile);
                }, true);
        }

        private string Key() => view?.Decision == null ? "" : view.Decision.Id + "/" + view.Revision;

        private ActionOption EffectOption() => view?.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect);

        public void Dispose()
        {
            forcedStep?.Pause();
            board.Sovereigns.Activate = previousActivate;
            board.Sovereigns.Choice = _ => null;
            board.Sovereigns.AttemptChosen -= Attempt;
            board.Sovereigns.TileChosen -= Tile;
        }
    }
}
