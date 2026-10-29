using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using Wotr.Client;
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

        // UI-274: die → king → destination: the lit portrait takes the awakening; a single destination (the king's home)
        // completes it with that click, otherwise the lit region on the map does. No window or inspector hosts it.
        private static async Task Awaken(bool variant)
        {
            using var room = await Connect(variant ? "ui59-awaken-variant" : "ui59-awaken", ScenarioKind.Interaction, openPanels: false);
            using var watch = new GenericWindowWatch(room.Fp, room.Sh);
            var board = room.Fp.Board;
            var id = variant ? "kings.sovereign.thranduil" : "kings.sovereign.dain";
            var option = board.View.Options.Single(o => o.Kind == CommandKind.AwakenSovereign && o.EligibleIds.Contains(id));
            var die = option.EligibleDice.First();
            var targets = option.Travel.Single(t => t.Id == id).Targets;
            var revision = board.View.Revision;
            Assert.AreEqual(variant ? 3 : 5, board.Sovereigns.Root.childCount);
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die));
            await room.Settle();
            var portrait = await BoardNavigation.Sovereign(board, id);
            Assert.IsTrue(portrait.ClassListContains("legal-target"), "The die lights the king it can awaken.");
            await SovereignPanelLayoutChecks.Slots(room, room.Fp, variant ? "awaken-variant" : "awaken");
            await KingsCardNetworkChecks.Reconnect(room, room.Fp);
            portrait = await BoardNavigation.Sovereign(board, id);
            PointerGestureEvents.ClickAt(portrait);
            if (targets.Count > 1)
            {
                await room.Settle();
                Assert.IsFalse(board.Sovereigns.IsOpen);
                Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains(targets[0]));
                board.Map.Select(targets[0]);
            }
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
            Assert.IsTrue(room.Sh.Board.Sovereigns.Root.Q("sovereign-slot-" + id).Q("sovereign-status").ClassListContains("sovereign-status-awakened"),
                "The awakened king keeps his slot for both players.");
            watch.NeverOpened();
        }

        // UI-274: Sequential corruption: the latest tile stands beside the king for the Shadow (the Free Peoples see its
        // back); Draw again is the console's button; a click on the latest tile keeps it.
        private static async Task Sequential()
        {
            using var room = await Connect("ui59-sequential", ScenarioKind.Interaction, openPanels: false);
            using var watch = new GenericWindowWatch(room.Fp, room.Sh);
            var board = room.Sh.Board;
            var id = board.View.CorruptionAttempt.SovereignId;
            var first = board.View.CorruptionAttempt.OwnChoices.Single().TileId;
            Assert.AreEqual(3, board.View.CorruptionAttempt.OwnChoices.Single().Value);
            await room.Settle();
            watch.OnItsObject(room.Sh);
            var tile = await Tile(room, room.Sh, id, first);
            Assert.AreEqual("3", tile.Q<Label>().text);
            Assert.IsNull(room.Fp.Board.Root.Q<Button>("corruption-choice-" + first), "The Free Peoples never see the tile.");
            Assert.IsTrue(room.Fp.Board.Sovereigns.Root.Q("sovereign-slot-" + id).Q<Button>("corruption-choice-drawn-0").ClassListContains("sovereign-tile-back"));
            await SovereignPanelLayoutChecks.Slots(room, room.Fp, "sequential-fp");
            await SovereignPanelLayoutChecks.Slots(room, room.Sh, "sequential-sh");
            var revision = board.View.Revision;
            var draw = board.Console.Status.Options.Q<Button>("action-DrawCorruptionTile");
            await Ready(room, draw);
            PointerGestureEvents.ClickAt(draw);
            await room.At(revision + 1);
            var latest = board.View.CorruptionAttempt.OwnChoices.Single();
            Assert.AreEqual(1, latest.Value);
            Assert.AreNotEqual(first, latest.TileId);
            await KingsCardNetworkChecks.Reconnect(room, room.Sh);
            tile = await Tile(room, room.Sh, id, latest.TileId);
            Assert.IsNull(board.Root.Q<Button>("corruption-choice-" + first));
            PointerGestureEvents.ClickAt(tile);
            await room.At(revision + 2);
            Assert.IsNull(board.View.CorruptionAttempt);
            Assert.AreEqual(1, board.View.Sovereigns.Single(s => s.Id == id).Corruption);
            Assert.AreEqual(1, room.Fp.Board.View.Sovereigns.Single(s => s.Id == id).Tiles.Single().Value);
            watch.NeverOpened();
        }

        // UI-279: a draw that is the only legal step waits for the console's Draw tiles; opening the game sends nothing
        // (UI-12). The replacing Eye stays public and the Kings tile ends the attempt.
        private static async Task Replacement()
        {
            using var room = await Connect("ui59-replacement-kings", ScenarioKind.Interaction, openPanels: false);
            using var watch = new GenericWindowWatch(room.Fp, room.Sh);
            var board = room.Sh.Board;
            var attempt = board.View.CorruptionAttempt;
            Assert.AreEqual(1, attempt.RevealedTiles.Count);
            Assert.IsEmpty(attempt.OwnChoices);
            var draw = board.Console.Status.Options.Q<Button>("action-DrawCorruptionTile");
            await Ready(room, draw);
            Assert.AreEqual("Draw tiles", draw.text);
            Assert.AreEqual(attempt.DrawnCount, board.View.CorruptionAttempt.DrawnCount, "Opening the game draws nothing.");
            PointerGestureEvents.ClickAt(draw);
            await room.Until(() => room.Fp.Board.View.CorruptionAttempt == null);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                Assert.IsNull(client.Board.View.CorruptionAttempt);
                var tile = client.Board.View.Sovereigns.Single(s => s.Id == attempt.SovereignId).Tiles.Single();
                Assert.IsTrue(tile.Kings);
                Assert.AreEqual(0, tile.Value);
                Assert.AreEqual(DieLocation.Spent, client.Board.View.Dice.Single(d => d.Id == attempt.DieId).Location);
                Assert.IsFalse(client.Board.Hunt.Pinned);
            }
            watch.NeverOpened();
        }

        // UI-274: the cleanse is the glowing king's click; UI-278 (D7): Keep tiles is the console's Pass.
        private static async Task Cleansing()
        {
            foreach (var choice in new[] { "remove", "decline" })
            {
                using var room = await Connect("ui59-cleanse-" + choice, ScenarioKind.Interaction, openPanels: false);
                using var watch = new GenericWindowWatch(room.Fp, room.Sh);
                var board = room.Fp.Board;
                var id = "kings.sovereign.dain";
                var revision = board.View.Revision;
                var cost = board.View.Dice.Single(d => d.Location == DieLocation.Resolving).Id;
                Assert.AreEqual("CorruptionRemoval", board.View.Decision.Presentation);
                Assert.IsNull(board.View.CorruptionAttempt);
                Assert.AreEqual("Return a Corruption tile?", Wotr.Client.DecisionNoticeText.Describe(board.View));
                await room.Settle();
                watch.OnItsObject(room.Fp);
                var portrait = await BoardNavigation.Sovereign(board, id);
                Assert.IsTrue(portrait.ClassListContains("legal-target"));
                StringAssert.StartsWith("Click to return one random tile", portrait.tooltip);
                Assert.IsNull(board.Console.Status.Options.Q<Button>("effect-choice-decline"), "No Keep tiles button.");
                await SovereignPanelLayoutChecks.Slots(room, room.Fp, "cleanse-" + choice);
                await KingsCardNetworkChecks.Reconnect(room, room.Fp);
                if (choice == "remove")
                    PointerGestureEvents.ClickAt(await BoardNavigation.Sovereign(board, id));
                else
                    await room.Pass(room.Fp, "Pass · Keep the king's tiles in place");
                await room.At(revision + 1);
                var sovereign = board.View.Sovereigns.Single(s => s.Id == id);
                Assert.AreEqual(SovereignStatus.Initial, sovereign.Status);
                Assert.AreEqual(choice == "remove" ? 1 : 2, sovereign.Tiles.Count);
                Assert.IsTrue(sovereign.Tiles.Any(t => t.Kings));
                Assert.AreEqual(DieLocation.Spent, board.View.Dice.Single(d => d.Id == cost).Location);
                watch.NeverOpened();
            }
        }

        // A tile beside the king in the Characters panel, lit and enabled.
        private static async Task<Button> Tile(MatchTestRoom room, MountedMatchClient client, string king, string tile)
        {
            client.Board.Characters.Open();
            await room.Settle();
            var button = client.Board.Sovereigns.Root.Q("sovereign-slot-" + king).Q<Button>("corruption-choice-" + tile);
            Assert.IsNotNull(button, tile + " stands beside the king.");
            Assert.IsTrue(button.ClassListContains("legal-target"));
            // UI-411 (owner): a tile that can be chosen wears the choice glow and its panel pulses it.
            Assert.IsTrue(button.ClassListContains(ChoiceGlow.ClassName), "The tile wears the choice glow.");
            var pulsing = Pulsing(button);
            for (var waited = 0; waited < 2000 && !pulsing; waited += 50)
            {
                await Task.Delay(50);
                pulsing = Pulsing(button);
            }
            Assert.IsTrue(pulsing, "The tile pulses.");
            return button;
        }

        // The shared rhythm toggles its class on an ancestor (the Characters panel) every 750 ms.
        private static bool Pulsing(VisualElement element)
        {
            for (var parent = element.parent; parent != null; parent = parent.parent)
            {
                if (parent.ClassListContains(ChoiceGlow.PulseClass))
                    return true;
            }
            return false;
        }

        private static Task Ready(MatchTestRoom room, Button button)
        {
            Assert.IsNotNull(button);
            return room.Until(() => button.enabledInHierarchy);
        }
    }
}
