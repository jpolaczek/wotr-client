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
    public class ExpansionTokenNetworkTests
    {
        [UnityTest] public IEnumerator FarSightedSeparationPreservesItsExactRulerCost() => Wait(Separate(false));
        [UnityTest] public IEnumerator FarSightedTokenSeparationKeepsTheConsumedTokenAcrossReconnect() => Wait(Separate(true));

        private static async Task Separate(bool token)
        {
            using (var room = await Connect(token ? "ui22-separate-token" : "ui22-separate", ScenarioKind.CardPack))
            {
                var client = room.Fp;
                if (token)
                    Token(client, "thranduil");
                var die = client.Board.View.Dice.Single(d => d.Source == "ruler" && d.Owner == Side.FreePeoples).Id;
                await ComponentGestures.Cost(room, client, die);
                await ComponentGestures.Select(client, "far-sighted-SeparateCompanions");
                const string companion = "base.character.legolas";
                FellowshipGestures.Portrait(client, companion);
                Assert.IsTrue(client.Board.Fellowship.Root.Q<Button>("fellowship-portrait-" + companion).ClassListContains("selected"));
                var origin = client.Board.View.Fellowship.RegionId;
                await Reconnect(room, client);
                var revision = client.Board.View.Revision;
                FellowshipGestures.FinishRoute(client, origin);
                await room.At(revision + 1);
                Assert.AreEqual(origin, room.Sh.Board.View.Characters.Single(c => c.Id == companion).Location);
                Assert.AreEqual(DecisionKind.Guide, client.Board.View.Decision.Kind);
                Assert.AreEqual(DieLocation.Resolving, client.Board.View.Dice.Single(d => d.Id == die).Location);
                await Reconnect(room, client);
                FellowshipGestures.Portrait(client, client.Board.View.Fellowship.Guide);
                await room.At(revision + 2);
                Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == die).Location);
                Assert.AreEqual(token ? "dismay" : "free-peoples", room.Sh.Board.View.RulerTokens.Single(t => t.Id.EndsWith("thranduil")).Location);
            }
        }

        [UnityTest] public IEnumerator RulerTokenPaysForAllThreeExplicitRecruitments() => Wait(Recruit());
        [UnityTest] public IEnumerator DenethorsTokenConvertsTheRulerAndCompletesBothDraws() => Wait(Denethor(false));
        [UnityTest] public IEnumerator DenethorsTokenConvertsTheRulerAndPlaysAnEvent() => Wait(Denethor(true));
        [UnityTest] public IEnumerator FarSightedIsAnExplicitModeForMovement() => Wait(Travel(false));
        [UnityTest] public IEnumerator ThranduilsTokenPaysForFarSightedInTheSameAction() => Wait(Travel(true));
        [UnityTest] public IEnumerator CouncilPoliticsSpendsTheTokenWithoutADie() => Wait(Council(false));
        [UnityTest] public IEnumerator CouncilTravelMovesTheChosenNazgulWithoutADie() => Wait(Council(true));
        [UnityTest] public IEnumerator CouncilTokenAndMusterEnterAKingsChieftain() => Wait(Entry());
        [UnityTest] public IEnumerator BalrogEntryOffersAnOptionalReserveFigure() => Wait(Balrog());

        private static void Token(MountedMatchClient client, string name) =>
            CardNetworkTests.Click(client.Board.OwnDice.Q<Button>("ruler-token-kings.token." + name));

        private static async Task Recruit()
        {
            using (var room = await Connect("ui09-ruler-recruit", ScenarioKind.CardPack))
            {
                var edoras = room.Fp.Board.View.Units.Count(u => u.Location == "edoras");
                Token(room.Fp, "theoden");
                var selectedRuler = room.Fp.Board.View.Dice.Single(d => d.Source == "ruler" && d.Owner == Side.FreePeoples).Id;
                await ComponentGestures.Cost(room, room.Fp, selectedRuler);
                await ComponentGestures.Select(room.Fp, "character-kings.sovereign.theoden-kings.mustering-of-the-mark");
                foreach (var region in new[] { "edoras", "helms-deep", "edoras" })
                {
                    room.Fp.Board.Map.Select(region);
                    Click(room.Fp, "character-reserve-rohan-Regular");
                }
                Assert.AreSame(room.Fp.Board.Characters.DecisionHost, room.Fp.Board.Tray.parent,
                    "The selected ability stays in its character inspector without a second action selection.");
                var cost = room.Fp.Board.View.Dice.Single(d => d.Source == "ruler" && d.Owner == Side.FreePeoples);
                Assert.IsNull(room.Fp.Board.Tray.Q<Button>("cost-" + cost.Id), "UI-239: the only die is taken for the player.");
                Assert.AreEqual(DieFace.Character, cost.Face, "The draft must not change the projected die.");
                await Layout(room, room.Fp, "ruler-recruit", "ui09");
                await Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                Assert.AreEqual(edoras + 2, room.Sh.Board.View.Units.Count(u => u.Location == "edoras"));
                Assert.AreEqual("dismay", room.Sh.Board.View.RulerTokens.Single(t => t.Id.EndsWith("theoden")).Location);
                var die = room.Sh.Board.View.Dice.Single(d => d.Source == "ruler" && d.Owner == Side.FreePeoples);
                Assert.AreEqual(DieLocation.Spent, die.Location);
                Assert.AreEqual(DieFace.Muster, die.Face);
            }
        }

        private static async Task Denethor(bool play)
        {
            using (var room = await Connect(play ? "ui09-ruler-event" : "ui09-ruler-draw", ScenarioKind.CardPack))
            {
                var cards = room.Fp.Board.View.Hand.Count;
                Token(room.Fp, "denethor");
                var ruler = room.Fp.Board.View.Dice.Single(d => d.Source == "ruler" && d.Owner == Side.FreePeoples);
                // UI-380: the token's click takes the Ruler die; UI-386: a click on the chosen die would release it.
                if (room.Fp.Board.ActionDie.DieId != ruler.Id)
                    CardNetworkTests.Click(room.Fp.Board.OwnDice.Q<Button>(ruler.Id));
                await Layout(room, room.Fp, play ? "ruler-event" : "ruler-draw", "ui09");
                await Reconnect(room, room.Fp);
                if (play)
                    await CardGestures.Play(room, room.Fp, "base.fp.character.02");
                else
                    await CardGestures.Draw(room, room.Fp, "character");
                Assert.AreEqual(cards + (play ? -1 : 1), room.Fp.Board.View.Hand.Count);
                Assert.AreEqual("dismay", room.Sh.Board.View.RulerTokens.Single(t => t.Id.EndsWith("denethor")).Location);
                Assert.AreEqual("Deck", room.Fp.Board.View.Decision.Presentation);
                Assert.IsNull(room.Fp.Board.Tray.Q<Button>("effect-choice-decline"));
                await Layout(room, room.Fp, play ? "ruler-event-extra" : "ruler-draw-extra", "ui09");
                await Reconnect(room, room.Fp);
                await CardGestures.Draw(room, room.Fp, "strategy");
                Assert.AreEqual(cards + (play ? 0 : 2), room.Fp.Board.View.Hand.Count);
                var die = room.Sh.Board.View.Dice.Single(d => d.Source == "ruler" && d.Owner == Side.FreePeoples);
                Assert.AreEqual(DieFace.Event, die.Face);
                Assert.AreEqual(DieLocation.Spent, die.Location);
            }
        }

        private static async Task Travel(bool token)
        {
            using (var room = await Connect(token ? "ui09-travel-token" : "ui09-travel", ScenarioKind.CardPack))
            {
                if (token)
                    Token(room.Fp, "thranduil");
                var die = room.Fp.Board.View.Dice.Single(d => d.Source == "ruler" && d.Owner == Side.FreePeoples).Id;
                await ComponentGestures.Cost(room, room.Fp, die);
                await ComponentGestures.Select(room.Fp, "far-sighted-MoveCompanions");
                var figure = room.Fp.Board.View.Characters.Single(c => c.Id == "kings.sovereign.thranduil");
                FellowshipGestures.Figure(room.Fp, figure.Location, figure.Id);
                Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.Targets.Contains("dale"));
                await Layout(room, room.Fp, token ? "ruler-travel-token" : "ruler-travel", "ui09");
                await Reconnect(room, room.Fp);
                var revision = room.Fp.Board.View.Revision;
                FellowshipGestures.FinishRoute(room.Fp, "dale");
                await room.At(revision + 1);
                if (room.Fp.Board.View.ActionSequence != null)
                    await ArmyGestures.Finish(room, room.Fp);
                Assert.AreEqual("dale", room.Sh.Board.View.Characters.Single(c => c.Id.EndsWith("thranduil")).Location);
                Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == die).Location);
                Assert.AreEqual(token ? "dismay" : "free-peoples", room.Sh.Board.View.RulerTokens.Single(t => t.Id.EndsWith("thranduil")).Location);
            }
        }

        private static async Task Council(bool travel)
        {
            using (var room = await Connect(travel ? "ui09-council-travel" : "ui09-council-politics", ScenarioKind.CardPack))
            {
                var dice = room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Available);
                var token = travel ? "council.move-shadow-characters" : "council.advance-politics";
                var initialRevision = room.Sh.Board.View.Revision;
                // UI-347 (D13): the Black Serpent can enter with the token. UI-388 (owner): its one click lights both uses
                // — his Characters row and the token's printed use — and opens no panel.
                PointerGestureEvents.ClickAt(room.Sh.Board.OwnDice.Q<Button>("pool-" + token));
                await room.Settle();
                var hint = room.Sh.Board.Console.Status.Hint.text;
                StringAssert.StartsWith("Click a lit Chieftain's row", hint);
                StringAssert.Contains(travel ? "the Nazgûl above the token" : "a glowing Shadow nation's step in Politics", hint);
                Assert.IsTrue(room.Sh.Board.OwnDice.Q<Button>("pool-" + token).ClassListContains("selected"));
                Assert.IsTrue(room.Sh.Board.Characters.Tab.ClassListContains("legal-target"), "The Chieftain's entry is lit.");
                Assert.IsFalse(room.Sh.Board.Nations.IsOpen, "A token of several uses opens no panel.");
                Assert.AreEqual(initialRevision, room.Sh.Board.View.Revision, "Its use still waits for its nation or figures.");
                string figure = "";
                string partner = "";
                var politics = room.Sh.Board.View.Nations.Single(n => n.Id == "sauron").StepsToWar;
                // UI-376/UI-377 (owner): the token's use opens no decision window.
                Assert.IsFalse(room.Sh.Board.DecisionWindow.IsOpen, "The Council token's use stands on its objects.");
                if (travel)
                {
                    var movable = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.UseCouncilToken).Travel.Select(t => t.Id).ToList();
                    figure = movable[0];
                    partner = movable[1];
                    var picker = await CouncilPicker(room);
                    Assert.IsFalse(room.Sh.Board.Nations.IsOpen);
                    // UI-379: a group of several Nazgûl is chosen, and a second click releases one of them.
                    foreach (var id in new[] { figure, partner, movable[2], movable[2] })
                    {
                        PointerGestureEvents.ClickAt(picker.Root.Q<Button>("council-travel-" + id));
                        await room.Settle();
                    }
                    Assert.IsTrue(picker.Root.Q<Button>("council-travel-" + figure).ClassListContains("selected"));
                    Assert.IsTrue(picker.Root.Q<Button>("council-travel-" + partner).ClassListContains("selected"));
                    Assert.IsFalse(picker.Root.Q<Button>("council-travel-" + movable[2]).ClassListContains("selected"));
                    Assert.IsTrue(room.Sh.Board.Map.DraftOverlay.Targets.Contains("hollin"));
                    StringAssert.Contains("Click a highlighted region to move there", room.Sh.Board.Console.Status.Hint.text);
                }
                else
                    Assert.IsTrue(room.Sh.Board.Nations.Root.ClassListContains("legal-target"), "Politics glows for the printed use.");
                await Layout(room, room.Sh, travel ? "council-travel" : "council-politics", "ui09");
                await Reconnect(room, room.Sh);
                if (travel)
                {
                    // UI-379: the destination click plans the group there, faintly with its count; Finish sends it.
                    var revision = room.Sh.Board.View.Revision;
                    FellowshipGestures.FinishRoute(room.Sh, "hollin");
                    await room.Settle();
                    Assert.AreEqual(revision, room.Sh.Board.View.Revision, "Planning a group does not end the action.");
                    Assert.IsTrue(room.Sh.Board.Map.PlannedFlights.Regions.Contains("hollin"));
                    StringAssert.Contains("click ✓ to finish", room.Sh.Board.Console.Status.Hint.text);
                    await Layout(room, room.Sh, "council-travel-planned", "ui379");
                    // UI-381: the console's ✓ finishes; the popover has none of its own.
                    var finish = room.Sh.Board.Console.Actions.Confirm;
                    await room.Until(() => finish.enabledInHierarchy);
                    PointerGestureEvents.ClickAt(finish);
                    await room.At(revision + 1);
                }
                else
                {
                    var revision = room.Sh.Board.View.Revision;
                    room.Sh.Board.Nations.Open();
                    await room.Settle();
                    PoliticalRecoveryTests.AssertNoBottomPolitics(room.Sh.Board);
                    Assert.AreEqual(revision, room.Sh.Board.View.Revision);
                    Assert.IsNull(room.Sh.Board.Tray.Q<Button>("confirm-decision"));
                    // UI-144: the Council token's step shows without selecting the nation.
                    var arrow = room.Sh.Board.Nations.Drawer.Q<Button>("political-slot-sauron-" + (politics - 1));
                    Assert.IsTrue(arrow.enabledInHierarchy);
                    CardNetworkTests.Click(arrow);
                    await room.At(revision + 1);
                }
                Assert.IsTrue(room.Fp.Board.View.CouncilTokens.Single(t => t.Id == token).Spent);
                Assert.AreEqual(dice, room.Fp.Board.View.Dice.Count(d => d.Location == DieLocation.Available));
                if (travel)
                {
                    Assert.AreEqual("hollin", room.Fp.Board.View.Units.Single(u => u.Id == figure).Location);
                    Assert.AreEqual("hollin", room.Fp.Board.View.Units.Single(u => u.Id == partner).Location);
                }
                else
                    Assert.AreEqual(politics - 1, room.Fp.Board.View.Nations.Single(n => n.Id == "sauron").StepsToWar);
            }
        }

        // UI-377: the Council token's travel is a small popover above the token pool, holding only the Nazgûl and the ✓.
        private static async Task<Wotr.Client.CouncilTravelPicker> CouncilPicker(MatchTestRoom room)
        {
            var board = room.Sh.Board;
            var picker = board.CouncilPicker;
            await room.Settle();
            Assert.IsTrue(picker.IsOpen, "The token's travel shows its popover.");
            Assert.LessOrEqual(picker.Root.worldBound.yMax, board.OwnDice.worldBound.yMin, "It stands above the token pool.");
            var token = board.OwnDice.Q<Button>("pool-" + Wotr.Client.ExpansionDraft.CouncilTravel).worldBound;
            Assert.That(picker.Root.worldBound.center.x, Is.EqualTo(token.center.x).Within(picker.Root.worldBound.width / 2));
            var figures = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.UseCouncilToken).Travel.Select(t => t.Id).ToList();
            CollectionAssert.AreEquivalent(figures.Select(id => "council-travel-" + id),
                picker.Root.Query<Button>(className: "council-travel-figure").ToList().Select(b => b.name));
            Assert.AreEqual(figures.Count, picker.Root.Query<Button>().ToList().Count, "Only the Nazgûl (UI-381: the console's ✓ finishes).");
            return picker;
        }

        private static async Task Entry()
        {
            using (var room = await Connect("ui09-entry", ScenarioKind.CardPack))
            {
                var choice = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.EnterCharacter).CharacterActions
                    .Single(c => c.CharacterId == "kings.character.black-serpent");
                const string token = "council.advance-politics";
                var initialRevision = room.Sh.Board.View.Revision;
                // UI-347 (D13): a Chieftain can enter with the token, so its click marks it and lights his row; no panel.
                PointerGestureEvents.ClickAt(room.Sh.Board.OwnDice.Q<Button>("pool-" + token));
                await room.Settle();
                StringAssert.StartsWith("Click a lit Chieftain's row", room.Sh.Board.Console.Status.Hint.text);
                Assert.IsTrue(room.Sh.Board.OwnDice.Q<Button>(choice.Dice[0]).ClassListContains("resource-candidate"));
                await ComponentGestures.Cost(room, room.Sh, choice.Dice[0]);
                Assert.IsEmpty(room.Sh.Board.ActionDie.DieId, "An entry cost must not enable the die's ordinary actions.");
                await Reconnect(room, room.Sh);
                Assert.AreEqual(initialRevision, room.Sh.Board.View.Revision);
                Assert.IsTrue(room.Sh.Board.OwnDice.Q<Button>("pool-" + token).ClassListContains("selected"));
                // UI-108/UI-142: the Serpent's row enters by a click; no panel offers it.
                await ComponentGestures.Enter(room, room.Sh, choice);
                Assert.IsNull(room.Sh.Board.Tray.Q<Button>("cost-" + choice.Dice[0]), "UI-239: the only die is not a button.");
                await Layout(room, room.Sh, "entry-token", "ui09");
                await Reconnect(room, room.Sh);
                var revision = room.Sh.Board.View.Revision;
                room.Sh.Board.Map.Select("near-harad");
                await room.At(revision + 1);
                Assert.AreEqual("near-harad", room.Fp.Board.View.Characters.Single(c => c.Id == choice.CharacterId).Location);
                Assert.IsTrue(room.Fp.Board.View.CouncilTokens.Single().Spent);
                Assert.AreEqual(DieLocation.Spent, room.Fp.Board.View.Dice.Single(d => d.Id == choice.Dice[0]).Location);
            }
        }

        private static async Task Balrog()
        {
            using (var room = await Connect("ui09-balrog", ScenarioKind.CardPack))
            {
                var choice = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.EnterCharacter).CharacterActions
                    .Single(c => c.CharacterId == "lords.character.balrog");
                await ComponentGestures.Cost(room, room.Sh, choice.Dice[0]);
                await ComponentGestures.Enter(room, room.Sh, choice);
                Click(room.Sh, "character-reserve-sauron-Elite");
                var before = room.Sh.Board.View.Units.Count(u => u.Location == "moria" && u.Kind == UnitKind.Elite);
                await Layout(room, room.Sh, "balrog-entry", "ui09");
                await room.Confirm(room.Sh);
                Assert.AreEqual(before + 1, room.Fp.Board.View.Units.Count(u => u.Location == "moria" && u.Kind == UnitKind.Elite));
                Assert.AreEqual("moria", room.Fp.Board.View.Characters.Single(c => c.Id == choice.CharacterId).Location);
            }
        }
    }
}
