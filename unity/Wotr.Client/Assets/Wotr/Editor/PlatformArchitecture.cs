using System;
using System.Reflection;
using UnityEditor.Build;

namespace Wotr.Editor
{
    // Sets the player architecture through each platform module's UserBuildSettings, found at runtime. The modules are
    // optional: a CI editor image carries only the platform it builds, so referencing both types directly would not
    // compile there. A missing module is skipped here; building for it still fails in DesktopBuildSetup.RequirePlatform.
    internal static class PlatformArchitecture
    {
        private const string MacSettings = "UnityEditor.OSXStandalone.UserBuildSettings";
        private const string WindowsSettings = "UnityEditor.WindowsStandalone.UserBuildSettings";

        internal static void UseMacUniversal()
        {
            Set(MacSettings, OSArchitecture.x64ARM64);
        }

        internal static void UseWindowsX64()
        {
            Set(WindowsSettings, OSArchitecture.x64);
        }

        private static void Set(string settingsType, OSArchitecture architecture)
        {
            var type = FindType(settingsType);
            if (type == null)
                return;
            var property = type.GetProperty("architecture", BindingFlags.Public | BindingFlags.Static);
            if (property == null)
                throw new MissingMemberException(settingsType, "architecture");
            property.SetValue(null, architecture);
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
