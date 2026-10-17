using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    public class MenuBrowserNetworkTests
    {
        [UnityTest]
        public IEnumerator ScrollingLoadsRealPagesAndRefreshKeepsRowsAndOtherListsAfterFailure() => MatchTestRoom.Wait(Scroll());

        [UnityTest]
        public IEnumerator BackReturnsToMenuWhenTheLeaveRouteIsUnavailable() => MatchTestRoom.Wait(UnavailableBack());

        private static async Task<GameApi> Account(string address, CancellationToken token, HttpMessageHandler handler = null)
        {
            var api = new GameApi(address, handler);
            await TestAccounts.Register(api, "menu09_" + Guid.NewGuid().ToString("N").Substring(0, 16), token);
            return api;
        }

        private static async Task Scroll()
        {
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(100)))
            {
                var token = timeout.Token;
                var address = Load().Address;
                using (var host = await Account(address, token))
                {
                    var boundary = new MenuBoundary();
                    var guest = await Account(address, token, boundary);
                    var expectedMatches = new List<string>();
                    for (var i = 0; i < 5; i++)
                    {
                        var room = await host.Post<RoomView>("api/rooms", new CreateRoomRequest { Name = "Saved " + i, Visibility = RoomVisibility.Public }, token);
                        var path = "api/rooms/" + room.Id;
                        var joined = await guest.Post<RoomView>(path + "/join", null, token);
                        var ready = new RoomReadyRequest { ExpectedVersion = joined.Version, Ready = true };
                        await host.Put<RoomView>(path + "/ready", ready, token);
                        await guest.Put<RoomView>(path + "/ready", ready, token);
                        var started = await host.Post<RoomView>(path + "/start", new RoomRevisionRequest { ExpectedVersion = joined.Version }, token);
                        expectedMatches.Add(started.MatchId);
                    }
                    var expectedPublic = new List<string>();
                    for (var i = 0; i < 21; i++)
                    {
                        var room = await host.Post<RoomView>("api/rooms", new CreateRoomRequest { Name = "Scroll table " + i, Visibility = RoomVisibility.Public }, token);
                        expectedPublic.Add(room.Id);
                    }
                    var own = await guest.Post<RoomView>("api/rooms", new CreateRoomRequest { Name = "My waiting game" }, token);
                    var storage = Path.Combine(Path.GetTempPath(), "wotr-menu09-" + Guid.NewGuid().ToString("N"));
                    using (var client = new MountedFlowClient(guest, storage))
                    {
                        await Until(() => client.Root.Q<Button>("resume-match-" + expectedMatches.OrderBy(id => id, StringComparer.Ordinal).First()) != null, token);
                        await Until(() => client.Root.Query<Button>().ToList().Count(b => b.name.StartsWith("join-public-")) >= 20, token);
                        var publicScroll = client.Root.Q<ScrollView>("public-games-scroll");
                        var matchScroll = client.Root.Q<ScrollView>("my-games-scroll");
                        var firstPublicId = expectedPublic.OrderBy(id => id, StringComparer.Ordinal).First();
                        var firstMatchId = expectedMatches.OrderBy(id => id, StringComparer.Ordinal).First();
                        var publicRow = client.Root.Q<Button>("join-public-" + firstPublicId);
                        var matchRow = client.Root.Q<Button>("resume-match-" + firstMatchId);
                        await Until(() => publicScroll.verticalScroller.highValue > 0, token);
                        // A full first page fills its list; the next page waits until the player scrolls to it.
                        await Task.Delay(150, token);
                        Assert.AreEqual(0, boundary.LaterPublicPages);
                        publicScroll.verticalScroller.value = publicScroll.verticalScroller.highValue;
                        await Until(() => expectedPublic.All(id => client.Root.Q<Button>("join-public-" + id) != null), token);
                        await Task.Delay(150, token);
                        var offset = publicScroll.scrollOffset;
                        var before = boundary.PublicRequests;
                        await ClickReady(client.Root, "refresh-games", token);
                        await Until(() => boundary.PublicRequests >= before + 2, token);
                        await Task.Delay(200, token);
                        Assert.AreSame(publicRow, client.Root.Q<Button>("join-public-" + firstPublicId));
                        Assert.That(publicScroll.scrollOffset.y, Is.EqualTo(offset.y).Within(1));
                        Assert.IsTrue(expectedPublic.All(id => client.Root.Q<Button>("join-public-" + id) != null));
                        // MENU-11: My games is the other page of the same window.
                        await ClickReady(client.Root, "show-my-games", token);
                        await Until(() => matchRow.worldBound.height > 0, token);
                        if (matchScroll.verticalScroller.highValue > 0)
                            matchScroll.verticalScroller.value = matchScroll.verticalScroller.highValue;
                        await Until(() => expectedMatches.All(id => client.Root.Q<Button>("resume-match-" + id) != null), token);
                        await ClickReady(client.Root, "refresh-games", token);
                        await Task.Delay(200, token);
                        Assert.AreSame(matchRow, client.Root.Q<Button>("resume-match-" + firstMatchId));
                        Assert.IsTrue(expectedMatches.All(id => client.Root.Q<Button>("resume-match-" + id) != null));
                        boundary.FailPublic = true;
                        await ClickReady(client.Root, "refresh-games", token);
                        await Until(() => client.Root.Q<Label>("lobby-status").text.Contains("server needs a lobby update"), token);
                        Assert.IsNotNull(client.Root.Q<Button>("open-room-" + own.Id));
                        Assert.AreSame(matchRow, client.Root.Q<Button>("resume-match-" + firstMatchId));
                        Assert.IsNotNull(client.Root.Q<TextField>("table-invitation"));
                        boundary.FailPublic = false;
                        await ClickReady(client.Root, "refresh-games", token);
                        await Until(() => client.Root.Q<Label>("lobby-status").text.Length == 0, token);
                    }
                }
            }
        }

        private static async Task UnavailableBack()
        {
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
            {
                var token = timeout.Token;
                var boundary = new MenuBoundary { MissingLeaveRoute = true };
                var api = await Account(Load().Address, token, boundary);
                var room = await api.Post<RoomView>("api/rooms", new CreateRoomRequest { Name = "Unavailable route" }, token);
                var storage = Path.Combine(Path.GetTempPath(), "wotr-menu09-" + Guid.NewGuid().ToString("N"));
                using (var client = new MountedFlowClient(api, storage))
                {
                    await ClickReady(client.Root, "open-room-" + room.Id, token);
                    await ClickReady(client.Root, "waiting-back", token);
                    await Until(() => client.Root.Q("main-menu") != null, token);
                    Assert.AreEqual(1, boundary.MissingLeaveResponses);
                    // The unavailable endpoint could not release the seat; clean up via the real route.
                    boundary.MissingLeaveRoute = false;
                    await api.Post<object>("api/rooms/" + room.Id + "/leave", null, token);
                }
            }
        }

        // Faults use real HTTP 404s. All successful rows and cursors come from PostgreSQL.
        private sealed class MenuBoundary : DelegatingHandler
        {
            public bool FailPublic;
            public bool MissingLeaveRoute;
            public int MissingLeaveResponses;
            public int PublicRequests;
            public int LaterPublicPages;
            public MenuBoundary() : base(new HttpClientHandler { AllowAutoRedirect = false }) { }
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                var path = request.RequestUri.AbsolutePath;
                var builder = new UriBuilder(request.RequestUri);
                if (path == "/api/rooms/public")
                {
                    PublicRequests++;
                    if (!builder.Query.EndsWith("cursor=", StringComparison.Ordinal))
                        LaterPublicPages++;
                    if (FailPublic) builder.Path = "/api/rooms/unavailable-menu09";
                }
                if (path == "/api/matches" && !builder.Query.Contains("limit="))
                    builder.Query = builder.Query.TrimStart('?') + "&limit=4";
                if (MissingLeaveRoute && path.EndsWith("/leave", StringComparison.Ordinal))
                {
                    builder.Path += "-unavailable";
                    MissingLeaveResponses++;
                }
                request.RequestUri = builder.Uri;
                return await base.SendAsync(request, token);
            }
        }
    }
}
