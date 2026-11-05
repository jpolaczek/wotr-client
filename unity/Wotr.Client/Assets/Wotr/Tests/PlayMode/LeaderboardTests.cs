using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-503 (docs/ui/RANKED_UX.md): the top 25 in the four layouts without scrolling, and read from the real server with the
    // seeder's rated accounts.
    public class LeaderboardLayoutTests
    {
        [UnityTest]
        public IEnumerator TwentyFiveRowsStandInAllFourLayoutsWithoutScrolling()
        {
            using (var client = new MountedFlowClient(null, null))
            {
                var content = client.Interface.Content;
                foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    content.Clear();
                    var menu = new MainMenuScreen("Player");
                    content.Add(menu.Root);
                    yield return Settle();
                    var windowSize = menu.Root.Q("menu-window").worldBound.size;
                    ReturnTestFixture.Click(menu.Root.Q<Button>("show-leaderboards"));
                    menu.Leaderboards.Show(Full());
                    yield return Settle();

                    Assert.AreEqual("Leaderboards", menu.Root.Q<Label>("menu-window-title").text);
                    Assert.AreEqual(windowSize, menu.Root.Q("menu-window").worldBound.size, "The window keeps its size.");
                    var page = menu.Root.Q("leaderboards");
                    Assert.IsNull(page.Q<ScrollView>(), "Nothing scrolls.");
                    Fits(menu.Root.Q<Button>("show-leaderboards"), menu.Root.Q("menu-actions"));
                    Fits(menu.Root.Q<Button>("quit-game"), menu.Root.Q("menu-actions"));
                    LayoutTests.Save(target, $"ui503-leaderboards-{width}-{scale:0.00}.png");
                    for (var rank = 1; rank <= LeaderboardPage.Size; rank++)
                    {
                        var row = page.Q("leaderboard-row-" + rank);
                        Fits(row, page);
                        var size = row.Q<Label>(className: "leaderboard-name").resolvedStyle.fontSize;
                        Assert.GreaterOrEqual(row.worldBound.height, size + 2, "Each row holds its text: " + rank);
                        foreach (var cell in row.Query<Label>().ToList().Where(label => !label.ClassListContains("leaderboard-name")))
                            TextFits(cell);
                    }
                    Assert.IsTrue(page.Q("leaderboard-row-3").ClassListContains(LeaderboardPage.OwnClass), "The player's own row is marked.");
                    Assert.IsFalse(page.Q("leaderboard-row-4").ClassListContains(LeaderboardPage.OwnClass));
                    Assert.AreEqual("Your place: 3 · Rating 2998", menu.Root.Q<Label>("leaderboard-you").text);
                    Fits(menu.Root.Q<Label>("leaderboard-you"), page);

                    // A failed read keeps what stood there.
                    menu.Leaderboards.ShowFailed();
                    yield return Settle();
                    Fits(menu.Root.Q<Label>("leaderboard-status"), page);
                    Assert.AreEqual(Visibility.Visible, page.Q("leaderboard-row-1").resolvedStyle.visibility);
                    LayoutTests.Save(target, $"ui503-failed-{width}-{scale:0.00}.png");

                    menu.Leaderboards.Show(new LeaderboardView());
                    yield return Settle();
                    Assert.AreEqual(LeaderboardPage.Empty, menu.Root.Q<Label>("leaderboard-empty").text);
                    Fits(menu.Root.Q<Label>("leaderboard-empty"), page);
                    Assert.AreEqual(LeaderboardPage.NoPlace, menu.Root.Q<Label>("leaderboard-you").text);
                    Assert.AreEqual("", menu.Root.Q<Label>("leaderboard-status").text, "A good read clears the failure.");
                    LayoutTests.Save(target, $"ui503-empty-{width}-{scale:0.00}.png");
                    target.Release();
                }
            }
        }

        // Numbers as wide as they get, and the longest names.
        private static LeaderboardView Full()
        {
            var view = new LeaderboardView();
            for (var rank = 1; rank <= LeaderboardPage.Size; rank++)
                view.Rows.Add(new LeaderboardRow
                {
                    Rank = rank, Name = "PlayerWithLongUsername" + rank.ToString("00") + "345678901", Rating = 3001 - rank, Games = 10000 - rank, Wins = 5000, Losses = 5000 - rank
                });
            view.You = view.Rows[2];
            return view;
        }

        private static void TextFits(Label label)
        {
            var needed = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(needed.x, label.contentRect.width + 1, label.text);
        }

        private static void Fits(VisualElement element, VisualElement bounds)
        {
            Assert.IsNotNull(element);
            Assert.Greater(element.worldBound.height, 0, element.name);
            Assert.GreaterOrEqual(element.worldBound.xMin, bounds.worldBound.xMin - 1, element.name);
            Assert.GreaterOrEqual(element.worldBound.yMin, bounds.worldBound.yMin - 1, element.name);
            Assert.LessOrEqual(element.worldBound.xMax, bounds.worldBound.xMax + 1, element.name);
            Assert.LessOrEqual(element.worldBound.yMax, bounds.worldBound.yMax + 1, element.name);
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 8; i++)
                yield return null;
        }
    }

    public class LeaderboardNetworkTests
    {
        [UnityTest]
        public IEnumerator TheTopPlayerSeesTheirRowMarkedAndALowPlayerTheirPlaceBelow()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG")))
                Assert.Ignore("Real HTTP fixture required.");
            var task = Run();
            while (!task.IsCompleted)
                yield return null;
            task.GetAwaiter().GetResult();
        }

        [UnityTest]
        public IEnumerator AFailedReadSaysSoKeepsTheListAndRefreshBringsItBack()
        {
            var task = Failure();
            while (!task.IsCompleted)
                yield return null;
            task.GetAwaiter().GetResult();
        }

        private static async Task Run()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var token = timeout.Token;
            var fixture = ReturnTestFixture.Load();

            var topApi = await SignedIn(fixture.Address, fixture.LeaderboardTopUser, fixture.Password, token);
            var served = await topApi.Get<LeaderboardView>("api/leaderboard", token);
            Assert.AreEqual(25, served.Rows.Count, "The seeder's accounts fill the board.");
            using (var top = new MountedFlowClient(topApi, Storage()))
            {
                await Open(top, token);
                var page = top.Root.Q("leaderboards");
                await ReturnTestFixture.Until(() => page.Q("leaderboard-row-1").Q<Label>(className: "leaderboard-name").text.Length > 0, token);
                for (var rank = 1; rank <= 25; rank++)
                {
                    var row = served.Rows[rank - 1];
                    var cells = page.Q("leaderboard-row-" + rank).Query<Label>().ToList().Select(label => label.text).ToList();
                    CollectionAssert.AreEqual(new[] { row.Rank.ToString(), row.Name, row.Rating.ToString(), row.Games.ToString(), row.Wins.ToString(), row.Losses.ToString() }, cells);
                }
                Assert.AreEqual(fixture.LeaderboardTopUser, served.Rows[0].Name);
                Assert.IsTrue(page.Q("leaderboard-row-1").ClassListContains(LeaderboardPage.OwnClass));
                Assert.IsFalse(page.Q("leaderboard-row-2").ClassListContains(LeaderboardPage.OwnClass));
                StringAssert.StartsWith("Your place: 1 · Rating ", top.Root.Q<Label>("leaderboard-you").text);
            }

            var lowApi = await SignedIn(fixture.Address, fixture.LeaderboardLowUser, fixture.Password, token);
            using (var low = new MountedFlowClient(lowApi, Storage()))
            {
                await Open(low, token);
                await ReturnTestFixture.Until(() => low.Root.Q<Label>("leaderboard-you").text.Length > 0, token);
                var you = await lowApi.Get<LeaderboardView>("api/leaderboard", token);
                Assert.Greater(you.You.Rank, 25);
                Assert.AreEqual($"Your place: {you.You.Rank} · Rating 800", low.Root.Q<Label>("leaderboard-you").text, "Their place stands below the list.");
                Assert.AreEqual(0, low.Root.Query(className: LeaderboardPage.OwnClass).ToList().Count, "No row is theirs.");
            }

            var fresh = new GameApi(fixture.Address);
            await TestAccounts.Register(fresh, "lb_" + Guid.NewGuid().ToString("N").Substring(0, 16), token);
            using (var newcomer = new MountedFlowClient(fresh, Storage()))
            {
                await Open(newcomer, token);
                await ReturnTestFixture.Until(() => newcomer.Root.Q<Label>("leaderboard-you").text.Length > 0, token);
                Assert.AreEqual(LeaderboardPage.NoPlace, newcomer.Root.Q<Label>("leaderboard-you").text);
            }
        }

        private static async Task Failure()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var token = timeout.Token;
            var fixture = ReturnTestFixture.Load();
            using var server = new RestartTestServer();
            await server.Start(token);
            var api = await SignedIn(server.Address, fixture.LeaderboardTopUser, fixture.Password, token);
            using var client = new MountedFlowClient(api, Storage());
            await Open(client, token);
            var firstRow = client.Root.Q("leaderboard-row-1");
            await ReturnTestFixture.Until(() => firstRow.Q<Label>(className: "leaderboard-name").text.Length > 0, token);
            var shown = firstRow.Q<Label>(className: "leaderboard-name").text;

            server.Stop();
            ReturnTestFixture.Click(client.Root.Q<Button>("refresh-games"));
            await ReturnTestFixture.Until(() => client.Root.Q<Label>("leaderboard-status").text == LeaderboardPage.Failed, token);
            Assert.AreEqual(shown, firstRow.Q<Label>(className: "leaderboard-name").text, "What was shown stays.");

            await server.Start(token);
            ReturnTestFixture.Click(client.Root.Q<Button>("refresh-games"));
            await ReturnTestFixture.Until(() => client.Root.Q<Label>("leaderboard-status").text.Length == 0, token);
            Assert.AreEqual(shown, firstRow.Q<Label>(className: "leaderboard-name").text);
        }

        private static async Task<GameApi> SignedIn(string address, string user, string password, CancellationToken token)
        {
            var api = new GameApi(address);
            await api.Login(user, password, token);
            return api;
        }

        private static async Task Open(MountedFlowClient client, CancellationToken token)
        {
            await ReturnTestFixture.ClickReady(client.Root, "show-leaderboards", token);
            await ReturnTestFixture.Until(() => client.Root.Q("leaderboards").resolvedStyle.display == DisplayStyle.Flex, token);
        }

        private static string Storage() => Path.Combine(Path.GetTempPath(), "wotr-board-" + Guid.NewGuid().ToString("N"));
    }
}
