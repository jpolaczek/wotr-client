using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // MENU-14: asks before something that cannot be taken back (quitting the game, MENU-22 deleting a match).
    // Esc, Cancel and a click beside the window change nothing; only the confirm button does. Focus starts on
    // Cancel, so Enter alone never confirms. Element names start with the dialog's prefix ("quit-confirm").
    public sealed class ConfirmationDialog
    {
        public VisualElement Root { get; } = new VisualElement();
        public bool IsOpen => Root.style.display.value == DisplayStyle.Flex;
        public event Action Confirmed;
        private readonly Button cancel;
        private VisualElement returnFocus;
        private int outsidePointer = -1;

        public static ConfirmationDialog Quit(string note) => new ConfirmationDialog("quit", "Quit War of the Ring?", note, "Quit");

        private ConfirmationDialog(string prefix, string title, string note, string confirmText)
        {
            Root.name = prefix + "-confirmation";
            Root.AddToClassList("settings-scrim");
            Root.AddToClassList("confirmation");
            Root.style.display = DisplayStyle.None;
            var window = new VisualElement { name = prefix + "-window" };
            window.AddToClassList("confirm-window");
            window.Add(BoardScreen.Title(title));
            var text = new Label(note) { name = prefix + "-note" };
            text.AddToClassList("confirm-note");
            window.Add(text);
            var confirm = BoardScreen.Button(confirmText, Confirm);
            confirm.name = prefix + "-confirm";
            cancel = BoardScreen.Button("Cancel", Close);
            cancel.name = prefix + "-cancel";
            var actions = BoardScreen.Row("confirm-actions");
            actions.Add(confirm);
            actions.Add(cancel);
            window.Add(actions);
            Root.Add(window);
            Root.RegisterCallback<PointerDownEvent>(OutsideDown);
            Root.RegisterCallback<PointerUpEvent>(OutsideUp);
            Root.RegisterCallback<PointerCancelEvent>(_ => outsidePointer = -1);
            Root.RegisterCallback<KeyDownEvent>(KeyDown, TrickleDown.TrickleDown);
        }

        public void Open()
        {
            if (IsOpen)
                return;
            returnFocus = Root.panel?.focusController.focusedElement as VisualElement;
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            cancel.Focus();
        }

        // Cancel: the game goes on and the control that asked gets its focus back.
        public void Close()
        {
            if (!IsOpen)
                return;
            Hide();
            if (returnFocus?.panel != null && returnFocus.enabledInHierarchy && returnFocus.resolvedStyle.display != DisplayStyle.None)
                returnFocus.Focus();
        }

        // Closing the screen that holds the question hides it without moving focus.
        public void Hide()
        {
            Root.style.display = DisplayStyle.None;
            outsidePointer = -1;
        }

        private void Confirm()
        {
            Hide();
            Confirmed?.Invoke();
        }

        private void OutsideDown(PointerDownEvent e)
        {
            if (e.button != 0 || e.target != Root)
                return;
            outsidePointer = e.pointerId;
            Root.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        private void OutsideUp(PointerUpEvent e)
        {
            if (e.pointerId != outsidePointer)
                return;
            Root.ReleasePointer(e.pointerId);
            outsidePointer = -1;
            Close();
            e.StopPropagation();
        }

        private void KeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape || !IsOpen)
                return;
            Close();
            e.StopImmediatePropagation();
        }
    }
}
