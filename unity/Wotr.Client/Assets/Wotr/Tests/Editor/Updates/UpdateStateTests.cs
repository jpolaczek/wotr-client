using System;
using System.IO;
using NUnit.Framework;
using Wotr.Client.Updates;

namespace Wotr.Client.Tests
{
    // DIST-07: consent, the ready package and the server clock survive restarts and never mix releases.
    public class UpdateStateTests
    {
        private string folder;

        [SetUp]
        public void Create() => folder = Path.Combine(Path.GetTempPath(), "wotr-update-state-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void Remove()
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        [Test]
        public void StateSurvivesARestartAndBrokenFilesStartEmpty()
        {
            var store = new UpdateStore(folder);
            Assert.AreEqual("", store.Load().ConsentedVersion);
            store.Save(new UpdateState { ConsentedVersion = "0.9.1", ReadyVersion = "0.9.1", ReadyPackage = "p", ReadyIsRepair = true });
            store.Save(new UpdateState { ConsentedVersion = "0.9.2" });
            Assert.AreEqual("0.9.2", new UpdateStore(folder).Load().ConsentedVersion);
            File.WriteAllText(Path.Combine(folder, "state.json"), "{ broken");
            Assert.AreEqual("", new UpdateStore(folder).Load().ReadyVersion);
        }

        [Test]
        public void OnlyTheKeptReleaseFolderRemains()
        {
            Directory.CreateDirectory(Path.Combine(folder, "0.9.1"));
            Directory.CreateDirectory(Path.Combine(folder, "0.9.2"));
            new UpdateStore(folder).RemoveOtherDownloads("0.9.2");
            CollectionAssert.AreEqual(new[] { "0.9.2" }, Array.ConvertAll(Directory.GetDirectories(folder), Path.GetFileName));
        }

        [Test]
        public void ServerClockFollowsTheCatalogTime()
        {
            var local = new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero);
            var clock = new ServerClock(() => local);
            Assert.AreEqual(local, clock.Now);
            clock.Observe(local.AddMinutes(7));
            local = local.AddMinutes(1);
            Assert.AreEqual(local.AddMinutes(7), clock.Now);
        }

        [Test]
        public void SecondInstanceOfThisProcessNameIsRefusedUntilTheFirstEnds()
        {
            var pid = Path.Combine(folder, "instance.pid");
            using (var first = SingleInstanceLock.TryAcquire(folder))
            {
                Assert.IsNotNull(first);
                Assert.IsTrue(File.Exists(pid));
            }
            Assert.IsFalse(File.Exists(pid));
            File.WriteAllText(pid, "999999999");
            using (var afterStale = SingleInstanceLock.TryAcquire(folder))
                Assert.IsNotNull(afterStale, "A process id that no longer runs does not block the start.");
        }
    }
}
