using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    public class ReturnListTests
    {
        [UnityTest]
        public IEnumerator SavedGamesScrollAndChangesOnlyAcknowledgeDisplayedBoard() => MatchTestRoom.Wait(List());

        [UnityTest]
        public IEnumerator BothPlayersReturnToEverySaveFamilyWithCurrentPrivateViewAndNoReplay() => MatchTestRoom.Wait(Families());

        private static async Task List()
        {
            var fixture = Load();
            var storage = Path.Combine(Path.GetTempPath(), "wotr-list-" + Guid.NewGuid().ToString("N"));
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(80)))
            {
                var token = timeout.Token;
                var api = await fixture.Login(Side.FreePeoples, token);
                try
                {
                    using (var client = new MountedFlowClient(api, storage))
                    {
                        await Until(() => client.Root.Query(className: "saved-match").ToList().Count == 20, token);
                        await ClickReady(client.Root, "show-my-games", token);
                        var first = client.Root.Q(className: "saved-match");
                        var firstId = first.name.Substring("saved-match-".Length);
                        var page = await api.Get<MatchListView>("api/matches", token);
                        Click(client.Root.Query<Button>().ToList().Single(b => b.name is "refresh-lobby" or "refresh-games" &&
                            b.resolvedStyle.display != DisplayStyle.None && b.panel != null));
                        await Task.Delay(200, token);
                        Assert.AreSame(first, client.Root.Q(className: "saved-match"), "Refreshing must retain the row controls.");
                        Assert.IsNotEmpty(page.NextCursor, "The fixture must exercise real pagination.");
                        var nextPage = await api.Get<MatchListView>("api/matches?cursor=" + Uri.EscapeDataString(page.NextCursor), token);
                        Assert.IsNotEmpty(nextPage.Matches);
                        var scroll = client.Root.Q<ScrollView>("my-games-scroll");
                        await Until(() => scroll.verticalScroller.highValue > 0, token);
                        scroll.verticalScroller.value = scroll.verticalScroller.highValue;
                        await Until(() => nextPage.Matches.All(match => client.Root.Q("saved-match-" + match.MatchId) != null), token);
                        Assert.AreSame(first, client.Root.Q("saved-match-" + firstId), "Scrolling appends rows instead of replacing the first page.");
                        Assert.IsNull(client.Root.Q("next-matches"));
                        Assert.IsNull(client.Root.Q("previous-matches"));
                        foreach (var filter in new[] { MatchListFilter.Waiting, MatchListFilter.Finished, MatchListFilter.YourDecision, MatchListFilter.All })
                            Assert.IsNull(client.Root.Q("filter-" + filter));
                        var selected = fixture.ReturnMatches["waiting-0"];
                        while (client.Root.Q<Button>("resume-match-" + selected) == null)
                        {
                            scroll.verticalScroller.value = scroll.verticalScroller.highValue;
                            await Task.Delay(100, token);
                        }
                        // MENU-13: the row itself resumes the game; there is no separate recent-changes page.
                        Assert.IsNull(client.Root.Q<Button>("changes-match-" + selected));
                        await ClickReady(client.Root, "resume-match-" + selected, token);
                        await Until(() => client.Board?.View?.MatchId == selected, token);
                        MatchActivityView activity;
                        do
                        {
                            activity = await api.Get<MatchActivityView>($"api/matches/{selected}/activity", token);
                            await Task.Delay(25, token);
                        } while (activity.LastSeenRevision < client.Board.View.Revision);
                        Assert.AreEqual(client.Board.View.Revision, activity.LastSeenRevision);
                        client.Flow.Dispose();
                        await client.LocalWrites;
                    }
                }
                finally { if (Directory.Exists(storage)) Directory.Delete(storage, true); }
            }
        }

        private static async Task Families()
        {
            var fixture = Load();
            var storage = Path.Combine(Path.GetTempPath(), "wotr-families-" + Guid.NewGuid().ToString("N"));
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(140)))
            {
                var token = timeout.Token;
                try
                {
                    foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                    {
                        var api = await fixture.Login(side, token);
                        using (var client = new MountedFlowClient(api, Path.Combine(storage, side.ToString())))
                        {
                            foreach (var name in new[] { "ring-paid", "secret-combat", "no-regular", "sh-str02-corsairs-relief",
                                "boundary-random-companion", "reveal", "another-way", "council", "corruption", "ring-destroyed" })
                            {
                                var id = fixture.ReturnMatches[name];
                                var current = await api.Get<PlayerView>($"api/matches/{id}", token);
                                await Open(client, id, token);
                                Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(current), JToken.FromObject(client.Board.View)), name + "/" + side);
                                if (name == "ring-destroyed")
                                    await InspectRingResult(client, token);
                                else
                                    Assert.IsNotEmpty(client.Board.Tray.Query<Label>().ToList(), name);
                                Assert.AreEqual(current.Decision?.Kind, client.Board.View.Decision?.Kind);
                                Assert.IsEmpty(client.Board.Tray.Query<ScrollView>().ToList(), name);
                                Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList(), name);
                                var own = current.Decision != null;
                                Assert.AreEqual(own ? DisplayStyle.Flex : DisplayStyle.None, client.Board.Notice.Button.resolvedStyle.display);
                                if (own)
                                {
                                    var camera = client.Board.Map.Camera;
                                    camera.Pan = new Vector2(-301, -48);
                                    camera.Zoom = 1.2f;
                                    client.Board.Map.Refresh();
                                    var attention = DecisionAttention.For(current);
                                    Click(client.Board.Notice.Button);
                                    if (attention.Region.Length == 0 || attention.Context is DecisionContext.Fellowship or DecisionContext.Hunt or DecisionContext.Sovereign)
                                    {
                                        Assert.AreEqual(new Vector2(-301, -48), camera.Pan, "Panel navigation preserves the camera: " + name);
                                        Assert.AreEqual(1.2f, camera.Zoom);
                                    }
                                    else
                                        Assert.AreEqual(attention.Region, client.Board.Map.Selected, name);
                                    if (attention.Context == DecisionContext.Battle)
                                        Assert.IsTrue(client.Board.Battles.IsOpen);
                                    if (attention.Context == DecisionContext.Fellowship)
                                        Assert.IsTrue(client.Board.Fellowship.IsOpen);
                                    if (attention.Context == DecisionContext.Hunt)
                                        Assert.IsTrue(client.Board.Hunt.IsOpen);
                                    if (attention.Context == DecisionContext.Sovereign)
                                        await InspectSovereign(client, attention, token);
                                }
                                await Task.Delay(60, token);
                                Assert.AreEqual(current.Revision, (await api.Get<PlayerView>($"api/matches/{id}", token)).Revision,
                                    "Opening a saved decision must not submit it: " + name);
                                await ClickReady(client.Root, "back-to-games", token);
                            }
                            client.Flow.Dispose();
                            await client.LocalWrites;
                        }
                    }
                }
                finally { if (Directory.Exists(storage)) Directory.Delete(storage, true); }
            }
        }

        private static async Task InspectSovereign(MountedFlowClient client, DecisionAttention attention, CancellationToken token)
        {
            var board = client.Board;
            // UI-274: the king's decision opens the Characters panel, where his portrait and tiles make it.
            Assert.AreEqual(DisplayStyle.Flex, board.Characters.Root.resolvedStyle.display);
            Assert.IsNotNull(board.Sovereigns.Root.Q("sovereign-slot-" + attention.Subject));
            Assert.IsFalse(board.DecisionWindow.IsOpen);
            var decision = board.View.Decision.Id;
            board.Sovereigns.Show(attention.Subject);
            var location = board.Sovereigns.Inspector.Q<Button>("sovereign-location");
            await Until(() => location?.enabledInHierarchy == true && location.worldBound.height > 0, token);
            await Task.Delay(80, token);
            PointerGestureEvents.ClickAt(location);
            await Task.Delay(80, token);
            Assert.AreEqual(attention.Region, board.Map.Selected, "Only the explicit Locate click moves to the Sovereign.");
            Assert.AreEqual(decision, board.View.Decision.Id);
        }

        private static async Task InspectRingResult(MountedFlowClient client, CancellationToken token)
        {
            var board = client.Board;
            Assert.AreEqual(Side.FreePeoples, board.View.Winner);
            Assert.AreEqual(VictoryCondition.RingDestroyed, board.View.Victory);
            Assert.AreEqual("Free Peoples win · Ring destroyed", board.Victory.Banner.text);
            Assert.AreEqual(DisplayStyle.Flex, board.Victory.Banner.resolvedStyle.display);
            Assert.IsEmpty(board.View.Options);
            await BoardNavigation.Victory(board);
            await Task.Delay(80, token);
            Assert.IsTrue(board.Victory.IsOpen, "The saved final result remains inspectable.");
            PointerGestureEvents.ClickAt(board.Victory.Root.Q<Button>("close-victory"));
            await Task.Delay(80, token);
            Assert.IsFalse(board.Victory.IsOpen);
            Assert.AreEqual(DisplayStyle.Flex, board.Victory.Banner.resolvedStyle.display);
        }
    }
}
