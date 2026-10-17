#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client.Diagnostics
{
    // Bounded, buffered diagnostics. Never calls Pick or dispatches an event.
    internal sealed class MapEdgeProbeTrace : IDisposable
    {
        private const int MaximumRecords = 25000;
        private readonly StreamWriter writer;
        private readonly MapCanvas map;
        public int Records { get; private set; }
        public int Dropped { get; private set; }

        public MapEdgeProbeTrace(string path, MapCanvas map)
        {
            this.map = map;
            writer = new StreamWriter(path, false);
        }

        public void Record(string kind, int reset, Vector2? position = null, string target = null)
        {
            if (Records >= MaximumRecords)
            {
                Dropped++;
                return;
            }
            writer.WriteLine(JsonConvert.SerializeObject(new
            {
                Kind = kind,
                Reset = reset,
                EventPosition = position.HasValue ? Point(position.Value) : null,
                Target = target,
                Snapshot = Snapshot()
            }));
            Records++;
        }

        public object Snapshot()
        {
            Vector2 raw = Input.mousePosition;
            var topLeft = new Vector2(raw.x, Screen.height - raw.y);
            var panel = map.panel;
            var mapped = panel != null ? RuntimePanelUtils.ScreenToPanel(panel, topLeft) : Vector2.zero;
            var capture = panel?.GetCapturingElement(PointerId.mousePointerId) as VisualElement;
            return new
            {
                Time = Time.realtimeSinceStartupAsDouble,
                Frame = Time.frameCount,
                Focused = Application.isFocused,
                ScreenWidth = Screen.width,
                ScreenHeight = Screen.height,
                FullScreenMode = Screen.fullScreenMode.ToString(),
                DisplayWidth = Display.main.systemWidth,
                DisplayHeight = Display.main.systemHeight,
                RenderingWidth = Display.main.renderingWidth,
                RenderingHeight = Display.main.renderingHeight,
                ScreenDpi = Screen.dpi,
                PixelsPerPoint = panel?.scaledPixelsPerPoint ?? 0,
                RawMouse = Point(raw),
                ScreenTopLeft = Point(topLeft),
                ScreenToPanel = Point(mapped),
                MapBounds = Bounds(map.worldBound),
                RootBounds = panel != null ? Bounds(panel.visualTree.worldBound) : null,
                Active = map.EdgePan.Active,
                Pan = Point(map.Camera.Pan),
                map.Camera.Zoom,
                Capture = capture?.name,
                Capturing = capture != null,
                LeftButton = Input.GetMouseButton(0),
                RightButton = Input.GetMouseButton(1),
                MiddleButton = Input.GetMouseButton(2)
            };
        }

        public void Flush() => writer.Flush();
        public void Dispose() => writer.Dispose();
        private static float[] Point(Vector2 point) => new[] { point.x, point.y };
        private static float[] Bounds(Rect rect) => new[] { rect.xMin, rect.yMin, rect.width, rect.height };
    }
}
#endif
