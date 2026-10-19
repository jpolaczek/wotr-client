using System;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class CardContextInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private readonly Action<string> problem;
        private PlayerView view;
        private TurnDraft draft;
        private string politicsOpenedFor = "";

        public CardContextInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy, Action<string> problem)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            this.problem = problem;
            board.CardSelected += Click;
            board.Cards.PlayRequested += PlayCard;
            board.Cards.Reaction = ReactionChoice;
            board.Cards.ReactionRequested += React;
            board.Cards.DiscardRequested += DiscardFromInspector;
            board.Decks.Selected += Draw;
            board.CardCosts.Selected += Cost;
            board.Nations.SelectTarget = Nation;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            board.Cards.Actions(draft, board.ActionDie, !busy());
            board.Decks.Refresh(view, draft, board.ActionDie, busy());
            board.CardCosts.Refresh(view, draft, board.ActionDie, busy());
            var targets = draft.Kind == CommandKind.PlayEvent ? CardDraft.Event(draft, DraftOptions.For(view, draft))?.NationTargets : null;
            board.Nations.Highlight(targets ?? Enumerable.Empty<string>());
            OpenPolitics(targets?.Count > 0 && draft.Payload.NationId.Length == 0);
        }

        // UI-148: a played card that waits for a nation opens Politics once per play, so its highlighted nations
        // are in view; a panel the player then closes stays closed for that play. UI-179: a play that ends (Back to
        // the die, whose Reinforcements tab takes the rail) is over, so arming the card again opens Politics again.
        private void OpenPolitics(bool waiting)
        {
            var key = waiting ? view.MatchId + "/" + view.Decision?.Id + "/" + draft.Payload.CardId : "";
            if (key.Length == 0)
                politicsOpenedFor = "";
            if (key.Length == 0 || key == politicsOpenedFor)
                return;
            politicsOpenedFor = key;
            board.Nations.Open();
        }

        // UI-164: a reaction the decision offers from a card in the hand is used at that card.
        private string ReactionChoice(string card)
        {
            if (view == null || view.DecisionOwner != view.You || !view.Hand.Contains(card))
                return "";
            var option = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.RespondEffect);
            return option?.EffectChoices.FirstOrDefault(c => c.SourceId == card)?.Id ?? "";
        }

        private void React(string card)
        {
            var id = ReactionChoice(card);
            if (busy() || id.Length == 0)
                return;
            board.Cards.Close();
            edit(() =>
            {
                if (draft.Kind != CommandKind.RespondEffect)
                    draft.Choose(CommandKind.RespondEffect);
                draft.Change(p => p.ReactionId = id);
            }, true);
        }

        private void Click(string id)
        {
            if (busy() || view == null)
                return;
            var option = DraftOptions.For(view, draft);
            if (option == null || !option.EligibleIds.Contains(id))
                return;
            // UI-222: a removal cost is sent once both cards are chosen; a single card choice (ChooseEffect) is sent at
            // once by the selection's own rule (CardActions.SingleEffect).
            if (draft.Kind == CommandKind.DiscardProtection)
            {
                var next = draft.Fork();
                CardDraft.Choose(next, option, id);
                edit(() => draft.Adopt(next), CardDraft.Ready(next, option));
            }
            else if (draft.Kind == CommandKind.ChooseEffect && view.Hand.Contains(id))
                edit(() => CardDraft.Choose(draft, option, id), false);
        }

        public bool Play(DraftToken token)
        {
            if (token?.Object == DraftObject.Card && !busy() && view != null && draft.Kind == CommandKind.DiscardCards)
                return Discard(token.Id);
            if (token?.Object != DraftObject.Card || busy() || view == null ||
                draft.Kind is CommandKind.DiscardCards or CommandKind.DiscardProtection ||
                !CardActions.CanPlay(view, draft, board.ActionDie, token.Id))
                return false;
            Played(token.Id);
            edit(() => CardActions.Start(draft, view, board.ActionDie, token.Id), true);
            return true;
        }

        // UI-147: playing a card opens no window with its text, and a preview of that card closes with the play.
        private void Played(string id)
        {
            if (board.Cards.CardId == id)
                board.Cards.Close();
        }

        private void DiscardFromInspector(string id) => Play(new DraftToken(DraftObject.Card, id));

        private bool Discard(string id)
        {
            var option = DraftOptions.For(view, draft);
            if (option?.EligibleIds.Contains(id) != true)
                return false;
            if (option.Minimum > 1)
            {
                problem("Server update required.\nAsk the host to update the server to discard cards one at a time.");
                return true;
            }
            edit(() => draft.Change(payload =>
            {
                payload.Ids.Clear();
                payload.Ids.Add(id);
            }), true);
            return true;
        }

        private void PlayCard(string id, DieFace? face)
        {
            if (busy() || view == null || draft.Kind is CommandKind.DiscardCards or CommandKind.DiscardProtection)
                return;
            var next = draft.Fork();
            if (!CardActions.Start(next, view, board.ActionDie, id, face))
                return;
            Played(id);
            edit(() => draft.Adopt(next), true);
        }

        public bool Drop(DraftToken source, DraftToken target)
        {
            if (source?.Object != DraftObject.Card || target == null || busy())
                return false;
            if (target.Object is not (DraftObject.Tray or DraftObject.Region or DraftObject.Nation))
                return false;
            var next = draft.Fork();
            if (!CardActions.Start(next, view, board.ActionDie, source.Id))
                return false;
            if (target.Object == DraftObject.Nation)
            {
                var card = CardDraft.Event(next, DraftOptions.For(view, next));
                if (card?.NationTargets.Contains(target.Id) != true)
                    return false;
                next.Change(p => p.NationId = target.Id);
            }
            Played(source.Id);
            edit(() => draft.Adopt(next), true);
            return true;
        }

        private void Draw(string deck)
        {
            if (busy() || view == null)
                return;
            var spear = ChiefReactionObjects.DeckReaction(view, deck);
            if (spear.Length > 0)
                edit(() => Respond(spear), true);
            else if (CardActions.CanDraw(view, draft, board.ActionDie, deck))
                edit(() => CardActions.Draw(draft, view, board.ActionDie, deck), true);
        }

        private void Respond(string reaction)
        {
            if (draft.Kind != CommandKind.RespondEffect)
                draft.Choose(CommandKind.RespondEffect);
            draft.Change(p => p.ReactionId = reaction);
        }

        private void Cost(EventCostOption cost)
        {
            if (busy())
                return;
            edit(() =>
            {
                draft.Clear();
                draft.Change(p => { p.DieId = cost.DieId; p.Face = cost.Face; p.AbilityId = cost.AbilityId; });
            }, false);
        }

        private bool Nation(string nation)
        {
            if (view == null || draft.Kind != CommandKind.PlayEvent ||
                CardDraft.Event(draft, DraftOptions.For(view, draft))?.NationTargets.Contains(nation) != true)
                return false;
            if (!busy())
                edit(() => draft.Change(p => p.NationId = nation), true);
            return true;
        }

        public void Dispose()
        {
            board.CardSelected -= Click;
            board.Cards.PlayRequested -= PlayCard;
            board.Cards.ReactionRequested -= React;
            board.Cards.Reaction = null;
            board.Cards.DiscardRequested -= DiscardFromInspector;
            board.Decks.Selected -= Draw;
            board.CardCosts.Selected -= Cost;
            board.Nations.SelectTarget = null;
        }
    }
}
