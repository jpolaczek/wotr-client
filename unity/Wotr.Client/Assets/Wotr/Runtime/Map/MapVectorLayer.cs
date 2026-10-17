using System;
using System.Diagnostics;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI Toolkit retains each layer's tessellated mesh. Camera transforms do not regenerate paths.
    internal sealed class MapVectorLayer : VisualElement
    {
        public int Repaints { get; private set; }
        public double LastRepaintMilliseconds { get; private set; }

        public MapVectorLayer(string layerName, float width, float height, Action<MeshGenerationContext> draw)
        {
            name = layerName;
            pickingMode = PickingMode.Ignore;
            usageHints = UsageHints.DynamicTransform;
            style.position = Position.Absolute;
            style.left = style.top = 0;
            style.width = width;
            style.height = height;
            generateVisualContent += context =>
            {
                var started = Stopwatch.GetTimestamp();
                draw(context);
                Repaints++;
                LastRepaintMilliseconds = (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;
            };
        }
    }
}
