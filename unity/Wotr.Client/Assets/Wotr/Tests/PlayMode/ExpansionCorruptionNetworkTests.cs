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

        private static async Task Attempt(string scenario, bool sequential, bool stop)
        {
            using (var room = await Connect("ui09-corruption-" + scenario, ScenarioKind.CardPack))
            {
                const string sovereign = "kings.sovereign.thranduil";
                const string die = "lords.die.nenya";
                CardNetworkTests.Click(room.Sh.Board.OwnDice.Q<Button>("pool-Hunt"));
                await room.Settle();
                CardNetworkTests.Click(room.Sh.Board.Resources.Root.Q<Button>("resource-die-" + die));
                await Layout(room, room.Sh, "corrupt-attempt-" + scenario, "ui09");
                var revision = room.Sh.Board.View.Revision;
                CardNetworkTests.Click(await BoardNavigation.Sovereign(room.Sh.Board, sovereign));
                await room.At(revision + 1);
                Assert.AreEqual(DecisionKind.CorruptionTile, room.Sh.Board.View.Decision.Kind);
                Assert.IsNull(room.Fp.Board.View.Decision);
                Assert.IsEmpty(room.Fp.Board.View.CorruptionAttempt.OwnChoices);
                Assert.IsEmpty(room.Fp.Board.View.Sovereigns.Single(s => s.Id == sovereign).Tiles);
                var tile = room.Sh.Board.View.CorruptionAttempt.OwnChoices.Single().TileId;
                Assert.IsNull(room.Fp.Board.Root.Q<Button>("corruption-choice-" + tile));
                await Layout(room, room.Sh, "corrupt-choice-" + scenario, "ui09");
                await Reconnect(room, room.Sh);
                if (sequential && stop)
                {
                    revision = room.Sh.Board.View.Revision;
                    Click(room.Sh, "corruption-choice-" + tile);
                    await room.At(revision + 1);
                }
                else
                    await room.Confirm(room.Sh);
                if (!sequential)
                {
                    Assert.AreEqual(DecisionKind.CorruptionTile, room.Sh.Board.View.Decision.Kind);
                    await Layout(room, room.Sh, "corrupt-place-" + scenario, "ui09");
                    revision = room.Sh.Board.View.Revision;
                    Click(room.Sh, "corruption-choice-" + tile);
                    await room.At(revision + 1);
                }
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
