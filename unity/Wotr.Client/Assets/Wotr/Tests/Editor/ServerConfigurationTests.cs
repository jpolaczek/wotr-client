using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Client.Updates;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // MENU-20/MENU-28: the hosts come from outside git — the environment first, then wotr-client/.env — and a build
    // carries them in its own configuration file; a test can point the client at its own server.
    public sealed class ServerConfigurationTests
    {
        private string folder;

        [SetUp]
        public void Create() => folder = Path.Combine(Path.GetTempPath(), "wotr-env-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void Restore()
        {
            ServerConfiguration.Use(null);
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        [Test]
        public void TheEnvFileNamesTheHostsAndTheEnvironmentWins()
        {
            var path = Write("# hosts of this machine\n\nWOTR_SERVER_URL=https://game.example.org\nexport WOTR_DOWNLOADS_URL=\"https://downloads.example.org:50443/\"\n");
            var environment = new Dictionary<string, string> { [ClientEnvironment.ServerKey] = " https://other.example.org " };

            var fromFile = ClientEnvironment.Read(path, _ => null);
            var overridden = ClientEnvironment.Read(path, key => environment.TryGetValue(key, out var value) ? value : null);

            Assert.AreEqual("https://game.example.org", fromFile.Player);
            Assert.AreEqual("https://downloads.example.org:50443/", fromFile.Downloads);
            Assert.AreEqual(ClientEnvironment.LocalServer, fromFile.Editor, "The editor uses the local server unless set.");
            Assert.AreEqual("https://other.example.org", overridden.Player);
        }

        [Test]
        public void WithoutAFileNothingIsAssumedButTheLocalEditorServer()
        {
            var addresses = ClientEnvironment.Read(Path.Combine(folder, ".env"), _ => null);
            Assert.AreEqual("", addresses.Player);
            Assert.AreEqual("", addresses.Downloads);
            Assert.AreEqual(ClientEnvironment.LocalServer, addresses.Editor);
        }

        // DIST-22 (owner, 2026-11-22): the downloads address is optional; only the server is needed.
        [Test]
        public void ABuildsFileNeedsTheServerAndMayHaveNoDownloadsAddress()
        {
            var addresses = ServerConfiguration.Parse("{\"player\":\"https://game.example.org\",\"downloads\":\"https://downloads.example.org/\"}");
            Assert.AreEqual("https://game.example.org", addresses.Player);
            Assert.AreEqual(ClientEnvironment.LocalServer, addresses.Editor);
            var serverOnly = ServerConfiguration.Parse("{\"player\":\"https://game.example.org\"}");
            Assert.AreEqual("https://game.example.org", serverOnly.Player);
            Assert.AreEqual("", serverOnly.Downloads, "No downloads address: no updates.");
            foreach (var json in new[] { "{}", "{\"player\":\" \",\"downloads\":\"https://downloads.example.org/\"}" })
                Assert.Throws<InvalidOperationException>(() => ServerConfiguration.Parse(json), json);
        }

        // DIST-22: without a downloads address a refused version says the game cannot update itself; with one, it says
        // to install the latest version, as before.
        [Test]
        public void ARefusedVersionSaysWhetherThisCopyCanUpdate()
        {
            StringAssert.Contains(VersionRefusal.CannotUpdate, VersionRefusal.Text(ClientCompatibility.ClientUpdateRequired, false));
            StringAssert.DoesNotContain(VersionRefusal.CannotUpdate, VersionRefusal.Text(ClientCompatibility.ClientUpdateRequired, true));
            StringAssert.Contains("cannot download or install updates", VersionRefusal.Board(false));
            StringAssert.DoesNotContain("cannot download", VersionRefusal.Board(true));
            Assert.AreEqual(VersionRefusal.Text(ClientCompatibility.ServerUpdateRequired, true),
                VersionRefusal.Text(ClientCompatibility.ServerUpdateRequired, false), "A server that needs an update is not about this copy.");
        }

        [Test]
        public void UpdatesTrustThePublishersKeyAndTheConfiguredOrigin()
        {
            var trust = ReleaseTrust.ForOrigin("https://downloads.example.org:50443/");
            Assert.AreEqual(32, trust.PublicKey.Length);
            Assert.AreEqual(ReleasePublisher.KeyId, trust.KeyId);
            Assert.AreEqual("https://downloads.example.org:50443/", trust.DownloadsOrigin);
            Assert.Throws<ArgumentException>(() => ReleaseTrust.ForOrigin(""), "No origin, no updates.");
        }

        [Test]
        public void ATestPointsTheClientAtItsOwnServerAndBack()
        {
            ServerConfiguration.Use("  http://127.0.0.1:5098 ");
            Assert.AreEqual("http://127.0.0.1:5098", ServerConfiguration.Address);
            ServerConfiguration.Use(null);
            Assert.AreEqual(ServerConfiguration.For(editor: true), ServerConfiguration.Address, "Edit mode runs in the editor.");
        }

        private string Write(string text)
        {
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, ".env");
            File.WriteAllText(path, text);
            return path;
        }
    }
}
