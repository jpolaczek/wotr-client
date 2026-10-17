#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using UnityEngine;

namespace Wotr.Client.Diagnostics
{
    // Desktop profiles measure exact render sizes. A monitor whose own resolution equals the
    // requested size cannot fit a window that large below the menu bar (a 1080p monitor gives a
    // 1920×1080 window 1027 px), so the size falls back to a full-screen window, which renders
    // exactly what a player sees on that monitor.
    internal static class ProfileResolution
    {
        private const int MinimumFrames = 10;
        private const float SettleSeconds = 3f;
        private static readonly FullScreenMode[] Modes = { FullScreenMode.Windowed, FullScreenMode.FullScreenWindow };

        public static string Mode => Screen.fullScreenMode.ToString();

        public static bool Matches(int width, int height) => Screen.width == width && Screen.height == height;

        public static IEnumerator Apply(int width, int height)
        {
            // Keep a reached size: switching modes per workload would add window transitions.
            if (Matches(width, height))
                yield break;
            foreach (var mode in Modes)
            {
                Screen.SetResolution(width, height, mode);
                var settle = Settle(width, height);
                while (settle.MoveNext())
                    yield return settle.Current;
                if (Matches(width, height))
                    yield break;
            }
        }

        private static IEnumerator Settle(int width, int height)
        {
            var deadline = Time.realtimeSinceStartup + SettleSeconds;
            for (var frame = 0; frame < MinimumFrames; frame++)
                yield return null;
            while (!Matches(width, height) && Time.realtimeSinceStartup < deadline)
                yield return null;
        }
    }
}
#endif
