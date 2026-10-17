using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public class CharacterNetworkTests
    {
        [UnityTest]
        public IEnumerator VersionsAndMinionEntryThenFlightReachBothClients() => Wait(Entries());
        [UnityTest]
        public IEnumerator GreyResponseSurvivesReconnectAndDrawIsPrivate() => Wait(Grey());
        [UnityTest]
        public IEnumerator BothHobbitsUseOnePlacementBeforeGollumReveals() => Wait(Hobbits());
        [UnityTest]
        public IEnumerator WhiteRiderChoiceChangesBothPublicLeadershipTotals() => Wait(WhiteRider());

        private static async Task Entries()
        {
            using (var room = await Connect("entry", ScenarioKind.Character))
            {
                await Enter(room, room.Fp, "gandalf-white", "fangorn");
                var gandalf = room.Fp.Board.View.Characters.Single(c => c.DefinitionId == "base.character.gandalf-white");
                Assert.AreEqual("base.character.gandalf-grey", gandalf.Id);
                await Enter(room, room.Sh, "witch-king", "minas-morgul");
                await Enter(room, room.Fp, "aragorn", "pelargir");
                Assert.AreEqual("base.character.strider", room.Sh.Board.View.Characters.Single(c => c.DefinitionId == "base.character.aragorn").Id);
                FellowshipGestures.Die(room.Sh);
                await room.Settle();
                FellowshipGestures.Figure(room.Sh, "minas-morgul", "base.character.witch-king");
                await room.Settle();
                var nazgul = room.Sh.Board.View.Units.First(u => u.Kind == UnitKind.Nazgul && u.Location == "minas-morgul").Id;
                FellowshipGestures.Figure(room.Sh, "minas-morgul", nazgul);
                Assert.IsTrue(room.Sh.Board.Map.DraftOverlay.Targets.Contains("hollin"), "Selected flight must offer Hollin.");
                var revision = room.Sh.Board.View.Revision;
                await room.Settle();
                FellowshipGestures.FinishRoute(room.Sh, "hollin");
                await room.At(revision + 1);
                await ArmyGestures.Finish(room, room.Sh);
                Assert.AreEqual("hollin", room.Fp.Board.View.Characters.Single(c => c.DefinitionId == "base.character.witch-king").Location);
                Assert.AreEqual("hollin", room.Fp.Board.View.Units.Single(u => u.Id == nazgul).Location);
            }
        }

        private static async Task Enter(MatchTestRoom room, MountedMatchClient client, string version, string region)
        {
            var choice = client.Board.View.Options.Single(o => o.Kind == CommandKind.EnterCharacter).CharacterActions.Single(c => c.CharacterId.EndsWith("." + version));
            await ComponentGestures.Cost(room, client, choice.Dice[0]);
            var revision = client.Board.View.Revision;
            await ComponentGestures.Enter(room, client, choice);
            if (choice.Targets.Count > 1)
            {
                await room.Settle();
                client.Board.Map.Select(region);
            }
            await room.At(revision + 1);
        }

        private static async Task Grey()
        {
            using (var room = await Connect("grey", ScenarioKind.Character))
            {
                var count = room.Fp.Board.View.Hand.Count;
                var connections = room.Fp.Connected;
                room.Fp.Connection.Reconnect();
                await room.Until(() => room.Fp.Connected > connections);
                await room.Settle();
                Assert.AreEqual("event.completed", room.Fp.Board.View.Decision.Window);
                Assert.IsNull(room.Sh.Board.View.Decision);
                await Respond(room, "grey-draw");
                Assert.AreEqual(count + 1, room.Fp.Board.View.Hand.Count);
                Assert.AreEqual(count + 1, room.Sh.Board.View.Players.Single(p => p.Side == Side.FreePeoples).HandCount);
                Assert.IsFalse(room.Sh.Board.View.History.Any(h => h.Text.Contains(room.Fp.Board.View.Hand.Last())));
            }
        }

        private static async Task Hobbits()
        {
            using (var room = await Connect("hobbits", ScenarioKind.Character))
            {
                await Respond(room, "merry-guide");
                await Respond(room, "pippin-guide");
                await Respond(room, "gollum-reveal");
                Assert.AreEqual(DecisionKind.CompanionSeparation, room.Fp.Board.View.Decision.Kind);
                Assert.AreEqual(2, room.Fp.Board.Tray.Query<Button>(className: "companion-portrait").ToList().Count(b => b.ClassListContains("selected")));
                // UI-105: a separation has no drawn steps; Cancel keeps the pending decision and the chosen companions.
                Assert.IsNull(room.Fp.Board.Tray.Q<Button>("route-undo"));
                PointerGestureEvents.ClickAt(room.Fp.Board.Tray.Q<Button>("route-cancel"));
                Assert.AreEqual(DecisionKind.CompanionSeparation, room.Fp.Board.View.Decision.Kind);
                Assert.AreEqual(2, room.Fp.Board.Tray.Query<Button>(className: "companion-portrait").ToList().Count(b => b.ClassListContains("selected")));
                await room.Settle();
                var placedAt = room.Fp.Board.View.Revision;
                FellowshipGestures.FinishRoute(room.Fp, "south-dunland");
                await room.At(placedAt + 1);
                Assert.AreEqual(DecisionKind.FellowshipRoute, room.Fp.Board.View.Decision.Kind);
                var revision = room.Fp.Board.View.Revision;
                room.Fp.Board.Map.Select("fords-of-bruinen");
                await room.Settle();
                FellowshipGestures.FinishRoute(room.Fp, "fords-of-bruinen");
                await room.At(revision + 1);
                Assert.AreEqual(0, room.Sh.Board.View.Fellowship.Corruption);
                Assert.IsFalse(room.Sh.Board.View.Fellowship.Hidden);
                Assert.IsTrue(room.Sh.Board.View.Characters.Where(c => c.Id.EndsWith("merry") || c.Id.EndsWith("pippin")).All(c => c.Location == "south-dunland"));
            }
        }

        private static async Task WhiteRider()
        {
            using (var room = await Connect("white-rider", ScenarioKind.Character))
            {
                Assert.AreEqual("battle.start", room.Fp.Board.View.Decision.Window);
                await Respond(room, "white-rider");
                Assert.IsTrue(room.Sh.Board.View.Battle.WhiteRiderActive);
                Assert.AreEqual(0, room.Sh.Board.View.Battle.Attacker.Leadership);
                Assert.AreEqual(0, room.Sh.Board.View.Battle.Defender.Leadership);
                Assert.AreEqual(DecisionKind.CombatCard, room.Fp.Board.View.Decision.Kind);
            }
        }

        private static async Task Respond(MatchTestRoom room, string response)
        {
            if (room.Fp.Board.Tray.parent == room.Fp.Board.Fellowship.Root)
                room.Fp.Board.Fellowship.Open();
            var revision = room.Fp.Board.View.Revision;
            await room.Settle();
            // UI-214: a response outside the Hunt and battles is a console button.
            PointerGestureEvents.ClickAt(MatchTestRoom.TrayOrConsole(room.Fp, "effect-choice-character." + response));
            await room.At(revision + 1);
        }
    }
}
