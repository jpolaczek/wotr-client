using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Wotr.Client.Tests
{
    public sealed class NotificationReadTests
    {
        private string root;
        [SetUp] public void SetUp() => root = Path.Combine(Path.GetTempPath(), "wotr-read-" + Guid.NewGuid().ToString("N"));
        [TearDown] public void TearDown() { if (Directory.Exists(root)) Directory.Delete(root, true); }
        private LocalMatchFiles Files(string server = "https://localhost/", string account = "one", string match = "match") =>
            new LocalMatchFiles(root, server, account, match);

        [Test]
        public async Task RestartRestoresOnlyThisAccountServerAndMatchAndStoresNoGameData()
        {
            var store = await NotificationReadStore.Open(Files());
            store.Acknowledge(new[] { "step1", "step2" });
            await store.Flushed;
            CollectionAssert.AreEquivalent(store.Steps, (await NotificationReadStore.Open(Files())).Steps);
            Assert.IsEmpty((await NotificationReadStore.Open(Files(account: "two"))).Steps);
            Assert.IsEmpty((await NotificationReadStore.Open(Files(server: "https://another/"))).Steps);
            Assert.IsEmpty((await NotificationReadStore.Open(Files(match: "other-match"))).Steps);
            var path = Directory.GetFiles(root, "notification-read.json", SearchOption.AllDirectories).Single();
            var text = File.ReadAllText(path);
            Assert.Less(text.Length, 300);
            foreach (var forbidden in new[] { "PlayerView", "Password", "Token", "Hand", "Command", "Movements" })
                StringAssert.DoesNotContain(forbidden, text);
        }

        [Test]
        public async Task ConcurrentLocalInstancesMergeMonotonicReadIds()
        {
            var first = await NotificationReadStore.Open(Files());
            var second = await NotificationReadStore.Open(Files());
            first.Acknowledge(new[] { "a" });
            second.Acknowledge(new[] { "b" });
            first.Acknowledge(new[] { "c" });
            await Task.WhenAll(first.Flushed, second.Flushed);
            CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, (await NotificationReadStore.Open(Files())).Steps);
        }

        [Test]
        public async Task CorruptOrUnavailableStorageNeverPretendsThatUpdatesWereRead()
        {
            var store = await NotificationReadStore.Open(Files());
            store.Acknowledge(new[] { "before" });
            await store.Flushed;
            var path = Directory.GetFiles(root, "notification-read.json", SearchOption.AllDirectories).Single();
            File.WriteAllText(path, "{ broken");
            store = await NotificationReadStore.Open(Files());
            Assert.IsEmpty(store.Steps);
            Assert.IsNotEmpty(store.Error);
            store.Acknowledge(new[] { "actually-displayed" });
            await store.Flushed;
            CollectionAssert.AreEqual(new[] { "actually-displayed" }, (await NotificationReadStore.Open(Files())).Steps);
            var unavailable = Path.Combine(root, "not-a-directory");
            File.WriteAllText(unavailable, "blocked");
            store = await NotificationReadStore.Open(new LocalMatchFiles(unavailable, "https://localhost/", "one", "match"));
            Assert.IsEmpty(store.Steps);
            Assert.IsNotEmpty(store.Error);
            store.Acknowledge(new[] { "shown-this-session" });
            await store.Flushed;
            Assert.IsNotEmpty(store.Error);
            Assert.Contains("shown-this-session", store.Steps.ToArray());
        }
    }
}
