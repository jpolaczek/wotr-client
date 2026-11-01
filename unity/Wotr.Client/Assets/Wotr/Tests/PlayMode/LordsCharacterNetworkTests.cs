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
    // UI-271: the Lords characters act on their objects: entries from their rows in the Characters panel (UI-438: the Mouth's
    // and the Witch-king's versions inside one row each), the Chief's reactions at the Character deck and at his figure, and
    // the Balrog card's order on his figure and his army (UI-347, D11) with his walk on the map. No decision window opens.
    public class LordsCharacterNetworkTests
    {
        private const string Mouth = "base.character.mouth-of-sauron";
        private const string LordsMouth = "lords.character.mouth-of-sauron";
        private const string WitchKing = "base.character.witch-king";
        private const string Chief = "lords.character.witch-king";
        private const string Balrog = "lords.character.balrog";

        [UnityTest] public IEnumerator ElrondEntersFromHisRowAndGaladrielsRowIsLit() => Wait(Keepers());
        [UnityTest] public IEnumerator TheLordsMouthEntersFromTheMouthsOneRow() => Wait(Minions());
        [UnityTest] public IEnumerator TheWitchKingsRowTurnsTheBlackCaptainInPlace() => Wait(Switch());
        [UnityTest] public IEnumerator SpearOfTerrorDrawsFromTheCharacterDeck() => Wait(Spear());
        [UnityTest] public IEnumerator HeSeesMovesTheChiefFromHisFigure() => Wait(HeSees());
        [UnityTest] public IEnumerator TheBalrogsCardMovesHimAloneAlongHisPath() => Wait(BalrogAlone());
        [UnityTest] public IEnumerator TheBalrogsCardMovesHisArmyFromItsFigure() => Wait(BalrogArmy());
        [UnityTest] public IEnumerator TheBalrogsCardFinishesWithTheConfirm() => Wait(BalrogFinish());

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

        // UI-438 (owner): the Mouth of Sauron and the Witch-king have one row each, like Strider/Aragorn, and their windows
        // switch between the base and the Lords version. Every Free Peoples nation is at war here, so one Muster die may bring
        // in either version of each: the row's click opens the window (UI-108's explicit choice), the other version's tab
        // glows, and the shown version's Enter sends the same entry the version's own row sent. The Mouth's row stays his
        // one row on the board.
        private static async Task Minions()
        {
            using (var room = await Connect("ui271-entries-sh", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                client.Board.Characters.Open();
                await room.Settle();
                OneRowEach(client);
                await Versions(room, client, Mouth, (Mouth, "Lieutenant"), (LordsMouth, "Númenórean"));
                await Versions(room, client, WitchKing, (WitchKing, "Black Captain"), (Chief, "Chief"));
                var choice = Entry(client, LordsMouth);
                Assert.IsTrue(Entry(client, Mouth).Dice.Contains(choice.Dice[0]), "One Muster die may bring in either Mouth.");
                await ComponentGestures.Cost(room, client, choice.Dice[0]);
                client.Board.Characters.Open();
                await room.Settle();
                OneRowEach(client);
                foreach (var figure in new[] { Mouth, WitchKing })
                {
                    var row = Row(client, figure);
                    Assert.IsFalse(row.ClassListContains(CharacterRosterPanel.EntryClass), figure + ": its click opens the window");
                    Assert.IsTrue(row.ClassListContains(ChoiceGlow.ClassName), figure);
                    StringAssert.StartsWith(CharacterRosterPresentation.ChooseVersionOffer, CharacterRowTooltip.Plain(row.tooltip), figure);
                }
                foreach (var row in new[] { "lords.character.gothmog", Balrog })
                    Assert.IsTrue(Row(client, row).ClassListContains(CharacterRosterPanel.EntryClass), row);
                await Layout(room, client, "shadow-entries", "ui438");
                await EnterFromWindow(room, client, Mouth, choice, "barad-dur");
                var mouth = room.Fp.Board.View.Characters.Single(c => c.Id == Mouth);
                Assert.AreEqual((LordsMouth, "barad-dur"), (mouth.DefinitionId, mouth.Location));
                await room.Settle();
                Assert.AreEqual("characters-section-OnBoard", Row(client, Mouth).parent.parent.name);
                OneRowEach(client);
                watch.NeverOpened();
            }
        }

        // The row opens the window; the version's glowing tab shows it, and its Enter starts the version's entry.
        private static async Task EnterFromWindow(MatchTestRoom room, MountedMatchClient client, string figure, CharacterActionOption choice,
            string region)
        {
            var revision = client.Board.View.Revision;
            PointerGestureEvents.ClickAt(Row(client, figure));
            await room.Settle();
            var inspector = client.Board.Characters.Inspector;
            Assert.AreEqual(figure, inspector.FigureId);
            var tab = inspector.Root.Q<Button>("character-version-" + choice.CharacterId);
            Assert.IsTrue(tab.ClassListContains(ChoiceGlow.ClassName), "The other version that may enter glows.");
            PointerGestureEvents.ClickAt(tab);
            await room.Settle();
            Assert.AreEqual(choice.CharacterId, inspector.DefinitionId);
            await Shot(room, client, "ui438-mouth-choice-window.png");
            var enter = inspector.Actions.Query<Button>().ToList().Single(button => button.name.StartsWith("component-character-" + choice.CharacterId));
            PointerGestureEvents.ClickAt(enter);
            if (choice.Targets.Count > 1)
            {
                await room.Settle();
                client.Board.Map.Select(region);
            }
            await room.At(revision + 1);
        }

        // UI-438: with the Black Captain in play his one row turns him into the Chief in place; his window switches between
        // the two versions.
        private static async Task Switch()
        {
            using (var room = await Connect("ui271-witch-king-switch", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                client.Board.Characters.Open();
                await room.Settle();
                await Versions(room, client, WitchKing, (WitchKing, "Black Captain"), (Chief, "Chief"));
                var choice = Entry(client, Chief);
                await ComponentGestures.Cost(room, client, choice.Dice[0]);
                client.Board.Characters.Open();
                await room.Settle();
                OneRowEach(client);
                var row = Row(client, WitchKing);
                Assert.AreEqual("characters-section-OnBoard", row.parent.parent.name);
                StringAssert.StartsWith("Click to turn The Witch-king, the Black Captain into The Witch-king, Chief of the Ringwraiths",
                    CharacterRowTooltip.Plain(row.tooltip));
                await Layout(room, client, "witch-king-switch", "ui438");
                await Enter(room, client, choice, "minas-morgul");
                var witchKing = room.Fp.Board.View.Characters.Single(c => c.Id == WitchKing);
                Assert.AreEqual((Chief, "minas-morgul"), (witchKing.DefinitionId, witchKing.Location));
                watch.NeverOpened();
            }
        }

        // No character has two rows in the Characters panel.
        private static void OneRowEach(MountedMatchClient client)
        {
            var roster = client.Board.View.OwnCharacterRoster;
            var identities = client.Board.Characters.Root.Query<Button>(className: "character-roster-row").ToList()
                .Select(row => roster.Single(c => "character-roster-" + c.FigureId == row.name).Identity).ToList();
            CollectionAssert.AllItemsAreUnique(identities);
            Assert.AreEqual(1, identities.Count(identity => identity == "mouth-of-sauron"));
            Assert.AreEqual(1, identities.Count(identity => identity == "witch-king"));
        }

        // A click on the character's row opens his window, whose tabs name his versions and switch the card it shows.
        private static async Task Versions(MatchTestRoom room, MountedMatchClient client, string figure, params (string Id, string Name)[] versions)
        {
            PointerGestureEvents.ClickAt(Row(client, figure));
            await room.Settle();
            var inspector = client.Board.Characters.Inspector;
            Assert.AreEqual(figure, inspector.FigureId);
            foreach (var (id, name) in versions)
            {
                var tab = inspector.Root.Q<Button>("character-version-" + id);
                Assert.IsNotNull(tab, id);
                Assert.AreEqual(name, tab.text);
                PointerGestureEvents.ClickAt(tab);
                await room.Settle();
                Assert.AreEqual(id, inspector.DefinitionId);
            }
            await Shot(room, client, $"ui438-{figure}-window.png");
            CardNetworkTests.Click(inspector.Root.Q<Button>("close-character"));
            await room.Settle();
        }

        private static async Task Shot(MatchTestRoom room, MountedMatchClient client, string file)
        {
            var target = new UnityEngine.RenderTexture(1920, 1080, 24);
            target.Create();
            client.Panel.targetTexture = target;
            await room.Settle();
            LayoutTests.Save(target, file);
            client.Panel.targetTexture = client.Viewport;
            target.Release();
            UnityEngine.Object.Destroy(target);
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
                foreach (var choice in new[] { BalrogCardOrder.Alone, BalrogCardOrder.Army, FinishingOption.BalrogFinish })
                    Assert.IsNull(client.Board.Console.Status.Options.Q<Button>("effect-choice-" + choice), choice);
                StringAssert.StartsWith("Click the Balrog's glowing army to move it with him, or his region and then his figure",
                    client.Board.Console.Status.Hint.text);
                StringAssert.EndsWith("Or ✓ to finish without moving the Balrog.", client.Board.Console.Status.Hint.text);
                var figure = await MapFigure(room, client, "moria", Balrog);
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
                // UI-417 (owner): his figure's click armed him: his destinations are lit without another click.
                await room.Until(() => client.Board.Map.DraftOverlay.Targets.Contains(far));
                Assert.That(client.Board.Console.Status.Hint.text, Does.Contain("move the armed Balrog"));
                var committed = client.Board.Console.Status.Trail.Query<Button>(className: "committed-choice").ToList();
                Assert.AreEqual(1, committed.Count, "One committed button for the card.");
                Assert.AreEqual("✓ A Balrog is Come! · Balrog alone", committed[0].text);
                await Layout(room, client, "balrog-alone", "ui271");
                // His one destination's click sends the move: no ✓.
                var revision = client.Board.View.Revision;
                FellowshipGestures.Destination(client, far);
                await room.At(revision + 1);
                Assert.AreEqual(far, room.Fp.Board.View.Characters.Single(c => c.Id == Balrog).Location);
                watch.NeverOpened();
            }
        }

        // UI-347 (D11): a figure of the army the Balrog stands with moves that army with him: the card's army step opens
        // with that army's move already started and its panel open. UI-417 (owner): the army's lit marker on the map does
        // the same with one click.
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
                client.Board.Hover.Close();
                var marker = client.Board.Map.Q<Button>("army-moria-Shadow");
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(marker);
                await room.At(revision + 1);
                Assert.AreEqual(DecisionKind.ArmyEvent, client.Board.View.Decision.Kind);
                await room.Until(() => client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + unit.Id)?.ClassListContains("selected") == true);
                Assert.AreEqual("moria", client.Board.Hover.Panel.Region, "The army's move starts with its panel open.");
                watch.NeverOpened();
            }
        }

        // UI-347 (D11), UI-430 (owner's rule): the console's ✓ finishes the card without moving the Balrog.
        private static async Task BalrogFinish()
        {
            using (var room = await Connect("ui347-balrog-finish", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var client = room.Sh;
                await room.Settle();
                var finish = client.Board.Console.Actions.Confirm;
                Assert.IsTrue(finish.enabledSelf && finish.ClassListContains(ChoiceGlow.ClassName), "The ✓ finishes.");
                Assert.AreEqual("Finish · End A Balrog is Come! without moving the Balrog", finish.tooltip);
                Assert.IsFalse(client.Board.Console.Actions.Pass.ClassListContains(ChoiceGlow.ClassName), "The Pass declines nothing here.");
                var revision = client.Board.View.Revision;
                PointerGestureEvents.ClickAt(finish);
                await room.At(revision + 1);
                Assert.AreEqual("moria", room.Fp.Board.View.Characters.Single(c => c.Id == Balrog).Location);
                Assert.AreNotEqual(BalrogCardOrder.Presentation, client.Board.View.Decision?.Presentation ?? "");
                watch.NeverOpened();
            }
        }

        // UI-374 (owner's report): the Balrog's army glows on the map and a click on his region opens the panel of his
        // figures while the card's order is drafted.
        private static async Task<Button> MapFigure(MatchTestRoom room, MountedMatchClient client, string region, string id)
        {
            var marker = client.Board.Map.Q<Button>("army-" + region + "-Shadow");
            Assert.IsNotNull(marker, region);
            Assert.AreEqual(DisplayStyle.Flex, marker.Q(className: "army-choice-glow").resolvedStyle.display, region + " glows.");
            MapContourProbe.ClickRegion(client.Board.Map, region);
            await room.Settle();
            var figure = client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + id);
            Assert.IsNotNull(figure, id + " in the panel the click opened");
            Assert.IsTrue(figure.ClassListContains("eligible-figure"), id + " is lit.");
            return figure;
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
