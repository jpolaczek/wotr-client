using System;
using System.IO;
using NUnit.Framework;
using Wotr.Client.Updates;

namespace Wotr.Client.Tests
{
    // SEC-01: `--wotr-self-check` writes only a new .json report at an absolute path in an existing folder.
    public sealed class InstallationSelfCheckTests
    {
        private string folder;

        [SetUp]
        public void CreateFolder()
        {
            folder = Path.Combine(Path.GetTempPath(), "wotr-self-check-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        [TearDown]
        public void RemoveFolder() => Directory.Delete(folder, true);

        [Test]
        public void TheSmokeTestsReportIsAccepted()
        {
            var report = Path.Combine(folder, "self-check-release.json");
            Assert.AreEqual("", InstallationSelfCheck.Problem(report));
            InstallationSelfCheck.Write(report, "{}");
            Assert.AreEqual("{}", File.ReadAllText(report));
        }

        [Test]
        public void OtherPathsAreRefusedAndNothingIsOverwritten()
        {
            var existing = Path.Combine(folder, "existing.json");
            File.WriteAllText(existing, "keep");
            Directory.CreateDirectory(Path.Combine(folder, "folder.json"));

            StringAssert.Contains("absolute", InstallationSelfCheck.Problem("self-check-release.json"));
            StringAssert.Contains("absolute", InstallationSelfCheck.Problem(""));
            StringAssert.Contains(".json", InstallationSelfCheck.Problem(Path.Combine(folder, "report.txt")));
            StringAssert.Contains("folder", InstallationSelfCheck.Problem(Path.Combine(folder, "missing", "report.json")));
            StringAssert.Contains("exists", InstallationSelfCheck.Problem(existing));
            StringAssert.Contains("exists", InstallationSelfCheck.Problem(Path.Combine(folder, "folder.json")));
            Assert.Throws<IOException>(() => InstallationSelfCheck.Write(existing, "{}"));
            Assert.AreEqual("keep", File.ReadAllText(existing));
        }
    }
}
