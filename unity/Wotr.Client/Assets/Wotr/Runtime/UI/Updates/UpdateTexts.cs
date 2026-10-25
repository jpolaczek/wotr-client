using System;
using System.Globalization;
using Wotr.Client.Updates;
using Wotr.Contracts;

namespace Wotr.Client
{
    // DIST-07: the words of the updater, shared by the notice, the required-update overlay and Settings.
    public static class UpdateTexts
    {
        // DIST-18: the steps the exit lock names.
        public const string Saving = "Saving your game…";
        public const string Installing = "Installing the update…";
        public const string Restarting = "Restarting…";
        public const string Closing = "Closing…";

        public static string Platform(string platform) =>
            platform == ClientPlatforms.Windows ? "Windows" : platform == ClientPlatforms.MacOS ? "macOS"
            : platform == ClientPlatforms.Linux ? "Linux" : platform;

        public static string Size(long bytes) =>
            bytes <= 0 ? "" : Math.Max(1, (long)Math.Round(bytes / 1048576.0)).ToString(CultureInfo.InvariantCulture) + " MB";

        public static string Percent(DownloadProgress progress) =>
            progress.Total > 0 ? (progress.Received * 100 / progress.Total).ToString(CultureInfo.InvariantCulture) + "%" : "";

        // "at 20:00 (in 1 h 12 min)", with the date when the deadline is not today; local wall-clock time, server
        // time for what is left.
        public static string Deadline(DateTimeOffset deadline, DateTimeOffset now)
        {
            var local = deadline.ToLocalTime();
            var day = local.Date == DateTimeOffset.Now.Date ? "" : local.ToString("d MMM ", CultureInfo.InvariantCulture);
            return $"at {day}{local.ToString("HH:mm", CultureInfo.InvariantCulture)} ({Remaining(deadline - now)})";
        }

        public static string Remaining(TimeSpan left)
        {
            if (left <= TimeSpan.Zero)
                return "now";
            if (left < TimeSpan.FromMinutes(1))
                return "in less than a minute";
            var minutes = (int)Math.Ceiling(left.TotalMinutes);
            return minutes < 60 ? $"in {minutes} min" : $"in {minutes / 60} h {minutes % 60} min";
        }

        // One line for Settings and the overlay.
        public static string Status(UpdateSnapshot update)
        {
            var version = update.Version;
            var message = update.Message.Length > 0 ? " " + update.Message : "";
            switch (update.Status)
            {
                case UpdateStatus.Checking:
                    return "Checking for updates…";
                case UpdateStatus.Available:
                    var size = Size(update.Manifest?.Package.Size ?? 0);
                    return (update.CanInstall
                        ? $"Version {version} is available{(size.Length > 0 ? " (" + size + ")" : "")}."
                        : $"Version {version} is available on the game page.") + message;
                case UpdateStatus.Waiting:
                    return $"Version {version} starts downloading shortly.";
                case UpdateStatus.Downloading:
                    return (update.Repair ? $"Downloading version {version} for the repair" : $"Downloading version {version}") +
                        (update.Progress.Total > 0 ? " · " + Percent(update.Progress) : "…");
                case UpdateStatus.Ready:
                    return (update.Repair ? $"The repair of version {version} is ready." : $"Version {version} is ready to install.") + message;
                case UpdateStatus.Applying:
                    return "Starting the update…";
                case UpdateStatus.Failed:
                    return update.Message;
                default:
                    if (update.Message.Length > 0)
                        return update.Message;
                    return update.Plan == null ? "Updates have not been checked yet." : "This is the latest version.";
            }
        }
    }
}
