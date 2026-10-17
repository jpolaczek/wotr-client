using System;
using UnityEngine;
using Velopack.Logging;

namespace Wotr.Client.Updates
{
    // Velopack's messages from information up go to the player log; trace and debug detail stay out of it.
    internal sealed class VelopackUnityLogger : IVelopackLogger
    {
        public void Log(VelopackLogLevel level, string message, Exception exception)
        {
            if (level < VelopackLogLevel.Information)
                return;
            var text = "[Velopack] " + message + (exception == null ? "" : "\n" + exception);
            if (level >= VelopackLogLevel.Warning)
                Debug.LogWarning(text);
            else
                Debug.Log(text);
        }
    }
}
