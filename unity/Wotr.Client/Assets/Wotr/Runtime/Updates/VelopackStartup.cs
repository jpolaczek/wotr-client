using System;
using UnityEngine;
using Velopack;

namespace Wotr.Client.Updates
{
    // DIST-06: Velopack's start-up step, before the splash screen as its Unity Mono sample requires. It never applies a
    // downloaded package by itself (DIST-07 installs only what the game verified) and never runs in the Editor. On
    // Windows the launcher (release/windows/WotrLauncher.c) answers Velopack's --veloapp-* hooks, so they never reach
    // the player. A failure leaves the game playable, only without updates.
    internal static class VelopackStartup
    {
        internal const string NotRun = "not run";
        internal const string Succeeded = "ok";

        internal static string Result { get; private set; } = NotRun;

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Run()
        {
            try
            {
                VelopackApp.Build()
                    .SetAutoApplyOnStartup(false)
                    .SetLogger(new VelopackUnityLogger())
                    .Run();
                Result = Succeeded;
            }
            catch (Exception error)
            {
                Result = "failed: " + error.GetType().Name + ": " + error.Message;
                Debug.LogWarning("Velopack start-up failed; this copy cannot update itself. " + error);
            }
        }
#endif
    }
}
