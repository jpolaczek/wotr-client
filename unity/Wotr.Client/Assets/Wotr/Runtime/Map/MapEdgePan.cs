using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // Presentation-only navigation. The timer runs only while an exposed edge can move.
    public sealed class MapEdgePan
    {
        public bool Active { get; private set; }
        private const float EdgeWidth = 26f;
        private const float OuterRimPixels = 4f;
        private const float Speed = 650f;
        private readonly MapCanvas map;
        private readonly IVisualElementScheduledItem timer;
        private VisualElement input;
        private Vector2 pointer;
        private Vector2 velocity;
        private double previousTime;
        private bool focused = true;
        private bool suspended;
        private int pressedButtons;

        public MapEdgePan(MapCanvas map)
        {
            this.map = map;
            timer = map.schedule.Execute(Tick).Every(16);
            timer.Pause();
            map.RegisterCallback<AttachToPanelEvent>(_ => Attach());
            map.RegisterCallback<DetachFromPanelEvent>(_ => Detach());
        }

        public static Vector2 VelocityAt(Vector2 point, Rect viewport)
        {
            if (!ContainsViewportPoint(viewport, point))
                return Vector2.zero;
            var x = Edge(point.x - viewport.xMin) - Edge(viewport.xMax - point.x);
            var y = Edge(point.y - viewport.yMin) - Edge(viewport.yMax - point.y);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1) * Speed;
        }

        private static float Edge(float distance) => Mathf.Clamp01(1 - distance / EdgeWidth);

        private static bool ContainsViewportPoint(Rect viewport, Vector2 point) =>
            viewport.width > 0 && viewport.height > 0 &&
            point.x >= viewport.xMin && point.x <= viewport.xMax &&
            point.y >= viewport.yMin && point.y <= viewport.yMax;

        private void Attach()
        {
            if (input != null || map.panel == null)
                return;
            input = map.panel.visualTree;
            input.RegisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            input.RegisterCallback<PointerEnterEvent>(Enter, TrickleDown.TrickleDown);
            input.RegisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            input.RegisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            input.RegisterCallback<PointerCaptureEvent>(Captured, TrickleDown.TrickleDown);
            input.RegisterCallback<PointerLeaveEvent>(Leave);
            input.RegisterCallback<MouseLeaveWindowEvent>(LeaveWindow, TrickleDown.TrickleDown);
            input.RegisterCallback<PointerCancelEvent>(Cancel, TrickleDown.TrickleDown);
            Application.focusChanged += SetFocused;
        }

        private void Detach()
        {
            Stop();
            if (input == null)
                return;
            input.UnregisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            input.UnregisterCallback<PointerEnterEvent>(Enter, TrickleDown.TrickleDown);
            input.UnregisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            input.UnregisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            input.UnregisterCallback<PointerCaptureEvent>(Captured, TrickleDown.TrickleDown);
            input.UnregisterCallback<PointerLeaveEvent>(Leave);
            input.UnregisterCallback<MouseLeaveWindowEvent>(LeaveWindow, TrickleDown.TrickleDown);
            input.UnregisterCallback<PointerCancelEvent>(Cancel, TrickleDown.TrickleDown);
            Application.focusChanged -= SetFocused;
            input = null;
        }

        private void Move(PointerMoveEvent e) => Observe(e);

        private void Enter(PointerEnterEvent e)
        {
            if (e.target == input)
                Observe(e);
        }

        private void Observe(IPointerEvent e)
        {
            if (e.pointerId != PointerId.mousePointerId)
                return;
            ObservePointer(e.position, e.pressedButtons);
        }

        public void SamplePointer(Vector2 point, int buttons)
        {
            // A stationary native sample needs no second hit-test. The active
            // timer still checks overlays/capture and the camera's remaining range.
            if (Active && pointer.Equals(point) && pressedButtons == buttons)
                return;
            ObservePointer(point, buttons);
        }

        private void ObservePointer(Vector2 point, int buttons)
        {
            pointer = point;
            pressedButtons = buttons;
            velocity = VelocityAt(pointer, map.worldBound);
            if (velocity == Vector2.zero || !CanPan())
            {
                Stop();
                return;
            }
            if (Active)
                return;
            Active = true;
            previousTime = Time.realtimeSinceStartupAsDouble;
            timer.Resume();
        }

        // UI-305: an automatic camera travel suspends passive edge pan, so the two never fight.
        public bool Suspended
        {
            get => suspended;
            set
            {
                suspended = value;
                if (value)
                    Stop();
            }
        }

        private bool CanPan()
        {
            if (suspended)
                return false;
            var panel = map.panel;
            var viewport = map.worldBound;
            if (!focused || pressedButtons != 0 || map.Panning || !map.enabledInHierarchy || panel == null ||
                !map.Camera.CanPan(velocity) ||
                panel.GetCapturingElement(PointerId.mousePointerId) != null || !ContainsViewportPoint(viewport, pointer))
                return false;

            // The exact outer coordinate belongs to navigation, while UI Toolkit
            // picking needs a point strictly within the rendered panel.
            var pixel = 1f / panel.scaledPixelsPerPoint;
            var pickPoint = new Vector2(
                Mathf.Clamp(pointer.x, viewport.xMin + pixel * .25f, viewport.xMax - pixel * .25f),
                Mathf.Clamp(pointer.y, viewport.yMin + pixel * .25f, viewport.yMax - pixel * .25f));
            // Pick caches whole panel-coordinate pixels. At fractional scale, an
            // outside boundary and its inset can share that cached null result.
            // Only the adjusted probe bypasses the cache; no result list is allocated.
            var picked = pickPoint.Equals(pointer) ? panel.Pick(pointer) : panel.PickAll(pickPoint, null);
            if (picked == map || picked != null && map.Contains(picked))
                return true;

            var rim = OuterRimPixels * pixel;
            var onRim = pointer.x - viewport.xMin <= rim || viewport.xMax - pointer.x <= rim ||
                pointer.y - viewport.yMin <= rim || viewport.yMax - pointer.y <= rim;
            var blockedControl = false;
            for (var element = picked; element != null; element = element.parent)
            {
                if (element.ClassListContains("floating-window") || element.ClassListContains("board-menu") ||
                    element.ClassListContains("settings-scrim"))
                    return false;
                if (element.ClassListContains("map-edge-control"))
                    return onRim;
                if (element.ClassListContains("map-edge-surface"))
                    return onRim || !blockedControl;
                // A focus anchor's blank background may pass through to the HUD surface.
                // When reparented into a floating decision panel it has no such ancestor.
                if (element.focusable && !element.ClassListContains("map-edge-focus-anchor") ||
                    !string.IsNullOrEmpty(element.tooltip))
                    blockedControl = true;
            }
            return false;
        }

        private void Tick()
        {
            if (!CanPan() || !Active)
            {
                Stop();
                return;
            }
            var now = Time.realtimeSinceStartupAsDouble;
            var elapsed = Mathf.Min(.05f, (float)(now - previousTime));
            previousTime = now;
            velocity = VelocityAt(pointer, map.worldBound);
            if (elapsed <= 0)
                return;
            var before = map.Camera.Pan;
            map.Camera.Pan += velocity * elapsed;
            map.Refresh();
            map.SetHovered("");
            if (map.Camera.Pan == before)
                Stop();
        }

        public void SetFocused(bool value)
        {
            if (focused == value)
                return;
            focused = value;
            if (!value)
            {
                Stop();
                map.CancelPress();
                map.CancelPan();
            }
        }

        public void Stop()
        {
            Active = false;
            velocity = Vector2.zero;
            timer.Pause();
        }

        private void Down(PointerDownEvent e)
        {
            pressedButtons = e.pressedButtons;
            Stop();
        }

        private void Up(PointerUpEvent e)
        {
            pressedButtons = e.pressedButtons;
            Stop();
        }

        private void Captured(PointerCaptureEvent _) => Stop();
        private void Cancel(PointerCancelEvent _) => Stop();
        private void LeaveWindow(MouseLeaveWindowEvent _) => Stop();

        private void Leave(PointerLeaveEvent e)
        {
            if (e.target != input)
                return;
            var area = map.worldBound;
            Vector2 point = e.position;
            // UI Toolkit emits Leave at the exclusive right/bottom pick boundary.
            // That geometric transition is still on our inclusive navigation rim.
            // A real window exit is handled separately, even with its last position.
            var onBoundary = ContainsViewportPoint(area, point) &&
                (point.x == area.xMin || point.x == area.xMax || point.y == area.yMin || point.y == area.yMax);
            if (!onBoundary)
                Stop();
        }
    }
}
