using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-160: feeds the turn console from the projection and the draft, and routes its permanent actions through the
    // tray's own paths: Back, Confirm (Check result), finishing a multi-step action, the Pass and discard
    // components and the Elven Ring choice. The decision window opens only while the docked tray offers choices.
    public sealed class TurnConsoleInteraction : IDisposable
    {
        private readonly BoardScreen board;
        private readonly DecisionTray tray;
        private readonly Action back;
        private readonly Action confirm;
        private readonly Action finish;
        private readonly Action<ComponentAction> component;
        private readonly Action<string> ring;
        private readonly Func<bool> reinforcing;
        private PlayerView view;
        private TurnDraft draft;
        private bool idle;
        private bool uncertain;
        private string fallback = "";
        private string trayHint = "";
        private string trayDetail = "";

        // reinforcing: a card or ability recruitment plan is under way; the Reinforcements panel holds its choices
        // and its own ✓, so the decision window stays closed and the status opens that panel.
        public TurnConsoleInteraction(BoardScreen board, DecisionTray tray, Action back, Action confirm, Action finish,
            Action<ComponentAction> component, Action<string> ring, Func<bool> reinforcing)
        {
            this.board = board;
            this.tray = tray;
            this.back = back;
            this.confirm = confirm;
            this.finish = finish;
            this.component = component;
            this.ring = ring;
            this.reinforcing = reinforcing;
            var console = board.Console;
            console.Status.Clicked += OpenDecisionHost;
            console.Actions.BackClicked += Back;
            console.Actions.ConfirmClicked += Confirm;
            console.Actions.PassClicked += Pass;
            console.Actions.DiscardClicked += Discard;
            console.Rings.Chosen += ring;
            tray.HintChanged += Hint;
            tray.LegalChanged += LegalChanged;
            board.Hunt.AllocationConfirmed += ConfirmAllocation;
        }

        public void Refresh(PlayerView latest, TurnDraft current, bool online, bool busy, bool pending)
        {
            view = latest;
            draft = current;
            idle = online && !busy && !pending;
            uncertain = pending && !busy;
            var docked = board.Tray.parent == board.DecisionWindow.Dock;
            var choices = docked && (uncertain || !reinforcing()) && (tray.HasChoices || Shown(board.Components.Root) || Shown(board.CardCosts.Root)) &&
                !SendingWithWindowClosed(busy);
            board.DecisionWindow.Apply(choices, Heading(), view.MatchId + "/" + view.Decision?.Id + "/" + draft.Kind + (uncertain ? "/check" : ""));
            var state = TurnConsoleState.For(view, draft, board.ActionDie, RegionName);
            fallback = state.Guidance;
            board.Console.Status.Apply(state, Opens(choices));
            board.Console.Status.SetYourTurn(idle && TurnSignal.Waiting(view, draft));
            board.Console.Status.SetMustAct(idle && TurnSignal.MustAct(view));
            // UI-171: the tray reports its hint before this refresh; a hint-less step shows the new guidance, not the last.
            ShowHint();
            var rings = ExpansionActions.RingKind(view, draft);
            board.Console.Rings.Apply(view, draft.Payload.RingId, idle, DraftOptions.For(view, draft, rings), rings);
            board.Console.Hunt.SetAllocating(view.Decision?.Kind == DecisionKind.AllocateHunt && view.DecisionOwner == view.You);
            RefreshActions(docked && !board.DecisionWindow.IsOpen);
        }

        // UI-227: a step being sent (a discarded die, a pass) has its choices made, so it never opens the window for the
        // moment until the answer; a window already open keeps its place.
        private bool SendingWithWindowClosed(bool busy) => busy && !uncertain && !board.DecisionWindow.IsOpen;

        private void RefreshActions(bool onBoard)
        {
            var actions = board.Console.Actions;
            var components = ComponentActions.For(view, draft).ToList();
            var pass = components.FirstOrDefault(c => c.Kind == CommandKind.Pass);
            var discard = components.FirstOrDefault(c => c.Kind == CommandKind.DiscardDie);
            ConsoleActions.Set(actions.Pass, idle && pass?.Enabled == true, Tooltip("Pass", CommandKind.Pass, pass));
            ConsoleActions.Set(actions.Discard, idle && discard?.Enabled == true,
                Tooltip("Discard the selected die without effect", CommandKind.DiscardDie, discard));
            ConsoleActions.Set(actions.Back, idle && onBoard && (draft.Kind.HasValue || draft.Payload.DieId.Length > 0),
                "Back · Undo the last unsent step");
            var finishing = Finishing();
            var card = CardChoice(onBoard);
            ConsoleActions.Set(actions.Confirm, uncertain || idle && (finishing || card && tray.Legal),
                uncertain ? "Check result · Recover the interrupted response"
                : finishing ? "Finish · End this action; saved steps stay"
                : card ? ConfirmChoiceTooltip()
                : "Finish · Available during an action of several steps");
            RefreshAllocation();
        }

        // UI-172: the Hunt plaque submits the Shadow's own allocation when its limits and the server preview allow.
        private void RefreshAllocation()
        {
            var allocation = HuntActions.Allocation(view);
            if (allocation == null)
                return;
            var ready = AllocationReady();
            board.Hunt.SetAllocationReady(ready, HuntAllocationText.Plaque(draft, allocation, ready));
        }

        private bool AllocationReady() => idle && Allocating() && tray.Legal;

        private void ConfirmAllocation()
        {
            if (AllocationReady())
                confirm();
        }

        // The preview of an allocation answers after the console was drawn; only the actions follow it.
        private void LegalChanged(bool _)
        {
            if (view != null)
                RefreshActions(OnBoard());
        }

        // An unavailable action keeps its place and says why (the server's reason when it gave one).
        private string Tooltip(string label, CommandKind kind, ComponentAction item)
        {
            if (item?.Enabled == true)
                return label;
            var reason = view.Options.FirstOrDefault(o => o.Kind == kind)?.Reason ?? "";
            return label + " · " + (reason.Length > 0 ? reason : kind == CommandKind.DiscardDie ? "Select one of your dice first" : "Not available now");
        }

        private void Hint(string text, string detail)
        {
            trayHint = text;
            trayDetail = detail;
            ShowHint();
        }

        // A step without its own hint shows the console's guidance, so the line never stands empty. UI-211: a card's
        // decision leads with what has been chosen so far.
        private void ShowHint()
        {
            var own = trayHint.Length > 0;
            var text = own ? trayHint : fallback;
            var progress = view != null && draft != null && CardDecisions.Owns(view, draft) ? CardDecisions.Progress(view, draft) : "";
            board.Console.Status.SetHint(progress.Length > 0 ? progress + " · " + text : text, own ? trayDetail : fallback);
        }

        private bool Finishing() => view.Decision?.Kind == DecisionKind.ActionSequence && view.ActionSequence?.Owner == view.You;

        // UI-211: a card's decision whose choices are on the board or in the console is confirmed with the console's ✓.
        private bool CardChoice(bool onBoard) => onBoard && CardDecisions.Owns(view, draft);

        private string ConfirmChoiceTooltip()
        {
            if (tray.Legal)
                return "Confirm choice" + (tray.ConfirmTitle.Length > 0 ? " · " + tray.ConfirmTitle : "");
            return "Confirm choice · " + (tray.Problem.Length > 0 ? tray.Problem : "Complete the choice first");
        }

        private void Back()
        {
            if (board.Console.Actions.Back.enabledSelf)
                back();
        }

        private void Confirm()
        {
            if (uncertain)
                confirm();
            else if (idle && Finishing())
                finish();
            else if (idle && tray.Legal && CardChoice(OnBoard()))
                confirm();
        }

        private bool OnBoard() => board.Tray.parent == board.DecisionWindow.Dock && !board.DecisionWindow.IsOpen;

        private bool Allocating() => HuntActions.Allocation(view) is { } option && draft.Kind == CommandKind.AllocateHunt &&
            draft.Ready(option);

        private void Pass() => Choose(CommandKind.Pass);

        private void Discard() => Choose(CommandKind.DiscardDie);

        private void Choose(CommandKind kind)
        {
            if (view == null || !idle)
                return;
            var item = ComponentActions.For(view, draft).FirstOrDefault(c => c.Kind == kind && c.Enabled);
            if (item != null)
                component(item);
        }

        private enum Host { None, Reinforcements, DecisionWindow, Battle, Hunt, Notice, GameEnd, Fellowship, Effects }

        // The status opens wherever the current decision lives: Reinforcements, the decision window, the battle or
        // Hunt (own or the opponent's, without moving the camera), or the host the decision notice opens.
        private Host Target(bool choices)
        {
            var own = view.Decision != null && view.DecisionOwner == view.You;
            // UI-169: GAME OVER opens the end-of-game screen again.
            if (view.Winner.HasValue)
                return Host.GameEnd;
            if (tray.DirectMuster || reinforcing())
                return Host.Reinforcements;
            if (choices)
                return Host.DecisionWindow;
            if (view.Battle != null || view.EventLoss != null)
                return Host.Battle;
            if (board.Tray.parent == board.Hunt.DecisionHost || view.Hunt != null && !own)
                return Host.Hunt;
            // UI-244: an own decision whose objects live in a panel reopens that panel.
            switch (DecisionRoutes.Of(view))
            {
                case DecisionHost.Fellowship:
                    return Host.Fellowship;
                case DecisionHost.Effects:
                    return Host.Effects;
            }
            return own ? Host.Notice : Host.None;
        }

        private void OpenDecisionHost()
        {
            if (view == null)
                return;
            switch (Target(board.DecisionWindow.HasChoices))
            {
                case Host.Reinforcements:
                    board.Reinforcements.Show();
                    break;
                case Host.DecisionWindow:
                    board.DecisionWindow.Open();
                    break;
                case Host.Battle:
                    board.Battles.Open();
                    break;
                case Host.Hunt:
                    board.Hunt.Open();
                    break;
                case Host.Notice:
                    board.Notice.Open();
                    break;
                case Host.GameEnd:
                    board.GameEnd.Open();
                    break;
                case Host.Fellowship:
                    board.Fellowship.Open();
                    break;
                case Host.Effects:
                    board.TableEffects.Open();
                    break;
            }
        }

        private string Opens(bool choices) => Target(choices) switch
        {
            Host.Reinforcements => "Click to open Reinforcements",
            Host.DecisionWindow => "Click to open the decision window",
            Host.Battle => "Click to open the battle window",
            Host.Hunt => "Click to open the Hunt panel",
            Host.Notice => "Click to show your decision",
            Host.GameEnd => "Click to show the end of the game",
            Host.Fellowship => "Click to open the Fellowship panel",
            Host.Effects => "Click to open the table effects",
            _ => ""
        };

        private string Heading() => uncertain ? "Check result"
            : view.Decision == null ? "Decision" : DecisionTray.Title(view.Decision.Kind, draft.Kind);

        private string RegionName(string id) => board.Presentation.Regions.TryGetValue(id, out var region) ? region.Name : id;

        private static bool Shown(VisualElement element) => element.style.display.value != DisplayStyle.None && element.childCount > 0;

        public void Dispose()
        {
            var console = board.Console;
            console.Status.Clicked -= OpenDecisionHost;
            console.Actions.BackClicked -= Back;
            console.Actions.ConfirmClicked -= Confirm;
            console.Actions.PassClicked -= Pass;
            console.Actions.DiscardClicked -= Discard;
            console.Rings.Chosen -= ring;
            tray.HintChanged -= Hint;
            board.Hunt.AllocationConfirmed -= ConfirmAllocation;
            tray.LegalChanged -= LegalChanged;
        }
    }
}
