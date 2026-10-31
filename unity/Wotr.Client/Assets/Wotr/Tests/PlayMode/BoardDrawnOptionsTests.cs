using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-431 (owner's rule, UI-430): options the board draws are chosen on the board, on a real server. Each object's
    // click sends a payload JSON-equal to the one its old button sent, recorded from the buttons at 16f8c966 (a temporary
    // probe on the same seeded matches). A seeded match is played once per run, so every path is its own test and the
    // tests of one seed run in separate PlayMode runs. Each test leaves .artifacts/screenshots/c431-<case>-1366-1.00.png.
    public sealed class BoardDrawnOptionsTests
    {
        private const string Captain = "base.sh.character.24";

        [UnityTest] public IEnumerator ARegionsGlowingGarrisonSendsTheOldGarrisonButtonsPayload() => Wait(Garrison());
        [UnityTest] public IEnumerator TheReservesNazgulRecruitAndTheCheckMarkSkipsTheManeuverAsTheirButtonsDid() => Wait(RecruitThenFinish());
        [UnityTest] public IEnumerator TheMapsNazgulSendTheOldMoveNazgulPayloadAndArmTheirStack() => Wait(Fly());
        [UnityTest] public IEnumerator ARedAttackTargetSendsTheOldAttackButtonsPayload() => Wait(Attack());
        [UnityTest] public IEnumerator TheCompanionsRedPortraitSendsTheOldEliminatePayload() => Wait(Eliminate());
        [UnityTest] public IEnumerator TheCorruptionMarkerSendsTheOldCorruptionPayload() => Wait(Corruption());

        // The payload the old buttons sent at 16f8c966: every field at its default but these.
        private static string OldPayload(string ids, string region = "", string moves = "[]") =>
            "{\"Stepwise\":false,\"DieId\":\"\",\"RingId\":\"\",\"RulerTokenId\":\"\",\"CouncilTokenId\":\"\",\"Deck\":\"\",\"Face\":null," +
            "\"Ids\":" + ids + ",\"NationId\":\"\",\"Placements\":[],\"MovementCost\":1,\"Moves\":" + moves + ",\"CardId\":\"\",\"CardCost\":4," +
            "\"ReactionId\":\"\",\"GuideId\":\"\",\"Route\":[],\"CompanionMoves\":[],\"HuntResponse\":0,\"Losses\":[],\"RegionId\":\"" + region + "\"," +
            "\"AutoDecision\":null,\"CharacterId\":\"\",\"AbilityId\":\"\"}";

        private static string Ids(string id) => "[\"" + id + "\"]";

        private const string OldAttackMoves = "[{\"Route\":[],\"From\":\"osgiliath\",\"To\":\"minas-tirith\",\"UnitIds\":[\"base.sauron.regular.01\"," +
            "\"base.sauron.regular.02\",\"base.sauron.regular.03\",\"base.sauron.nazgul.01\",\"base.sauron.nazgul.02\",\"base.sauron.nazgul.03\"]," +
            "\"CharacterIds\":[\"base.character.witch-king\"]}]";

        // A region's two forces (Dreadful Spells; the seeded match is the Kings card Work of the Enemy, the same decision):
        // the lit region opens its panel, whose glowing garrison chooses.
        private static async Task Garrison()
        {
            using var room = await Connect("kings-enemy-army", ScenarioKind.CardPack);
            var sent = Sent(room.Sh);
            AssertConsoleOptions(room.Sh);
            Assert.IsTrue(room.Sh.Board.Map.DraftOverlay.Targets.Contains("minas-tirith"), "The region is lit.");
            using (var viewport = new BoardCompletionLayoutTests.LayoutViewport(room.Sh, 1366, 1f))
            {
                MapContourProbe.ClickRegion(room.Sh.Board.Map, "minas-tirith");
                await room.Settle();
                Assert.AreEqual("minas-tirith", room.Sh.Board.Hover.Panel.Region, "The region's click opens its panel.");
                Assert.IsNull(room.Sh.Board.Tray.Q<Button>("effect-choice-minas-tirith:garrison"), "No tray button.");
                var garrison = ForceHeader(room.Sh, true);
                Assert.IsTrue(garrison.parent.ClassListContains(ChoiceGlow.ClassName) && ForceHeader(room.Sh, false).parent.ClassListContains(ChoiceGlow.ClassName));
                Assert.IsTrue(garrison.ClassListContains(PressFeedback.PressableClass), "The header signals its click (UI-113).");
                // The server's title, the click and the card, then the server's detail.
                StringAssert.Contains("Click to choose the garrison because of " + CardLibrary.Title(room.Sh.Board.View.Decision.SourceId), garrison.tooltip);
                StringAssert.EndsWith("Click the glowing field army or garrison in the region's panel", room.Sh.Board.Console.Status.Hint.text);
                LayoutTests.Save(viewport.Texture, "c431-forces-1366-1.00.png");
                CardNetworkTests.Click(garrison);
                await room.Until(() => sent.Count > 0);
            }
            AssertSent(sent[0], CommandKind.ChooseEffect, OldPayload(Ids("minas-tirith:garrison"), "minas-tirith"));
        }

        // The Black Captain's recruit is the Reserve's Nazgûl; the army step that follows has no buttons and its ✓ skips it.
        private static async Task RecruitThenFinish()
        {
            using var room = await Connect("shadow-captain", ScenarioKind.CardPack);
            var sent = Sent(room.Sh);
            using (var viewport = new BoardCompletionLayoutTests.LayoutViewport(room.Sh, 1366, 1f))
            {
                await RecruitFromTheReserve(room, viewport);
                AssertSent(sent[0], CommandKind.ChooseEffect, OldPayload(Ids("captain.recruit")));
                await FinishWithTheCheckMark(room, viewport);
            }
            AssertSent(sent[1], CommandKind.FinishArmyEvent, OldPayload("[]"));
        }

        // Move Nazgûl is the map's Nazgûl: their force's click chooses it and arms their stack in the flight.
        private static async Task Fly()
        {
            using var room = await Connect("shadow-captain", ScenarioKind.CardPack);
            var sent = Sent(room.Sh);
            AssertConsoleOptions(room.Sh);
            var force = room.Sh.Board.Map.Q("army-osgiliath-Shadow");
            Assert.IsTrue(force.ClassListContains("eligible-army"), "The Nazgûl's force glows.");
            StringAssert.StartsWith("Move Nazgûl · Click to move these Nazgûl and the others, the Witch-king among them, because of " +
                CardLibrary.Title(Captain), force.tooltip);
            using (var viewport = new BoardCompletionLayoutTests.LayoutViewport(room.Sh, 1366, 1f))
                await Shot(room, room.Sh, viewport, "c431-captain-fly", "osgiliath");
            var revision = room.Sh.Board.View.Revision;
            MapContourProbe.ClickRegion(room.Sh.Board.Map, "osgiliath");
            await room.At(revision + 1);
            AssertSent(sent[0], CommandKind.ChooseEffect, OldPayload(Ids("captain.fly")));
            Assert.AreEqual(DecisionKind.CharacterEvent, room.Sh.Board.View.Decision.Kind);
            await room.Until(() => room.Sh.Board.Map.DraftOverlay.SourceRegions.Contains("osgiliath"));
            Assert.IsTrue(room.Sh.Board.Map.DraftOverlay.Targets.Count > 0, "The carried stack is armed: its destinations light.");
        }

        // The Witch-king's maneuver lights its attack in red with its moves; the attack target's click and the ✓ attack.
        private static async Task Attack()
        {
            using var room = await Connect("shadow-captain", ScenarioKind.CardPack);
            var sent = Sent(room.Sh);
            await RecruitFromTheReserve(room);
            room.Sh.Board.Map.Select("osgiliath");
            await room.Settle();
            var overlay = room.Sh.Board.Map.DraftOverlay;
            Assert.IsTrue(overlay.Targets.Contains("minas-tirith") && overlay.AttackTargets.Contains("minas-tirith"), "The attack lights red.");
            Assert.IsTrue(overlay.Targets.Contains("north-ithilien") && !overlay.AttackTargets.Contains("north-ithilien"), "The moves light with it.");
            StringAssert.EndsWith("Click a lit destination to move or a red region to attack", room.Sh.Board.Console.Status.Hint.text);
            using (var viewport = new BoardCompletionLayoutTests.LayoutViewport(room.Sh, 1366, 1f))
            {
                room.Sh.Board.Hover.Close();
                await Shot(room, room.Sh, viewport, "c431-army-attack", "osgiliath");
            }
            MapContourProbe.ClickRegion(room.Sh.Board.Map, "minas-tirith");
            await room.Settle();
            var confirm = room.Sh.Board.Console.Actions.Confirm;
            await room.Until(() => confirm.enabledSelf);
            var revision = room.Sh.Board.View.Revision;
            PointerGestureEvents.ClickAt(confirm);
            await room.Until(() => sent.Count > 1);
            AssertSent(sent[1], CommandKind.AttackArmy, OldPayload("[]", moves: OldAttackMoves));
            await room.At(revision + 1);
        }

        // Lure of the Ring: Gimli's red portrait eliminates him.
        private static async Task Eliminate()
        {
            using var room = await Connect("shadow-lure", ScenarioKind.CardPack);
            var sent = Sent(room.Fp);
            AssertConsoleOptions(room.Fp);
            await room.Settle();
            Assert.IsTrue(room.Fp.Board.Fellowship.IsOpen, "The panel opens for the choice.");
            var gimli = room.Fp.Board.Fellowship.Root.Q<Button>("fellowship-portrait-base.character.gimli");
            Assert.IsTrue(gimli.ClassListContains(LureChoice.EliminateClass));
            StringAssert.StartsWith("Eliminate · Click to eliminate Gimli because of Lure of the Ring", gimli.tooltip);
            var marker = room.Fp.Board.Fellowship.CorruptionMarker;
            Assert.IsTrue(marker.ClassListContains(ChoiceGlow.ClassName), "The corruption marker is offered.");
            StringAssert.StartsWith("Click Gimli's red portrait to eliminate Gimli", room.Fp.Board.Console.Status.Hint.text);
            using (var viewport = new BoardCompletionLayoutTests.LayoutViewport(room.Fp, 1366, 1f))
            {
                room.Fp.Board.Fellowship.Open();
                await room.Settle();
                LayoutTests.Save(viewport.Texture, "c431-lure-1366-1.00.png");
                var revision = room.Fp.Board.View.Revision;
                CardNetworkTests.Click(gimli);
                await room.At(revision + 1);
            }
            AssertSent(sent[0], CommandKind.ChooseEffect, OldPayload(Ids("lure.eliminate")));
            Assert.IsFalse(room.Sh.Board.View.Fellowship.CompanionIdentities.Contains("base.character.gimli"));
        }

        // Lure of the Ring: the offered corruption marker keeps Gimli and takes his Level in Corruption.
        private static async Task Corruption()
        {
            using var room = await Connect("shadow-lure", ScenarioKind.CardPack);
            var sent = Sent(room.Fp);
            await room.Settle();
            var revision = room.Fp.Board.View.Revision;
            CardNetworkTests.Click(room.Fp.Board.Fellowship.CorruptionMarker);
            await room.At(revision + 1);
            AssertSent(sent[0], CommandKind.ChooseEffect, OldPayload(Ids("lure.corruption")));
            Assert.AreEqual(2, room.Sh.Board.View.Fellowship.Corruption);
        }

        // A screenshot centred on a region at the present zoom, without selecting it: Osgiliath stands under the console's
        // bottom row at the fitted view, where Reveal counts it as shown (the viewport's new size refits the camera).
        private static async Task Shot(MatchTestRoom room, MountedMatchClient client, BoardCompletionLayoutTests.LayoutViewport viewport,
            string name, string region)
        {
            await room.Settle();
            var map = client.Board.Map;
            var anchor = map.Data.Shapes[region].Anchor;
            map.FocusOn(new Vector2(anchor[0], anchor[1]), map.Camera.Zoom);
            await room.Settle();
            LayoutTests.Save(viewport.Texture, name + "-1366-1.00.png");
        }

        // The Reserve's lit Nazgûl recruit them (UI-431); a viewport, when given, keeps the screenshot of the lit token.
        internal static async Task RecruitFromTheReserve(MatchTestRoom room, BoardCompletionLayoutTests.LayoutViewport viewport = null)
        {
            var board = room.Sh.Board;
            Assert.AreEqual(BlackCaptainOrder.Presentation, board.View.Decision.Presentation);
            board.Reinforcements.Open();
            await room.Settle();
            var token = board.Reinforcements.Root.Q<Button>("captain-reserve-sauron-Nazgul");
            Assert.IsTrue(token.enabledSelf && token.ClassListContains("eligible-token"), "The Reserve's Nazgûl glow.");
            StringAssert.Contains("Click to recruit them with the Witch-king because of " + CardLibrary.Title(Captain), token.tooltip);
            StringAssert.StartsWith("Click a glowing Nazgûl in the Reserve to recruit, or a glowing Nazgûl on the map to move them",
                board.Console.Status.Hint.text);
            if (viewport != null)
                LayoutTests.Save(viewport.Texture, "c431-captain-recruit-1366-1.00.png");
            var revision = board.View.Revision;
            CardNetworkTests.Click(token);
            await room.At(revision + 1);
        }

        // The army step's ✓ is its Skip maneuver while no army is chosen (UI-431).
        internal static async Task FinishWithTheCheckMark(MatchTestRoom room, BoardCompletionLayoutTests.LayoutViewport viewport = null)
        {
            var board = room.Sh.Board;
            Assert.AreEqual(DecisionKind.ArmyEvent, board.View.Decision.Kind);
            AssertConsoleOptions(room.Sh);
            var confirm = board.Console.Actions.Confirm;
            await room.Until(() => confirm.enabledSelf);
            Assert.IsTrue(confirm.ClassListContains(ChoiceGlow.ClassName), "The ✓ is lit.");
            Assert.AreEqual("Finish · End " + CardLibrary.Title(Captain) + " without moving or attacking", confirm.tooltip);
            StringAssert.EndsWith("Or ✓ to finish without moving or attacking.", board.Console.Status.Hint.text);
            if (viewport != null)
                LayoutTests.Save(viewport.Texture, "c431-army-finish-1366-1.00.png");
            var revision = board.View.Revision;
            PointerGestureEvents.ClickAt(confirm);
            await room.At(revision + 1);
        }

        // A force section's header in the open region panel: the field army's or the garrison's.
        internal static VisualElement ForceHeader(MountedMatchClient client, bool garrison)
        {
            var section = client.Board.Hover.Panel.Root.Q("hover-force-FreePeoples-" + (garrison ? "garrison" : "field"));
            Assert.IsNotNull(section, "The region panel shows the force.");
            return section.Q(className: "hover-force-header");
        }

        private static List<GameCommand> Sent(MountedMatchClient client)
        {
            var sent = new List<GameCommand>();
            client.Interaction.CommandAnswered += (command, receipt, _) =>
            {
                Assert.IsTrue(receipt.Accepted, receipt.Error);
                sent.Add(command);
            };
            return sent;
        }

        private static void AssertSent(GameCommand command, CommandKind kind, string payload)
        {
            Assert.AreEqual(kind, command.Kind);
            Assert.AreEqual(payload, JsonConvert.SerializeObject(command.Payload), "The object sends what the old button sent.");
        }
    }
}
