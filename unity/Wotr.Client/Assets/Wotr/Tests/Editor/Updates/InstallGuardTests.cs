using System.IO;
using NUnit.Framework;
using Wotr.Client.Updates;

namespace Wotr.Client.Tests
{
    // SEC-01: Velopack installs only a newer version, or the running one as a repair, whose package carries the
    // manifest's version, from a file that still has the manifest's SHA-256.
    public sealed class InstallGuardTests
    {
        private static StagedRelease Release(string version, bool repair = false, string sha256 = "") =>
            new StagedRelease("package.nupkg", version, sha256, repair);

        [Test]
        public void AnUpdateMustBeNewerAndARepairExactlyTheRunningVersion()
        {
            InstallGuard.CheckVersions("0.9.0-rc.9", Release("0.9.0-rc.10"), "0.9.0-rc.10");
            InstallGuard.CheckVersions("0.9.0-rc.9", Release("0.9.0"), "0.9.0");
            InstallGuard.CheckVersions("0.9.0-rc.9", Release("0.9.0-rc.9", true), "0.9.0-rc.9");

            StringAssert.Contains("not newer", Refusal("0.9.0-rc.9", Release("0.9.0-rc.8"), "0.9.0-rc.8"));
            StringAssert.Contains("not newer", Refusal("0.9.0-rc.9", Release("0.9.0-rc.9"), "0.9.0-rc.9"));
            StringAssert.Contains("repair", Refusal("0.9.0-rc.9", Release("0.9.0-rc.10", true), "0.9.0-rc.10"));
            StringAssert.Contains("repair", Refusal("0.9.0-rc.9", Release("0.9.0-rc.8", true), "0.9.0-rc.8"));
        }

        [Test]
        public void ThePackageMustCarryTheManifestsVersionAndTheCopyItsVersionKnown()
        {
            StringAssert.Contains("not the release's", Refusal("0.9.0-rc.9", Release("0.9.0-rc.10"), "0.9.0-rc.8"));
            StringAssert.Contains("not the release's", Refusal("0.9.0-rc.9", Release("0.9.0-rc.10"), null));
            StringAssert.Contains("installed version", Refusal(null, Release("0.9.0-rc.10"), "0.9.0-rc.10"));
        }

        [Test]
        public void AFileThatChangedAfterItsCheckIsRefused()
        {
            var path = Path.Combine(Path.GetTempPath(), "wotr-install-guard-" + System.Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, "verified package");
            try
            {
                var sha256 = PackageDownloader.Hash(path);
                InstallGuard.CheckFile(path, sha256);
                File.WriteAllText(path, "swapped package");
                Assert.Throws<UpdateRejected>(() => InstallGuard.CheckFile(path, sha256));
                File.Delete(path);
                Assert.Throws<UpdateRejected>(() => InstallGuard.CheckFile(path, sha256));
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static string Refusal(string running, StagedRelease release, string packageVersion) =>
            Assert.Throws<UpdateRejected>(() => InstallGuard.CheckVersions(running, release, packageVersion)).Message;
    }
}
