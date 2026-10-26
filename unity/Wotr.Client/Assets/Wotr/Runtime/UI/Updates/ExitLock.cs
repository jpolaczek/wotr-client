using UnityEngine.UIElements;

namespace Wotr.Client
{
    // DIST-18 (owner 2026-10-25: "block clicking other buttons and every other option, and signal it by greying the
    // screen, at the moment the button that closes the app and starts the updated one is clicked"): from Restart and
    // update or Decline and quit until the application quits, one grey layer over every screen, Settings and the update
    // window takes every pick and names the step the exit is at (InterfaceController blocks the keys). It has no control
    // of its own; a failed exit lifts it. It fades in quickly, at once under Skip animations.
    public sealed class ExitLock
    {
        public const string LockedClass = "exit-locked";
        public VisualElement Root { get; } = new VisualElement { name = "exit-lock", focusable = true };
        public Label Step { get; } = new Label { name = "exit-lock-step", pickingMode = PickingMode.Ignore, enableRichText = false };
        public bool IsShown => !Appearance.Closed(Root);
        private Focusable previous;

        public ExitLock()
        {
            Root.AddToClassList("exit-lock");
            Root.style.display = DisplayStyle.None;
            Step.AddToClassList("exit-lock-step");
            Root.Add(Step);
        }

        public void Show(string step)
        {
            Step.text = step;
            if (IsShown)
                return;
            // Keys go to the focused element: the lock takes the focus, so none reaches the game.
            previous = Root.focusController?.focusedElement;
            Root.pickingMode = PickingMode.Position;
            Appearance.Display(Root, true);
            Root.BringToFront();
            Root.Focus();
        }

        // The screen takes input again at once; the grey layer fades out over it (UI-322).
        public void Hide()
        {
            Root.Blur();
            Root.pickingMode = PickingMode.Ignore;
            Appearance.Display(Root, false);
        }

        // After a failed exit, once the screen takes input again: the focus goes back where it was.
        public void ReturnFocus()
        {
            if (previous is VisualElement element && element.panel != null && element.enabledInHierarchy)
                element.Focus();
            previous = null;
        }
    }
}
