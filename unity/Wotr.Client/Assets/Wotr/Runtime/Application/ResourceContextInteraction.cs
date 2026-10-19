using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ResourceContextInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private PlayerView view;
        private TurnDraft draft;
        private string forcedSent = "";
        private bool forcedScheduled;

        public ResourceContextInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            board.Sovereigns.Activate = Sovereign;
            board.CouncilTokenSelected += Council;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            board.ResourceDecision.Context(view, draft);
            board.Sovereigns.Context(view, draft);
            board.Root.EnableInClassList("sovereign-context", board.Sovereigns.DecisionTarget.Length > 0);
            HighlightCandidates();
            board.Sovereigns.Highlight(id => !busy() && ExpansionActions.Sovereign(view, draft, id) != null);
            SendForcedChoice();
        }

        // UI-267: a special-die choice that allows one die (an Eye forced among several dice) is sent without a click,
        // once per decision, after the refresh that showed it.
        private void SendForcedChoice()
        {
            if (forcedScheduled || ForcedDie().Length == 0)
                return;
            forcedScheduled = true;
            board.Root.schedule.Execute(() =>
            {
                forcedScheduled = false;
                var die = ForcedDie();
                if (die.Length == 0 || busy())
                    return;
                forcedSent = view.Decision.Id;
                edit(() => draft.Change(p => p.DieId = die), true);
            });
        }

        private string ForcedDie()
        {
            if (!SpecialDice.Choosing(view) || view.Decision.Id == forcedSent || !ExpansionActions.Immediate(draft?.Kind))
                return "";
            var option = DraftOptions.For(view, draft);
            return option?.Enabled == true && option.EligibleDice.Count == 1 ? option.EligibleDice[0] : "";
        }

        private void HighlightCandidates()
        {
            var option = DraftOptions.For(view, draft);
            var choosing = (ResourcePresentation.OwnsDecision(view, draft) || ResourcePresentation.PoolDecision(view)) &&
                option?.Enabled == true;
            var councilDice = ResourcePresentation.CouncilDice(view, draft).ToHashSet();
            var councilToken = draft.Kind == CommandKind.UseCouncilToken ? draft.Payload.AbilityId : draft.Payload.CouncilTokenId;
            foreach (var pool in new[] { board.OwnDice, board.OpponentDice })
                foreach (var button in pool.Query<Button>().ToList())
                {
                    if (button.name.StartsWith("pool-council."))
                    {
                        button.EnableInClassList("selected", button.name == "pool-" + councilToken);
                        continue;
                    }
                    if (button.userData is not DraftToken token || token.Object != DraftObject.Die)
                        continue;
                    var eligible = councilDice.Contains(token.Id) || choosing && (option.EligibleDice.Contains(token.Id) ||
                        view.Decision.Presentation == "DieAndResult" && option.EligibleIds.Contains(token.Id));
                    button.EnableInClassList("resource-candidate", eligible);
                    if (eligible && view.Decision.Kind == DecisionKind.KeepActionDie)
                        button.SetEnabled(true);
                    SpecialCandidate(button, token.Id, eligible);
                }
        }

        // UI-267: during the special-die choice a die an Eye rules out is disabled (its tooltip, DieHints, says why).
        private void SpecialCandidate(Button button, string id, bool eligible)
        {
            var die = view.Dice.FirstOrDefault(d => d.Id == id);
            if (die != null && SpecialDice.Candidate(view, die))
                button.SetEnabled(eligible);
        }

        public bool Die(string id)
        {
            if (view?.Decision == null || busy())
                return false;
            if (ResourcePresentation.CouncilPending(draft) ||
                draft.Kind == CommandKind.EnterCharacter && draft.Payload.CouncilTokenId.Length > 0)
            {
                if (ResourcePresentation.CouncilDice(view, draft).Contains(id))
                    edit(() => draft.Change(p => p.DieId = id), draft.Kind == CommandKind.EnterCharacter);
                return true;
            }
            var next = draft.Fork();
            if (ExpansionActions.Eye(view, next, id))
            {
                edit(() => draft.Adopt(next), next.Payload.CharacterId.Length > 0);
                return true;
            }
            var option = DraftOptions.For(view, draft);
            if (!ExpansionActions.Immediate(draft.Kind) || option?.EligibleDice.Contains(id) != true)
                return false;
            edit(() => draft.Change(p => p.DieId = id), true);
            return true;
        }

        public bool Sovereign(string id)
        {
            if (view?.Decision == null || busy())
                return false;
            var next = draft.Fork();
            if (!ExpansionActions.SelectSovereign(view, next, id))
                return false;
            edit(() => draft.Adopt(next), ExpansionActions.Immediate(next.Kind));
            return true;
        }

        public bool Ring(string id)
        {
            if (view?.Decision == null || busy())
                return false;
            // UI-268: the glowing Nenya socket answers the Hunt's tile step with its Keeper ability.
            if (id == NamedRings.Nenya && NamedRings.NenyaResponse(view) != null)
            {
                edit(() =>
                {
                    if (draft.Kind != CommandKind.RespondEffect)
                        draft.Choose(CommandKind.RespondEffect);
                    draft.Change(p => p.ReactionId = NamedRings.NenyaReaction);
                }, true);
                return true;
            }
            var next = draft.Fork();
            if (!ExpansionActions.Ring(view, next, id))
                return false;
            edit(() => draft.Adopt(next), next.Kind == CommandKind.UseVilya);
            return true;
        }

        private void Council(string id)
        {
            // UI-270: the Council's token choice is made on the offered token in the pool, sent at once.
            if (view != null && !busy() && CouncilTokenSlots.Offered(view, id))
            {
                edit(() =>
                {
                    if (draft.Kind != CommandKind.ChooseCouncilToken)
                        draft.Choose(CommandKind.ChooseCouncilToken);
                    draft.Change(p => p.Ids = new System.Collections.Generic.List<string> { id });
                }, true);
                return;
            }
            if (view == null || busy() || !ResourcePresentation.CanSelectCouncil(view, id))
                return;
            var die = draft.Payload.DieId;
            edit(() =>
            {
                draft.Clear();
                draft.Change(p =>
                {
                    p.CouncilTokenId = id;
                    if (ResourcePresentation.CouncilDice(view, draft).Contains(die))
                        p.DieId = die;
                });
            }, false);
            board.ResourceDecision.Open();
        }

        public bool Drop(DraftToken source, DraftToken target)
        {
            if (view?.Decision == null || busy() || source == null || target == null)
                return false;
            var next = draft.Fork();
            if (source.Object == DraftObject.Ring && target.Object == DraftObject.Die)
            {
                if (!ExpansionActions.Ring(view, next, source.Id) || DraftOptions.For(view, next)?.EligibleDice.Contains(target.Id) != true)
                    return false;
                next.Change(p => p.DieId = target.Id);
            }
            else if (source.Object == DraftObject.RulerToken && target.Object == DraftObject.Die)
            {
                if (ExpansionActions.RulerToken(view, next, source.Id, target.Id))
                {
                    edit(() => draft.Adopt(next), false);
                    return true;
                }
                var option = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.UseDismay &&
                    o.EligibleIds.Contains(source.Id) && o.EligibleDice.Contains(target.Id));
                if (option == null)
                    return false;
                next.Choose(CommandKind.UseDismay, target.Id);
                next.Change(p => p.Ids.Add(source.Id));
            }
            else if (source.Object == DraftObject.Die && target.Object == DraftObject.Sovereign)
            {
                next.Change(p => p.DieId = source.Id);
                if (!ExpansionActions.SelectSovereign(view, next, target.Id))
                    return false;
            }
            else
                return false;
            edit(() => draft.Adopt(next), ExpansionActions.Immediate(next.Kind));
            return true;
        }

        public void Dispose()
        {
            board.Sovereigns.Activate = null;
            board.CouncilTokenSelected -= Council;
        }
    }
}
