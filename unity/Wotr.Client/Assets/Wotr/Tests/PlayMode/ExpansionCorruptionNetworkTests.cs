using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;
using static Wotr.PlayTests.KingsCardNetworkChecks;

namespace Wotr.PlayTests
{
    public class ExpansionCorruptionNetworkTests
    {
        [UnityTest] public IEnumerator HiddenCorruptionStaysFaceDownForFreePeoples() => Wait(Attempt("hidden", false, false));
        [UnityTest] public IEnumerator VisibleCorruptionIsRevealedOnlyAfterAttachment() => Wait(Attempt("visible", false, false));
        [UnityTest] public IEnumerator SequentialCorruptionCanStopAfterTheFirstTile() => Wait(Attempt("stop", true, true));
        [UnityTest] public IEnumerator SequentialCorruptionCanDrawAgainInsteadOfStopping() => Wait(Attempt("draw", true, false));
        [UnityTest] public IEnumerator TwoEyesPlaceTheOnlyTileWithTheAttemptsClick() => Wait(SingleTile());

        // UI-442 (owner, EXP-15 D8): with two Eyes in the Hunt Box the attempt draws one tile, the only possible choice, so
        // the attempt's click places it on the king: one command, and neither a tile choice nor a draw for the Shadow.
        private static async Task SingleTile()
        {
            var session = await TestScenarioRoom.Start("kings-sh-corruption");
            try
            {
                var board = session.Client(Side.Shadow).Board;
                var option = board.View.Options.Single(o => o.Enabled && o.Kind == CommandKind.AttemptCorruption);
                Assert.AreEqual(2, option.EligibleDice.Count, "Two Eyes stand in the Hunt Box.");
                var king = SafestKing(board.View, option);
                var tiles = board.View.Sovereigns.Single(s => s.Id == king).Tiles.Count;
                var die = option.EligibleDice[0];
                var eye = board.OwnDice.Q<Button>(die);
                Assert.IsNotNull(eye, "The Eye that may pay stands in the Shadow's row.");
                CardNetworkTests.Click(eye);
                await session.Settle();
                board.Characters.Open();
                await session.Settle();
                var attempt = board.Sovereigns.Root.Q<Button>("sovereign-attempt-" + king);
                Assert.IsTrue(attempt.enabledSelf && attempt.ClassListContains("legal-target"), "The chosen Eye lights the attempt.");
                StringAssert.Contains("\nDraws a Corruption tile and places it on ", attempt.tooltip);
                var revision = board.View.Revision;
                PointerGestureEvents.ClickAt(attempt);
                await session.Until(() => board.View.Revision > revision);
                await session.Settle();
                Assert.AreEqual(revision + 1, board.View.Revision, "The attempt's one command places the tile.");
                Assert.IsNull(board.View.CorruptionAttempt, "The attempt has ended.");
                Assert.AreNotEqual(DecisionKind.CorruptionTile, board.View.Decision?.Kind);
                Assert.IsNull(board.Console.Status.Options.Q<Button>("action-DrawCorruptionTile"));
                Assert.AreEqual(tiles + 1, board.View.Sovereigns.Single(s => s.Id == king).Tiles.Count);
                Assert.AreEqual(DieLocation.Spent, board.View.Dice.Single(d => d.Id == die).Location);
                var fp = session.Client(Side.FreePeoples).Board;
                await session.Until(() => fp.View.Revision == board.View.Revision);
                Assert.AreEqual(tiles + 1, fp.View.Sovereigns.Single(s => s.Id == king).Tiles.Count);
            }
            finally
            {
                await session.End();
            }
        }

        // The king furthest from his resistance, so the one tile (at most 3) cannot corrupt him and resolve his tiles.
        private static string SafestKing(PlayerView view, ActionOption option) => option.EligibleIds
            .Select(id => view.Sovereigns.Single(s => s.Id == id))
            .OrderByDescending(s => s.Resistance - (s.Corruption ?? 0)).First().Id;

