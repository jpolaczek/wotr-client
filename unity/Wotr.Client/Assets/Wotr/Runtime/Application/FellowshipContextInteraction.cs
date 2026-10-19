using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class FellowshipContextInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly Action<Action, bool> edit;
        private readonly Func<bool> busy;
        private PlayerView view;
        private TurnDraft draft;
        // UI-155: this player asked to enter Mordor, so the entry brings the track into its view.
        private bool entering;

        public FellowshipContextInteraction(BoardScreen board, Action<Action, bool> edit, Func<bool> busy)
        {
            this.board = board;
            this.edit = edit;
            this.busy = busy;
            board.Fellowship.ActionSelected += Choose;
            board.Fellowship.PortraitSelected += Portrait;
            board.Fellowship.GuideChosen += Guide;
            board.Fellowship.VisibilityChanged += HighlightDice;
            board.Map.MordorTrack.StepClicked += Step;
            board.Map.MordorTrack.GateClicked += Gate;
            board.Map.FellowshipMarker.HideRequested += Hide;
            board.Map.FellowshipMarker.DeclareRequested += Declare;
            board.Map.MordorTrack.Entered += Entered;
        }

        public void Refresh(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            board.Fellowship.Context(view, draft, board.ActionDie, !busy());
            HighlightDice();
            OfferTrack();
        }

        // UI-155: the track takes the clicks of the server's options: step 0 and the gate enter, the next step
        // attempts a move with the chosen die, and the revealed figure hides.
        private void OfferTrack()
        {
            var fellowship = view.Fellowship;
            var step = fellowship.MordorStep;
            var enter = !busy() && Offered(CommandKind.EnterMordor);
            var move = !busy() && step < MordorTrackGeometry.LastStep && Offered(CommandKind.MoveFellowship);
            board.Map.MordorTrack.Offer(enter ? 0 : move ? step.Value + 1 : -1, enter ? fellowship.RegionId : "");
            board.Map.FellowshipMarker.OfferHide(!busy() && step.HasValue && !fellowship.Hidden && Offered(CommandKind.HideFellowship));
            board.Map.FellowshipMarker.OfferDeclare(!busy() && !step.HasValue && draft.Kind == null && Offered(CommandKind.DeclareFellowship));
        }

        private bool Offered(CommandKind kind) => FellowshipActions.Option(view, board.ActionDie, kind, draft) != null ||
            FellowshipCardActions.Choice(view, kind).Length > 0;

        // UI-269: a candidate's crown sets the starting Guide in the private versions' draft.
        private void Guide(string id)
        {
            var option = CompanionSetupPresentation.VersionOption(view);
            if (busy() || option == null || !option.Targets.Contains(id) || draft.Kind != CommandKind.ChooseCompanionVersions)
                return;
            edit(() => draft.Change(p => p.GuideId = id), false);
        }

        private void Step(int step) => Choose(step == 0 && view?.Fellowship.MordorStep == null ? CommandKind.EnterMordor : CommandKind.MoveFellowship);

        private void Gate(string region) => Choose(CommandKind.EnterMordor);

        private void Hide() => Choose(CommandKind.HideFellowship);

        private void Declare() => Choose(CommandKind.DeclareFellowship);

        private void HighlightDice()
        {
            var eligible = new HashSet<string>();
            // UI-98: a docked panel is often rolled up under its plaque, yet its dice keep their hint as before.
            if (view != null && (board.Fellowship.Docked || board.Fellowship.IsOpen) && board.ActionDie.DieId.Length == 0 &&
                view.Decision?.Kind == DecisionKind.Action && view.DecisionOwner == view.You)
            {
                var kind = view.Fellowship.Hidden ? CommandKind.MoveFellowship : CommandKind.HideFellowship;
                var option = DraftOptions.For(view, draft, kind);
                if (option?.Enabled == true)
                    eligible.UnionWith(option.EligibleDice);
            }
            foreach (var button in board.OwnDice.Query<Button>().ToList())
                button.EnableInClassList("fellowship-cost", eligible.Contains(button.name));
        }

        private void Choose(CommandKind kind)
        {
            if (busy())
                return;
            // UI-249: a card's Move or Hide (There Is Another Way) resolves the card's choice with this one click.
            var card = FellowshipCardActions.Choice(view, kind);
            if (card.Length > 0)
            {
                edit(() => FellowshipCardActions.Choose(view, draft, card), true);
                return;
            }
            if (FellowshipActions.Option(view, board.ActionDie, kind, draft) == null)
                return;
            board.Fellowship.Open();
            entering |= kind == CommandKind.EnterMordor;
            edit(() => FellowshipActions.Choose(view, draft, board.ActionDie, kind), kind != CommandKind.DeclareFellowship);
        }

        // The player who entered sees the track; anyone else keeps the view and sees the stones swell once.
        private void Entered()
        {
            if (entering)
                board.Map.FocusMordorTrack();
            else
                board.Map.MordorTrack.Pulse();
            entering = false;
        }

        private void Portrait(string id)
        {
            var kind = FellowshipActions.PortraitKind(view, board.ActionDie, id, draft);
            if (busy() || !kind.HasValue || HuntActions.UsesPanel(view, draft))
                return;
            board.Fellowship.Open();
            // The Guide and a reaction send with their click; a separation keeps building its group.
            edit(() => FellowshipActions.Portrait(view, draft, board.ActionDie, id), kind is CommandKind.SelectGuide or CommandKind.RespondEffect);
        }

        public void Dispose()
        {
            board.Fellowship.ActionSelected -= Choose;
            board.Fellowship.PortraitSelected -= Portrait;
            board.Fellowship.GuideChosen -= Guide;
            board.Fellowship.VisibilityChanged -= HighlightDice;
            board.Map.MordorTrack.StepClicked -= Step;
            board.Map.MordorTrack.GateClicked -= Gate;
            board.Map.FellowshipMarker.HideRequested -= Hide;
            board.Map.FellowshipMarker.DeclareRequested -= Declare;
            board.Map.MordorTrack.Entered -= Entered;
        }
    }
}
