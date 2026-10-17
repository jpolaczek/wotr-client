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
    public class Shadow02NetworkTests
    {
        [UnityTest]
        public IEnumerator SearchUsesMapFlightThenTransfersTheRevealDecisionToFp() => Wait(Search());
        [UnityTest]
        public IEnumerator StrikeKeepsDraftAcrossReconnectThenDiscardsOneTableCard() => Wait(Strike());
        [UnityTest]
        public IEnumerator WeatherMovesOnlyTheFigureThroughMapChoice() => Wait(Weather());
        [UnityTest]
        public IEnumerator DreadSupportsAllTenPointsAndUsesItsOwnSummary() => Wait(Leadership("shadow-dread", true));
        [UnityTest]
        public IEnumerator TerribleUsesExactlyOnePointForTheReroll() => Wait(Leadership("shadow-terrible", false));
        [UnityTest]
        public IEnumerator BlackBreathTargetsCompanionAndKeepsNormalHits() => Wait(BlackBreath());

        private static async Task Search()
        {
            using (var room = await Connect("shadow-search", ScenarioKind.CardPack))
            {
                Assert.AreEqual(DecisionKind.CharacterEvent, room.Sh.Board.View.Decision.Kind);
                Assert.IsNull(room.Fp.Board.View.Decision);
                Assert.IsTrue(room.Sh.Board.Tray.Query<Label>().ToList().Any(label => label.text.Contains("No extra die")));
                // UI-212: the flights are chosen on the map, with no decision window. A click on the lit region arms
                // it and opens its panel, where the Witch-king is picked; the Mouth of Sauron is not a Nazgûl.
                Assert.IsFalse(room.Sh.Board.DecisionWindow.IsOpen);
                Assert.IsTrue(Marker(room.Sh, "barad-dur").ClassListContains("eligible-army"));
                FellowshipGestures.Region(room.Sh, "barad-dur");
                await room.Settle();
                Assert.IsTrue(Marker(room.Sh, "barad-dur").ClassListContains("armed-force"));
                Assert.AreEqual("barad-dur", room.Sh.Board.Hover.Panel.Region);
                Assert.IsFalse(PanelFigure(room.Sh, "base.character.mouth-of-sauron").ClassListContains("eligible-figure"));
                PointerGestureEvents.ClickAt(PanelFigure(room.Sh, "base.character.witch-king"));
                await room.Settle();
                Assert.IsTrue(PanelFigure(room.Sh, "base.character.witch-king").ClassListContains("selected"));
                await Flight(room, "base.character.witch-king");
                Assert.IsFalse(room.Sh.Board.DecisionWindow.IsOpen, "UI-164 CC09: waiting after the flights opens no window.");
                Assert.AreEqual(DecisionKind.FellowshipRoute, room.Fp.Board.View.Decision.Kind);
                Assert.AreEqual(2, room.Fp.Board.View.Fellowship.Progress);
                Assert.IsNull(room.Sh.Board.View.Decision);
                var revision = room.Fp.Board.View.Revision;
                FellowshipGestures.Region(room.Fp, room.Fp.Board.View.Fellowship.RegionId);
                await room.Settle();
                FellowshipGestures.FinishRoute(room.Fp, room.Fp.Board.View.Fellowship.RegionId);
                await room.At(revision + 1);
                Assert.AreEqual(0, room.Sh.Board.View.Fellowship.Progress);
                Assert.IsFalse(room.Sh.Board.View.Fellowship.Hidden);
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Spent));
            }
        }

        private static async Task Strike()
        {
            using (var room = await Connect("shadow-strike", ScenarioKind.CardPack))
            {
                var id = room.Sh.Board.View.Units.First(u => u.Kind == UnitKind.Nazgul && u.Location == "barad-dur").Id;
                // UI-212: a click on Barad-dûr arms its Nazgûl (the Witch-king only when he is picked himself), and the
                // armed source survives a reconnect.
                FellowshipGestures.Region(room.Sh, "barad-dur");
                await room.Settle();
                Assert.IsTrue(Marker(room.Sh, "barad-dur").ClassListContains("armed-force"));
                var connected = room.Sh.Connected;
                room.Sh.Connection.Reconnect();
                await room.Until(() => room.Sh.Connected > connected);
                await room.Settle();
                Assert.IsTrue(Marker(room.Sh, "barad-dur").ClassListContains("armed-force"));
                await Flight(room, id);
                Assert.AreEqual("barad-dur", Location(room.Fp.Board.View, "base.character.witch-king"));
                Assert.AreEqual("NazgulStrike", room.Sh.Board.View.Decision.Presentation);
                await DiscardTableCard(room, "base.fp.character.08"); // Wizard's Staff.

                Assert.IsFalse(room.Fp.Board.View.Effects.Any(c => c.SourceId == "base.fp.character.08"));
                Assert.IsTrue(room.Fp.Board.View.Fellowship.Hidden);
                Assert.AreEqual(2, room.Fp.Board.View.Fellowship.Progress);
                Assert.AreEqual(0, room.Fp.Board.View.Fellowship.Corruption);
            }
        }

        // UI-214 CC06: Roll for the Hunt and Finish are console buttons beside "Discard a table card", which opens the lit
        // Effects panel; a table card's × discards it and sends the decision. No decision window opens.
        private static async Task DiscardTableCard(MatchTestRoom room, string card)
        {
            var options = room.Sh.Board.Console.Status.Options;
            CollectionAssert.AreEqual(new[] { "console-option-table-cards", "effect-choice-nazgul-strike.hunt", "effect-choice-nazgul-strike.decline" },
                options.Query<Button>().ToList().Select(b => b.name));
            Assert.IsFalse(room.Sh.Board.DecisionWindow.IsOpen);
            Assert.IsTrue(room.Sh.Board.TableEffects.Tab.ClassListContains("legal-target"));
            CardNetworkTests.Click(options.Q<Button>("console-option-table-cards"));
            var discard = room.Sh.Board.TableEffects.Root.Q<Button>("remove-table-" + card);
            await room.Until(() => discard.enabledInHierarchy && discard.worldBound.width > 0);
            await room.Settle();
            var revision = room.Sh.Board.View.Revision;
            CardNetworkTests.Click(discard);
            await room.At(revision + 1);
        }

        // UI-212: these events may fly several Nazgûl, so the destination click records the flight on the map (a ghost
        // and the console's progress) and the console's ✓ sends the shared command.
        private static async Task Flight(MatchTestRoom room, string figure)
        {
            var revision = room.Sh.Board.View.Revision;
            Assert.IsTrue(room.Sh.Board.Map.DraftOverlay.Targets.Contains("hollin"));
            FellowshipGestures.Destination(room.Sh, "hollin");
            await room.Settle();
            Assert.AreEqual(revision, room.Sh.Board.View.Revision);
            Assert.IsTrue(room.Sh.Board.Map.DraftOverlay.PlannedRegions.Contains("hollin"));
            Assert.IsTrue(room.Sh.Board.Map.PlannedFlights.Regions.Contains("hollin"));
            Assert.That(room.Sh.Board.Console.Status.Hint.text, Does.StartWith("Nazgûl moved 1"));
            Assert.IsTrue(room.Sh.Board.Console.Actions.Confirm.enabledSelf);
            await room.Confirm(room.Sh);
            Assert.AreEqual("hollin", Location(room.Fp.Board.View, figure));
        }

        private static Button Marker(MountedMatchClient client, string region) => client.Board.Map.Q<Button>("army-" + region + "-Shadow");

        private static Button PanelFigure(MountedMatchClient client, string id) => client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + id);

        private static string Location(PlayerView view, string id) =>
            view.Characters.FirstOrDefault(c => c.Id == id)?.Location ?? view.Units.Single(u => u.Id == id).Location;

        private static async Task Weather()
        {
            using (var room = await Connect("shadow-weather", ScenarioKind.CardPack))
            {
                var id = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds[0];
                var revision = room.Sh.Board.View.Revision;
                Assert.AreEqual("rivendell", room.Sh.Board.View.Fellowship.RegionId);
                MapContourProbe.ClickRegion(room.Sh.Board.Map, id);
                await room.At(revision + 1);
                Assert.AreEqual(id, room.Fp.Board.View.Fellowship.RegionId);
                Assert.AreEqual(2, room.Fp.Board.View.Fellowship.Progress);
                Assert.IsTrue(room.Fp.Board.View.Fellowship.Hidden);
            }
        }

        private static async Task Leadership(string scenario, bool variable)
        {
            using (var room = await Connect(scenario, ScenarioKind.CardPack))
            {
                var choices = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EffectChoices;
                Assert.AreEqual(10, choices.Count);
                foreach (var choice in choices.Take(variable ? 10 : 1))
                    Click(room.Sh, "effect-choice-" + choice.Id);
                await room.Settle();
                if (variable)
                    Assert.That(room.Sh.Board.Tray.Q<Label>(className: "draft-status").text, Does.Contain("Fewer opposing Combat dice"));
                await room.Confirm(room.Sh);
                var battle = room.Fp.Board.View.Battle;
                Assert.AreEqual(variable ? 10 : 1, battle.Defender.ForfeitedLeadership.Values.Sum());
                Assert.AreEqual(variable ? 1 : 5, battle.Attacker.Dice.Count);
                Assert.IsTrue(battle.Defender.Dice.All(d => d.Modifier == 0));
            }
        }

        private static async Task BlackBreath()
        {
            using (var room = await Connect("shadow-black-breath", ScenarioKind.CardPack))
            {
                FellowshipLayoutTests.Check(room.Sh.Board);
                Click(room.Sh, "effect-choice-base.character.gimli");
                await room.Confirm(room.Sh);
                Assert.IsFalse(room.Fp.Board.View.Characters.Any(c => c.Id == "base.character.gimli" && c.Location == "minas-tirith"));
                Assert.AreEqual(DecisionKind.Losses, room.Fp.Board.View.Decision.Kind);
                Assert.AreEqual(2, room.Fp.Board.View.Battle.PendingHits);
            }
        }
    }
}