        // UI-274: the Lords special Eye (Nenya) pays only when clicked first in the Hunt Box, where it stands in the
        // Shadow's row; the attempt button beside the king starts the attempt. UI-442 (owner): a standard attempt's click
        // draws both tiles at once and the choice waits; Sequential corruption's next draw is the console's Draw again
        // (UI-279). The tiles stand beside the king and the click on one attaches it. No window opens.
        private static async Task Attempt(string scenario, bool sequential, bool stop)
        {
            using (var room = await Connect("ui09-corruption-" + scenario, ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                const string sovereign = "kings.sovereign.thranduil";
                const string die = "lords.die.nenya";
                var eye = room.Sh.Board.OwnDice.Q<Button>(die);
                Assert.IsNotNull(eye, "The Keeper Eye that may pay stands in the Shadow's row.");
                CardNetworkTests.Click(eye);
                await room.Settle();
                room.Sh.Board.Characters.Open();
                await room.Settle();
                var attempt = room.Sh.Board.Sovereigns.Root.Q<Button>("sovereign-attempt-" + sovereign);
                Assert.IsTrue(attempt.enabledSelf && attempt.ClassListContains("legal-target"));
                StringAssert.StartsWith("Click to attempt to corrupt Thranduil", attempt.tooltip);
                await SovereignPanelLayoutChecks.Slots(room, room.Sh, "attempt-" + scenario);
                room.Sh.Board.Characters.Open();
                await room.Settle();
                var revision = room.Sh.Board.View.Revision;
                CardNetworkTests.Click(room.Sh.Board.Sovereigns.Root.Q<Button>("sovereign-attempt-" + sovereign));
                await room.Until(() => room.Fp.Board.View.Revision > revision && room.Sh.Board.View.CorruptionAttempt != null);
                Assert.AreEqual(DecisionKind.CorruptionTile, room.Sh.Board.View.Decision.Kind);
                Assert.IsNull(room.Fp.Board.View.Decision);
                Assert.IsEmpty(room.Fp.Board.View.CorruptionAttempt.OwnChoices);
                Assert.IsEmpty(room.Fp.Board.View.Sovereigns.Single(s => s.Id == sovereign).Tiles);
                if (!sequential)
                {
                    Assert.AreEqual(revision + 1, room.Sh.Board.View.Revision, "The attempt's one command draws both tiles.");
                    Assert.AreEqual(2, room.Fp.Board.View.CorruptionAttempt.DrawnCount, "The Free Peoples count both drawn tiles.");
                    await AtTileChoice(room, room.Sh, 2);
                }
                var tile = room.Sh.Board.View.CorruptionAttempt.OwnChoices.First().TileId;
                Assert.IsNull(room.Fp.Board.Root.Q<Button>("corruption-choice-" + tile));
                await SovereignPanelLayoutChecks.Slots(room, room.Sh, "choice-" + scenario);
                await SovereignPanelLayoutChecks.Slots(room, room.Fp, "choice-fp-" + scenario);
                await Reconnect(room, room.Sh);
                room.Sh.Board.Characters.Open();
                await room.Settle();
                if (sequential && !stop)
                {
                    var draw = room.Sh.Board.Console.Status.Options.Q<Button>("action-DrawCorruptionTile");
                    await room.Until(() => draw.enabledInHierarchy);
                    PointerGestureEvents.ClickAt(draw);
                }
                else
                {
                    var button = room.Sh.Board.Sovereigns.Root.Q("sovereign-slot-" + sovereign).Q<Button>("corruption-choice-" + tile);
                    Assert.IsTrue(button.ClassListContains("legal-target"));
                    StringAssert.StartsWith("Click to attach this tile to Thranduil", button.tooltip);
                    PointerGestureEvents.ClickAt(button);
                }
                await room.Until(() => room.Fp.Board.View.CorruptionAttempt == null && room.Sh.Board.View.CorruptionAttempt == null);
                watch.NeverOpened();
                var fp = room.Fp.Board.View.Sovereigns.Single(s => s.Id == sovereign).Tiles.Single();
                var sh = room.Sh.Board.View.Sovereigns.Single(s => s.Id == sovereign).Tiles.Single();
                Assert.AreEqual(1, sh.Value);
                Assert.AreEqual(room.Fp.Board.View.Configuration.VisibleCorruption ? (int?)1 : null, fp.Value);
                if (!room.Fp.Board.View.Configuration.VisibleCorruption)
                    Assert.IsEmpty(fp.TileId);
                var cost = room.Fp.Board.View.Dice.Single(d => d.Id == die);
                Assert.AreEqual(DieLocation.Spent, cost.Location);
                Assert.AreEqual(Side.FreePeoples, cost.SpentWith);
                var publicRevision = room.Fp.Board.View.Revision;
                var camera = room.Fp.Board.Map.Camera.Pan;
                room.Fp.Board.Sovereigns.Close();
                var portrait = await BoardNavigation.Sovereign(room.Fp.Board, sovereign);
                await ComponentGestures.Hover(portrait);
                Assert.AreEqual(DisplayStyle.None, room.Fp.Board.Sovereigns.Inspector.resolvedStyle.display);
                PointerGestureEvents.ClickAt(portrait);
                await room.Settle();
                Assert.AreEqual(DisplayStyle.Flex, room.Fp.Board.Sovereigns.Inspector.resolvedStyle.display);
                var face = room.Fp.Board.Sovereigns.Inspector.Q<Label>("attached-tile-" + fp.Id);
                Assert.AreEqual(room.Fp.Board.View.Configuration.VisibleCorruption ? "1" + (fp.Reveal ? " ◉" : "") : "▧", face.text);
                await ComponentGestures.Hover(room.Fp.Board.Phase);
                Assert.AreEqual(DisplayStyle.Flex, room.Fp.Board.Sovereigns.Inspector.resolvedStyle.display,
                    "The inspected public tile stays readable after pointer exit.");
                Assert.AreEqual(publicRevision, room.Fp.Board.View.Revision);
                Assert.AreEqual(camera, room.Fp.Board.Map.Camera.Pan);
                room.Fp.Board.Sovereigns.Show(sovereign);
                await Layout(room, room.Fp, "corrupt-public-" + scenario, "ui09");
            }
        }

    }
}
