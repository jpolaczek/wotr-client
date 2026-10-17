using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    public sealed class BoardMenu
    {
        // What the menu last asked the flow for; Retry repeats it.
        private enum Leave { Games, Quit }

        public VisualElement Root { get; } = new VisualElement { name = "board-menu" };
        public VisualElement Panel { get; } = new VisualElement { name = "board-menu-panel" };
        public Button Toggle { get; }
        public bool IsOpen => Root.style.display.value != DisplayStyle.None;
        public int Request { get; private set; }
        public event Action<bool> GamesRequested;
        // MENU-14: a confirmed Quit game; the flow saves the recovery data first, like Games, and then closes the game.
        public event Action<bool> QuitRequested;
        public ConfirmationDialog Quit { get; } = ConfirmationDialog.Quit("This game stays on the server. Resume it from My games.");
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

        public BoardMenu()
        {
            Root.AddToClassList("board-menu");
            Panel.AddToClassList("board-menu-panel");
            Root.style.display = DisplayStyle.None;
            Root.Add(Panel);
            Toggle = BoardScreen.Button("Menu", () => { if (IsOpen) Close(); else Open(); });
            Toggle.name = "open-board-menu";
            Toggle.AddToClassList("board-menu-toggle");
            // MENU-32: no Delete game here; Exit match (formerly Games) leaves the match on the server.
            Toggle.tooltip = "Resume · Victory · Settings · Exit match · Quit game";
            resume = Add("Resume", "board-menu-resume", Close);
            Add("Victory", "board-menu-victory", () => { Close(); VictoryRequested?.Invoke(); });
            Add("Settings", "open-interface", OpenSettings);
            games = Add("Exit match", "back-to-games", () => RequestGames(false));
            quit = Add("Quit game", "board-menu-quit", Quit.Open);
            status.AddToClassList("board-menu-status");
            Panel.Add(status);
            retry = Add("Retry", "board-menu-retry", Retry);
            Quit.Confirmed += () => RequestQuit(false);
            Root.Add(Quit.Root);
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
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            resume.Focus();
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            Request++;
            Quit.Hide();
            Root.style.display = DisplayStyle.None;
            SetSaving(false);
            if (returnFocus?.panel != null && returnFocus.enabledInHierarchy && returnFocus.resolvedStyle.display != DisplayStyle.None)
                returnFocus.Focus();
            else
                Toggle.Focus();
        }

        public void SetSaving(bool saving, string error = "")
        {
            status.text = saving ? Progress : error;
            status.style.display = status.text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            games.SetEnabled(!saving);
            quit.SetEnabled(!saving);
            retry.style.display = error.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            retry.SetEnabled(!saving);
        }

        private const string Progress = "Saving recovery data…";

        // The question in front answers Escape before the menu (BoardEscape).
        public ConfirmationDialog OpenQuestion => Quit.IsOpen ? Quit : null;

        private void OutsideDown(PointerDownEvent e)
        {
            var target = e.target as VisualElement;
            if (e.button != 0 || Panel.Contains(target) || Inside(Quit, target))
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
