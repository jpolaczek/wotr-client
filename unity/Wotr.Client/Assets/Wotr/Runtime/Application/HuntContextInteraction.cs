using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class HuntContextInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        // UI-204: while the Shadow allocates, the dice the Hunt can still take pulse (with motion on), so adding them
        // is the evident next step; the dice panel carries the beat for all of them.
        public const string CandidatePulseClass = "hunt-candidates-bright";
        // UI-205: before any die is in the allocation the candidates call loudest — the dice are the first step.
        public const string ChooseClass = "hunt-choose-dice";
        private const long PulseMilliseconds = 700;
        private readonly IVisualElementScheduledItem pulse;
        private PlayerView view;
        private TurnDraft draft;

        public HuntContextInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            pulse = board.OwnDice.schedule.Execute(() => board.OwnDice.ToggleInClassList(CandidatePulseClass)).Every(PulseMilliseconds);
            pulse.Pause();
            board.Hunt.Dice.Selected += Reroll;
            board.Hunt.Guides.Selected += Guide;
            board.Hunt.Responses.Selected += Respond;
            board.Hunt.Responses.GuideResponse += GuideResponse;
            board.Hunt.Responses.CardResponse += CardResponse;
            board.Hunt.Tile.Selected += ApplyResponse;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            board.Hunt.Context(view, draft, board.ActionDie, !busy());
            var allocation = HuntActions.Allocation(view);
            var candidates = false;
            foreach (var button in board.OwnDice.Query<Button>().ToList())
            {
                if (button.userData is not DraftToken { Object: DraftObject.Die } die)
                    continue;
                button.EnableInClassList("hunt-allocated", allocation != null && draft.Payload.Ids.Contains(die.Id));
                var candidate = allocation != null && HuntActions.CanAdd(draft, allocation, die.Id);
                button.EnableInClassList("hunt-candidate", candidate);
                candidates |= candidate;
            }
            PulseCandidates(candidates);
            board.OwnDice.EnableInClassList(ChooseClass, candidates && draft.Payload.Ids.Count == 0);
        }

        private void PulseCandidates(bool on)
        {
            if (on)
            {
                pulse.Resume();
                return;
            }
            pulse.Pause();
            board.OwnDice.RemoveFromClassList(CandidatePulseClass);
        }

        public bool Die(string id)
        {
            var option = HuntActions.Allocation(view);
            if (option == null)
                return false;
            if (!busy() && option.EligibleIds.Contains(id))
                edit(() => HuntActions.ToggleDie(draft, option, id), false);
            return true;
        }

        public bool Drop(DraftToken source, DraftToken target)
        {
            if (target?.Object == DraftObject.HuntTile && HuntResponseActions.Read(source, out var response))
                return CompleteResponse(response);
            if (busy() || source?.Object != DraftObject.Die || target?.Object != DraftObject.HuntEye ||
                HuntActions.Allocation(view) is not { } option)
                return false;
            var next = draft.Fork();
            if (!HuntActions.Allocate(next, option, source.Id))
                return false;
            edit(() => draft.Adopt(next), false);
            return true;
        }

        // UI-142: a response is a complete choice, so its click applies it at once.
        private void Respond(HuntResponse response)
        {
            if (HuntResponseActions.Option(view)?.HuntResponses.Contains(response) == true)
                CompleteResponse(response);
        }

        // UI-270: an alternate companion's Guide response, sent with its click.
        private void GuideResponse(string id)
        {
            if (busy() || draft == null || !HuntGuideResponses.InRow(view, id))
                return;
            edit(() =>
            {
                if (draft.Kind != CommandKind.RespondEffect)
                    draft.Choose(CommandKind.RespondEffect);
                draft.Change(p => p.ReactionId = id);
            }, true);
        }

        // UI-272: a Lords table card's response in the row: a reaction, or We Won't Go Back's cost; its click sends it.
        private void CardResponse(string id)
        {
            var response = busy() || draft == null ? null : HuntCardResponses.Find(view, id);
            if (response == null)
                return;
            edit(() =>
            {
                if (response.Reaction)
                {
                    if (draft.Kind != CommandKind.RespondEffect)
                        draft.Choose(CommandKind.RespondEffect);
                    draft.Change(p => p.ReactionId = id);
                }
                else
                    CardDraft.Choose(draft, DraftOptions.For(view, draft), id);
            }, true);
        }

        private void ApplyResponse()
        {
            if (draft?.HuntChoice is { } response)
                CompleteResponse(response);
        }

        private bool CompleteResponse(HuntResponse response)
        {
            if (busy() || draft == null)
                return false;
            var next = draft.Fork();
            if (!HuntResponseActions.Select(view, next, response))
                return false;
            edit(() => draft.Adopt(next), true);
            return true;
        }

        private void Reroll(string id)
        {
            if (busy() || draft.Kind != CommandKind.RerollHunt || view.DecisionOwner != view.You)
                return;
            var option = DraftOptions.For(view, draft);
            if (option?.EligibleIds.Contains(id) == true)
                edit(() => FellowshipDraft.ToggleReroll(draft, option, id), false);
        }

        private void Guide(string id)
        {
            var kind = FellowshipActions.PortraitKind(view, board.ActionDie, id, draft);
            if (busy() || !HuntActions.UsesPanel(view, draft) || kind is not (CommandKind.SelectGuide or CommandKind.ChooseEffect))
                return;
            // The Guide sends with its click; the Companions of The Breaking of the Fellowship keep the one confirmation.
            edit(() => FellowshipActions.Portrait(view, draft, board.ActionDie, id), kind == CommandKind.SelectGuide);
        }

        public void Dispose()
        {
            pulse.Pause();
            board.Hunt.Dice.Selected -= Reroll;
            board.Hunt.Guides.Selected -= Guide;
            board.Hunt.Responses.Selected -= Respond;
            board.Hunt.Responses.GuideResponse -= GuideResponse;
            board.Hunt.Responses.CardResponse -= CardResponse;
            board.Hunt.Tile.Selected -= ApplyResponse;
        }
    }
}
