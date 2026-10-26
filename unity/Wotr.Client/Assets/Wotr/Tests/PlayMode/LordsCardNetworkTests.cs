using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;
using static Wotr.PlayTests.KingsCardNetworkChecks;

namespace Wotr.PlayTests
{
    // UI-272: the Lords cards' choices on their objects: The Gates are Closed at its × on a declaration, Wind from the
    // West and Safe Paths in the Dark at the Shadow table cards' ×, Wind's inspected Shadow hand in the drawer, and Three
    // Rings for the Elven-Kings at the Ring sockets. No decision window opens.
    public class LordsCardNetworkTests
    {
        private const string Gates = "lords.sh.character.27";
        private const string Lore = "lords.sh.character.26";

        [UnityTest] public IEnumerator TheGatesAnswerADeclarationAtTheirCross() => Wait(GatesDeclaration());
        [UnityTest] public IEnumerator WindDiscardsAShadowTableCardAtItsCross() => Wait(WindTable());
        [UnityTest] public IEnumerator WindShowsTheShadowHandInTheDrawer() => Wait(WindHand());
        [UnityTest] public IEnumerator SafePathsDiscardsASmeagolCardAtItsCross() => Wait(SafePaths());
        [UnityTest] public IEnumerator ThreeRingsRecoversRingsOnTheirSockets() => Wait(ThreeRings());

        private static async Task GatesDeclaration()
        {
            using (var room = await Connect("ui272-gates-declaration", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                await room.Settle();
                watch.OnItsObject(client);
                // UI-291: the console's Pass declines the window; no Decline option.
                Assert.IsTrue(client.Board.Console.Actions.Pass.enabledSelf);
                StringAssert.Contains("The Gates are Closed", client.Board.Console.Actions.Pass.tooltip);
                var cross = await Cross(room, client, Gates);
                await Layout(room, client, "gates-declaration", "ui272");
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(cross);
                await room.At(revision + 1);
                Assert.IsFalse(room.Fp.Board.View.Effects.Any(e => e.SourceId == Gates), "Using the Gates discards them.");
                watch.NeverOpened();
            }
        }

        private static async Task WindTable()
        {
            using (var room = await Connect("ui272-wind-table", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Fp;
                await room.Settle();
                watch.OnItsObject(client);
                Assert.IsNotNull(client.Board.Console.Status.Options.Q<Button>("console-option-table-cards"));
                var cross = await Cross(room, client, Gates);
                StringAssert.StartsWith("Discard The Gates are Closed", cross.tooltip);
                await Layout(room, client, "wind-table", "ui272");
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(cross);
                await room.At(revision + 1);
                Assert.IsFalse(room.Sh.Board.View.Effects.Any(e => e.SourceId == Gates));
                Assert.IsTrue(room.Sh.Board.View.Effects.Any(e => e.SourceId == Lore));
                watch.NeverOpened();
            }
        }

        private static async Task WindHand()
        {
            using (var room = await Connect("ui272-wind-hand", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Fp;
                await room.Settle();
                watch.OnItsObject(client);
                Assert.IsNotNull(client.Board.Console.Status.Options.Q<Button>("effect-choice-wind.finish"));
                await room.Direct(client, "effect-choice-wind.inspect-hand");
                Assert.AreEqual(InspectedHand.Presentation, client.Board.View.Decision.Presentation);
                await CardGestures.Open(client);
                var card = client.Board.Hand.Q<Button>("base.sh.character.01");
                Assert.IsNotNull(card, "The inspected Shadow card stands in the drawer.");
                Assert.IsTrue(card.ClassListContains("inspected-card") && card.ClassListContains(HandCard.DiscardClass));
                StringAssert.StartsWith("Click a Shadow card", client.Board.Console.Status.Hint.text);
                await Layout(room, client, "wind-hand", "ui272");
                await CardGestures.Open(client);
                var shadowHand = room.Sh.Board.View.Hand.Count;
                var revision = client.Board.View.Revision;
                PointerGestureEvents.ClickAt(client.Board.Hand.Q<Button>("base.sh.character.01"));
                await room.At(revision + 1);
                Assert.AreEqual(shadowHand - 1, room.Sh.Board.View.Hand.Count);
                Assert.IsFalse(room.Sh.Board.View.Hand.Contains("base.sh.character.01"));
                Assert.IsNull(client.Board.Hand.Q<Button>("base.sh.strategy.02"), "The Shadow's other card leaves the drawer.");
                watch.NeverOpened();
            }
        }

        private static async Task SafePaths()
        {
            using (var room = await Connect("ui272-safe-paths", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Fp;
                await room.Settle();
                watch.OnItsObject(client);
                var cross = await Cross(room, client, "lords.sh.special.05");
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(cross);
                await room.At(revision + 1);
                Assert.IsFalse(room.Sh.Board.View.Effects.Any(e => e.SourceId == "lords.sh.special.05"));
                watch.NeverOpened();
            }
        }

        // Two successes: the Ring out of play and one Ring the Shadow holds come back with two socket clicks.
        private static async Task ThreeRings()
        {
            using (var room = await Connect("ui272-three-rings", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Fp;
                await room.Settle();
                watch.OnItsObject(client);
                var removed = client.Board.View.Rings.Single(r => r.Owner == null).Id;
                var held = client.Board.View.Rings.First(r => r.Owner == Side.Shadow).Id;
                var socket = client.Board.Console.Rings.For(removed);
                Assert.IsTrue(socket.enabledSelf && socket.ClassListContains("legal-target"));
                StringAssert.Contains("Click to recover this Ring", socket.tooltip);
                StringAssert.StartsWith("Click 2 glowing Rings", client.Board.Console.Status.Hint.text);
                await Layout(room, client, "three-rings", "ui272");
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(socket);
                await room.Settle();
                Assert.AreEqual(revision, client.Board.View.Revision, "One Ring of two is no complete choice.");
                Assert.IsTrue(socket.ClassListContains("selected"));
                CardNetworkTests.Click(client.Board.Console.Rings.For(held));
                await room.At(revision + 1);
                Assert.AreEqual(Side.FreePeoples, room.Sh.Board.View.Rings.Single(r => r.Id == removed).Owner);
                Assert.AreEqual(Side.FreePeoples, room.Sh.Board.View.Rings.Single(r => r.Id == held).Owner);
                watch.NeverOpened();
            }
        }

        // The table card's × in the Effects panel, lit and enabled.
        private static async Task<Button> Cross(MatchTestRoom room, MountedMatchClient client, string card)
        {
            client.Board.TableEffects.Open();
            await room.Settle();
            var cross = client.Board.TableEffects.Root.Q<Button>("remove-table-" + card);
            Assert.IsNotNull(cross, card + " offers its ×.");
            await room.Until(() => cross.enabledInHierarchy && cross.worldBound.width > 0);
            return cross;
        }
    }
}
