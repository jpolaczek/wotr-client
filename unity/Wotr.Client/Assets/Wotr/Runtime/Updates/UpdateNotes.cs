namespace Wotr.Client.Updates
{
    // DIST-15: the game site's What's new page at a version's own entry, opened from the Updates row of Settings.
    public static class UpdateNotes
    {
        public static string Url(string gamePage, string version) =>
            gamePage.TrimEnd('/') + "/updates#v" + System.Uri.EscapeDataString(version);
    }
}
