using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class VictoryPanelNetworkTests
    {
        [UnityTest]
        public IEnumerator MilitaryThresholdsWaitForTurnEndAndShadowWinsTheTie() => Wait(Military());

        [UnityTest]
        public IEnumerator RecaptureLosesTheThresholdBeforeTheEndOfTheTurn() => Wait(Recapture());

        [UnityTest]
        public IEnumerator FateNewCitiesAndKingsKeepTheirAuthoritativeScoringOrigins() => Wait(FateKings());

        [UnityTest]
        public IEnumerator BesiegersDoNotScoreWhileTheDefenderStillControlsTheStronghold() => Wait(Siege());

        [UnityTest]
        public IEnumerator RingEndingsTakePriorityAndKeepInspectionPrivateAfterRetryAndReconnect() => Wait(RingEndings());

        private static async Task Military()
        {
            foreach (var ending in new[] { "fp", "sh", "both" })
            {
                using var room = await Connect("victory-military-" + ending, ScenarioKind.BaseAcceptance);
                foreach (var actor in new[] { room.Fp, room.Sh })
                {
                    var board = actor.Board;
                    var pan = board.Map.Camera.Pan;
                    // Victory lives in Menu; its hidden tab has no place to hover. Hovering the Menu opens nothing.
                    await ComponentGestures.Hover(board.Menu.Toggle);
                    Assert.IsFalse(board.Victory.IsOpen);
                    await BoardNavigation.Victory(board);
                    await room.Settle();
                    Assert.IsTrue(board.Victory.IsOpen);
                    Assert.Less(Vector2.Distance(pan, board.Map.Camera.Pan), .001f);
                    Assert.AreEqual("Threshold reached · Checked at turn end", board.Victory.Root.Q<Label>("victory-timing").text);
                    Assert.AreEqual(DisplayStyle.None, board.Victory.Banner.style.display.value);
                    AssertScores(board.View);
                    LayoutTests.Save(actor.Viewport, "ui60-threshold-" + ending + "-" + board.View.You + ".png");
                    board.Victory.Close();
                }
                await Discard(room, room.Fp);
                Assert.IsNull(room.Fp.Board.View.Winner);
                await Discard(room, room.Sh);
                var winner = ending == "fp" ? Side.FreePeoples : Side.Shadow;
                foreach (var actor in new[] { room.Fp, room.Sh })
                {
                    Assert.AreEqual(winner, actor.Board.View.Winner);
                    StringAssert.Contains("Military victory", actor.Board.Victory.Banner.text);
                    Assert.AreEqual(DisplayStyle.Flex, actor.Board.Victory.Banner.style.display.value);
                    Assert.IsNull(actor.Board.Tray.Q<Button>("confirm-decision"));
                    Assert.IsEmpty(actor.Board.View.Options);
                    await Reconnect(room, actor);
                    Assert.AreEqual(winner, actor.Board.View.Winner);
                    LayoutTests.Save(actor.Viewport, "ui60-finished-" + ending + "-" + actor.Board.View.You + ".png");
                }
            }
        }

        private static async Task Recapture()
        {
            using var room = await Connect("victory-lost-threshold", ScenarioKind.BaseAcceptance);
            Assert.IsTrue(room.Fp.Board.View.MilitaryVictory.Single(score => score.Side == Side.FreePeoples).ThresholdReached);
            room.Fp.Board.Victory.Open();
            var board = room.Sh.Board;
            board.Map.Focus("dimrill-dale");
            ArmyGestures.Die(room.Sh, ArmyGestures.ArmyDie(room.Sh));
            await ArmyGestures.All(room.Sh, "dimrill-dale");
            await ArmyGestures.Target(room, room.Sh, "moria");
            // Finish the optional second march so the same paid action can end.
            if (room.Sh.Board.View.Decision?.Kind == DecisionKind.ActionSequence)
            {
                var revision = board.View.Revision;
                PointerGestureEvents.ClickAt(board.Console.Actions.Confirm);
                await room.At(revision + 1);
            }
            Assert.AreEqual(2, room.Fp.Board.View.MilitaryVictory.Single(score => score.Side == Side.FreePeoples).Points);
            Assert.IsNull(room.Fp.Board.Victory.Root.Q<Button>("victory-locate-moria"));
            Assert.AreEqual("Checked at turn end · Shadow first", room.Fp.Board.Victory.Root.Q<Label>("victory-timing").text);
            await Discard(room, room.Fp);
            Assert.IsNull(room.Fp.Board.View.Winner);
            Assert.IsNull(room.Sh.Board.View.Winner);
            Assert.AreEqual(2, room.Fp.Board.View.Turn);
            AssertScores(room.Fp.Board.View);
        }

        private static async Task FateKings()
        {
            using var room = await Connect("victory-fate-kings", ScenarioKind.BaseAcceptance);
            foreach (var actor in new[] { room.Fp, room.Sh })
            {
                var board = actor.Board;
                await BoardNavigation.Victory(board);
                await room.Settle();
                var fp = board.View.MilitaryVictory.Single(score => score.Side == Side.FreePeoples);
                var sh = board.View.MilitaryVictory.Single(score => score.Side == Side.Shadow);
                Assert.AreEqual(3, fp.Points);
                Assert.AreEqual(4, sh.Points);
                Assert.AreEqual(2, fp.Regions.Single(region => region.RegionId == "erebor").Points);
                Assert.AreEqual(1, fp.Regions.Single(region => region.RegionId == "south-rhun").Points);
                Assert.AreEqual(1, sh.Regions.Single(region => region.RegionId == "ered-luin").Points);
                Assert.AreEqual(1, sh.Regions.Single(region => region.RegionId == "iron-hills").Points);
                Assert.AreEqual(2, sh.Regions.Single(region => region.RegionId == "helms-deep").Points);
                Assert.AreEqual("isengard", board.Presentation.Regions["helms-deep"].Nation);
                AssertScores(board.View);
                var pan = board.Map.Camera.Pan;
                PointerGestureEvents.ClickAt(board.Victory.Root.Q<Button>("victory-locate-helms-deep"));
                await room.Settle();
                Assert.AreNotEqual(pan, board.Map.Camera.Pan);
                Assert.IsTrue(board.Victory.Pinned);
                Assert.AreEqual("helms-deep", board.Map.Selected);
                Assert.IsFalse(board.Victory.Root.worldBound.Contains(board.Map.LocalToWorld(board.Map.contentRect.center)),
                    "Locate leaves the centered region uncovered.");
                LayoutTests.Save(actor.Viewport, "ui60-fate-kings-" + board.View.You + ".png");
                // UI-265: a Fate game draws Erebor held by the Shadow, its gem a Shadow Stronghold, Dale a neutral
                // Fortification and the Iron Hills a Free Peoples City.
                CollectionAssert.Contains(board.Presentation.Modules, "fate");
                Assert.AreEqual(("Stronghold", "sauron"), (board.Presentation.Regions["erebor"].Settlement, board.Presentation.Regions["erebor"].Nation));
                Assert.AreEqual("Fortification", board.Presentation.Regions["dale"].Settlement);
                Assert.AreEqual("City", board.Presentation.Regions["iron-hills"].Settlement);
                board.Victory.Close();
                board.Map.Focus("erebor");
                await room.Settle();
                LayoutTests.Save(actor.Viewport, "ui265-fate-erebor-" + board.View.You + ".png");
            }
        }

        private static async Task Siege()
        {
            using var room = await Connect("victory-siege", ScenarioKind.BaseAcceptance);
            foreach (var actor in new[] { room.Fp, room.Sh })
            {
                var view = actor.Board.View;
                Assert.IsTrue(view.Units.Any(unit => unit.Location == "minas-tirith" && unit.Side == Side.Shadow));
                Assert.IsTrue(view.Units.Any(unit => unit.Location == "minas-tirith" && unit.Side == Side.FreePeoples && unit.Garrison));
                Assert.AreEqual(0, view.MilitaryVictory.Single(score => score.Side == Side.Shadow).Points);
                Assert.IsFalse(view.MilitaryVictory.Any(score => score.Regions.Any(region => region.RegionId == "minas-tirith")));
                actor.Board.Victory.Open();
                await room.Settle();
                Assert.IsNull(actor.Board.Victory.Root.Q<Button>("victory-locate-minas-tirith"));
                AssertScores(view);
            }
        }

        private static async Task RingEndings()
        {
            foreach (var ending in new[] { "fp", "sh" })
            {
                var boundary = new UI10HttpBoundary();
                using var room = await Connect("victory-ring-" + ending, ScenarioKind.BaseAcceptance, fpHandler: boundary);
                boundary.MatchPath = "/api/matches/" + room.Fp.Board.View.MatchId;
                var ownHand = room.Fp.Board.View.Hand.ToArray();
                var enemyHand = room.Sh.Board.View.Hand.ToArray();
                Assert.IsNotEmpty(ownHand);
                Assert.IsNotEmpty(enemyHand);
                var before = room.Fp.Board.View.Revision;
                FellowshipGestures.Die(room.Fp);
                FellowshipGestures.Action(room.Fp, "fellowship-progress");
                await room.At(before + 1);
                Assert.IsNull(room.Fp.Board.View.Winner);
                Assert.AreEqual(DisplayStyle.None, room.Fp.Board.Victory.Banner.style.display.value);
                Assert.AreEqual(4, room.Fp.Board.View.Fellowship.MordorStep);
                room.Fp.Board.Hunt.Open();
                await room.Settle();
                var ring = room.Fp.Board.Hunt.Root.Q<Button>("hunt-response-Ring");
                await room.Until(() => ring?.enabledInHierarchy == true);
                // UI-142: the response's click applies it.
                PointerGestureEvents.ClickAt(ring);
                await room.At(before + 2);
                var finalCommand = boundary.LastCommand;
                var winner = ending == "fp" ? Side.FreePeoples : Side.Shadow;
                var reason = ending == "fp" ? VictoryCondition.RingDestroyed : VictoryCondition.RingCorrupted;
                foreach (var actor in new[] { room.Fp, room.Sh })
                {
                    Assert.AreEqual(winner, actor.Board.View.Winner);
                    Assert.AreEqual(reason, actor.Board.View.Victory);
                    Assert.AreEqual(1, actor.Board.View.History.Count(item => item.Kind == reason.ToString()));
                    Assert.IsEmpty(actor.Board.View.Options);
                    Assert.AreEqual(DisplayStyle.Flex, actor.Board.Victory.Banner.style.display.value);
                    AssertScores(actor.Board.View);
                }
                CollectionAssert.AreEqual(ownHand, room.Fp.Board.View.Hand);
                CollectionAssert.AreEqual(enemyHand, room.Sh.Board.View.Hand);
                Assert.IsEmpty(room.Fp.Board.View.Hand.Intersect(enemyHand));
                AssertPrivateHand(room.Fp.Board.View, enemyHand);
                AssertPrivateHand(room.Sh.Board.View, ownHand);
                await RetryAndReject(room, finalCommand);
                await InspectFinished(room, room.Fp);
                await InspectFinished(room, room.Sh);
                AssertPrivateHand(room.Fp.Board.View, enemyHand);
                AssertPrivateHand(room.Sh.Board.View, ownHand);
            }
        }

        private static async Task InspectFinished(MatchTestRoom room, MountedMatchClient actor)
        {
            await Reconnect(room, actor);
            var board = actor.Board;
            board.Hunt.Close();
            board.Fellowship.Close();
            board.Victory.Open();
            await room.Settle();
            var pan = board.Map.Camera.Pan;
            PointerGestureEvents.ClickAt(board.Victory.Root.Q<Button>("victory-fellowship"));
            await room.Settle();
            Assert.IsTrue(board.Fellowship.IsOpen);
            Assert.Less(Vector2.Distance(pan, board.Map.Camera.Pan), .001f);
            Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
            board.Victory.Close();
            board.Fellowship.Close();
            await CardGestures.Open(actor);
            Assert.IsTrue(board.HandDrawer.IsOpen);
            await CardGestures.Preview(actor, board.View.Hand[0]);
            Assert.IsFalse(board.Hand.Q<Button>(board.View.Hand[0]).ClassListContains("playable-card"), "A finished match cannot play a card.");
            Assert.IsEmpty(board.Cards.CardId);
            Assert.IsNotEmpty(board.Hand.Q<Button>(board.View.Hand[0]).Q<Label>("hand-card-event").text);
            board.Cards.Close();
            board.HandDrawer.Close();
            CardNetworkTests.Click(board.Log.Toggle);
            await room.Until(() => actor.Activity.Loaded);
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Log.Root.style.display.value);
            LayoutTests.Save(actor.Viewport, "ui60-ring-ended-" + board.View.Winner + "-" + board.View.You + ".png");
        }

        private static async Task RetryAndReject(MatchTestRoom room, GameCommand finalCommand)
        {
            var api = room.Fp.Api;
            var path = "api/matches/" + finalCommand.MatchId;
            var before = await api.Get<PlayerView>(path, room.Token);
            var retry = await api.Post<CommandReceipt>(path + "/commands", finalCommand, room.Token);
            Assert.IsTrue(retry.Accepted);
            Assert.AreEqual(before.Revision, retry.Revision);
            var denied = await api.Post<CommandReceipt>(path + "/commands", new GameCommand
            {
                MatchId = before.MatchId, ExpectedRevision = before.Revision,
                CommandId = Guid.NewGuid().ToString("N"), Kind = CommandKind.HideFellowship
            }, room.Token);
            Assert.IsFalse(denied.Accepted);
            Assert.AreEqual("GameFinished", denied.Error);
            var after = await api.Get<PlayerView>(path, room.Token);
            Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(after)));
        }

        private static async Task Discard(MatchTestRoom room, MountedMatchClient actor)
        {
            var view = actor.Board.View;
            var die = view.Dice.Single(value => value.Owner == view.You && value.Location == DieLocation.Available);
            await ComponentGestures.Cost(room, actor, die.Id);
            await ComponentGestures.Select(actor, "DiscardDie");
            await room.At(view.Revision + 1);
        }

        private static async Task Reconnect(MatchTestRoom room, MountedMatchClient actor)
        {
            var count = actor.Connected;
            actor.Connection.Reconnect();
            await room.Until(() => actor.Connected > count);
            await room.Settle();
        }

        private static void AssertScores(PlayerView view)
        {
            foreach (var score in view.MilitaryVictory)
            {
                Assert.AreEqual(score.Points, score.Regions.Sum(region => region.Points));
                Assert.AreEqual(score.Points, view.Players.Single(player => player.Side == score.Side).VictoryPoints);
            }
        }

        private static void AssertPrivateHand(PlayerView view, string[] opponentHand)
        {
            var json = JToken.FromObject(view).ToString();
            foreach (var card in opponentHand)
                StringAssert.DoesNotContain(card, json, "Finishing or inspecting a match must not reveal an opponent's hand.");
        }
    }
}
