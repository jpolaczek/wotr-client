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

        // UI-274: the Lords special Eye (Nenya) pays only when clicked first in the Hunt Box, where it stands in the
        // Shadow's row; the attempt button beside the king starts the attempt. Every draw is the console's Draw tiles
        // (Draw again in Sequential), even when it is the only legal step (UI-279); the tiles stand beside the king and
        // the click on one attaches it. No window opens.
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
                    await DrawTiles(room, room.Sh);
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
