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
    // UI-267: the Lords special-die choice in the dice pool on two clients. The kept die is chosen by a click on its lit
    // medallion; the dice not kept stay dimmed in their slot, in the own pool and in the opponent's top bar; a tie in
    // rolled dice lets the Free Peoples choose first; an Eye forced among several dice is kept without a click; a
    // reconnect keeps the lit dice; no window opens.
    public sealed class SpecialDiceNetworkTests
    {
        private const string Vilya = "lords.die.vilya";
        private const string Nenya = "lords.die.nenya";
        private const string Balrog = "lords.die.balrog";
        private const string Gothmog = "lords.die.gothmog";

        [UnityTest]
        public IEnumerator OnATieTheFreePeoplesKeepFirstAndTheOtherDiceWaitDimmedInTheirSlots() => Wait(Tie());

        [UnityTest]
        public IEnumerator AnEyeForcedAmongTheDiceIsKeptWithoutAClick() => Wait(ForcedEye());

        private static async Task Tie()
        {
            using (var room = await Connect("ui267-tie", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                Assert.AreEqual(DecisionKind.SpecialDieChoice, room.Fp.Board.View.Decision?.Kind, "A tie lets the Free Peoples choose first.");
                Assert.IsNull(room.Sh.Board.View.Decision);
                watch.OnItsObject(room.Fp);
                Lit(room.Fp, Vilya, "Click to keep Vilya · Army.");
                Lit(room.Fp, Nenya, "Click to keep Nenya · Event.");
                var vilya = room.Fp.Board.OwnDice.Q<Button>(Vilya);
                Assert.IsNotNull(vilya.Q("die-remove-star"), "Vilya's Army carries the Remove star.");
                Assert.IsNotNull(vilya.Q(className: "die-origin-vilya"));
                StringAssert.Contains("★ Remove · kept", vilya.tooltip);
                StringAssert.StartsWith("Click a highlighted die", room.Fp.Board.Tray.Q<Label>(className: "draft-status").text);
                await Layout(room, room.Fp, "tie-free-peoples", "ui267");
                await Reconnect(room, room.Fp);
                Lit(room.Fp, Nenya, "Click to keep Nenya · Event.");
                await Keep(room, room.Fp, Nenya);
                Assert.AreEqual(DieLocation.Available, room.Fp.Board.View.Dice.Single(d => d.Id == Nenya).Location);
                SetAside(room.Fp.Board.OwnDice, Vilya);
                SetAside(room.Sh.Board.OpponentDice, Vilya);
                Assert.AreEqual("0", room.Fp.Board.OwnDice.Q<Button>("pool-Discarded").text, "A die set aside is not counted out of play.");

                Assert.AreEqual(DecisionKind.SpecialDieChoice, room.Sh.Board.View.Decision?.Kind);
                watch.OnItsObject(room.Sh);
                Lit(room.Sh, Balrog, "Click to keep Balrog · Character.");
                Lit(room.Sh, Gothmog, "Click to keep Gothmog · Muster.");
                await Layout(room, room.Sh, "tie-shadow", "ui267");
                await Keep(room, room.Sh, Gothmog);
                SetAside(room.Sh.Board.OwnDice, Balrog);
                SetAside(room.Fp.Board.OpponentDice, Balrog);
                Assert.AreEqual(DecisionKind.Action, room.Fp.Board.View.Decision?.Kind);
                watch.NeverOpened();
            }
        }

        private static async Task ForcedEye()
        {
            using (var room = await Connect("ui267-forced-eye", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                Assert.AreEqual(DecisionKind.SpecialDieChoice, room.Sh.Board.View.Decision?.Kind, "The Shadow rolled more dice and chooses first.");
                watch.OnItsObject(room.Sh);
                Lit(room.Sh, Balrog, "Click to keep Balrog · Use Balrog.");
                Lit(room.Sh, Gothmog, "Click to keep Gothmog · Card Draw.");
                var revision = room.Sh.Board.View.Revision;
                await Keep(room, room.Sh, Balrog);
                // Vilya rolled an Eye and Nenya a Character: the Eye is the Free Peoples' only choice and needs no click.
                await room.At(revision + 2);
                Assert.AreEqual(DieLocation.Hunt, room.Sh.Board.View.Dice.Single(d => d.Id == Vilya).Location);
                SetAside(room.Fp.Board.OwnDice, Nenya);
                SetAside(room.Sh.Board.OpponentDice, Nenya);
                SetAside(room.Sh.Board.OwnDice, Gothmog);
                Assert.AreEqual(DecisionKind.Action, room.Fp.Board.View.Decision?.Kind);
                watch.NeverOpened();
            }
        }

        private static void Lit(MountedMatchClient client, string id, string tooltip)
        {
            var die = client.Board.OwnDice.Q<Button>(id);
            Assert.IsNotNull(die, id);
            Assert.IsTrue(die.ClassListContains("resource-candidate"), id + " glows as a die the choice can keep.");
            Assert.IsTrue(die.enabledSelf, id);
            Assert.IsTrue(die.ClassListContains(ActionPoolView.LordsDieClass), id);
            StringAssert.StartsWith(tooltip, die.tooltip);
        }

        private static async Task Keep(MatchTestRoom room, MountedMatchClient client, string id)
        {
            var revision = client.Board.View.Revision;
            PointerGestureEvents.ClickAt(client.Board.OwnDice.Q<Button>(id));
            await room.At(revision + 1);
        }

        private static void SetAside(VisualElement pool, string id)
        {
            var die = pool.Q<Button>(id);
            Assert.IsNotNull(die, id + " keeps its slot after the choice.");
            Assert.IsTrue(die.ClassListContains(ActionPoolView.SetAsideClass), id + " is dimmed as set aside.");
            Assert.IsFalse(die.enabledSelf, id);
            StringAssert.Contains("Not kept this turn", die.tooltip);
        }
    }
}
