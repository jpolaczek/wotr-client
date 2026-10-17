using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // OBS-03/OBS-05: an observer's board reads the observer's routes for the side shown, names the deciding side
    // in neutral words and the winner without Victory or Defeat; a change of side shows the same revision from the
    // other side, and an answer still on its way for the previous side cannot replace it (OB06).
    public class ObserverViewTests
    {
        [Test]
        public void PlayersAndObserversReadTheirOwnRoutes()
        {
            var player = MatchReadRoutes.Player("m1");
            Assert.AreEqual("api/matches/m1", player.View);
            Assert.AreEqual("api/matches/m1/actions?filter=All&limit=50", player.Path("/actions", "filter=All&limit=50"));
            var side = Side.FreePeoples;
            var observer = MatchReadRoutes.Observer("m1", () => side);
            Assert.AreEqual("api/observe/m1?perspective=FreePeoples", observer.View);
            Assert.AreEqual("api/observe/m1/updates", observer.Updates);
            side = Side.Shadow;
            Assert.AreEqual("api/observe/m1/battles/b?perspective=Shadow", observer.Path("/battles/b", ""));
            Assert.AreEqual("api/observe/m1/actions?filter=All&before=x&perspective=Shadow", observer.Path("/actions", "filter=All&before=x"));
        }

        [Test]
        public void AnObserverIsToldWhoWinsAndWhoDecides()
        {
            var view = new PlayerView { MatchId = "m", You = Side.FreePeoples, Winner = Side.Shadow, Victory = VictoryCondition.ShadowMilitary };
            Assert.AreEqual("DEFEAT", GameEndState.For(view).Title);
            var observed = GameEndState.For(view, true);
            Assert.AreEqual("SHADOW WINS", observed.Title);
            Assert.IsFalse(observed.Won);
            view.Winner = null;
            view.Phase = GamePhase.HuntAllocation;
            view.DecisionOwner = Side.FreePeoples;
            Assert.AreEqual("Waiting for Free Peoples", HuntPresentation.Owner(view), "No decision of their own on an observer's board.");
            var state = TurnConsoleState.For(view, new TurnDraft(), new ActionDieContext(), id => id);
            Assert.AreEqual("Free Peoples is deciding · Allocate Hunt dice",
                ObserverConsole.Hint(view, new DecisionView { Kind = DecisionKind.AllocateHunt }, state));
        }

        [Test]
        public void TheViewingBarOffersTheOtherSideOnly()
        {
            var bar = new ViewingBar();
            var chosen = new List<Side>();
            bar.PerspectiveChosen += chosen.Add;
            bar.Show(Side.FreePeoples);
            Assert.IsTrue(bar.Visible);
            Assert.IsTrue(bar.Root.Q<Button>("viewing-free").ClassListContains("selected"));
            Assert.IsFalse(bar.Root.Q<Button>("viewing-shadow").ClassListContains("selected"));
            Assert.AreEqual("OBSERVING · VIEWING", bar.Root.Q<Label>("viewing-title").text);
            var board = new BoardScreen();
            board.Observe(Side.Shadow);
            Assert.IsTrue(board.Observing);
            Assert.IsTrue(board.GameEnd.Observing);
            Assert.IsTrue(board.Viewing.Root.Q<Button>("viewing-shadow").ClassListContains("selected"));
        }

        [Test]
        public async Task TheSameRevisionShowsAgainFromTheOtherSideAndALateAnswerIsDropped()
        {
            using var handler = new Answers();
            using var api = new GameApi("https://example.invalid", handler);
            var side = Side.FreePeoples;
            using var connection = new MatchConnection(api, "m", MatchReadRoutes.Observer("m", () => side));
            var shown = new List<Side>();
            connection.Updated += view => shown.Add(view.You);
            var first = connection.Refresh();
            handler.Complete(0, View(Side.FreePeoples));
            await first;
            var late = connection.Refresh();
            side = Side.Shadow;
            connection.Rebase();
            await handler.Requested(3);
            handler.Complete(2, View(Side.Shadow));
            await Until(() => shown.Count == 2);
            handler.Complete(1, View(Side.FreePeoples));
            await late;
            CollectionAssert.AreEqual(new[] { Side.FreePeoples, Side.Shadow }, shown);
            StringAssert.EndsWith("perspective=FreePeoples", handler.Paths[1]);
            StringAssert.EndsWith("perspective=Shadow", handler.Paths[2]);
        }

        // OB08: the observer's session renews its place while it lives and stops at once when the board closes; the
        // board shows no decision of its own, and the console names the side that decides.
        [Test]
        public async Task PresenceStopsWithTheSessionAndTheBoardHoldsNoDecision()
        {
            using var handler = new Server();
            using var api = new GameApi("https://example.invalid", handler);
            var board = new BoardScreen();
            var room = new RoomView { Id = "r", MatchId = "m", Role = RoomRole.Observer, Status = RoomStatus.Started };
            var session = new ObserverSession(api, room, board, System.TimeSpan.FromMilliseconds(30));
            await Until(() => board.View != null && handler.Presence >= 2);
            Assert.IsNull(board.View.Decision);
            Assert.AreEqual("Free Peoples is deciding · Fellowship phase", board.Console.Status.Hint.text);
            session.Dispose();
            var renewed = handler.Presence;
            await Task.Delay(200);
            Assert.AreEqual(renewed, handler.Presence, "No presence after the board closed.");
        }

        private static PlayerView View(Side side) => new PlayerView { MatchId = "m", Revision = 4, You = side };

        private static async Task Until(System.Func<bool> condition)
        {
            for (var i = 0; i < 200 && !condition(); i++)
                await Task.Delay(10);
            Assert.IsTrue(condition());
        }

        // The observer's view of a match where the Free Peoples decide, and presence renewals counted.
        private sealed class Server : HttpMessageHandler
        {
            private int presence;
            public int Presence => Volatile.Read(ref presence);

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                var path = request.RequestUri.AbsolutePath;
                if (path.EndsWith("/presence"))
                {
                    Interlocked.Increment(ref presence);
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent) { Content = new StringContent("") });
                }
                if (path == "/api/observe/m")
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(new PlayerView
                    {
                        MatchId = "m", Revision = 3, You = Side.FreePeoples, Phase = GamePhase.Fellowship, DecisionOwner = Side.FreePeoples,
                        Decision = new DecisionView { Kind = DecisionKind.FellowshipPhase }
                    })) });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("") });
            }
        }

        // Answers each request only when the test says so, and remembers the routes asked.
        private sealed class Answers : HttpMessageHandler
        {
            private readonly List<TaskCompletionSource<HttpResponseMessage>> pending = new List<TaskCompletionSource<HttpResponseMessage>>();
            public readonly List<string> Paths = new List<string>();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                var answer = new TaskCompletionSource<HttpResponseMessage>();
                lock (pending)
                {
                    pending.Add(answer);
                    Paths.Add(request.RequestUri.PathAndQuery);
                }
                return answer.Task;
            }

            public async Task Requested(int count)
            {
                for (var i = 0; i < 200; i++)
                {
                    lock (pending)
                        if (pending.Count >= count)
                            return;
                    await Task.Delay(10);
                }
                Assert.Fail("Expected " + count + " requests.");
            }

            public void Complete(int index, object data)
            {
                lock (pending)
                    pending[index].SetResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(data)) });
            }
        }
    }
}
