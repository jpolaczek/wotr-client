using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class CorrespondenceClientTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public async Task MainMenuIgnoresRepliesFromOlderRefreshOrDisposedScreen(bool close)
        {
            var handler = new HeldReplies();
            using (var api = new GameApi("http://localhost", handler))
            {
                var screen = new MainMenuScreen("Player");
                var browser = new MainMenuBrowser(api, screen);
                browser.Refresh();
                await Until(() => handler.Requests.Count == 3);
                browser.Refresh();
                await Until(() => handler.Requests.Count == 6);
                handler.Reply(3, new List<RoomView>());
                handler.Reply(4, new PublicRoomListView());
                handler.Reply(5, Page("newer"));
                await Until(() => screen.Root.Q("saved-match-newer") != null);
                if (close) browser.Dispose();
                handler.Reply(0, new List<RoomView>());
                handler.Reply(1, new PublicRoomListView());
                handler.Reply(2, Page("older"));
                await Task.Delay(50);
                Assert.NotNull(screen.Root.Q("saved-match-newer"));
                Assert.IsNull(screen.Root.Q("saved-match-older"));
                if (!close) browser.Dispose();
            }
        }

        [Test]
        public async Task ReadPositionCoalescesOnlyDisplayedRevisionsAndNeverRegresses()
        {
            var handler = new HeldReplies();
            using (var api = new GameApi("http://localhost", handler))
            using (var seen = new MatchSeenTracker(api, "match"))
            {
                seen.Displayed(5);
                seen.Displayed(4);
                seen.Displayed(8);
                await Until(() => handler.Requests.Count == 1);
                Assert.AreEqual("/api/matches/match/seen", handler.Requests[0].Path);
                Assert.AreEqual("PUT", handler.Requests[0].Method);
                Assert.AreEqual(5, JObject.Parse(handler.Requests[0].Body)["Revision"].Value<long>());
                handler.Reply(0, new SeenRevisionView { LastSeenRevision = 5 });
                await Until(() => handler.Requests.Count == 2);
                Assert.AreEqual(8, JObject.Parse(handler.Requests[1].Body)["Revision"].Value<long>());
                handler.Reply(1, new SeenRevisionView { LastSeenRevision = 8 });
                await Task.Delay(50);
                seen.Displayed(7);
                await Task.Delay(50);
                Assert.AreEqual(2, handler.Requests.Count);
                Assert.AreEqual(1, JObject.Parse(handler.Requests[1].Body).Count, "Read positions never contain game state.");
            }
        }

        private static MatchListView Page(string id) => new MatchListView
        {
            Matches = new List<MatchSummary> { new MatchSummary { MatchId = id, Opponent = "Opponent" } }
        };

        private static async Task Until(Func<bool> condition)
        {
            using (var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                while (!condition())
                    await Task.Delay(5, deadline.Token);
        }

        private sealed class HeldReplies : HttpMessageHandler
        {
            public readonly List<Request> Requests = new List<Request>();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellation)
            {
                var request = new Request
                {
                    Path = message.RequestUri.AbsolutePath,
                    Method = message.Method.Method,
                    Body = message.Content == null ? "" : await message.Content.ReadAsStringAsync()
                };
                Requests.Add(request);
                return await request.Reply.Task;
            }

            public void Reply(int index, object value) => Requests[index].Reply.TrySetResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(value))
            });
        }

        private sealed class Request
        {
            public string Path, Method, Body;
            public readonly TaskCompletionSource<HttpResponseMessage> Reply = new TaskCompletionSource<HttpResponseMessage>();
        }
    }
}
