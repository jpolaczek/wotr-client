using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // VisualElement.tooltip is metadata in a Player; Unity renders it only in Editor UI.
    public sealed class RuntimeTooltip : IDisposable
    {
        public const string RichHoverClass = "rich-hover";
        // UI-553 (owner, 2026-11-19: "reading a card in Effects, the tooltip covers the card's text"): a surface the player
        // reads (a card's text unrolled in Effects, the card inspector) carries this class, and a tooltip never covers it.
        public const string AvoidClass = "tooltip-avoid";
        // UI-409: a source whose tooltip is styled text (a roster row's parts); others stay plain.
        public const string FormattedClass = "formatted-tooltip";
        private readonly VisualElement root;
        private readonly Label label = new Label { name = "runtime-tooltip", pickingMode = PickingMode.Ignore };
        private readonly IVisualElementScheduledItem opening;
        private readonly IVisualElementScheduledItem submissionEnd;
        private VisualElement pendingSource;
        private Vector2 pointerPosition;
        private bool keyboardSource;
        private bool keyboardBlocked;
        private bool submitting;
        private float wheelBlockedUntil;
        private readonly IVisualElementScheduledItem tracking;
        private VisualElement source;
        private VisualElement pressedControl;
        private bool pressed;

        public RuntimeTooltip(VisualElement root)
        {
            this.root = root;
            label.AddToClassList("runtime-tooltip");
            label.displayTooltipWhenElided = false;
            label.enableRichText = false;
            label.style.display = DisplayStyle.None;
            root.Add(label);
            opening = root.schedule.Execute(OpenPending);
            submissionEnd = root.schedule.Execute(() => submitting = false);
            tracking = root.schedule.Execute(Refresh).Every(50);
            opening.Pause();
            submissionEnd.Pause();
            tracking.Pause();
            root.RegisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerCancelEvent>(Cancel, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerCaptureOutEvent>(CaptureLost, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerLeaveEvent>(Leave);
            root.RegisterCallback<WheelEvent>(Wheel, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(Key, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyUpEvent>(Activated, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationSubmitEvent>(Submitted, TrickleDown.TrickleDown);
            root.RegisterCallback<FocusInEvent>(Focused, TrickleDown.TrickleDown);
            root.RegisterCallback<FocusOutEvent>(Blurred, TrickleDown.TrickleDown);

            root.RegisterCallback<GeometryChangedEvent>(Resize);
            root.RegisterCallback<DetachFromPanelEvent>(Detach);
            label.RegisterCallback<GeometryChangedEvent>(_ => Position());
            Application.focusChanged += ApplicationFocus;
        }

        private void Move(PointerMoveEvent evt)
        {
            pointerPosition = evt.position;
            if (Blocked || evt.pressedButtons != 0)
            {
                Dismiss();
                return;
            }
            Queue(Find(root.panel?.Pick(pointerPosition)), false);
        }

        private bool Blocked => pressed || keyboardBlocked || submitting || Time.realtimeSinceStartup < wheelBlockedUntil;

        private VisualElement Find(VisualElement target)
        {
            VisualElement candidate = null;
            for (var element = target; element != null && element != root; element = element.parent)
            {
                if (element is TextField)
                    return null;
                // Terrain has no passive description. Markers may supply their own metadata.
                if (element is MapCanvas)
                    break;
                if (candidate == null && !string.IsNullOrEmpty(element.tooltip))
                    candidate = element;
            }
            return candidate;
        }

        private void Queue(VisualElement target, bool fromKeyboard)
        {
            if (target != null && (target == source || target == pendingSource) && keyboardSource == fromKeyboard)
                return;
            Dismiss();
            if (target == null || Blocked)
                return;
            keyboardSource = fromKeyboard;
            pendingSource = target;
            // A zero-delay focus opening waits only for the focus event to finish.
            opening.ExecuteLater(fromKeyboard ? 0 : 200);
        }

        private void OpenPending()
        {
            var target = pendingSource;
            var valid = !Blocked && Available(target) && IsCurrent(target);
            if (pendingSource != target)
                return;
            if (!valid)
            {
                Dismiss();
                return;
            }
            pendingSource = null;
            source = target;
            source.RegisterCallback<DetachFromPanelEvent>(SourceRemoved);
            Show();
        }

        private bool Available(VisualElement target)
        {
            if (target?.panel == null || !root.Contains(target) || string.IsNullOrEmpty(target.tooltip))
                return false;
            for (var element = target; element != null; element = element.parent)
                if (element.resolvedStyle.display == DisplayStyle.None || element.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            // Disabled actions still explain their state through hover.
            return target.worldBound.width > 0 && target.worldBound.height > 0;
        }

        private bool IsCurrent(VisualElement target)
        {
            var panel = target?.panel;
            if (panel == null)
                return false;
            if (keyboardSource && Find(panel.focusController.focusedElement as VisualElement) != target)
                return false;
            var picked = panel.Pick(keyboardSource ? target.worldBound.center : pointerPosition);
            // Pick may flush layout and detach a source or dispatch a focus change.
            return target.panel == panel && picked != null && (keyboardSource
                ? picked == target || target.Contains(picked)
                : Find(picked) == target);
        }

        private void Show()
        {
            if (!Available(source))
            {
                Dismiss();
                return;
            }
            label.enableRichText = source.ClassListContains(FormattedClass);
            label.text = source.tooltip;
            label.style.maxWidth = Mathf.Max(32, Mathf.Min(360, root.contentRect.width - 16));
            label.style.display = DisplayStyle.Flex;
            label.style.visibility = Visibility.Hidden;
            label.BringToFront();
            tracking.Resume();
            Position();
        }

        private void Refresh()
        {
            var current = source;
            var valid = !Blocked && Available(current) && IsCurrent(current);
            if (source != current)
                return;
            if (!valid)
            {
                Dismiss();
                return;
            }
            if (label.text != source.tooltip)
                label.text = source.tooltip;
            Position();
        }

        private void Position()
        {
            if (source == null || label.style.display == DisplayStyle.None || label.layout.width <= 0 || label.layout.height <= 0)
                return;
            var bounds = root.WorldToLocal(source.worldBound);
            var size = label.layout.size;
            var (x, y) = Place(bounds, size);
            if (Mathf.Abs(label.layout.x - x) > .5f)
                label.style.left = x;
            if (Mathf.Abs(label.layout.y - y) > .5f)
                label.style.top = y;
            label.style.visibility = Visibility.Visible;
        }

        // Below the source, else above it; UI-553: the first of below, above, right and left that covers no reading surface.
        private (float X, float Y) Place(Rect bounds, Vector2 size)
        {
            var avoided = Avoided();
            var candidates = new[]
            {
                new Vector2(bounds.xMin, bounds.yMax + 6),
                new Vector2(bounds.xMin, bounds.yMin - size.y - 6),
                new Vector2(bounds.xMax + 6, bounds.yMin),
                new Vector2(bounds.xMin - size.x - 6, bounds.yMin)
            }.Select(Clamp).ToList();
            var first = bounds.yMax + 6 + size.y > root.contentRect.height - 8 ? candidates[1] : candidates[0];
            if (avoided.Count == 0)
                return (first.x, first.y);
            var free = candidates.FirstOrDefault(c => !avoided.Any(r => r.Overlaps(new Rect(c, size))) &&
                !new Rect(c, size).Overlaps(bounds));
            var chosen = free == default ? first : free;
            return (chosen.x, chosen.y);

            Vector2 Clamp(Vector2 point) => new Vector2(
                Mathf.Clamp(point.x, 8, Mathf.Max(8, root.contentRect.width - size.x - 8)),
                Mathf.Clamp(point.y, 8, Mathf.Max(8, root.contentRect.height - size.y - 8)));
        }

        // The reading surfaces shown now, in the tooltip's coordinates.
        private List<Rect> Avoided() => root.Query(className: AvoidClass).ToList()
            .Where(e => e.resolvedStyle.display != DisplayStyle.None && e.resolvedStyle.visibility == Visibility.Visible &&
                e.worldBound.width > 0 && e.worldBound.height > 0)
            .Select(e => root.WorldToLocal(e.worldBound)).ToList();

        public void Dismiss()
        {
            opening.Pause();
            tracking.Pause();
            if (source != null)
                source.UnregisterCallback<DetachFromPanelEvent>(SourceRemoved);
            source = null;
            pendingSource = null;
            label.style.display = DisplayStyle.None;
            label.text = "";
        }

        private void Down(PointerDownEvent evt)
        {
            ReleasePress();
            pressed = true;
            var target = evt.target as VisualElement;
            pressedControl = target as Button ?? target?.GetFirstAncestorOfType<Button>() ?? target;
            if (pressedControl != null && pressedControl != root)
            {
                pressedControl.RegisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
                pressedControl.RegisterCallback<PointerCancelEvent>(Cancel, TrickleDown.TrickleDown);
                pressedControl.RegisterCallback<DetachFromPanelEvent>(PressRemoved);
            }
            Dismiss();
        }
        private void Up(PointerUpEvent evt)
        {
            if (!pressed)
                return;
            ReleasePress();
            pressed = evt.pressedButtons != 0;
            Dismiss();
        }
        private void Cancel(PointerCancelEvent evt)
        {
            ReleasePress();
            Dismiss();
        }
        private void CaptureLost(PointerCaptureOutEvent evt)
        {
            if (pressed)
                Dismiss();
        }
        private void PressRemoved(DetachFromPanelEvent evt)
        {
            ReleasePress();
            Dismiss();
        }
        private void ReleasePress()
        {
            pressed = false;
            if (pressedControl == null)
                return;
            if (pressedControl != root)
            {
                pressedControl.UnregisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
                pressedControl.UnregisterCallback<PointerCancelEvent>(Cancel, TrickleDown.TrickleDown);
                pressedControl.UnregisterCallback<DetachFromPanelEvent>(PressRemoved);
            }
            pressedControl = null;
        }
        private void Wheel(WheelEvent evt)
        {
            wheelBlockedUntil = Time.realtimeSinceStartup + .2f;
            Dismiss();
        }
        private void SourceRemoved(DetachFromPanelEvent evt) => Dismiss();
        private void Focused(FocusInEvent evt)
        {
            Queue(Find(evt.target as VisualElement), true);
        }
        private void Blurred(FocusOutEvent evt)
        {
            if (keyboardSource)
                Dismiss();
        }
        private void Submitted(NavigationSubmitEvent evt)
        {
            submitting = true;
            Dismiss();
            submissionEnd.ExecuteLater(0);
        }
        private void Activated(KeyUpEvent evt)
        {
            if (evt.keyCode is KeyCode.Escape or KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Space)
                keyboardBlocked = false;
        }
        private void Key(KeyDownEvent evt)
        {
            if (evt.keyCode is not (KeyCode.Escape or KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Space))
                return;
            keyboardBlocked = true;
            Dismiss();
            // A passive description never owns Escape; the current window or gesture does.
        }
        private void Leave(PointerLeaveEvent evt)
        {
            if (evt.target == root)
            {
                ReleasePress();
                Dismiss();
            }
        }
        private void Resize(GeometryChangedEvent evt)
        {
            if (evt.target == root && PopoverPlacement.SizeChanged(evt.oldRect, evt.newRect))
                Dismiss();
        }
        private void Detach(DetachFromPanelEvent evt)
        {
            if (evt.target == root)
            {
                keyboardBlocked = false;
                submitting = false;
                submissionEnd.Pause();
                ReleasePress();
                Dismiss();
            }
        }
        private void ApplicationFocus(bool focused)
        {
            if (!focused)
            {
                keyboardBlocked = false;
                submitting = false;
                submissionEnd.Pause();
                ReleasePress();
                Dismiss();
            }
        }

        public void Dispose()
        {
            ReleasePress();
            Dismiss();
            submissionEnd.Pause();
            Application.focusChanged -= ApplicationFocus;
            root.UnregisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerCancelEvent>(Cancel, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerCaptureOutEvent>(CaptureLost, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerLeaveEvent>(Leave);
            root.UnregisterCallback<WheelEvent>(Wheel, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyDownEvent>(Key, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyUpEvent>(Activated, TrickleDown.TrickleDown);
            root.UnregisterCallback<NavigationSubmitEvent>(Submitted, TrickleDown.TrickleDown);
            root.UnregisterCallback<FocusInEvent>(Focused, TrickleDown.TrickleDown);
            root.UnregisterCallback<FocusOutEvent>(Blurred, TrickleDown.TrickleDown);

            root.UnregisterCallback<GeometryChangedEvent>(Resize);
            root.UnregisterCallback<DetachFromPanelEvent>(Detach);
            label.RemoveFromHierarchy();
        }
    }
}
