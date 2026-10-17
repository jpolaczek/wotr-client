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

        public static string Text(string code)
        {
            switch (code)
            {
                case ClientCompatibility.ClientUpdateRequired:
                    return "This version of War of the Ring is no longer supported. Install the latest version to keep playing online.";
                case ClientCompatibility.ServerUpdateRequired:
                    return "The server has not been updated for this version yet. Try again after the server update.";
                default:
                    return null;
            }
        }
    }
}
