using System;

namespace Wotr.Client.Updates
{
    // DIST-17: what the first menus say about an update beside Settings: a short note and, for the tooltip, the full
    // sentence. Empty when there is nothing to tell.
    public sealed class UpdateNote
    {
        public static readonly UpdateNote None = new UpdateNote("", "");

        public UpdateNote(string text, string detail)
        {
            Text = text;
            Detail = detail;
        }

        public string Text { get; }
        public string Detail { get; }
        public bool Shown => Text.Length > 0;
    }

    // DIST-17: the updater's state as the note beside Settings. A repair is the player's own work in Settings, and a
    // failure, no release or a server behind this copy say nothing here; Settings tells the details.
    public static class UpdateHint
    {
        private const string OpenSettings = " Open Settings to install it.";

        public static UpdateNote For(UpdateSnapshot update, DateTimeOffset now) =>
            Note(update.Mode, update.Status, update.Repair, update.Blocked, update.Version, UpdateTexts.Percent(update.Progress),
                update.Plan?.Deadline, now);

        public static UpdateNote Note(UpdateMode mode, UpdateStatus status, bool repair, bool blocked, string version,
            string percent, DateTimeOffset? deadline, DateTimeOffset now)
        {
            if (repair || mode == UpdateMode.None || mode == UpdateMode.ServerBehind)
                return UpdateNote.None;
            if (status == UpdateStatus.Ready)
                return new UpdateNote("Update ready", $"Version {version} is downloaded." + OpenSettings);
            if (blocked || mode == UpdateMode.RequiredNow)
                return new UpdateNote("Update required", "This version can no longer play online." + OpenSettings);
            if (status == UpdateStatus.Downloading)
                return new UpdateNote(percent.Length > 0 ? "Downloading " + percent : "Downloading update",
                    $"Version {version} is downloading. Settings shows the progress.");
            if (mode == UpdateMode.RequiredSoon && deadline.HasValue)
                return new UpdateNote("Update required soon",
                    $"Version {version} is required {UpdateTexts.Deadline(deadline.Value, now)}; it downloads by itself.");
            if (status == UpdateStatus.Available)
                return new UpdateNote("Update available", $"Version {version} is available. Open Settings to download it.");
            return UpdateNote.None;
        }
    }
}
