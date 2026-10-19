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
    public class ExpansionWindowNetworkTests
    {
        [UnityTest] public IEnumerator EvilKnowledgeRetainsTheEliteAndFullBattleResourcesFit() => Wait(Siege());
        [UnityTest] public IEnumerator NenyaTransfersOnlyItsOwnRingAndResumesTheHunt() => Wait(Nenya());
        [UnityTest] public IEnumerator LoreCanResolveBeforeDismayWithoutSpendingItsToken() => Wait(Lore(false));
        [UnityTest] public IEnumerator LoreCanChooseALesserEyeFromTheHuntInspector() => Wait(Lore(true));
        [UnityTest] public IEnumerator KeepingVilyaCompletesThePaidActionWithOneClick() => Wait(DeclineVilya());

        private static async Task Siege()
        {
            using (var room = await Connect("t34-evil-knowledge", ScenarioKind.CardPack))
            {
                Assert.AreEqual(DecisionKind.SiegeContinuation, room.Sh.Board.View.Decision.Kind);
                var elites = room.Sh.Board.View.Units.Where(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Elite).Select(u => u.Id).ToArray();
                var paid = room.Sh.Board.View.Dice.Count(d => d.Location is DieLocation.Resolving or DieLocation.Spent);
                var evilKnowledge = room.Sh.Board.Tray.Q<Button>("action-UseEvilKnowledge");
                Assert.IsTrue(evilKnowledge.enabledSelf);
                Assert.IsNotNull(evilKnowledge.Q(className: "battle-direct-icon-evil-knowledge"), "UI-271: the siege footer shows Evil Knowledge as a graphic choice.");
                StringAssert.StartsWith("Click to extend the siege", evilKnowledge.tooltip);
                Assert.IsTrue(room.Sh.Board.View.Options.Any(o => o.Kind == CommandKind.ExtendSiege));
                Assert.IsTrue(room.Sh.Board.View.Options.Any(o => o.Kind == CommandKind.StopBattle));
                await Layout(room, room.Sh, "evil-knowledge", "t34");
                await Layout(room, room.Fp, "siege-defender", "t34");
                await Reconnect(room, room.Sh);
                await room.Direct(room.Sh, "action-UseEvilKnowledge");
                Assert.AreEqual(2, room.Fp.Board.View.Battle.Round);
                Assert.IsTrue(elites.All(id => room.Fp.Board.View.Units.Any(u => u.Id == id && u.Location == "minas-tirith")));
                Assert.AreEqual(paid, room.Fp.Board.View.Dice.Count(d => d.Location is DieLocation.Resolving or DieLocation.Spent));
                CardNetworkTests.Click(room.Sh.Board.BattlePolicy.Toggle);
                await room.Until(() => room.Sh.Board.View.AutoDecision.Version == 1);
                await room.Settle();
                await Layout(room, room.Sh, "battle-resources-policy", "t34");
            }
        }

        // UI-268: in the Hunt's tile step the Nenya socket glows and its click replaces the Eye tile; the Hunt window
        // lists no Nenya button and no window opens.
        private static async Task Nenya()
        {
            using (var room = await Connect("t34-nenya", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                Assert.IsNull(room.Sh.Board.View.Decision);
                await room.Settle();
                watch.OnItsObject(room.Fp);
                NenyaReady(room.Fp);
                await Layout(room, room.Fp, "nenya", "ui268");
                await Reconnect(room, room.Fp);
                NenyaReady(room.Fp);
                var revision = room.Fp.Board.View.Revision;
                CardNetworkTests.Click(room.Fp.Board.Console.Rings.For(NamedRings.Nenya));
                await room.At(revision + 1);
                Assert.IsTrue(room.Fp.Board.Console.Rings.For(NamedRings.Nenya).ClassListContains("ring-captured"));
                watch.NeverOpened();
                Assert.AreEqual(Side.Shadow, room.Sh.Board.View.Rings.Single(r => r.Id == "lords.ring.nenya").Owner);
                Assert.AreEqual(DecisionKind.HuntDamage, room.Fp.Board.View.Decision.Kind);
                Assert.AreEqual("base.hunt.standard.3.0.1", room.Sh.Board.View.Hunt.TileId);
                var damageRevision = room.Fp.Board.View.Revision;
                Click(room.Fp, "hunt-response-Ring");
                await room.At(damageRevision + 1);
                Assert.AreEqual(3, room.Sh.Board.View.Fellowship.Corruption);
            }
        }

        private static void NenyaReady(MountedMatchClient client)
        {
            var socket = client.Board.Console.Rings.For(NamedRings.Nenya);
            Assert.IsTrue(socket.ClassListContains(ConsoleRings.KeeperReadyClass), "Nenya's socket glows in the tile step.");
            StringAssert.Contains("Click to remove this Eye tile", socket.tooltip);
            Assert.IsNull(client.Board.Tray.Q<Button>("effect-choice-" + NamedRings.NenyaReaction), "Nenya is answered on its ring.");
            Assert.IsNotNull(client.Board.Tray.Q<Label>("nenya-socket-hint"));
            Assert.IsFalse(client.Board.Console.Rings.For(NamedRings.Vilya).ClassListContains(ConsoleRings.KeeperReadyClass));
        }

        private static async Task Lore(bool hunt)
        {
            using (var room = await Connect(hunt ? "t34-lore-eye" : "t34-lore-dismay", ScenarioKind.CardPack))
            {
                var tokens = room.Sh.Board.View.RulerTokens.Count(t => t.Location == "dismay");
                Assert.AreEqual(DecisionKind.Dismay, room.Sh.Board.View.Decision.Kind);
                await Layout(room, room.Sh, "lore-order", "t34");
                await room.Direct(room.Sh, "action-UseLoreBeforeActions");
                var die = room.Sh.Board.View.Dice.Single(d => d.Owner == Side.Shadow &&
                    (hunt ? d.Source == "lords.die.balrog" && d.Location == DieLocation.Hunt : d.Source == "ruler"));
                StringAssert.Contains(hunt ? "Balrog" : "Ruler", room.Sh.Board.Tray.Q<Button>("effect-choice-" + die.Id).text);
                if (hunt)
                {
                    // UI-230: the Balrog die in the Hunt box stands in the pool's row while Lore may choose it.
                    PointerGestureEvents.ClickAt(room.Sh.Board.OwnDice.Q<Button>(die.Id));
                    await room.Settle();
                    Assert.IsTrue(room.Sh.Board.Tray.Q<Button>("effect-choice-" + die.Id).ClassListContains("selected"));
                }
                else
                    CardNetworkTests.Click(room.Sh.Board.OwnDice.Q<Button>(die.Id));
                Click(room.Sh, "effect-choice-lore.Muster.keep");
                await Layout(room, room.Sh, hunt ? "lore-eye" : "lore-ruler", "t34");
                await Reconnect(room, room.Sh);
                await room.Confirm(room.Sh);
                Assert.AreEqual(DecisionKind.Dismay, room.Sh.Board.View.Decision.Kind);
                Assert.AreEqual(DieFace.Muster, room.Fp.Board.View.Dice.Single(d => d.Id == die.Id).Face);
                Assert.AreEqual(tokens, room.Fp.Board.View.RulerTokens.Count(t => t.Location == "dismay"));
                Assert.IsNull(room.Sh.Board.Tray.Q<Button>("action-UseLoreBeforeActions"));
                await room.Direct(room.Sh, "action-DeclineDismay");
                Assert.AreEqual(DecisionKind.Action, room.Fp.Board.View.Decision.Kind);
            }
        }

        private static async Task DeclineVilya()
        {
            using (var room = await Connect("t34-vilya-decline", ScenarioKind.CardPack))
            {
                var paid = room.Fp.Board.View.Dice.Single(d => d.Location == DieLocation.Resolving).Id;
                await room.Direct(room.Fp, "action-DeclineVilya");
                Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == paid).Location);
                Assert.AreEqual(Side.FreePeoples, room.Sh.Board.View.Rings.Single(r => r.Id == "lords.ring.vilya").Owner);
            }
        }
    }
}
