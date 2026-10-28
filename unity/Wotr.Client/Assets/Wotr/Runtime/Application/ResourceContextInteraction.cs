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
        private readonly Action<ComponentAction> select;
        private readonly ChoicePulse litArt;
        private readonly ChoicePulse litOpponentArt;
        private PlayerView view;
        private TurnDraft draft;

        // select: a component's own path (UI-347: a Council token's printed use, started on its medallion).
        public ResourceContextInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy, Action<ComponentAction> select)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            this.select = select;
            litArt = new ChoicePulse(board.OwnDice, ChoiceGlow.LitArtPulseClass);
            litOpponentArt = new ChoicePulse(board.OpponentDice, ChoiceGlow.LitArtPulseClass);
            board.Sovereigns.Activate = Sovereign;
            board.CouncilTokenSelected += Council;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            board.Sovereigns.Context(view, draft);
            board.Root.EnableInClassList("sovereign-context", board.Sovereigns.DecisionTarget.Length > 0);
            HighlightCandidates();
            board.Sovereigns.Highlight(id => !busy() && ExpansionActions.Sovereign(view, draft, id) != null);
        }

        private void HighlightCandidates()
        {
            var option = DraftOptions.For(view, draft);
            var choosing = (ResourcePresentation.PoolDecision(view) || LoreChoice.Active(view)) && option?.Enabled == true;
            var councilDice = ResourcePresentation.CouncilDice(view, draft).ToHashSet();
            var councilToken = draft.Kind == CommandKind.UseCouncilToken ? draft.Payload.AbilityId : draft.Payload.CouncilTokenId;
            var lit = false;
            var litOpponent = false;
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
                    lit |= eligible && pool == board.OwnDice;
                    litOpponent |= eligible && pool == board.OpponentDice;
                    // UI-276 (D20): the opponent's Ruler die says what the Shadow's click on it does; every other die keeps
                    // the pool's own tooltip (a Lords die set aside says so).
                    if (pool == board.OpponentDice && view.Dice.FirstOrDefault(d => d.Id == token.Id) is { } die &&
                        DismayChoice.Token(view, die.Id).Length > 0)
                        button.tooltip = DieDescriptions.Tooltip(die) + "\n" + DismayChoice.Offer(view, die.Id);
                    if (eligible && view.Decision.Kind == DecisionKind.KeepActionDie)
                        button.SetEnabled(true);
                    SpecialCandidate(button, token.Id, eligible);
                }
            // UI-345 (D6): the lit dice and Council medallions pulse in the shared rhythm.
            lit |= board.OwnDice.Query<Button>(className: ActionPoolView.CouncilTokenClass).ToList()
                .Any(button => button.ClassListContains("legal-target"));
            litArt.Run(lit);
            litOpponentArt.Run(litOpponent);
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
            // UI-276 (D20): a glowing Ruler die's click spends a Dismay token picked for it and re-rolls the die.
            var dismay = DismayChoice.Token(view, id);
            if (dismay.Length > 0)
            {
                edit(() => DismayChoice.Choose(draft, id, dismay), true);
                return true;
            }
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
            // UI-274: an awakening with one legal destination (most kings wake at home) is complete with the click.
            var single = next.Kind == CommandKind.AwakenSovereign && AwakeningTarget(next) is { Length: > 0 } target &&
                TravelDraft.RouteTo(next, view, board.Presentation, target);
            edit(() => draft.Adopt(next), single || ExpansionActions.Immediate(next.Kind));
            return true;
        }

        private string AwakeningTarget(TurnDraft selection)
        {
            var travel = DraftOptions.For(view, selection)?.Travel.FirstOrDefault();
            return travel?.Targets.Count == 1 ? travel.Targets[0] : "";
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
            if (view == null || busy())
                return;
            // UI-347 (D13): a medallion's click starts its printed use at once. When a Kings Chieftain can enter with the
            // token, the first click marks it and lights his Characters row instead; a click on the marked token starts its
            // printed use.
            switch (ResourcePresentation.Clicked(view, draft, id))
            {
                case CouncilClick.EntryCost:
                    EntryCost(id);
                    break;
                case CouncilClick.Mark:
                    Mark(id);
                    break;
                case CouncilClick.Use:
                    select(ResourcePresentation.CouncilUse(view, draft, id));
                    break;
                case CouncilClick.Release:
                    edit(() => draft.Clear(), false);
                    break;
            }
        }

        // UI-347 (D13): during a Chieftain's entry the token is his entry cost: its click adds it, and takes it back when
        // the entry does not require it.
        private void EntryCost(string id)
        {
            var entry = CharacterDraft.Selected(draft, DraftOptions.For(view, draft));
            var remove = draft.Payload.CouncilTokenId == id && !entry.RequiresCouncilToken;
            edit(() => draft.Change(p => p.CouncilTokenId = remove ? "" : id), false);
        }

        private void Mark(string id)
        {
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
                if (!ExpansionActions.RulerToken(view, next, source.Id, target.Id))
                    return false;
                edit(() => draft.Adopt(next), false);
                return true;
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
