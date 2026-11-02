using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-440: a bare skill icon wears its focus ring only while the keyboard brought the focus. A click focuses the icon
    // too, and a ring left round the clicked icon would read as the frame the owner removed; a press marks the next focus
    // as the pointer's.
    public sealed class KeyboardFocusRing
    {
        public const string ClassName = "keyboard-focus";
        private readonly VisualElement control;
        private bool pointer;

        private KeyboardFocusRing(VisualElement control)
        {
            this.control = control;
            control.RegisterCallback<PointerDownEvent>(Pressed, TrickleDown.TrickleDown);
            control.RegisterCallback<PointerUpEvent>(Released, TrickleDown.TrickleDown);
            control.RegisterCallback<FocusInEvent>(Focused);
            control.RegisterCallback<FocusOutEvent>(Blurred);
        }

        public static void Attach(VisualElement control) => _ = new KeyboardFocusRing(control);

        private void Pressed(PointerDownEvent evt) => pointer = true;

        private void Released(PointerUpEvent evt) => pointer = false;

        private void Focused(FocusInEvent evt) => control.EnableInClassList(ClassName, !pointer);

        private void Blurred(FocusOutEvent evt) => control.RemoveFromClassList(ClassName);
    }
}
