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
    public class ExpansionDecisionNetworkTests
    {
        [UnityTest] public IEnumerator CouncilVersionsHomelandsAndShadowTokenSurviveReconnect() => Wait(Council());
        [UnityTest] public IEnumerator KeeperAndLesserChoicesPrecedeBothRulerDiscards() => Wait(Dice());
        [UnityTest] public IEnumerator DismayConsumesOneTokenAndRerollsTheSelectedRuler() => Wait(Dismay());
        [UnityTest] public IEnumerator NaryaActivatesItsNationWithTheChosenDie() => Wait(Narya());
        [UnityTest] public IEnumerator VilyaKeepsTheResolvingDieAvailable() => Wait(Vilya());
        [UnityTest] public IEnumerator AwakeningSelectsAmongAllFiveSovereignsAndTracesARoute() => Wait(Awaken());
        [UnityTest] public IEnumerator CorruptedSovereignRemovalIsAnExplicitAction() => Wait(Remove());

        private static async Task Council()
        {
            using (var room = await Connect("ui09-council", ScenarioKind.CardPack))
            {
                Assert.AreEqual(DecisionKind.CouncilVersions, room.Fp.Board.View.Decision.Kind);
                Assert.IsNull(room.Sh.Board.View.Decision);
                Assert.IsEmpty(room.Sh.Board.View.OwnCompanionVersions);
                Assert.IsEmpty(room.Sh.Board.Tray.Query<Button>().ToList());
                await Layout(room, room.Fp, "versions", "ui09");
                Click(room.Fp, "version-base.character.gandalf-grey");
                Click(room.Fp, "fellowship-portrait-base.character.gimli");
                Click(room.Fp, "version-fate.character.gimli-dwarven-lord");
                Click(room.Fp, "starting-guide-base.character.strider");
                await Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                Assert.AreEqual(DecisionKind.CouncilHomelands, room.Fp.Board.View.Decision.Kind);
                Assert.IsFalse(room.Sh.Board.View.Fellowship.RevealedCompanionVersions.ContainsKey("base.character.gimli"));
                // UI-270: the homelands on the Fellowship panel's portraits; a drag onto the map places one.
                Click(room.Fp, "fellowship-portrait-base.character.gimli");
                await room.Settle();
                await CardGestureNetworkTests.Drop(room, room.Fp,
                    room.Fp.Board.Fellowship.Root.Q<Button>("fellowship-portrait-base.character.gimli"),
                    room.Fp.Board.Map, false, dropPoint: MapContourProbe.ReachRegion(room.Fp.Board.Map, "ered-luin"));
                Assert.AreEqual("→ Ered Luin", room.Fp.Board.Fellowship.Root.Q<Button>("fellowship-portrait-base.character.gimli")
                    .Q<Label>("homeland-location").text);
                await Layout(room, room.Fp, "homeland", "ui09");
                await Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                Assert.AreEqual("ered-luin", room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.gimli").Location);
                Assert.AreEqual(DecisionKind.CouncilToken, room.Sh.Board.View.Decision.Kind);
                await Layout(room, room.Sh, "council-token", "ui09");
                var revision = room.Sh.Board.View.Revision;
                CardNetworkTests.Click(room.Sh.Board.OwnDice.Q<Button>("pool-council.advance-politics"));
                await room.At(revision + 1);
                Assert.IsTrue(room.Sh.Board.View.CouncilTokens.Any(t => t.Id == "council.advance-politics" && !t.Spent));
                Assert.IsTrue(room.Fp.Board.View.Configuration.NewCities);
                Assert.IsFalse(room.Fp.Board.View.Sovereigns.Any(s => s.Id.EndsWith("dain") || s.Id.EndsWith("brand")));
            }
        }

        private static async Task Dice()
        {
            using (var room = await Connect("ui09-dice", ScenarioKind.CardPack))
            {
                foreach (var client in new[] { room.Sh, room.Fp })
                {
                    Assert.AreEqual(DecisionKind.SpecialDieChoice, client.Board.View.Decision.Kind);
                    var id = client.Board.View.Options.Single().EligibleDice[0];
                    await Layout(room, client, "special-" + client.Board.View.You, "ui09");
                    await Reconnect(room, client);
                    var revision = client.Board.View.Revision;
                    PointerGestureEvents.ClickAt(client.Board.OwnDice.Q<Button>(id));
                    await room.At(revision + 1);
                    Assert.AreEqual(DieLocation.Hunt, client.Board.View.Dice.Single(d => d.Id == id).Location);
                }
                foreach (var client in new[] { room.Sh, room.Fp })
                {
                    Assert.AreEqual(DecisionKind.RulerDiscard, client.Board.View.Decision.Kind);
                    var id = client.Board.View.Options.Single().EligibleDice.First(d => client.Board.View.Dice.Single(x => x.Id == d).Source == "standard");
                    await Layout(room, client, "ruler-discard-" + client.Board.View.You, "ui09");
                    var revision = client.Board.View.Revision;
                    PointerGestureEvents.ClickAt(client.Board.OwnDice.Q<Button>(id));
                    await room.At(revision + 1);
                    Assert.AreEqual(DieLocation.Discarded, client.Board.View.Dice.Single(d => d.Id == id).Location);
                }
                Assert.AreEqual(DecisionKind.Action, room.Fp.Board.View.Decision.Kind);
            }
        }

        private static async Task Dismay()
        {
            using (var room = await Connect("ui09-dismay", ScenarioKind.CardPack))
            {
                Assert.AreEqual(DecisionKind.Dismay, room.Sh.Board.View.Decision.Kind);
                var option = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.UseDismay);
                CardNetworkTests.Click(room.Sh.Board.OwnDice.Q<Button>("ruler-token-" + option.EligibleIds[0]));
                var id = option.EligibleDice.Last();
                Assert.IsTrue(room.Sh.Board.OwnDice.Q<Button>("ruler-token-" + option.EligibleIds[0]).ClassListContains("selected"));
                await Layout(room, room.Sh, "dismay", "ui09");
                await Reconnect(room, room.Sh);
                var revision = room.Sh.Board.View.Revision;
                CardNetworkTests.Click(room.Sh.Board.Root.Q<Button>(id));
                await room.At(revision + 1);
                Assert.AreEqual("removed", room.Sh.Board.View.RulerTokens.Single(t => t.Id == option.EligibleIds[0]).Location);
                Assert.AreEqual(DecisionKind.Action, room.Fp.Board.View.Decision.Kind);
            }
        }

        private static async Task Narya()
        {
            using (var room = await Connect("ui09-narya", ScenarioKind.CardPack))
            {
                var option = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.UseNarya);
                await ComponentGestures.Cost(room, room.Fp, option.EligibleDice[0]);
                Assert.IsTrue(room.Fp.Board.Nations.Root.ClassListContains("legal-target"));
                await Layout(room, room.Fp, "narya", "ui09");
                var revision = room.Fp.Board.View.Revision;
                PointerGestureEvents.ClickAt(room.Fp.Board.Nations.Root);
                await room.Settle();
                // UI-144: Activate stands beside the passive nation without a click on its token.
                var activate = room.Fp.Board.Nations.Drawer.Q<Button>("nation-activate-north");
                Assert.AreEqual(Visibility.Visible, activate.resolvedStyle.visibility);
                Assert.IsTrue(activate.enabledInHierarchy);
                StringAssert.Contains("transfer Narya", activate.tooltip);
                Assert.AreEqual(revision, room.Fp.Board.View.Revision);
                PointerGestureEvents.ClickAt(activate);
                await room.At(revision + 1);
                Assert.AreEqual(0, room.Sh.Board.View.Nations.Single(n => n.Id == "north").StepsToWar);
                Assert.IsTrue(room.Sh.Board.View.Nations.Single(n => n.Id == "north").Active);
                Assert.AreEqual(Side.Shadow, room.Sh.Board.View.Rings.Single(r => r.Id.EndsWith("narya")).Owner);
            }
        }

        // UI-268: Vilya's socket glows beside the lit paid die; Spend the die is the console's button; one click on the
        // socket keeps the die, with no window.
        private static async Task Vilya()
        {
            using (var room = await Connect("ui09-vilya", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                Assert.AreEqual(DecisionKind.KeepActionDie, room.Fp.Board.View.Decision.Kind);
                await room.Settle();
                watch.OnItsObject(room.Fp);
                var die = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.UseVilya).EligibleDice.Single();
                VilyaReady(room.Fp, die);
                await Layout(room, room.Fp, "vilya", "ui268");
                await Reconnect(room, room.Fp);
                VilyaReady(room.Fp, die);
                var revision = room.Fp.Board.View.Revision;
                CardNetworkTests.Click(room.Fp.Board.Console.Rings.For(NamedRings.Vilya));
                await room.At(revision + 1);
                watch.NeverOpened();
                Assert.AreEqual(DieLocation.Available, room.Sh.Board.View.Dice.Single(d => d.Id == die).Location);
                Assert.AreEqual(Side.Shadow, room.Sh.Board.View.Rings.Single(r => r.Id.EndsWith("vilya")).Owner);
            }
        }

        private static void VilyaReady(MountedMatchClient client, string die)
        {
            var socket = client.Board.Console.Rings.For(NamedRings.Vilya);
            Assert.IsTrue(socket.ClassListContains(ConsoleRings.KeeperReadyClass), "Vilya's socket glows for its Keeper ability.");
            Assert.IsTrue(socket.ClassListContains("ring-vilya"));
            StringAssert.Contains("Click to keep the die that paid for this action", socket.tooltip);
            Assert.IsTrue(client.Board.OwnDice.Q<Button>(die).ClassListContains("resource-candidate"), "The paid die glows in the pool.");
            Assert.IsNull(client.Board.Tray.Q<Button>("cost-" + die), "No second die picker.");
            var spend = client.Board.Console.Status.Options.Q<Button>("action-DeclineVilya");
            Assert.IsNotNull(spend, "Spend the die is the console's button.");
            Assert.AreEqual("Spend the die", spend.text);
            Assert.IsFalse(client.Board.ResourceDecision.IsOpen, "No resource panel opens.");
        }

        private static async Task Awaken()
        {
            using (var room = await Connect("ui09-awaken", ScenarioKind.CardPack))
            {
                var option = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.AwakenSovereign && o.EligibleIds.Contains("kings.sovereign.dain"));
                await ComponentGestures.Cost(room, room.Fp, option.EligibleDice[0]);
                await Layout(room, room.Fp, "awaken", "ui09");
                // UI-274: Erebor is Dáin's only destination, so the click on his lit portrait completes the awakening
                // along his route from the Iron Hills (UI-142: a click that completes a choice sends it).
                var portrait = await BoardNavigation.Sovereign(room.Fp.Board, "kings.sovereign.dain");
                StringAssert.StartsWith("Click to awaken", portrait.tooltip);
                var revision = room.Fp.Board.View.Revision;
                CardNetworkTests.Click(portrait);
                await room.At(revision + 1);
                await Reconnect(room, room.Fp);
                Assert.AreEqual(SovereignStatus.Awakened, room.Sh.Board.View.Sovereigns.Single(s => s.Id.EndsWith("dain")).Status);
                Assert.AreEqual("erebor", room.Sh.Board.View.Characters.Single(c => c.Id.EndsWith("dain")).Location);
            }
        }

        private static async Task Remove()
        {
            using (var room = await Connect("ui09-remove", ScenarioKind.CardPack))
            {
                var die = room.Fp.Board.View.Options.First(o => o.Kind == CommandKind.RemoveCorruptedSovereign).EligibleDice[0];
                T22NetworkActions.Die(room.Fp, die);
                await Layout(room, room.Fp, "remove", "ui09");
                var revision = room.Fp.Board.View.Revision;
                PointerGestureEvents.ClickAt(await BoardNavigation.Sovereign(room.Fp.Board, "kings.sovereign.denethor"));
                await room.At(revision + 1);
                Assert.AreEqual(SovereignStatus.Removed, room.Sh.Board.View.Sovereigns.Single(s => s.Id.EndsWith("denethor")).Status);
            }
        }
    }
}
