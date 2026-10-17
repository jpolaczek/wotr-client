#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace Wotr.Client.Diagnostics
{
    internal sealed class FrameProfile : IDisposable
    {
        public const int WarmupFrames = 120;
        public const int SampleFrames = 360;
        private ProfilerRecorder main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
        private ProfilerRecorder allocated = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        private readonly double[] frames = new double[SampleFrames];
        private readonly double[] cpu = new double[SampleFrames];
        private readonly double[] gc = new double[SampleFrames];
        private readonly double[] handlers = new double[SampleFrames];
        private readonly double[] feedback = new double[SampleFrames];
        private int cpuCount;
        private int gcCount;
        private int actions;

        public void Frame(int index)
        {
            frames[index] = Time.unscaledDeltaTime * 1000d;
            if (main.Valid && main.Count > 0)
                cpu[cpuCount++] = main.LastValue / 1000000d;
            if (allocated.Valid && allocated.Count > 0)
                gc[gcCount++] = allocated.LastValue;
        }

        public void Action(double handlerMilliseconds, double renderMilliseconds)
        {
            handlers[actions] = handlerMilliseconds;
            feedback[actions++] = renderMilliseconds;
        }

        public object Result() => new
        {
            WarmupFrames,
            SampleFrames,
            Fps = 1000d / frames.Average(),
            FrameMilliseconds = Summarize(frames, SampleFrames),
            MainThreadMilliseconds = Summarize(cpu, cpuCount),
            GcBytesPerFrame = Summarize(gc, gcCount),
            MainThreadCounter = new { main.Valid, Unit = main.Valid ? main.UnitType.ToString() : "unavailable" },
            AllocationCounter = new { allocated.Valid, Unit = allocated.Valid ? allocated.UnitType.ToString() : "unavailable" },
            HandlerMilliseconds = Summarize(handlers, actions),
            ToRenderEndMilliseconds = Summarize(feedback, actions)
        };

        private static object Summarize(double[] samples, int count)
        {
            if (count == 0)
                return null;
            var sorted = samples.Take(count).OrderBy(value => value).ToArray();
            return new
            {
                Count = count,
                Mean = sorted.Average(),
                P95 = sorted[(int)Math.Ceiling(count * .95d) - 1],
                P99 = sorted[(int)Math.Ceiling(count * .99d) - 1],
                Max = sorted[count - 1]
            };
        }

        public void Dispose()
        {
            main.Dispose();
            allocated.Dispose();
        }
    }
}
#endif
