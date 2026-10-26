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
    // UI-271: the Lords characters act on their objects: entries from their rows in the Characters panel (the Mouth's and
    // the Witch-king's versions as rows of their own), the Chief's reactions at the Character deck and at his figure, and
    // the Balrog card's order on his figure and his army (UI-347, D11) with his walk on the map. No decision window opens.
    public class LordsCharacterNetworkTests
    {
        private const string Mouth = "base.character.mouth-of-sauron";
        private const string LordsMouth = "lords.character.mouth-of-sauron";
        private const string WitchKing = "base.character.witch-king";
        private const string Chief = "lords.character.witch-king";
        private const string Balrog = "lords.character.balrog";

        [UnityTest] public IEnumerator ElrondEntersFromHisRowAndGaladrielsRowIsLit() => Wait(Keepers());
        [UnityTest] public IEnumerator TheLordsMouthEntersFromItsOwnVersionRow() => Wait(Minions());
        [UnityTest] public IEnumerator TheChiefsRowTurnsTheBlackCaptainInPlace() => Wait(Switch());
        [UnityTest] public IEnumerator SpearOfTerrorDrawsFromTheCharacterDeck() => Wait(Spear());
        [UnityTest] public IEnumerator HeSeesMovesTheChiefFromHisFigure() => Wait(HeSees());
        [UnityTest] public IEnumerator TheBalrogsCardMovesHimAloneAlongHisPath() => Wait(BalrogAlone());
        [UnityTest] public IEnumerator TheBalrogsCardMovesHisArmyFromItsFigure() => Wait(BalrogArmy());
        [UnityTest] public IEnumerator TheBalrogsCardFinishesWithThePass() => Wait(BalrogFinish());

        private static async Task Keepers()
        {
            using (var room = await Connect("ui271-entries-fp", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var choice = Entry(room.Fp, "lords.character.elrond");
                await ComponentGestures.Cost(room, room.Fp, choice.Dice[0]);
                Assert.IsTrue(Row(room.Fp, "lords.character.galadriel").ClassListContains(CharacterRosterPanel.EntryClass));
                await Layout(room, room.Fp, "keepers", "ui271");
                await Enter(room, room.Fp, choice, "rivendell");
                Assert.AreEqual("rivendell", room.Sh.Board.View.Characters.Single(c => c.DefinitionId == "lords.character.elrond").Location);
                watch.NeverOpened();
            }
        }

        // Both Mouths and both Witch-kings stand as rows of their own, named by their version; the Lords Mouth enters from
        // its row and the Mouth's rows become his one row on the board.
        private static async Task Minions()
        {
            using (var room = await Connect("ui271-entries-sh", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                var choice = Entry(client, LordsMouth);
                await ComponentGestures.Cost(room, client, choice.Dice[0]);
                client.Board.Characters.Open();
                await room.Settle();
                Assert.AreEqual("Lieutenant", Detail(client, CharacterRosterVersions.Key(Mouth, Mouth)));
                Assert.AreEqual("Númenórean", Detail(client, CharacterRosterVersions.Key(Mouth, LordsMouth)));
                Assert.AreEqual("Black Captain", Detail(client, CharacterRosterVersions.Key(WitchKing, WitchKing)));
                Assert.AreEqual("Chief", Detail(client, CharacterRosterVersions.Key(WitchKing, Chief)));
                foreach (var row in new[] { CharacterRosterVersions.Key(Mouth, LordsMouth), "lords.character.gothmog", Balrog })
                    Assert.IsTrue(Row(client, row).ClassListContains(CharacterRosterPanel.EntryClass), row);
                await Layout(room, client, "shadow-entries", "ui271");
                await Enter(room, client, choice, "barad-dur");
                var mouth = room.Fp.Board.View.Characters.Single(c => c.Id == Mouth);
                Assert.AreEqual((LordsMouth, "barad-dur"), (mouth.DefinitionId, mouth.Location));
                await room.Settle();
                Assert.IsNotNull(Row(client, Mouth));
                Assert.IsNull(client.Board.Characters.Root.Q<Button>("character-roster-" + CharacterRosterVersions.Key(Mouth, Mouth)));
                watch.NeverOpened();
            }
        }

        private static async Task Switch()
        {
            using (var room = await Connect("ui271-witch-king-switch", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                var choice = Entry(client, Chief);
                await ComponentGestures.Cost(room, client, choice.Dice[0]);
                client.Board.Characters.Open();
                await room.Settle();
                var chief = Row(client, CharacterRosterVersions.Key(WitchKing, Chief));
                Assert.AreEqual("characters-section-NotInPlay", chief.parent.parent.name);
                StringAssert.StartsWith("Click to turn The Witch-king, the Black Captain into The Witch-king, Chief of the Ringwraiths", chief.tooltip);
                Assert.IsFalse(Row(client, WitchKing).ClassListContains(CharacterRosterPanel.EntryClass),
                    "The Black Captain's own row inspects him; the Chief's row changes him.");
                await Enter(room, client, choice, "minas-morgul");
                var witchKing = room.Fp.Board.View.Characters.Single(c => c.Id == WitchKing);
                Assert.AreEqual((Chief, "minas-morgul"), (witchKing.DefinitionId, witchKing.Location));
                watch.NeverOpened();
            }
        }

        private static async Task Spear()
        {
            using (var room = await Connect("ui271-spear", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                await room.Settle();
                Assert.IsNull(client.Board.Console.Status.Options.Q<Button>("effect-choice-" + ChiefReactionObjects.Spear),
                    "Spear of Terror is used at the Character deck, not a console button.");
                // UI-291: the console's Pass declines the window; no Decline option.
                Assert.IsTrue(client.Board.Console.Actions.Pass.enabledSelf);
                StringAssert.Contains("Spear of Terror", client.Board.Console.Actions.Pass.tooltip);
                var deck = client.Board.Decks.Root.Q<Button>("deck-character");
                Assert.IsTrue(deck.enabledInHierarchy && deck.ClassListContains("legal-target"));
                StringAssert.StartsWith("Click to draw a Character card · Spear of Terror", deck.tooltip);
                await Layout(room, client, "spear", "ui271");
                var hand = client.Board.View.Hand.Count;
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(deck);
                await room.At(revision + 1);
                Assert.AreEqual(hand + 1, client.Board.View.Hand.Count);
                watch.NeverOpened();
            }
        }

        private static async Task HeSees()
        {
            using (var room = await Connect("ui271-he-sees", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                await room.Settle();
                Assert.IsNull(client.Board.Console.Status.Options.Q<Button>("effect-choice-" + ChiefReactionObjects.Follow));
                client.Board.Hover.Open("minas-morgul");
                await room.Settle();
                var figure = client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + WitchKing);
                Assert.IsTrue(figure.ClassListContains("eligible-figure"));
                StringAssert.Contains(ChiefReactionObjects.FollowOffer, figure.tooltip);
                await Layout(room, client, "he-sees", "ui271");
                client.Board.Hover.Open("minas-morgul");
                await room.Settle();
                figure = client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + WitchKing);
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(figure);
                await room.At(revision + 1);
                Assert.AreEqual("edoras", room.Fp.Board.View.Characters.Single(c => c.Id == WitchKing).Location);
                watch.NeverOpened();
            }
        }

        // UI-347 (D11): the Balrog card's order stands on his figure in his region's panel, with no console button; "alone"
        // arms his region on the map, and the click on a lit region two steps away walks him there along his path; the
        // console's ✓ sends the move.
        private static async Task BalrogAlone()
        {
            using (var room = await Connect("ui271-balrog-order", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                await room.Settle();
                foreach (var choice in new[] { BalrogCardOrder.Alone, BalrogCardOrder.Army, OptionalDecline.BalrogFinish })
                    Assert.IsNull(client.Board.Console.Status.Options.Q<Button>("effect-choice-" + choice), choice);
                StringAssert.StartsWith("Click the Balrog's glowing region, then his figure", client.Board.Console.Status.Hint.text);
                StringAssert.EndsWith("Or Pass to finish without moving the Balrog.", client.Board.Console.Status.Hint.text);
                var figure = await PanelFigure(room, client, "moria", Balrog);
                StringAssert.Contains(BalrogCardOrder.Offer(BalrogCardOrder.Alone), figure.tooltip);
                await Layout(room, client, "balrog-order", "ui347");
                figure = await PanelFigure(room, client, "moria", Balrog);
                var order = client.Board.View.Revision;
                CardNetworkTests.Click(figure);
                await room.At(order + 1);
                Assert.AreEqual(DecisionKind.CharacterEvent, client.Board.View.Decision.Kind);
                var travel = client.Board.View.Options.Single(o => o.Kind == CommandKind.MoveShadowCharacters).Travel.Single();
                var near = client.Board.Presentation.Regions["moria"].Neighbors;
                var far = travel.Targets.First(r => r != "moria" && !near.Contains(r));
                FellowshipGestures.Region(client, "moria");
                await room.Settle();
                Assert.That(client.Board.Console.Status.Hint.text, Does.Contain("move the armed Balrog"));
                Assert.IsTrue(client.Board.Map.DraftOverlay.Targets.Contains(far));
                await Layout(room, client, "balrog-alone", "ui271");
                FellowshipGestures.Destination(client, far);
                await room.Settle();
                Assert.IsTrue(client.Board.Map.PlannedFlights.Regions.Contains(far));
                Assert.That(client.Board.Console.Status.Hint.text, Does.StartWith("Figures moved 1"));
                // The console's ✓ sends the move; another click on the destination would only re-arm nothing.
                var confirm = client.Board.Console.Actions.Confirm;
                Assert.IsTrue(confirm.enabledSelf);
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(confirm);
                await room.At(revision + 1);
                Assert.AreEqual(far, room.Fp.Board.View.Characters.Single(c => c.Id == Balrog).Location);
                watch.NeverOpened();
            }
        }

        // UI-347 (D11): a figure of the army the Balrog stands with moves that army with him: the card's army step opens
        // with that army's move already started and its panel open.
        private static async Task BalrogArmy()
        {
            using (var room = await Connect("ui347-balrog", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                await room.Settle();
                var unit = client.Board.View.Units.First(u => u.Location == "moria" && u.Side == Side.Shadow);
                var figure = await PanelFigure(room, client, "moria", unit.Id);
                StringAssert.Contains(BalrogCardOrder.Offer(BalrogCardOrder.Army), figure.tooltip);
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(figure);
                await room.At(revision + 1);
                Assert.AreEqual(DecisionKind.ArmyEvent, client.Board.View.Decision.Kind);
                await room.Until(() => client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + unit.Id)?.ClassListContains("selected") == true);
                Assert.AreEqual("moria", client.Board.Hover.Panel.Region, "The army's move starts with its panel open.");
                watch.NeverOpened();
            }
        }

        // UI-347 (D7, D11): the console's Pass finishes the card without moving the Balrog.
        private static async Task BalrogFinish()
        {
            using (var room = await Connect("ui347-balrog-finish", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                await room.Settle();
                await room.Pass(client, "Pass · Finish without moving the Balrog");
                Assert.AreEqual("moria", room.Fp.Board.View.Characters.Single(c => c.Id == Balrog).Location);
                Assert.AreNotEqual(BalrogCardOrder.Presentation, client.Board.View.Decision?.Presentation ?? "");
                watch.NeverOpened();
            }
        }

        private static async Task<Button> PanelFigure(MatchTestRoom room, MountedMatchClient client, string region, string id)
        {
            client.Board.Hover.Open(region);
            await room.Settle();
            var figure = client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + id);
            Assert.IsNotNull(figure, id);
            Assert.IsTrue(figure.ClassListContains("eligible-figure"), id + " is lit.");
            return figure;
        }

        private static CharacterActionOption Entry(MountedMatchClient client, string version) => client.Board.View.Options
            .Single(o => o.Kind == CommandKind.EnterCharacter).CharacterActions.Single(c => c.CharacterId == version);

        private static async Task Enter(MatchTestRoom room, MountedMatchClient client, CharacterActionOption choice, string region)
        {
            var revision = client.Board.View.Revision;
            await ComponentGestures.Enter(room, client, choice);
            if (choice.Targets.Count > 1)
            {
                await room.Settle();
                client.Board.Map.Select(region);
            }
            await room.At(revision + 1);
        }

        private static Button Row(MountedMatchClient client, string key)
        {
            var row = client.Board.Characters.Root.Q<Button>("character-roster-" + key);
            Assert.IsNotNull(row, key);
            return row;
        }

        private static string Detail(MountedMatchClient client, string key) => Row(client, key).Q<Label>("character-roster-detail").text;
    }
}
