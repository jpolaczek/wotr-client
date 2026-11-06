using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    public sealed class BoardMenu
    {
        // What the menu last asked the flow for; Retry repeats it.
        private enum Leave { Games, Quit, Concede, QuitAndConcede }

        public VisualElement Root { get; } = new VisualElement { name = "board-menu" };
        public VisualElement Panel { get; } = new VisualElement { name = "board-menu-panel" };
        public Button Toggle { get; }
        public bool IsOpen => Root.style.display.value != DisplayStyle.None;
        public int Request { get; private set; }
        public event Action<bool> GamesRequested;
        // MENU-14: a confirmed Quit game; the flow saves the recovery data first, like Games, and then closes the game.
        public event Action<bool> QuitRequested;
        public ConfirmationDialog Quit { get; } = ConfirmationDialog.Quit("This game stays on the server. Resume it from My games.");
        // UI-501: in a ranked game Exit match is Concede and Quit game concedes; true when the game also quits.
        public ConfirmationDialog ConcedeQuestion { get; } = ConfirmationDialog.Concede();
        public ConfirmationDialog QuitAndConcedeQuestion { get; } = ConfirmationDialog.QuitAndConcede();
        public event Action<bool> ConcedeRequested;
        public const string ConcedeFailed = "The concession could not reach the server. You are still in the game.";
        public event Action SettingsRequested;
        public event Action VictoryRequested;
        private readonly Button resume;
        private readonly Button games;
        private readonly Button quit;
        private readonly Button retry;
        private readonly Label status = new Label { name = "board-menu-status" };
        private VisualElement returnFocus;
        private int outsidePointer = -1;
        private Leave leaving;
        private bool ranked;

        public BoardMenu()
        {
            Root.AddToClassList("board-menu");
            // UI-470 (owner, 2026-11-03): the Menu panel is a menu window too; its toggle stays in the top bar's look.
            FrontendTheme.ApplyChrome(Root);
            Panel.AddToClassList("board-menu-panel");
            Root.style.display = DisplayStyle.None;
            Root.Add(Panel);
            Toggle = BoardScreen.Button("Menu", () => { if (IsOpen) Close(); else Open(); });
            Toggle.name = "open-board-menu";
            Toggle.AddToClassList("board-menu-toggle");
            // MENU-32: no Delete game here; Exit match (formerly Games) leaves the match on the server.
            Toggle.tooltip = NormalTooltip;
            resume = Add("Resume", "board-menu-resume", Close);
            Add("Victory", "board-menu-victory", () => { Close(); VictoryRequested?.Invoke(); });
            Add("Settings", "open-interface", OpenSettings);
            games = Add(ExitText, "back-to-games", ExitPressed);
            quit = Add("Quit game", "board-menu-quit", QuitPressed);
            status.AddToClassList("board-menu-status");
            Panel.Add(status);
            retry = Add("Retry", "board-menu-retry", Retry);
            Quit.Confirmed += () => RequestQuit(false);
            Root.Add(Quit.Root);
            ConcedeQuestion.Confirmed += () => RequestConcede(false);
            Root.Add(ConcedeQuestion.Root);
            QuitAndConcedeQuestion.Confirmed += () => RequestConcede(true);
            Root.Add(QuitAndConcedeQuestion.Root);
            SetSaving(false);
            Root.RegisterCallback<PointerDownEvent>(OutsideDown, TrickleDown.TrickleDown);
            Root.RegisterCallback<PointerUpEvent>(OutsideUp, TrickleDown.TrickleDown);
            Root.RegisterCallback<PointerCancelEvent>(_ => outsidePointer = -1);
        }

        private Button Add(string text, string name, Action action)
        {
            var button = BoardScreen.Button(text, action);
            button.name = name;
            Panel.Add(button);
            return button;
        }

        // UI-169: the end-of-game screen's Main menu leaves the match by the same path as Games.
        public void LeaveToGames() => RequestGames(false);

        // UI-501: a ranked game names what leaving costs; a finished one is an ordinary game again.
        public void SetRanked(bool value)
        {
            ranked = value;
            games.text = value ? "Concede" : ExitText;
            Toggle.tooltip = value ? "Resume · Victory · Settings · Concede · Quit game" : NormalTooltip;
        }

        // The window's close asks the same question as Quit game.
        public void AskToQuitAndConcede()
        {
            Open();
            QuitAndConcedeQuestion.Open();
        }

        private void ExitPressed()
        {
            if (ranked)
                ConcedeQuestion.Open();
            else
                RequestGames(false);
        }

        private void QuitPressed()
        {
            if (ranked)
                QuitAndConcedeQuestion.Open();
            else
                Quit.Open();
        }

        private void RequestConcede(bool quitting)
        {
            leaving = quitting ? Leave.QuitAndConcede : Leave.Concede;
            Request++;
            ConcedeRequested?.Invoke(quitting);
        }

        private void RequestGames(bool retry)
        {
            leaving = Leave.Games;
            Request++;
            GamesRequested?.Invoke(retry);
        }

        private void RequestQuit(bool retry)
        {
            leaving = Leave.Quit;
            Request++;
            QuitRequested?.Invoke(retry);
        }

        // Retry repeats the request that failed: saving the recovery data.
        private void Retry()
        {
            if (leaving == Leave.Quit)
                RequestQuit(true);
            else if (leaving is Leave.Concede or Leave.QuitAndConcede)
                RequestConcede(leaving == Leave.QuitAndConcede);
            else
                RequestGames(true);
        }

        private void OpenSettings()
        {
            Close();
            var request = Request;
            // Finish the focus transfer out of this hidden menu before Settings
            // captures its return target from the focus controller.
            Root.schedule.Execute(() =>
            {
                if (Root.panel != null && Request == request && !IsOpen)
                    SettingsRequested?.Invoke();
            });
        }

        public void Open()
        {
            if (IsOpen)
                return;
            returnFocus = Root.panel?.focusController.focusedElement as VisualElement;
            Appearance.Display(Root, true);
            Root.BringToFront();
            resume.Focus();
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            Request++;
            Quit.Hide();
            ConcedeQuestion.Hide();
            QuitAndConcedeQuestion.Hide();
            Root.style.display = DisplayStyle.None;
            SetSaving(false);
            if (returnFocus?.panel != null && returnFocus.enabledInHierarchy && returnFocus.resolvedStyle.display != DisplayStyle.None)
                returnFocus.Focus();
            else
                Toggle.Focus();
        }

        public void SetSaving(bool saving, string error = "", string progress = Progress)
        {
            status.text = saving ? progress : error;
            status.style.display = status.text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            games.SetEnabled(!saving);
            quit.SetEnabled(!saving);
            retry.style.display = error.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            retry.SetEnabled(!saving);
        }

        private const string Progress = "Saving recovery data…";
        public const string Conceding = "Conceding…";
        private const string ExitText = "Exit match";
        private const string NormalTooltip = "Resume · Victory · Settings · Exit match · Quit game";

        // The question in front answers Escape before the menu (BoardEscape).
        public ConfirmationDialog OpenQuestion => Quit.IsOpen ? Quit
            : ConcedeQuestion.IsOpen ? ConcedeQuestion
            : QuitAndConcedeQuestion.IsOpen ? QuitAndConcedeQuestion : null;

        private void OutsideDown(PointerDownEvent e)
        {
            var target = e.target as VisualElement;
            if (e.button != 0 || Panel.Contains(target) || Inside(Quit, target) || Inside(ConcedeQuestion, target) ||
                Inside(QuitAndConcedeQuestion, target))
                return;
            outsidePointer = e.pointerId;
            Root.CapturePointer(e.pointerId);
            e.StopImmediatePropagation();
        }

        private static bool Inside(ConfirmationDialog question, VisualElement target) =>
            target == question.Root || question.Root.Contains(target);

        private void OutsideUp(PointerUpEvent e)
        {
            if (e.pointerId != outsidePointer)
                return;
            Root.ReleasePointer(e.pointerId);
            outsidePointer = -1;
            Close();
            e.StopImmediatePropagation();
        }

        public static bool ContainsInput(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.name == "board-menu" || current.name == "open-board-menu")
                    return true;
            return false;
        }
    }
}
