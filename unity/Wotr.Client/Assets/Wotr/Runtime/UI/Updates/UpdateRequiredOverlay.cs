using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client.Updates;

namespace Wotr.Client
{
    // DIST-07: shown while the server refuses this version or the deadline of a required update has passed (DS22).
    // The game stays visible under the scrim but takes no new moves; the release downloads on its own. There are
    // always two ways out (U8): Restart and update once the package is ready, and Decline and quit, which saves the
    // same way and closes the game without installing. Esc and clicks beside the window do nothing.
    public sealed class UpdateRequiredOverlay
    {
        public VisualElement Root { get; } = new VisualElement { name = "update-required" };
        public bool IsOpen => Root.style.display.value == DisplayStyle.Flex;
        public event Action Install;
        public event Action GamePage;
        public event Action Retry;
        public event Action Decline;
        private readonly Label note = new Label { name = "update-required-note" };
        private readonly Label status = new Label { name = "update-required-status" };
        private readonly Button install;
        private readonly Button retry;
        private readonly Button decline;
        private bool canInstall = true;

        public UpdateRequiredOverlay()
        {
            Root.AddToClassList("settings-scrim");
            Root.AddToClassList("update-required");
            Root.style.display = DisplayStyle.None;
            var window = new VisualElement { name = "update-required-window" };
            window.AddToClassList("quit-window");
            window.AddToClassList("update-window");
            window.Add(BoardScreen.Title("Update required"));
            note.AddToClassList("quit-note");
            window.Add(note);
            status.AddToClassList("update-status");
            window.Add(status);
            install = BoardScreen.Button("Restart and update", () => (canInstall ? Install : GamePage)?.Invoke());
            install.name = "update-install";
            retry = BoardScreen.Button("Retry", () => Retry?.Invoke());
            retry.name = "update-retry";
            decline = BoardScreen.Button("Decline and quit", () => Decline?.Invoke());
            decline.name = "update-decline";
            decline.tooltip = "Saves your game and closes it without installing. The choice returns at the next start.";
            var actions = BoardScreen.Row("quit-actions");
            actions.Add(retry);
            actions.Add(install);
            actions.Add(decline);
            window.Add(actions);
            Root.Add(window);
            // The game underneath waits: no pointer or key reaches it while the overlay is open.
            Root.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            Root.RegisterCallback<KeyDownEvent>(KeyDown, TrickleDown.TrickleDown);
        }

        public void Render(UpdateSnapshot update, string ownVersion, bool exiting, string exitError)
        {
            if (!update.Blocked)
            {
                Root.style.display = DisplayStyle.None;
                return;
            }
            note.text = $"The server no longer accepts moves from version {ownVersion}. Your games stay saved on the server " +
                "and continue after the update.";
            status.text = exiting ? "Saving your game…" : exitError.Length > 0 ? exitError : UpdateTexts.Status(update);
            canInstall = update.CanInstall;
            install.text = canInstall ? "Restart and update" : "Game page";
            install.tooltip = canInstall ? "Saves your game, installs the update and starts the game again."
                : "This copy cannot update itself. Install the latest version from the game page.";
            install.SetEnabled(!exiting && (!canInstall || update.Status == UpdateStatus.Ready));
            var stuck = update.Status == UpdateStatus.Failed || (update.Status == UpdateStatus.Idle && update.Plan?.Offer == null);
            retry.style.display = stuck ? DisplayStyle.Flex : DisplayStyle.None;
            retry.SetEnabled(!exiting);
            decline.SetEnabled(!exiting);
            if (IsOpen)
                return;
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            Root.schedule.Execute(() => (install.enabledSelf ? install : decline).Focus());
        }

        private void KeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Escape)
                e.StopImmediatePropagation();
        }
    }
}
