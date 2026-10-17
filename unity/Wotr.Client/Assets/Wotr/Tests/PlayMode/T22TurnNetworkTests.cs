using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class T22TurnNetworkTests
    {
        [UnityTest]
        public IEnumerator FellowshipCanContinueWithAnExplicitEmptyHuntAllocation() => Wait(Allocation(false));

        [UnityTest]
        public IEnumerator RecoveredFellowshipDieRequiresAnExplicitHuntDieBeforeRolling() => Wait(Allocation(true));

        [UnityTest]
        public IEnumerator RingChangesChosenDieThenPoliticsSpendsThatExactCost() => Wait(RingPolitics());

        [UnityTest]
        public IEnumerator LegalPassChangesActorWithoutSpendingADie() => Wait(Pass());

        private static async Task Allocation(bool mandatory)
        {
            using (var room = await Connect(mandatory ? "turn-hunt-mandatory" : "turn-hunt-optional", ScenarioKind.BaseAcceptance))
            {
                Assert.AreEqual(DecisionKind.FellowshipPhase, room.Fp.Board.View.Decision.Kind);
                Assert.IsNull(room.Sh.Board.View.Decision);
                var revision = room.Fp.Board.View.Revision;
                FellowshipGestures.Action(room.Fp, "fellowship-continue");
                await room.At(revision + 1);
                Assert.AreEqual(DecisionKind.AllocateHunt, room.Sh.Board.View.Decision.Kind);
                Assert.IsNull(room.Fp.Board.View.Decision);
                var option = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.AllocateHunt);
                Assert.AreEqual(mandatory ? 1 : 0, option.Minimum);
                var chosen = option.EligibleIds.Last();
                if (mandatory)
                {
                    Assert.IsTrue(room.Sh.Board.Hunt.Tab.ClassListContains("hunt-confirm-waiting"), "UI-172: one die is required.");
                    await T22NetworkActions.Reject(room, room.Sh, CommandKind.AllocateHunt, new CommandPayload());
                    PointerGestureEvents.ClickAt(room.Sh.Board.OwnDice.Q<Button>(chosen));
                }
                else
                {
                    PointerGestureEvents.ClickAt(room.Sh.Board.OwnDice.Q<Button>(chosen));
                    await room.Settle();
                    // UI-162: a second click on the die returns it from the console's allocation.
                    PointerGestureEvents.ClickAt(room.Sh.Board.OwnDice.Q<Button>(chosen));
                }
                await T22NetworkActions.Ready(room, room.Sh);
                Assert.AreEqual(1, room.Sh.Board.View.Revision, "Selections and previews do not roll the dice.");
                await room.Confirm(room.Sh);
                Assert.AreEqual(GamePhase.ActionResolution, room.Fp.Board.View.Phase);
                Assert.AreEqual(Side.FreePeoples, room.Sh.Board.View.DecisionOwner);
                Assert.AreEqual(DecisionKind.Action, room.Fp.Board.View.Decision.Kind);
                var allocated = room.Fp.Board.View.Dice.Where(d => d.HuntOrigin == "allocated").ToArray();
                Assert.AreEqual(mandatory ? 1 : 0, allocated.Length);
                if (mandatory)
                {
                    Assert.AreEqual(chosen, allocated.Single().Id);
                    Assert.AreEqual(DieLocation.Hunt, allocated.Single().Location);
                }
                T22NetworkActions.SameDice(room);
            }
        }

        private static async Task RingPolitics()
        {
            using (var room = await Connect("turn-ring-politics", ScenarioKind.BaseAcceptance))
            {
                var view = room.Fp.Board.View;
                var die = view.Dice.Single(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Army).Id;
                var ring = view.Rings.Last(r => r.Owner == Side.FreePeoples).Id;
                // UI-160: the ring is chosen at its socket in the turn console.
                var socket = room.Fp.Board.Console.Rings.For(ring);
                CardNetworkTests.Click(socket);
                T22NetworkActions.Die(room.Fp, die);
                await room.Settle();
                Assert.IsTrue(socket.ClassListContains("selected"));
                Assert.AreEqual(1, room.Fp.Board.OwnDice.Query<Button>(className: "selected").ToList().Count);
                Assert.AreEqual(DieFace.Army, room.Sh.Board.View.Dice.Single(d => d.Id == die).Face);
                Assert.AreEqual(Side.FreePeoples, room.Sh.Board.View.Rings.Single(r => r.Id == ring).Owner);
                var revision = room.Fp.Board.View.Revision;
                // UI-161: the result is chosen in the popover above the die.
                Assert.IsTrue(room.Fp.Board.RingPicker.IsOpen);
                var face = room.Fp.Board.RingPicker.Root.Q<Button>("ring-face-muster");
                CardNetworkTests.Click(face);
                await room.At(revision + 1);
                foreach (var client in new[] { room.Fp, room.Sh })
                {
                    Assert.AreEqual(DieFace.Muster, client.Board.View.Dice.Single(d => d.Id == die).Face);
                    Assert.AreEqual(DieLocation.Available, client.Board.View.Dice.Single(d => d.Id == die).Location);
                    Assert.AreEqual(Side.Shadow, client.Board.View.Rings.Single(r => r.Id == ring).Owner);
                    Assert.AreEqual(Side.FreePeoples, client.Board.View.DecisionOwner);
                }
                // QUAL-04 (BASE 2.4.1 p. 21): after the Ring the player may still pass when eligible (fewer unused
                // dice, B03); the console's Pass follows the server's option.
                var pass = room.Fp.Board.View.Options.FirstOrDefault(o => o.Kind == CommandKind.Pass);
                Assert.AreEqual(pass?.Enabled == true, room.Fp.Board.Console.Actions.Pass.enabledSelf);
                Assert.IsFalse(room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.UseRing).Enabled);
                await Politics(room, die);
            }
        }

        private static async Task Politics(MatchTestRoom room, string die)
        {
            ArmyGestures.Die(room.Fp, die, DieFace.Muster);
            room.Fp.Board.Nations.Open();
            await room.Settle();
            Assert.IsEmpty(room.Fp.Board.Nations.Drawer.Query<Button>(className: "destination-selected").ToList());
            // UI-144/UI-178: the lit station and the row's arrow are there without selecting the nation; one click on
            // the arrow moves it, while the other rows keep a greyed arrow in place.
            Assert.IsTrue(room.Fp.Board.Nations.Drawer.Q<Button>("political-slot-elves-1").ClassListContains("legal-target"));
            var arrow = room.Fp.Board.Nations.Drawer.Q<Button>("nation-advance-elves");
            Assert.IsTrue(arrow.ClassListContains("legal-target"));
            Assert.IsTrue(arrow.enabledInHierarchy);
            var greyed = room.Fp.Board.Nations.Drawer.Q<Button>("nation-advance-sauron");
            Assert.AreEqual(Visibility.Visible, greyed.resolvedStyle.visibility);
            Assert.IsFalse(greyed.enabledInHierarchy, "An opponent's nation keeps a greyed arrow in place.");
            Assert.IsTrue(room.Fp.Board.OwnDice.Q<Button>(die).ClassListContains("selected"));
            Assert.AreEqual(2, room.Sh.Board.View.Nations.Single(n => n.Id == "elves").StepsToWar);
            Assert.IsNull(room.Fp.Board.Tray.Q<Button>("confirm-decision"));
            Assert.AreEqual(DisplayStyle.None, room.Fp.Board.Information.Root.resolvedStyle.display);
            PoliticalRecoveryTests.AssertNoBottomPolitics(room.Fp.Board);
            var revision = room.Fp.Board.View.Revision;
            CardNetworkTests.Click(arrow);
            await room.At(revision + 1);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                Assert.AreEqual(1, client.Board.View.Nations.Single(n => n.Id == "elves").StepsToWar);
                Assert.AreEqual(Side.Shadow, client.Board.View.DecisionOwner);
            }
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
        }

        private static async Task Pass()
        {
            using (var room = await Connect("turn-pass", ScenarioKind.BaseAcceptance))
            {
                var dice = JsonConvert.SerializeObject(room.Fp.Board.View.Dice);
                Assert.IsTrue(room.Fp.Board.Console.Actions.Pass.enabledSelf);
                AssertWaiting(room.Sh, true);
                AssertWaiting(room.Fp, false);
                var revision = room.Fp.Board.View.Revision;
                await ComponentGestures.Select(room.Fp, "Pass");
                await room.At(revision + 1);
                await room.Settle();
                // UI-171: the console line of the side that now acts must not keep the waiting guidance.
                AssertWaiting(room.Sh, false);
                AssertWaiting(room.Fp, true);
                StringAssert.StartsWith("YOUR ACTION", room.Sh.Board.Console.Status.Headline.text);
                Assert.AreEqual(Side.Shadow, room.Fp.Board.View.DecisionOwner);
                Assert.IsNull(room.Fp.Board.View.Decision);
                Assert.AreEqual(DecisionKind.Action, room.Sh.Board.View.Decision.Kind);
                Assert.AreEqual(dice, JsonConvert.SerializeObject(room.Fp.Board.View.Dice));
                T22NetworkActions.SameDice(room);
                Assert.IsFalse(room.Sh.Board.Console.Actions.Pass.enabledSelf);
                await T22NetworkActions.Reject(room, room.Sh, CommandKind.Pass, new CommandPayload());
            }
        }

        private static void AssertWaiting(MountedMatchClient client, bool waiting)
        {
            var hint = client.Board.Console.Status.Hint.text;
            if (waiting)
                StringAssert.StartsWith("The opponent is deciding", hint, client.Board.View.You + " waits");
            else
                StringAssert.DoesNotContain("opponent is deciding", hint, client.Board.View.You + " acts");
        }
    }
}
