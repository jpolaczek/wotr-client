using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    public sealed class FloatingWindow
    {
        public const string HandleClass = "window-handle";
        private readonly VisualElement root;
        private readonly VisualElement viewport;
        private readonly WindowLayout layout;
        private readonly Func<bool> persistent;
        private readonly Action pin;
        private readonly Action anchor;
        private Vector2? defaultAnchor;
        private readonly bool rememberInitial;
        private readonly StyleLength initialLeft, initialTop, initialRight, initialBottom;
        private Vector2 origin, position;
        private int pointer = -1;
        private bool applied;
        private bool moved;

        public FloatingWindow(VisualElement root, WindowLayout layout, Func<bool> persistent = null, Action pin = null,
            Action anchor = null, Vector2? defaultAnchor = null, bool rememberInitial = true)
        {
            this.root = root;
            this.rememberInitial = rememberInitial;
            this.layout = layout;
            this.persistent = persistent ?? (() => true);
            this.pin = pin;
            this.anchor = anchor;
            this.defaultAnchor = defaultAnchor;
            viewport = root.parent;
            if (defaultAnchor.HasValue)
            {
                root.style.right = defaultAnchor.Value.x;
                root.style.top = defaultAnchor.Value.y;
            }
            initialLeft = root.style.left;
            initialTop = root.style.top;
            initialRight = root.style.right;
            initialBottom = root.style.bottom;
            root.focusable = true;
            root.AddToClassList("floating-window");
            root.RegisterCallback<PointerDownEvent>(Down);
            root.RegisterCallback<PointerMoveEvent>(Move);
            root.RegisterCallback<PointerUpEvent>(Up);
            root.RegisterCallback<PointerCancelEvent>(_ => End());
            root.RegisterCallback<PointerCaptureOutEvent>(_ => End());
            root.RegisterCallback<KeyDownEvent>(Key);
            root.RegisterCallback<FocusOutEvent>(e =>
            {
                if (e.relatedTarget is not VisualElement next || next != root && !root.Contains(next))
                    End();
            });
            root.RegisterCallback<GeometryChangedEvent>(_ => Refresh());
            viewport.RegisterCallback<GeometryChangedEvent>(_ => Refresh());
            root.RegisterCallback<AttachToPanelEvent>(e =>
            {
                if (e.target != root) return;
                layout.Reset += Reset;
                layout.MiddleMoved += Refresh;
                Application.focusChanged += Focus;
                Refresh();
            });
            root.RegisterCallback<DetachFromPanelEvent>(e =>
            {
                if (e.target != root) return;
                layout.Reset -= Reset;
                layout.MiddleMoved -= Refresh;
                Application.focusChanged -= Focus;
                End();
            });
        }

        public static VisualElement Header(string title)
        {
            var header = BoardScreen.Row("window-header");
            MarkHandle(header);
            header.Add(BoardScreen.Title(title));
            return header;
        }

        public static void MarkHandle(VisualElement header)
        {
            header.AddToClassList(HandleClass);
            header.focusable = true;
            header.tooltip = "Drag to move this window · Arrow keys when focused";
        }

        public static bool IsHandle(VisualElement target)
        {
            for (var element = target; element != null; element = element.parent)
            {
                if (element is Button or Toggle or TextField || element.userData is DraftToken)
                    return false;
                if (element.ClassListContains(HandleClass))
                    return true;
            }
            return false;
        }

        public static bool IsDragging(VisualElement target)
        {
            for (var element = target; element != null; element = element.parent)
                if (element.ClassListContains("window-dragging"))
                    return true;
            return false;
        }

        public static bool TryCancelDrag(VisualElement target)
        {
            for (var element = target; element != null; element = element.parent)
            {
                if (!element.ClassListContains("window-dragging"))
                    continue;
                // Cancel through the owner before another window interprets Escape.
                using var cancellation = PointerCancelEvent.GetPooled();
                element.SendEvent(cancellation);
                return true;
            }
            return false;
        }

        public bool Restore()
        {
            if (!persistent() || !Ready() || !layout.TryGet(root.name, out var saved))
                return false;
            Place(saved);
            return true;
        }

        // Drops a dragged position so the owner's own placement applies again.
        public void Forget() => layout.Forget(root.name);

        public void SetDefaultAnchor(Vector2 value)
        {
            if (defaultAnchor == value)
                return;
            defaultAnchor = value;
            Refresh();
        }

        public void Refresh()
        {
            if (defaultAnchor.HasValue && (!persistent() || !layout.TryGet(root.name, out _)))
            {
                FitDefaultAnchor();
                return;
            }
            // UI-324: a window without its own placement opens in the middle of the board until the player moves it.
            var ownPlacement = anchor != null || defaultAnchor.HasValue;
            if (!persistent())
            {
                if (ownPlacement || !Centre())
                    DefaultPosition();
                return;
            }
            if (!Restore() && Ready())
            {
                if (!ownPlacement && Centre())
                    return;
                // Anchored panels may have just written a position in their geometry
                // callback. Preserve that requested position, before the next layout.
                var x = root.style.left.keyword == StyleKeyword.Undefined ? root.style.left.value.value : root.layout.x;
                var y = root.style.top.keyword == StyleKeyword.Undefined ? root.style.top.value.value : root.layout.y;
                Place(new Vector2(x, y), rememberInitial);
            }
        }

        // UI-324: places the window in the middle of the board (WindowLayout.Middle), less a band kept free on the right (the
        // battle window's room for a card); not remembered as the player's position. False before it can.
        public bool Centre(float keepRight = 0)
        {
            if (!Ready() || !(layout.Middle?.Invoke() is Rect world))
                return false;
            var minimum = viewport.WorldToLocal(world.min);
            var area = new Rect(minimum, viewport.WorldToLocal(world.max) - minimum);
            area.xMax -= keepRight;
            var size = root.layout.size;
            var middle = new Vector2(area.center.x - size.x / 2, Mathf.Max(area.yMin, area.center.y - size.y / 2));
            if (!applied || !WithinPixel(root.layout.position, middle))
                Place(OnPixels(middle), remember: false);
            return true;
        }

        // A window already within a pixel of its middle stays: its size can round differently at each of two neighbouring
        // pixel positions, and a middle on a half pixel would then move it between them for ever (a layout loop).
        private bool WithinPixel(Vector2 current, Vector2 middle)
        {
            var pixel = 1 / (root.panel?.scaledPixelsPerPoint ?? 1);
            return Mathf.Abs(current.x - middle.x) <= pixel && Mathf.Abs(current.y - middle.y) <= pixel;
        }

        // On the screen's pixel grid: a subpixel position would change the rounded sizes of the window's controls and so
        // its centre (a layout loop), and at 125 % a whole point is not a whole pixel.
        private Vector2 OnPixels(Vector2 local)
        {
            var pixels = root.panel?.scaledPixelsPerPoint ?? 1;
            var world = viewport.LocalToWorld(local);
            return viewport.WorldToLocal(new Vector2(Mathf.Round(world.x * pixels) / pixels, Mathf.Round(world.y * pixels) / pixels));
        }

        private bool Ready() => root.style.display.value != DisplayStyle.None && root.resolvedStyle.display != DisplayStyle.None &&
            root.layout.width > 0 && root.layout.height > 0 && viewport.contentRect.width > 0 && viewport.contentRect.height > 0;

        private void FitDefaultAnchor()
        {
            if (!Ready())
                return;
            // Right/top offsets remain stable across content and viewport changes.
            // Clamp without treating automatic placement as a player's choice.
            Place(new Vector2(viewport.contentRect.xMax - defaultAnchor.Value.x - root.layout.width,
                defaultAnchor.Value.y), remember: false);
        }

        private void Place(Vector2 next, bool remember = true)
        {
            var clamped = WindowLayout.Clamp(next, root.layout.size, viewport.contentRect);
            if (float.IsNaN(clamped.x) || float.IsNaN(clamped.y))
                return;
            if (remember)
                layout.Remember(root.name, clamped);
            applied = true;
            root.style.right = StyleKeyword.Auto;
            root.style.bottom = StyleKeyword.Auto;
            // A subpixel clamp can change Yoga's rounded size, then reverse the
            // clamp in the next geometry callback. Keep an equivalent position.
            if (!SamePosition(root.style.left, clamped.x))
                root.style.left = clamped.x;
            if (!SamePosition(root.style.top, clamped.y))
                root.style.top = clamped.y;
        }

        private static bool SamePosition(StyleLength current, float next) =>
            current.keyword == StyleKeyword.Undefined && current.value.unit == LengthUnit.Pixel &&
            Mathf.Abs(current.value.value - next) < 0.1f;

        private void Down(PointerDownEvent e)
        {
            if (e.button != 0 || !IsHandle(e.target as VisualElement))
                return;
            origin = viewport.WorldToLocal(e.position);
            position = root.layout.position;
            pointer = e.pointerId;
            moved = false;
            root.AddToClassList("window-dragging");
            root.BringToFront();
            root.Focus();
            root.CapturePointer(pointer);
            e.StopPropagation();
        }

        private void Move(PointerMoveEvent e)
        {
            if (pointer != e.pointerId)
                return;
            var delta = viewport.WorldToLocal(e.position) - origin;
            if (!moved && delta.magnitude < 4)
                return;
            if (!moved)
                pin?.Invoke();
            moved = true;
            Place(position + delta);
            e.StopPropagation();
        }

        private void Up(PointerUpEvent e)
        {
            if (pointer != e.pointerId)
                return;
            End();
            e.StopPropagation();
        }

        private void End()
        {
            var captured = pointer;
            pointer = -1;
            root.RemoveFromClassList("window-dragging");
            if (captured >= 0 && root.HasPointerCapture(captured))
                root.ReleasePointer(captured);
        }

        private void Key(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Escape && pointer >= 0)
            {
                End();
                e.StopImmediatePropagation();
                return;
            }
            if (!IsHandle(e.target as VisualElement))
                return;
            var delta = e.keyCode switch
            {
                KeyCode.LeftArrow => Vector2.left,
                KeyCode.RightArrow => Vector2.right,
                KeyCode.UpArrow => Vector2.down,
                KeyCode.DownArrow => Vector2.up,
                _ => Vector2.zero
            };
            if (delta == Vector2.zero)
                return;
            pin?.Invoke();
            Place(root.layout.position + delta * 16);
            e.StopImmediatePropagation();
        }

        private void DefaultPosition()
        {
            if (!applied)
                return;
            applied = false;
            root.style.left = initialLeft;
            root.style.top = initialTop;
            root.style.right = initialRight;
            root.style.bottom = initialBottom;
        }

        private void Reset()
        {
            End();
            DefaultPosition();
            anchor?.Invoke();
        }

        private void Focus(bool focused)
        {
            if (!focused)
                End();
        }
    }
}
