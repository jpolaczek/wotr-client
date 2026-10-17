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
    public class CardPack04NetworkTests
    {
        [UnityTest]
        public IEnumerator EntsResolveRealLossesAndReturnToFreeCardWindow() => Wait(EntsLosses());
        [UnityTest]
        public IEnumerator EntsChainCharacterEventsWithoutAnotherDie() => Wait(FreeEvents());
        [UnityTest]
        public IEnumerator DeadMenMovesGroupThenResolvesEnemyLossesRetreatAndRecruitment() => Wait(DeadMen());
        [UnityTest]
        public IEnumerator StewardsRecruitsChosenEliteAndDrawsTwoStrategyCards() => Wait(Recruit("stewards"));
        [UnityTest]
        public IEnumerator GreyCompanyChoosesNationForAtomicReplacement() => Wait(Recruit("grey-company"));

        private static async Task EntsLosses()
        {
            using (var room = await Connect("ents-loss", ScenarioKind.CardPack))
            {
                var view = room.Sh.Board.View;
                Assert.IsNull(view.Battle);
                Assert.AreEqual(2, view.EventLoss.Hits);
                Assert.IsNotNull(room.Sh.Board.Tray.Q<Label>("event-loss-roll"));
                var units = view.Units.Where(u => view.Options.Single().EligibleIds.Contains(u.Id)).ToList();
                Click(room.Sh, "loss-" + units.Single(u => u.Kind == UnitKind.Regular).Id);
                Click(room.Sh, "loss-" + units.Single(u => u.Kind == UnitKind.Elite).Id);
                await Reconnect(room, room.Sh);
                await room.Confirm(room.Sh);
                Assert.AreEqual("ents.follow-up", room.Fp.Board.View.Decision.Window);
                Assert.AreEqual(1, room.Fp.Board.View.Units.Count(u => u.Location == "orthanc" && u.Kind == UnitKind.Regular));
                Assert.IsNull(room.Sh.Board.View.Decision);
            }
        }

        private static async Task FreeEvents()
        {
            using (var room = await Connect("ents-follow-up", ScenarioKind.CardPack))
            {
                foreach (var card in new[] { "base.fp.character.20", "base.fp.character.12" })
                {
                    if (card == "base.fp.character.12")
                    {
                        // UI-214: the follow-up's Decline is a console button; the card is played from the hand.
                        Assert.AreEqual("ents.follow-up", room.Fp.Board.View.Decision.Window);
                        AssertConsoleOptions(room.Fp, "action-DeclineReaction");
                    }
                    await CardGestures.Play(room, room.Fp, card);
                }
                Assert.AreEqual(1, room.Sh.Board.View.Fellowship.Corruption);
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Spent));
                Assert.AreEqual(0, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Resolving));
            }
        }

        private static async Task DeadMen()
        {
            using (var room = await Connect("dead-men", ScenarioKind.CardPack))
            {
                // UI-215: Gimli is chosen in his region's panel, the destination on the map; no window opens.
                await room.ChooseFigure(room.Fp, "base.character.gimli");
                Assert.IsFalse(room.Fp.Board.DecisionWindow.IsOpen);
                MapContourProbe.ClickRegion(room.Fp.Board.Map, "pelargir");
                await Reconnect(room, room.Fp);
                Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.PlannedRegions.Contains("pelargir"));
                await room.Confirm(room.Fp);
                Assert.AreEqual("edoras", room.Sh.Board.View.Characters.Single(c => c.DefinitionId == "base.character.legolas").Location);
                Assert.AreEqual("pelargir", room.Sh.Board.View.Characters.Single(c => c.DefinitionId == "base.character.gimli").Location);
                var losses = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.TakeEventLosses);
                foreach (var id in losses.EligibleIds.Take(room.Sh.Board.View.EventLoss.Hits))
                    Click(room.Sh, "loss-" + id);
                await room.Confirm(room.Sh);
                var destination = room.Sh.Board.View.Options.Single().EligibleIds[0];
                MapContourProbe.ClickRegion(room.Sh.Board.Map, destination);
                await room.Confirm(room.Sh);
                foreach (var id in room.Fp.Board.View.Options.Single().EligibleIds)
                    Click(room.Fp, "effect-choice-" + id);
                await room.Confirm(room.Fp);
                Assert.AreEqual(Side.FreePeoples, room.Sh.Board.View.Control["pelargir"]);
                Assert.AreEqual(3, room.Sh.Board.View.Units.Count(u => u.Location == "pelargir" && u.Kind == UnitKind.Regular));
                Assert.AreEqual(destination, room.Fp.Board.View.Characters.Single(c => c.DefinitionId == "base.character.witch-king").Location);
            }
        }

        private static async Task Recruit(string scenario)
        {
            using (var room = await Connect(scenario, ScenarioKind.CardPack))
            {
                var view = room.Fp.Board.View;
                var option = view.Options.Single();
                var figure = view.Units.Single(u => option.EligibleIds.Contains(u.Id) &&
                    (scenario == "stewards" ? u.Kind == UnitKind.Elite : u.Nation == "rohan"));
                var deck = view.Players.Single(p => p.Side == Side.FreePeoples).DeckCounts["strategy"];
                await room.ChooseFigure(room.Fp, figure.Id);
                await room.Confirm(room.Fp);
                Assert.AreEqual(deck - 2, room.Sh.Board.View.Players.Single(p => p.Side == Side.FreePeoples).DeckCounts["strategy"]);
                Assert.IsTrue(room.Sh.Board.View.Units.Any(u => u.Kind == UnitKind.Elite &&
                    (scenario == "stewards" ? u.Location == "minas-tirith" : u.Location == "fangorn" && u.Nation == "rohan")));
            }
        }

        private static async Task Reconnect(MatchTestRoom room, MountedMatchClient client)
        {
            var before = client.Connected;
            client.Connection.Reconnect();
            await room.Until(() => client.Connected > before);
            await room.Settle();
        }
    }
}
