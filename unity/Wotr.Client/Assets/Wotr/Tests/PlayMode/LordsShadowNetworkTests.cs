using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public class LordsShadowNetworkTests
    {
        [UnityTest] public IEnumerator LoreChangesOneRolledEyeAfterOneConfirmationAndReconnect() => Wait(Lore());
        [UnityTest] public IEnumerator LoreRemovalSpendsTheExactDieWhenTheRingIsChosen() => Wait(RemoveLore());
        [UnityTest] public IEnumerator GatesCanBeRemovedWithGreyGuidesCharacterResult() => Wait(RemoveGates());
        [UnityTest] public IEnumerator SmeagolCostIsOptionalAndOnlyShadowMakesTheChoice() => Wait(Smeagol());

        // UI-272: Lore is card → die → face: the rolled Eye in the Hunt box glows in the pool's row, its click opens the
        // new results over it, and the result's click sends the change; Keep results is the console's Pass (UI-347, D7).
        private static async Task Lore()
        {
            using (var room = await Connect("lords-lore-choice", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                Assert.IsNull(room.Fp.Board.View.Decision);
                await room.Settle();
                watch.OnItsObject(room.Sh);
                Assert.IsNull(room.Sh.Board.Console.Status.Options.Q<Button>("effect-choice-" + LoreChoice.Keep));
                Assert.AreEqual("Pass · Keep your dice as they are", room.Sh.Board.Console.Actions.Pass.tooltip);
                StringAssert.StartsWith("Click a lit die", room.Sh.Board.Console.Status.Hint.text);
                StringAssert.Contains("Or Pass to keep your dice as they are", room.Sh.Board.Console.Status.Hint.text);
                var die = room.Sh.Board.View.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Hunt);
                await LoreGestures.Die(room, room.Sh, die.Id);
                Assert.IsNotNull(LoreGestures.Face(room.Sh, "lore.Character.keep"));
                var connected = room.Sh.Connected;
                room.Sh.Connection.Reconnect();
                await room.Until(() => room.Sh.Connected > connected);
                await room.Settle();
                await CheckLayout(room, room.Sh, "lore");
                Assert.IsTrue(room.Sh.Board.LorePicker.IsOpen, "The chosen die survives a reconnect.");
                Assert.AreEqual(DieLocation.Hunt, room.Fp.Board.View.Dice.Single(d => d.Id == die.Id).Location);
                await LoreGestures.Result(room, room.Sh, "lore.Character.keep");
                watch.NeverOpened();
                Assert.AreEqual(DieFace.Character, room.Fp.Board.View.Dice.Single(d => d.Id == die.Id).Face);
                Assert.AreEqual(DieLocation.Available, room.Fp.Board.View.Dice.Single(d => d.Id == die.Id).Location);
                Assert.AreEqual(Side.FreePeoples, room.Fp.Board.View.DecisionOwner);
            }
        }

        private static async Task RemoveLore()
        {
            using (var room = await Connect("lords-lore-remove", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var die = room.Fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Army);
                var ring = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.DiscardLore).EligibleIds[1];
                await ComponentGestures.Cost(room, room.Fp, die.Id);
                await ComponentGestures.Remove(room.Fp, "lords.sh.character.26");
                await room.Settle();
                Assert.IsNull(room.Fp.Board.Tray.Q<Button>("confirm-decision"));
                // UI-272: the Ring is paid on its socket; the tray lists neither Rings nor dice.
                Assert.IsNull(room.Fp.Board.Tray.Q<Button>("lore-no-ring"));
                Assert.IsNull(room.Fp.Board.Tray.Q<Button>("ring-cost-" + ring));
                StringAssert.StartsWith("Click a lit Elven Ring", room.Fp.Board.Console.Status.Hint.text);
                Assert.AreEqual(DieLocation.Available, room.Fp.Board.View.Dice.Single(d => d.Id == die.Id).Location);
                await CheckLayout(room, room.Fp, "lore-remove");
                Assert.AreEqual(Side.FreePeoples, room.Sh.Board.View.Rings.Single(r => r.Id == ring).Owner);
                var revision = room.Fp.Board.View.Revision;
                CardNetworkTests.Click(room.Fp.Board.Console.Rings.For(ring));
                await room.At(revision + 1);
                Assert.IsFalse(room.Sh.Board.View.Effects.Any(e => e.SourceId == "lords.sh.character.26"));
                Assert.AreEqual(Side.Shadow, room.Sh.Board.View.Rings.Single(r => r.Id == ring).Owner);
                Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == die.Id).Location);
                watch.NeverOpened();
            }
        }

        private static async Task RemoveGates()
        {
            using (var room = await Connect("lords-gates-remove", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var die = room.Fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Character);
                await ComponentGestures.Cost(room, room.Fp, die.Id);
                await CheckLayout(room, room.Fp, "gates-remove");
                var revision = room.Fp.Board.View.Revision;
                await ComponentGestures.Remove(room.Fp, "lords.sh.character.27");
                await room.At(revision + 1);
                Assert.IsFalse(room.Sh.Board.View.Effects.Any(e => e.SourceId == "lords.sh.character.27"));
                Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == die.Id).Location);
                watch.NeverOpened();
            }
        }

        private static async Task Smeagol()
        {
            using (var room = await Connect("lords-smeagol-cost", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                Assert.IsNull(room.Fp.Board.View.Decision);
                // UI-272: We Won't Go Back's cost stands in the Hunt window's response row: the table card; the Character
                // cards it accepts glow in the hand; UI-347 (D7): No Corruption is the console's Pass.
                var row = room.Sh.Board.Hunt.Responses.Root.Q("hunt-card-responses");
                Assert.IsNull(row.Q<Button>("effect-choice-smeagol.no-corruption"));
                Assert.AreEqual("Pass · Take no Corruption and keep your cards", room.Sh.Board.Console.Actions.Pass.tooltip);
                var card = row.Q<Button>("effect-choice-lords.sh.character.25");
                StringAssert.StartsWith("Click to discard", card.tooltip);
                Assert.IsNull(room.Sh.Board.Tray.Q<Button>("effect-choice-smeagol.no-corruption"));
                StringAssert.Contains("Sméagol", room.Sh.Board.Hunt.Root.Q<Label>("hunt-tile-face").text);
                await CheckLayout(room, room.Sh, "smeagol-cost");
                Assert.AreEqual(0, room.Fp.Board.View.Fellowship.Corruption);
                var revision = room.Sh.Board.View.Revision;
                CardNetworkTests.Click(card);
                await room.At(revision + 1);
                watch.NeverOpened();
                Assert.AreEqual(2, room.Fp.Board.View.Fellowship.Corruption);
                Assert.IsFalse(room.Fp.Board.View.Effects.Any(e => e.SourceId == "lords.sh.character.25"));
            }
        }

        private static async Task CheckLayout(MatchTestRoom room, MountedMatchClient client, string name)
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    await room.Settle();
                    FellowshipLayoutTests.Check(client.Board);
                    LayoutTests.AssertNoDecisionScroll(client.Board);
                    Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                    LayoutTests.Save(target, $"lords02-{name}-{width}-{scale:0.00}.png");
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
            client.Panel.scale = 1f;
            await room.Settle();
        }
    }
}
