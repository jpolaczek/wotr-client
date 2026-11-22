using System;
using Wotr.Contracts;

namespace Wotr.Client
{
    // DIST-05/DIST-07: a server that refuses this version (409) does not change its mind on a retry. Screens show
    // these texts and stop polling until the player refreshes; the updater (GameApi.VersionRefused) takes over.
    public static class VersionRefusal
    {
        public static bool Is(Exception error) =>
            error is ApiException api && api.Status == 409 && Text(api.Message) != null;

        public static string Text(string code) => Text(code, ServerConfiguration.CanUpdate);

        // DIST-22: a copy built without a downloads origin cannot update itself, and the refusal says so.
        public const string CannotUpdate = "this copy cannot download or install updates";

        public static string Text(string code, bool canUpdate)
        {
            switch (code)
            {
                case ClientCompatibility.ClientUpdateRequired:
                    return canUpdate
                        ? "This version of War of the Ring is no longer supported. Install the latest version to keep playing online."
                        : "This version of War of the Ring is no longer supported, and " + CannotUpdate +
                            ". Install or build the latest version to keep playing online.";
                case ClientCompatibility.ServerUpdateRequired:
                    return "The server has not been updated for this version yet. Try again after the server update.";
                default:
                    return null;
            }
        }

        // The board's line for a game stopped by a refused version.
        public static string Board(bool canUpdate) => canUpdate
            ? "Install the latest version to keep playing. This game stays saved on the server."
            : "This copy cannot download or install updates. Install or build the latest version to keep playing. This game stays saved on the server.";
    }
}
