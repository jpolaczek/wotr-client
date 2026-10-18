using System.IO;
using Wotr.Contracts;

namespace Wotr.Client.Updates
{
    // The release Velopack is asked to install: the verified package file with the version and SHA-256 its signed
    // manifest named, and whether it is the player's repair of the running version.
    public sealed class StagedRelease
    {
        public StagedRelease(string package, string version, string sha256, bool repair)
        {
            Package = package;
            Version = version;
            Sha256 = sha256;
            Repair = repair;
        }

        public string Package { get; }
        public string Version { get; }
        public string Sha256 { get; }
        public bool Repair { get; }
    }

    // SEC-01: the last checks before Velopack installs. An update must be newer than the running version and a repair
    // must be exactly the running version, so neither a stale download nor a replayed old release can roll a copy back;
    // the package's own version must be the one its signed manifest names; and the file Velopack will apply must still
    // have the manifest's SHA-256, since it may have changed on disk after it was checked.
    public static class InstallGuard
    {
        public static void CheckVersions(string running, StagedRelease release, string packageVersion)
        {
            if (!ClientVersion.TryParse(release.Version, out var target) || !ClientVersion.TryParse(packageVersion, out var packed) ||
                packed.CompareTo(target) != 0)
                throw new UpdateRejected($"The package is version {packageVersion}, not the release's {release.Version}.");
            if (!ClientVersion.TryParse(running, out var own))
                throw new UpdateRejected("This copy does not know its installed version and cannot update itself.");
            var order = target.CompareTo(own);
            if (release.Repair && order != 0)
                throw new UpdateRejected($"A repair installs the running version {running}, not {release.Version}.");
            if (!release.Repair && order <= 0)
                throw new UpdateRejected($"Version {release.Version} is not newer than the running {running}.");
        }

        public static void CheckFile(string path, string sha256)
        {
            if (!File.Exists(path) || PackageDownloader.Hash(path) != sha256)
                throw new UpdateRejected("The update package changed after it was checked and was not installed.");
        }
    }
}
