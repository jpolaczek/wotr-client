using UnityEngine;

namespace Wotr.Client
{
    public sealed class CameraState
    {
        public float Zoom = .5f;
        public Vector2 Pan;
        public float MinimumZoom { get; private set; } = .28f;
        public float MaximumZoom => Mathf.Max(2.6f, MinimumZoom);
        private const float GeometryTolerance = .001f;
        private Rect board;
        private Rect viewport;
        private bool constrained;

        public Vector2 ToScreen(Vector2 point) => point * Zoom + Pan;
        public Vector2 ToBoard(Vector2 point) => (point - Pan) / Zoom;

        public static float CoverZoom(Rect board, Rect viewport)
        {
            if (!Valid(board) || !Valid(viewport))
                return .01f;
            var required = System.Math.Max((double)viewport.width / board.width,
                (double)viewport.height / board.height);
            var zoom = (float)required;
            // Rounding down leaves a subpixel gap and can invert the pan bounds.
            return zoom < required
                ? System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(zoom) + 1)
                : zoom;
        }

        public void Constrain(Rect bounds, Rect visible)
        {
            if (!Valid(bounds) || !Valid(visible))
                return;
            if (!constrained || !board.Equals(bounds) || !SameViewport(visible))
            {
                board = bounds;
                viewport = visible;
                constrained = true;
                MinimumZoom = CoverZoom(board, viewport);
            }
            Clamp();
        }

        // Repeated UI Toolkit layout can differ by a float ULP. Keep the accepted
        // rectangle until a real resize, without skipping the clamp of new input.
        private bool SameViewport(Rect visible) =>
            Mathf.Abs(viewport.xMin - visible.xMin) <= GeometryTolerance &&
            Mathf.Abs(viewport.yMin - visible.yMin) <= GeometryTolerance &&
            Mathf.Abs(viewport.xMax - visible.xMax) <= GeometryTolerance &&
            Mathf.Abs(viewport.yMax - visible.yMax) <= GeometryTolerance;

        private static bool Valid(Rect rect) => rect.width > 0 && rect.height > 0 &&
            !float.IsInfinity(rect.width) && !float.IsInfinity(rect.height);

        public void ZoomAt(Vector2 point, float factor)
        {
            var original = ToBoard(point);
            Zoom = Mathf.Clamp(Zoom * factor, MinimumZoom, MaximumZoom);
            Pan = point - original * Zoom;
            Clamp();
        }

        public bool TryFrame(Rect targets, Rect safeArea, float preferredZoom)
        {
            if (!constrained || !Valid(safeArea) || targets.width < 0 || targets.height < 0)
                return false;
            var minimum = (double)MinimumZoom;
            var maximum = (double)MaximumZoom;
            // Both the raster and every target must fit at the same pan. These four
            // lower bounds permit moving the targets away from an occupied edge.
            if (!LimitFrameZoom(safeArea.xMin - viewport.xMin, targets.xMin - board.xMin, ref minimum) ||
                !LimitFrameZoom(viewport.xMax - safeArea.xMax, board.xMax - targets.xMax, ref minimum) ||
                !LimitFrameZoom(safeArea.yMin - viewport.yMin, targets.yMin - board.yMin, ref minimum) ||
                !LimitFrameZoom(viewport.yMax - safeArea.yMax, board.yMax - targets.yMax, ref minimum))
                return false;
            if (targets.width > 0)
                maximum = System.Math.Min(maximum, (double)safeArea.width / targets.width);
            if (targets.height > 0)
                maximum = System.Math.Min(maximum, (double)safeArea.height / targets.height);
            var lowerZoom = RoundUp(minimum);
            var upperZoom = RoundDown(maximum);
            if (lowerZoom > upperZoom)
                return false;
            Zoom = Mathf.Clamp(preferredZoom, lowerZoom, upperZoom);
            var lower = Vector2.Max(viewport.max - board.max * Zoom, safeArea.min - targets.min * Zoom);
            var upper = Vector2.Min(viewport.min - board.min * Zoom, safeArea.max - targets.max * Zoom);
            var preferred = safeArea.center - targets.center * Zoom;
            Pan = new Vector2(Mathf.Clamp(preferred.x, lower.x, upper.x), Mathf.Clamp(preferred.y, lower.y, upper.y));
            Clamp();
            return true;
        }

        private static bool LimitFrameZoom(float inset, float distance, ref double minimum)
        {
            if (distance < 0 || distance == 0 && inset > 0)
                return false;
            if (distance > 0)
                minimum = System.Math.Max(minimum, (double)inset / distance);
            return true;
        }

        private static float RoundUp(double value)
        {
            var result = (float)value;
            return result < value
                ? System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(result) + 1)
                : result;
        }

        private static float RoundDown(double value)
        {
            var result = (float)value;
            return result > value
                ? System.BitConverter.Int32BitsToSingle(System.BitConverter.SingleToInt32Bits(result) - 1)
                : result;
        }

        private void Clamp()
        {
            Zoom = Mathf.Clamp(Zoom, MinimumZoom, MaximumZoom);
            if (!constrained)
                return;
            Pan = ConstrainedPan(Pan);
        }

        public bool CanPan(Vector2 delta) => !constrained || ConstrainedPan(Pan + delta) != Pan;

        private Vector2 ConstrainedPan(Vector2 pan) => new Vector2(
            ClampAxis(pan.x, viewport.xMax - board.xMax * Zoom, viewport.xMin - board.xMin * Zoom),
            ClampAxis(pan.y, viewport.yMax - board.yMax * Zoom, viewport.yMin - board.yMin * Zoom));

        private static float ClampAxis(float pan, float minimum, float maximum) =>
            Mathf.Clamp(pan, Mathf.Min(minimum, maximum), maximum);
    }
}
