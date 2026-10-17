using System;
using UnityEngine;
using Velopack.Locators;

namespace Wotr.Client.Updates
{
    // DIST-06: how this copy of the game was installed. Only a release build that Velopack installed (the updater and
    // sq.version beside it) may update or repair itself (DIST-07). The Editor, development builds and a copy unpacked
    // from a ZIP never replace their own files. On macOS Velopack reports every copy as portable, so the installed
    // version, not IsPortable, tells an installed copy apart.
    public static class ClientInstallation
    {
        private static readonly Lazy<IVelopackLocator> Locator = new Lazy<IVelopackLocator>(FindLocator);

        // The version Velopack installed, or null outside an installation.
        public static string InstalledVersion => Locator.Value?.CurrentlyInstalledVersion?.ToString();

        // Velopack's view of this installation for VelopackBridge; null outside an installation and in the Editor.
        internal static IVelopackLocator CurrentLocator => Locator.Value;

        public static bool CanUpdateItself =>
            !Application.isEditor && !ClientIdentity.Current.Development && VelopackStartup.Result == VelopackStartup.Succeeded &&
            InstalledVersion != null;

        private static IVelopackLocator FindLocator()
        {
            if (Application.isEditor)
                return null;
            try
            {
                var logger = new VelopackUnityLogger();
                return VelopackLocator.CreateDefaultForPlatform(new DefaultProcessImpl(logger), logger);
            }
            catch (Exception error)
            {
                Debug.Log("[Velopack] This copy is not an installation: " + error.Message);
                return null;
            }
        }
    }
}
