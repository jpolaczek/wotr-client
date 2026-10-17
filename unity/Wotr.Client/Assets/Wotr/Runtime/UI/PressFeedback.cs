using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-114: a clickable control visibly presses in while the pointer, or Enter/Space, is held on it, and
    // comes back up when the gesture ends. The class only marks the state. The action still belongs to the
    // control's own click, which UI Toolkit raises when the button is released inside the control.
    public sealed class PressFeedback : IDisposable
    {
        public const string PressedClass = "pressed";
        // Elements that are not buttons opt in with this class.
        public const string PressableClass = "pressable";
        // A draggable control (card, die, figure) that moves this far has started a drag, not a click.
        public const float DragTolerance = 6f;

        private readonly VisualElement root;
        private VisualElement pressed;
        private int pointer = -1;
        private Vector2 origin;
        private bool keyboard;

        public VisualElement Pressed => pressed;
        // AUDIO-04: a press began on a control, and a press that ended as a gesture — released inside its still
        // enabled control, or Enter/Space let go. A captured button may act before Completed, never before Began.
        public event Action<VisualElement> Began;
        public event Action<VisualElement> Completed;
        // Why the last press ended; diagnostics for tests and profiles, never shown to the player.
        public string LastRelease { get; private set; } = "";
        public string LastDown { get; private set; } = "";

        public PressFeedback(VisualElement root)
        {
            this.root = root;
            root.RegisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerCancelEvent>(Cancel, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(KeyDown, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyUpEvent>(KeyUp, TrickleDown.TrickleDown);
            root.RegisterCallback<DetachFromPanelEvent>(Detached);
        }

        // The control under the pointer that will click: the nearest enabled button, or an element that opted in.
        public static VisualElement Pressable(VisualElement target, VisualElement root)
        {
            for (var element = target; element != null && element != root; element = element.parent)
            {
                if (element is TextField)
                    return null;
                if (element is Button || element.ClassListContains(PressableClass))
                    return element.enabledInHierarchy && !ReadOnlyControl.Is(element) ? element : null;
            }
            return null;
        }

        private void Down(PointerDownEvent evt)
        {
            Release("new press");
            var target = evt.target as VisualElement;
            var control = evt.button == 0 ? Pressable(target, root) : null;
            LastDown = (target?.name ?? "null") + "/" + (target?.GetType().Name ?? "?") + " button " + evt.button +
                " pointer " + evt.pointerId + " control " + (control?.name ?? "none");
            if (control != null)
                Press(control, evt.pointerId, evt.position, false);
        }

        private void Press(VisualElement control, int pointerId, Vector2 position, bool fromKeyboard)
        {
            pressed = control;
            pointer = pointerId;
            origin = position;
            keyboard = fromKeyboard;
            control.AddToClassList(PressedClass);
            Began?.Invoke(control);
            // A button captures the pointer while held, so its release is watched on the control itself too.
            control.RegisterCallback<PointerUpEvent>(ControlUp);
            control.RegisterCallback<PointerLeaveEvent>(Left);
            control.RegisterCallback<PointerCaptureOutEvent>(CaptureLost);
            control.RegisterCallback<DetachFromPanelEvent>(ControlDetached);
        }

        private void Move(PointerMoveEvent evt)
        {
            if (pressed == null || keyboard || evt.pointerId != pointer)
                return;
            if (!pressed.worldBound.Contains(evt.position))
                Release("moved outside to " + evt.position);
            else if (pressed.userData is DraftToken && Vector2.Distance(origin, evt.position) >= DragTolerance)
                Release("dragged");
        }

        private void Up(PointerUpEvent evt)
        {
            if (!keyboard)
                Release("pointer up", Inside(evt.position));
        }

        private void ControlUp(PointerUpEvent evt)
        {
            if (!keyboard)
                Release("control pointer up", Inside(evt.position));
        }

        private bool Inside(Vector2 position) => pressed != null && pressed.worldBound.Contains(position);

        private void Left(PointerLeaveEvent evt)
        {
            if (!keyboard)
                Release("pointer left");
        }

        private void CaptureLost(PointerCaptureOutEvent evt)
        {
            if (!keyboard)
                Release("capture lost");
        }

        private void Cancel(PointerCancelEvent evt) => Release("pointer cancelled");

        private void ControlDetached(DetachFromPanelEvent evt) => Release("control detached");

        private void KeyDown(KeyDownEvent evt)
        {
            if (pressed != null || !Activates(evt.keyCode))
                return;
            var control = Pressable(root.panel?.focusController?.focusedElement as VisualElement, root);
            if (control != null)
                Press(control, -1, default, true);
        }

        private void KeyUp(KeyUpEvent evt)
        {
            if (keyboard && Activates(evt.keyCode))
                Release("key up", true);
        }

        private static bool Activates(KeyCode key) => key is KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Space;

        public void Release() => Release("released");

        private void Release(string reason, bool completed = false)
        {
            var control = pressed;
            if (control == null)
                return;
            if (completed && control.enabledInHierarchy && control.panel != null)
                Completed?.Invoke(control);
            LastRelease = reason;
            pressed = null;
            pointer = -1;
            keyboard = false;
            control.RemoveFromClassList(PressedClass);
            control.UnregisterCallback<PointerUpEvent>(ControlUp);
            control.UnregisterCallback<PointerLeaveEvent>(Left);
            control.UnregisterCallback<PointerCaptureOutEvent>(CaptureLost);
            control.UnregisterCallback<DetachFromPanelEvent>(ControlDetached);
        }

        private void Detached(DetachFromPanelEvent evt) => Release("root detached");

        public void Dispose()
        {
            Release();
            root.UnregisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerCancelEvent>(Cancel, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyDownEvent>(KeyDown, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyUpEvent>(KeyUp, TrickleDown.TrickleDown);
            root.UnregisterCallback<DetachFromPanelEvent>(Detached);
        }
    }
}
