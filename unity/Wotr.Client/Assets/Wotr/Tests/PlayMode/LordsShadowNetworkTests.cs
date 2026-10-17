using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
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

        private static async Task Lore()
        {
            using (var room = await Connect("lords-lore-choice", ScenarioKind.CardPack))
            {
                Assert.IsNull(room.Fp.Board.View.Decision);
                var die = room.Sh.Board.View.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Hunt);
                Click(room.Sh, "effect-choice-" + die.Id);
                await room.Settle();
                Assert.IsFalse(room.Sh.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                Click(room.Sh, "effect-choice-lore.Character.keep");
                await room.Settle();
                Assert.IsTrue(room.Sh.Board.Tray.Query<Label>().ToList().Any(l => l.text.Contains("Eye → Character")));
                CardNetworkTests.Click(room.Sh.Board.Tray.Q<Button>("decision-back"));
                await room.Settle();
                Assert.IsFalse(room.Sh.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                Click(room.Sh, "effect-choice-lore.Character.keep");
                var connected = room.Sh.Connected;
                room.Sh.Connection.Reconnect();
                await room.Until(() => room.Sh.Connected > connected);
                await CheckLayout(room, room.Sh, "lore");
                Assert.AreEqual(DieLocation.Hunt, room.Fp.Board.View.Dice.Single(d => d.Id == die.Id).Location);
                await room.Confirm(room.Sh);
                Assert.AreEqual(DieFace.Character, room.Fp.Board.View.Dice.Single(d => d.Id == die.Id).Face);
                Assert.AreEqual(DieLocation.Available, room.Fp.Board.View.Dice.Single(d => d.Id == die.Id).Location);
                Assert.AreEqual(Side.FreePeoples, room.Fp.Board.View.DecisionOwner);
            }
        }

        private static async Task RemoveLore()
        {
            using (var room = await Connect("lords-lore-remove", ScenarioKind.CardPack))
            {
                var die = room.Fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Army);
                var ring = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.DiscardLore).EligibleIds[1];
                await ComponentGestures.Cost(room, room.Fp, die.Id);
                await ComponentGestures.Remove(room.Fp, "lords.sh.character.26");
                await room.Settle();
                Assert.IsNull(room.Fp.Board.Tray.Q<Button>("confirm-decision"));
                Assert.AreEqual(DieLocation.Available, room.Fp.Board.View.Dice.Single(d => d.Id == die.Id).Location);
                await CheckLayout(room, room.Fp, "lore-remove");
                Assert.AreEqual(Side.FreePeoples, room.Sh.Board.View.Rings.Single(r => r.Id == ring).Owner);
                var revision = room.Fp.Board.View.Revision;
                CardNetworkTests.Click(room.Fp.Board.Console.Rings.For(ring));
                await room.At(revision + 1);
                Assert.IsFalse(room.Sh.Board.View.Effects.Any(e => e.SourceId == "lords.sh.character.26"));
                Assert.AreEqual(Side.Shadow, room.Sh.Board.View.Rings.Single(r => r.Id == ring).Owner);
                Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == die.Id).Location);
            }
        }

        private static async Task RemoveGates()
        {
            using (var room = await Connect("lords-gates-remove", ScenarioKind.CardPack))
            {

                var die = room.Fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Character);
                await ComponentGestures.Cost(room, room.Fp, die.Id);
                await CheckLayout(room, room.Fp, "gates-remove");
                var revision = room.Fp.Board.View.Revision;
                await ComponentGestures.Remove(room.Fp, "lords.sh.character.27");
                await room.At(revision + 1);
                Assert.IsFalse(room.Sh.Board.View.Effects.Any(e => e.SourceId == "lords.sh.character.27"));
                Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == die.Id).Location);
            }
        }

        private static async Task Smeagol()
        {
            using (var room = await Connect("lords-smeagol-cost", ScenarioKind.CardPack))
            {
                Assert.IsNull(room.Fp.Board.View.Decision);
                Assert.IsNotNull(room.Sh.Board.Tray.Q<Button>("effect-choice-smeagol.no-corruption"));
                StringAssert.Contains("Sméagol", room.Sh.Board.Hunt.Root.Q<Label>("hunt-tile-face").text);
                await CheckLayout(room, room.Sh, "smeagol-cost");
                Assert.AreEqual(0, room.Fp.Board.View.Fellowship.Corruption);
                Click(room.Sh, "effect-choice-lords.sh.character.25");
                await room.Confirm(room.Sh);
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
