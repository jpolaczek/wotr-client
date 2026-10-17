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
    public class CardPack03NetworkTests
    {
        [UnityTest]
        public IEnumerator MirrorSelectsAnUnusedDieDirectlyInThePool() => Wait(Mirror());
        [UnityTest]
        public IEnumerator GwaihirMovesOneGroupIntoBesiegedLorien() => Wait(Gwaihir());
        [UnityTest]
        public IEnumerator SwifterPreservesShadowfaxRangeAndRouteAcrossReconnect() => Wait(Swifter());
        [UnityTest]
        public IEnumerator ThereAndBackWaitsForGuideThenUpdatesPoliticalTrack() => Wait(ThereAndBack());
        [UnityTest]
        public IEnumerator EaglesSwitchesToShadowForMapRetreat() => Wait(Eagles());
        [UnityTest]
        public IEnumerator SuddenStrikeShowsSpecialLossesBeforeCombatRolls() => Wait(SuddenStrike());

        private static async Task Mirror()
        {
            using (var room = await Connect("mirror", ScenarioKind.CardPack))
            {
                var option = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect);
                var id = option.EligibleIds[1];
                var paid = room.Fp.Board.View.Dice.Single(d => d.Location == DieLocation.Resolving);
                Assert.IsFalse(room.Fp.Board.OwnDice.Q<Button>(paid.Id).enabledSelf);
                var revision = room.Fp.Board.View.Revision;
                CardNetworkTests.Click(room.Fp.Board.OwnDice.Q<Button>(id));
                await room.At(revision + 1);
                await Reconnect(room, room.Fp);
                Assert.AreEqual(DieFace.WillOfTheWest, room.Sh.Board.View.Dice.Single(d => d.Id == id).Face);
                Assert.AreEqual(2, room.Sh.Board.View.Fellowship.Corruption);
                Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == paid.Id).Location);
            }
        }

        private static async Task Gwaihir()
        {
            using (var room = await Connect("gwaihir", ScenarioKind.CardPack))
            {
                Assert.AreEqual(DecisionKind.CompanionSeparation, room.Fp.Board.View.Decision.Kind);
                await room.Settle();
                Assert.IsNull(room.Fp.Board.Tray.Q<Button>("new-companion-group"));
                room.Fp.Board.Hover.Open("dimrill-dale");
                await room.Settle();
                FellowshipGestures.Figure(room.Fp, "dimrill-dale", "base.character.gimli");
                await room.Settle();
                // UI-105: besieged Lórien is a highlighted destination and its click moves Gimli at once. A route
                // never continues beyond it (EditMode TravelActionTests); Parth Celebrant borders Dimrill Dale itself.
                Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.Targets.Contains("lorien"));
                var revision = room.Fp.Board.View.Revision;
                FellowshipGestures.FinishRoute(room.Fp, "lorien");
                await room.At(revision + 1);
                var gimli = room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.gimli");
                Assert.AreEqual("lorien", gimli.Location);
                Assert.IsTrue(gimli.Garrison);
                Assert.AreEqual(1, room.Fp.Board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent));
            }
        }

        private static async Task Swifter()
        {
            using (var room = await Connect("swifter", ScenarioKind.CardPack))
            {
                Assert.AreEqual(DecisionKind.CompanionSeparation, room.Fp.Board.View.Decision.Kind);
                room.Fp.Board.Hover.Open("rivendell");
                await room.Settle();
                FellowshipGestures.Figure(room.Fp, "rivendell", "base.character.gandalf-grey");
                FellowshipGestures.Figure(room.Fp, "rivendell", "base.character.merry");
                await Reconnect(room, room.Fp);
                // UI-105: the selected group survives the reconnect; one click on the distant destination moves it
                // and the client lays the route within the card's range.
                Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.Targets.Contains("fords-of-isen"));
                var revision = room.Fp.Board.View.Revision;
                FellowshipGestures.FinishRoute(room.Fp, "fords-of-isen");
                await room.At(revision + 1);
                Assert.AreEqual(2, room.Sh.Board.View.Characters.Count(c => c.Location == "fords-of-isen"));
                Assert.AreEqual(1, room.Fp.Board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent));
            }
        }

        private static async Task ThereAndBack()
        {
            using (var room = await Connect("there-and-back", ScenarioKind.CardPack))
            {
                var revision = room.Fp.Board.View.Revision;
                var before = room.Fp.Board.View.Nations.Single(n => n.Id == "north").StepsToWar;
                Click(room.Fp, "companion-base.character.gimli");
                await room.Settle();
                Assert.IsNull(room.Fp.Board.Tray.Q<Button>("confirm-decision"));
                Assert.AreEqual(revision, room.Fp.Board.View.Revision,
                    "The political effect must wait for the completed journey and guide decision.");
                // UI-105: the click on Dale sends Gimli there at once.
                await room.MoveTo(room.Fp, "dale");
                Assert.AreEqual(before, room.Sh.Board.View.Nations.Single(n => n.Id == "north").StepsToWar);
                Click(room.Fp, "companion-base.character.strider");
                await room.Confirm(room.Fp);
                Assert.AreEqual(before - 1, room.Sh.Board.View.Nations.Single(n => n.Id == "north").StepsToWar);
                Assert.IsTrue(room.Sh.Board.View.Nations.Single(n => n.Id == "north").Active);
            }
        }

        private static async Task Eagles()
        {
            using (var room = await Connect("eagles", ScenarioKind.CardPack))
            {
                Assert.IsNull(room.Sh.Board.View.Decision);
                MapContourProbe.ClickRegion(room.Fp.Board.Map, "south-anduin-vale");
                await room.Confirm(room.Fp);
                Assert.IsNull(room.Fp.Board.View.Decision);
                Assert.AreEqual(DecisionKind.EffectChoice, room.Sh.Board.View.Decision.Kind);
                Assert.IsEmpty(room.Sh.Board.View.EffectTrail);
                var survivors = room.Sh.Board.View.Units.Count(u => u.Location == "south-anduin-vale" && u.Kind == UnitKind.Nazgul);
                Assert.GreaterOrEqual(survivors, 2);
                var revision = room.Sh.Board.View.Revision;
                MapContourProbe.ClickRegion(room.Sh.Board.Map, "dol-guldur");
                await room.At(revision + 1);
                await Reconnect(room, room.Sh);
                Assert.AreEqual(survivors, room.Fp.Board.View.Units.Count(u => u.Location == "dol-guldur" && u.Kind == UnitKind.Nazgul));
                Assert.AreEqual("south-anduin-vale", room.Fp.Board.View.Characters.Single(c => c.DefinitionId == "base.character.witch-king").Location);
            }
        }

        private static async Task SuddenStrike()
        {
            using (var room = await Connect("sudden-strike", ScenarioKind.CardPack))
            {
                Assert.IsNull(room.Fp.Board.View.Decision);
                Assert.AreEqual(DecisionKind.Losses, room.Sh.Board.View.Decision.Kind);
                Assert.IsEmpty(room.Sh.Board.View.Battle.Attacker.Dice);
                var option = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.TakeBattleLosses);
                Click(room.Sh, "loss-" + option.EligibleIds[0]);
                await room.Confirm(room.Sh);
                Assert.AreEqual(5, room.Fp.Board.View.Battle.Attacker.Dice.Count);
                Assert.AreEqual(2, room.Fp.Board.View.Battle.Defender.Dice.Count);
            }
        }

        private static async Task Reconnect(MatchTestRoom room, MountedMatchClient client)
        {
            var connected = client.Connected;
            client.Connection.Reconnect();
            await room.Until(() => client.Connected > connected);
            await room.Settle();
        }
    }
}
