using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-502 (docs/ui/RANKED_UX.md): the end of a ranked game names why, and what it did to each player's rating. Against the
    // real server, whose clocks run at a tenth (scripts/test-client.sh).
    public class RankedEndNetworkTests
    {
        private const string Queue = RankedSearchController.Route;

        [UnityTest]
        public IEnumerator AConcessionNamesItselfOnBothSidesWithEachPlayersOwnRating() => Real(async (address, token) =>
        {
            var (a, b, match) = await Paired(address, RankedChoice.Enforce, RankedChoice.Off, token);
            using var first = new MountedFlowClient(a, Storage());
            using var second = new MountedFlowClient(b, Storage());
            await ReturnTestFixture.Until(() => first.Board?.View?.MatchId == match && second.Board?.View?.MatchId == match, token);

            await a.Post<CommandReceipt>($"api/matches/{match}/concede", new ConcedeRequest { CommandId = Guid.NewGuid().ToString("N") }, token);
            await ReturnTestFixture.Until(() => first.Board.GameEnd.IsOpen && second.Board.GameEnd.IsOpen, token);

            StringAssert.EndsWith("You conceded", first.Root.Q<Label>("game-end-subtitle").text);
            StringAssert.EndsWith("Your opponent conceded", second.Root.Q<Label>("game-end-subtitle").text);
            await SameAsTheServer(first, a, match, token);
            await SameAsTheServer(second, b, match, token);
            var lost = await a.Get<RankedResultView>($"api/matches/{match}/rating", token);
            var won = await b.Get<RankedResultView>($"api/matches/{match}/rating", token);
            Assert.Less(lost.Change, 0);
            Assert.Greater(won.Change, 0);
        });

        [UnityTest]
        public IEnumerator RunningOutOfTimeNamesItselfOnBothSidesWithEachPlayersOwnRating() => Real(async (address, token) =>
        {
            var (a, b, match) = await Paired(address, RankedChoice.Off, RankedChoice.Enforce, token);
            using var first = new MountedFlowClient(a, Storage());
            using var second = new MountedFlowClient(b, Storage());
            await ReturnTestFixture.Until(() => first.Board?.View?.MatchId == match && second.Board?.View?.MatchId == match, token);

            await ReturnTestFixture.Until(() => first.Board.GameEnd.IsOpen && second.Board.GameEnd.IsOpen, token);

            var owner = first.Board.View.Winner == first.Board.View.You ? second : first;
            var other = owner == first ? second : first;
            StringAssert.EndsWith("You ran out of time", owner.Root.Q<Label>("game-end-subtitle").text);
            StringAssert.EndsWith("Your opponent ran out of time", other.Root.Q<Label>("game-end-subtitle").text);
            await SameAsTheServer(first, a, match, token);
            await SameAsTheServer(second, b, match, token);
        });

        [UnityTest]
        public IEnumerator MyGamesListsThePlayersOwnWords() => Real(async (address, token) =>
        {
            var (a, _, match) = await Paired(address, RankedChoice.Enforce, RankedChoice.Off, token);
            await a.Post<CommandReceipt>($"api/matches/{match}/concede", new ConcedeRequest { CommandId = Guid.NewGuid().ToString("N") }, token);
            var finished = await a.Get<MatchListView>("api/matches?status=Finished", token);
            var row = finished.Matches.Find(game => game.MatchId == match);
            StringAssert.Contains("You conceded", SavedMatchRow.Status(row));
            StringAssert.StartsWith("Defeat", SavedMatchRow.Status(row));
        });

        private static async Task SameAsTheServer(MountedFlowClient client, GameApi api, string match, CancellationToken token)
        {
            var served = await api.Get<RankedResultView>($"api/matches/{match}/rating", token);
            Assert.AreEqual(served.RatingAfter, served.RatingBefore + served.Change, "The numbers add up.");
            await ReturnTestFixture.Until(() => client.Root.Q<Label>("game-end-rating") != null, token);
            Assert.AreEqual($"Rating {served.RatingBefore} → {served.RatingAfter} ({(served.Change >= 0 ? "+" : "")}{served.Change})",
                client.Root.Q<Label>("game-end-rating").text);
        }

        private static IEnumerator Real(Func<string, CancellationToken, Task> test)
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Real HTTP fixture required.");
            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var task = test(JObject.Parse(File.ReadAllText(path))["Address"].Value<string>(), timeout.Token);
            while (!task.IsCompleted)
                yield return null;
            timeout.Dispose();
            task.GetAwaiter().GetResult();
        }

        // Two fresh accounts searching through the API until the server pairs them.
        private static async Task<(GameApi, GameApi, string)> Paired(string address, RankedChoice longest, RankedChoice fastest, CancellationToken token)
        {
            var a = await Account(address, "re_a_", token);
            var b = await Account(address, "re_b_", token);
            var search = new RankedSearchRequest
            {
                Lords = RankedChoice.Off, Kings = RankedChoice.Off, Fate = RankedChoice.Off, Standard = RankedChoice.Off, Long = longest, Fast = fastest
            };
            await a.Post<RankedSearchView>(Queue, search, token);
            await b.Post<RankedSearchView>(Queue, search, token);
            while (true)
            {
                var view = await a.Get<RankedSearchView>(Queue, token);
                await b.Get<RankedSearchView>(Queue, token);
                if (view.Status == RankedSearchStatus.Matched)
                    return (a, b, view.MatchId);
                await Task.Delay(200, token);
            }
        }

        private static async Task<GameApi> Account(string address, string prefix, CancellationToken token)
        {
            var api = new GameApi(address);
            await TestAccounts.Register(api, prefix + Guid.NewGuid().ToString("N").Substring(0, 14), token);
            return api;
        }

        private static string Storage() => Path.Combine(Path.GetTempPath(), "wotr-end-" + Guid.NewGuid().ToString("N"));
    }

    // UI-502: the end screen with its rating line in the four layouts, and without one as it always was.
    public class GameEndRatingLayoutTests
    {
        [UnityTest]
        public IEnumerator TheRatingLineFitsAndANormalGameHasNoRoomForOne()
        {
            using var client = new MountedMatchClient("http://localhost", "game-end-rating");
            var view = new BoardFixture().View;
            view.Winner = Side.Shadow;
            view.Victory = VictoryCondition.TimedOut;
            view.You = Side.FreePeoples;
            client.Apply(view);
            var end = client.Board.GameEnd;
            var window = end.Root.Q("game-end-window");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    client.Panel.targetTexture = texture;
                    client.Interface.Preferences.SetScale(scale);
                    end.Root.Q("game-end-rating")?.RemoveFromHierarchy();
                    yield return Settle();
                    Assert.IsTrue(end.IsOpen);
                    var normalHeight = window.worldBound.height;
                    Assert.IsNull(end.Root.Q("game-end-rating"), "A normal game's screen holds no such line.");
                    end.ShowRating(new RankedResultView { RatingBefore = 1532, RatingAfter = 1547, Change = 15 });
                    yield return Settle();
                    var rating = end.Root.Q<Label>("game-end-rating");
                    Assert.Less(end.Root.Q("game-end-subtitle").worldBound.yMax, rating.worldBound.yMax, "It stands under the reason.");
                    Assert.AreEqual("Rating 1532 → 1547 (+15)", rating.text);
                    Assert.Greater(window.worldBound.height, normalHeight);
                    Inside(rating, window);
                    Inside(window, client.Board.Root);
                    foreach (var button in end.Root.Query<Button>().ToList())
                        Inside(button, window);
                    Inside(end.Root.Q<Label>("game-end-replay-status"), window);
                    LayoutTests.Save(texture, $"ui502-end-{width}-{scale:0.00}.png");
                    end.ShowRating(new RankedResultView { RatingBefore = 1500, RatingAfter = 1338, Change = -162 });
                    Assert.AreEqual("Rating 1500 → 1338 (-162)", rating.text);
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    UnityEngine.Object.Destroy(texture);
                }
        }

        private static void Inside(VisualElement element, VisualElement bounds)
        {
            Assert.Greater(element.worldBound.height, 0, element.name);
            Assert.GreaterOrEqual(element.worldBound.yMin, bounds.worldBound.yMin - 1, element.name);
            Assert.LessOrEqual(element.worldBound.yMax, bounds.worldBound.yMax + 1, element.name);
            Assert.GreaterOrEqual(element.worldBound.xMin, bounds.worldBound.xMin - 1, element.name);
            Assert.LessOrEqual(element.worldBound.xMax, bounds.worldBound.xMax + 1, element.name);
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 6; frame++)
                yield return null;
        }
    }
}
