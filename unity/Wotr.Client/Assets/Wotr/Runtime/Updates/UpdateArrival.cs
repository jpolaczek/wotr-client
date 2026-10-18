using Wotr.Contracts;

namespace Wotr.Client.Updates
{
    // DIST-15: the version this copy has just been updated to, shown once with a What's new link: a version newer than
    // the one that last ran here. A first installation (nothing ran before), a repair (the same version) and an older
    // copy show nothing.
    public static class UpdateArrival
    {
        public static string Version(string lastRun, string current) =>
            ClientVersion.TryParse(lastRun, out var before) && ClientVersion.TryParse(current, out var now) && now.CompareTo(before) > 0
                ? current
                : "";

        // The What's new page of the game site at the version's own entry.
        public static string NotesUrl(string gamePage, string version) =>
            gamePage.TrimEnd('/') + "/updates#v" + System.Uri.EscapeDataString(version);
    }
}
