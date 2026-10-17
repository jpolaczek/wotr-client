#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using Unity.Profiling;
using UnityEngine.Profiling;

namespace Wotr.Client.Diagnostics
{
    internal sealed class MapProfileMemory : IDisposable
    {
        private ProfilerRecorder meshes = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Mesh Memory", 1);
        private long meshPeak;
        private long graphicsPeak;
        private long unityPeak;

        // Run before constructing the first board. Retained managed bytes include
        // the contour index and validation/catalog objects, not only coordinates.
        public static object LoadGeometry()
        {
            var before = GC.GetTotalMemory(true);
            var data = BoardPresentation.Load();
            var contours = GC.GetTotalMemory(true);
            var bands = MapBandGeometry.Load(data.Borders);
            var after = GC.GetTotalMemory(true);
            var sourcePoints = data.Geometry.Regions.SelectMany(r => r.Rings).Sum(r => r.Points.Length) +
                data.Geometry.Boundaries.Sum(b => b.Points.Length);
            var bandPoints = bands.Water.Sum(r => r.Points.Length) + bands.Paths.SelectMany(p => p.Parts)
                .SelectMany(p => p.Positive.Concat(p.Negative).Concat(p.PositiveOutline).Concat(p.NegativeOutline))
                .Sum(r => r.Points.Length);
            GC.KeepAlive(data);
            GC.KeepAlive(bands);
            return new
            {
                BoardContoursAndIndexesRetainedManagedBytes = contours - before,
                RenderBandsRetainedManagedBytes = after - contours,
                CoordinatePayloadBytes = (long)(sourcePoints + bandPoints) * 2 * sizeof(float),
                SourcePoints = sourcePoints,
                BandPoints = bandPoints,
                Scope = "First-load retained managed heap deltas; coordinate payload excludes object/array headers. Native UIR buffers are not isolated by Unity's public API."
            };
        }

        public void Sample()
        {
            if (meshes.Valid && meshes.Count > 0)
                meshPeak = Math.Max(meshPeak, meshes.LastValue);
            graphicsPeak = Math.Max(graphicsPeak, Profiler.GetAllocatedMemoryForGraphicsDriver());
            unityPeak = Math.Max(unityPeak, Profiler.GetTotalAllocatedMemoryLong());
        }

        public object Result() => new
        {
            MeshMemory = new { CounterValid = meshes.Valid, PeakBytes = meshPeak,
                Scope = "Global Unity mesh counter, not map-only; unavailable when CounterValid is false." },
            GraphicsDriverEstimatedPeakBytes = graphicsPeak,
            UnityAllocatedPeakBytes = unityPeak,
            Scope = "Whole Player memory including UI, resources and render buffers. Graphics driver bytes are an estimate, not exact map geometry allocation."
        };

        public void Dispose() => meshes.Dispose();
    }
}
#endif
