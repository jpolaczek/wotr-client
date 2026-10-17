using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class SovereignPanelNetworkTests
    {
        [UnityTest]
        public IEnumerator FiveSovereignsAwakenByStayingAndPreserveExactCostThroughCloseAndReconnect() => Wait(Awaken(false));

        [UnityTest]
        public IEnumerator FateAndRemovingTokensKeepThreeSovereignsAndDoNotInventAnImmediateRuler() => Wait(Awaken(true));

        [UnityTest]
        public IEnumerator SequentialVisibleDrawOffersOnlyTheLatestPrivateTileAndKeepsOneDecisionHost() => Wait(Sequential());

        [UnityTest]
        public IEnumerator PublicReplacementAndKingsEndingStaySeparateFromOrdinaryHunt() => Wait(Replacement());

        [UnityTest]
        public IEnumerator PoliticalTileReturnIsOptionalRandomAndDistinctFromRemovingTheSovereign() => Wait(Cleansing());

        private static async Task Awaken(bool variant)
        {
            using var room = await Connect(variant ? "ui59-awaken-variant" : "ui59-awaken", ScenarioKind.Interaction, openPanels: false);
            var board = room.Fp.Board;
            var id = variant ? "kings.sovereign.thranduil" : "kings.sovereign.dain";
            var option = board.View.Options.Single(o => o.Kind == CommandKind.AwakenSovereign && o.EligibleIds.Contains(id));
            var die = option.EligibleDice.First();
            var revision = board.View.Revision;
            var pan = board.Map.Camera.Pan;
            Assert.AreEqual(variant ? 3 : 5, board.Sovereigns.Root.childCount);
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die));
            await room.Settle();
            PointerGestureEvents.ClickAt(await BoardNavigation.Sovereign(board, id));
            await room.Settle();
            Assert.AreSame(board.Sovereigns.DecisionHost, board.Tray.parent);
            Assert.AreEqual(id, board.Sovereigns.Selected);
            Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
            Assert.IsNull(board.Tray.Q<Button>("cost-" + die));
            StringAssert.Contains(variant ? "tokens are not used" : "Ruler token", board.Sovereigns.Inspector.Q<Label>("sovereign-consequence").text);
            PointerGestureEvents.ClickAt(board.Sovereigns.Inspector.Q<Button>("sovereign-close"));
            await KingsCardNetworkChecks.Reconnect(room, room.Fp);
            Assert.IsFalse(board.Sovereigns.IsOpen);
            Assert.AreEqual(revision, board.View.Revision);
            Assert.IsTrue(board.OwnDice.Q<Button>(die).ClassListContains("selected"));
            PointerGestureEvents.ClickAt(board.Notice.Button);
            await room.Settle();
            Assert.AreEqual(id, board.Sovereigns.Selected);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            await SovereignPanelLayoutChecks.Layout(room, room.Fp, variant ? "awaken-variant" : "awaken");
            await Ready(room, board.Tray.Q<Button>("travel-stay"));
            PointerGestureEvents.ClickAt(board.Tray.Q<Button>("travel-stay"));
            await room.At(revision + 1);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var view = client.Board.View;
                Assert.AreEqual(SovereignStatus.Awakened, view.Sovereigns.Single(s => s.Id == id).Status);
                Assert.AreEqual(1, view.Characters.Count(c => c.Id == id));
                Assert.IsFalse(view.Units.Any(u => u.Id == id));
                Assert.IsEmpty(view.Sovereigns.Single(s => s.Id == id).Tiles);
                Assert.AreEqual(variant ? 0 : 1, view.RulerTokens.Count(t => t.SovereignId == id));
                Assert.AreEqual(DieLocation.Removed, view.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == "ruler").Location);
                Assert.AreEqual(DieLocation.Spent, view.Dice.Single(d => d.Id == die).Location);
            }
            await KingsCardNetworkChecks.Reconnect(room, room.Fp);
            Assert.AreEqual(revision + 1, board.View.Revision);
            room.Sh.Board.Sovereigns.Show(id);
            await SovereignPanelLayoutChecks.Layout(room, room.Sh, "awakened-public-" + variant);
        }

        private static async Task Sequential()
        {
            using var room = await Connect("ui59-sequential", ScenarioKind.Interaction, openPanels: false);
            var board = room.Sh.Board;
            var id = board.View.CorruptionAttempt.SovereignId;
            var first = board.View.CorruptionAttempt.OwnChoices.Single().TileId;
            Assert.AreEqual(3, board.View.CorruptionAttempt.OwnChoices.Single().Value);
            Assert.AreSame(board.Sovereigns.DecisionHost, board.Tray.parent);
            Assert.AreEqual(id, board.Sovereigns.Selected);
            Assert.IsNull(board.Tray.Q<Button>("action-ChooseCorruptionTile"), "Tile selection must not need a redundant mode button.");
            room.Fp.Board.Sovereigns.Show(id);
            Assert.IsEmpty(room.Fp.Board.Sovereigns.DecisionHost.Children());
            Assert.IsEmpty(room.Fp.Board.View.CorruptionAttempt.OwnChoices);
            Assert.IsNull(room.Fp.Board.Root.Q<Button>("corruption-choice-" + first));
            await SovereignPanelLayoutChecks.Layout(room, room.Fp, "visible-private-fp");
            await SovereignPanelLayoutChecks.Layout(room, room.Sh, "sequential-sh");
            var other = board.View.Sovereigns.First(s => s.Id != id).Id;
            board.Sovereigns.Show(other);
            Assert.AreEqual(DisplayStyle.None, board.Sovereigns.DecisionHost.style.display.value);
            Assert.AreSame(board.Sovereigns.DecisionHost, board.Tray.parent);
            PointerGestureEvents.ClickAt(board.Notice.Button);
            await room.Settle();
            var revision = board.View.Revision;
            await Draw(room, room.Sh);
            await room.At(revision + 1);
            var latest = board.View.CorruptionAttempt.OwnChoices.Single();
            Assert.AreEqual(1, latest.Value);
            Assert.AreNotEqual(first, latest.TileId);
            Assert.IsNull(board.Tray.Q<Button>("corruption-choice-" + first));
            Assert.IsFalse(board.Hunt.Pinned);
            PointerGestureEvents.ClickAt(board.Sovereigns.Inspector.Q<Button>("sovereign-close"));
            await KingsCardNetworkChecks.Reconnect(room, room.Sh);
            Assert.IsFalse(board.Sovereigns.IsOpen);
            PointerGestureEvents.ClickAt(board.Notice.Button);
            await room.Settle();
            await Ready(room, board.Tray.Q<Button>("corruption-choice-" + latest.TileId));
            PointerGestureEvents.ClickAt(board.Tray.Q<Button>("corruption-choice-" + latest.TileId));
            await room.At(revision + 2);
            Assert.IsNull(board.View.CorruptionAttempt);
            Assert.AreEqual(1, board.View.Sovereigns.Single(s => s.Id == id).Corruption);
            Assert.AreEqual(1, room.Fp.Board.View.Sovereigns.Single(s => s.Id == id).Tiles.Single().Value);
            Assert.IsTrue(board.Sovereigns.IsOpen);
        }

        private static async Task Replacement()
        {
            using var room = await Connect("ui59-replacement-kings", ScenarioKind.Interaction, openPanels: false);
            var board = room.Sh.Board;
            var attempt = board.View.CorruptionAttempt;
            Assert.AreEqual(0, attempt.DrawnCount);
            Assert.AreEqual(1, attempt.RevealedTiles.Count);
            Assert.IsEmpty(attempt.OwnChoices);
            Assert.IsTrue(board.Sovereigns.IsOpen);
            room.Fp.Board.Sovereigns.Show(attempt.SovereignId);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                StringAssert.Contains("Eye", client.Board.Sovereigns.Inspector.Q<Label>("sovereign-revealed").text);
                Assert.IsFalse(client.Board.Hunt.Pinned);
                await SovereignPanelLayoutChecks.Layout(room, client, "replacement-" + client.Board.View.You);
            }
            var revision = board.View.Revision;
            await Draw(room, room.Sh);
            await room.At(revision + 1);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                Assert.IsNull(client.Board.View.CorruptionAttempt);
                var tile = client.Board.View.Sovereigns.Single(s => s.Id == attempt.SovereignId).Tiles.Single();
                Assert.IsTrue(tile.Kings);
                Assert.AreEqual(0, tile.Value);
                Assert.AreEqual(DieLocation.Spent, client.Board.View.Dice.Single(d => d.Id == attempt.DieId).Location);
                Assert.IsFalse(client.Board.Hunt.Pinned);
            }
            Assert.IsTrue(board.Sovereigns.IsOpen);
            Assert.AreEqual(DisplayStyle.None, board.Sovereigns.DecisionHost.style.display.value);
        }

        private static async Task Cleansing()
        {
            foreach (var choice in new[] { "remove", "decline" })
            {
                using var room = await Connect("ui59-cleanse-" + choice, ScenarioKind.Interaction, openPanels: false);
                var board = room.Fp.Board;
                var id = "kings.sovereign.dain";
                var revision = board.View.Revision;
                var cost = board.View.Dice.Single(d => d.Location == DieLocation.Resolving).Id;
                Assert.AreEqual("CorruptionRemoval", board.View.Decision.Presentation);
                Assert.AreEqual(id, board.Sovereigns.Selected);
                Assert.AreSame(board.Sovereigns.DecisionHost, board.Tray.parent);
                Assert.IsNull(board.View.CorruptionAttempt);
                StringAssert.Contains("random non-Kings", board.Sovereigns.Inspector.Q<Label>("sovereign-consequence").text);
                Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
                Assert.AreEqual("Return a Corruption tile?", Wotr.Client.DecisionNoticeText.Describe(board.View));
                foreach (var button in board.Tray.Query<Button>().ToList())
                    StringAssert.DoesNotContain("kings.sovereign", button.text.ToLowerInvariant());
                await SovereignPanelLayoutChecks.Layout(room, room.Fp, "cleanse-" + choice);
                PointerGestureEvents.ClickAt(board.Sovereigns.Inspector.Q<Button>("sovereign-close"));
                await KingsCardNetworkChecks.Reconnect(room, room.Fp);
                Assert.IsFalse(board.Sovereigns.IsOpen);
                PointerGestureEvents.ClickAt(board.Notice.Button);
                await room.Settle();
                await Ready(room, board.Tray.Q<Button>("effect-choice-" + choice));
                PointerGestureEvents.ClickAt(board.Tray.Q<Button>("effect-choice-" + choice));
                await room.At(revision + 1);
                var sovereign = board.View.Sovereigns.Single(s => s.Id == id);
                Assert.AreEqual(SovereignStatus.Initial, sovereign.Status);
                Assert.AreEqual(choice == "remove" ? 1 : 2, sovereign.Tiles.Count);
                Assert.IsTrue(sovereign.Tiles.Any(t => t.Kings));
                Assert.AreEqual(DieLocation.Spent, board.View.Dice.Single(d => d.Id == cost).Location);
            }
        }

        private static async Task Draw(MatchTestRoom room, MountedMatchClient client)
        {
            var button = client.Board.Tray.Q<Button>("action-DrawCorruptionTile") ?? client.Board.Tray.Q<Button>("confirm-decision");
            await Ready(room, button);
            PointerGestureEvents.ClickAt(button);
        }

        private static Task Ready(MatchTestRoom room, Button button)
        {
            Assert.IsNotNull(button);
            return room.Until(() => button.enabledInHierarchy);
        }
    }
}
