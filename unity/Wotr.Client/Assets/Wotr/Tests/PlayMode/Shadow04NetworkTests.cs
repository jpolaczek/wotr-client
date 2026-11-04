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
    public class Shadow04NetworkTests
    {
        [UnityTest] public IEnumerator SpellsChooseSourceThenTargetOnTheMap() => Wait(Spells());
        [UnityTest] public IEnumerator EventLossesRemainTheDefendersChoice() => Wait(Losses());
        [UnityTest] public IEnumerator GrondStartsWithoutSecondDieAndExplainsTheFirstRoundRestriction() => Wait(Grond());
        [UnityTest] public IEnumerator PalantirDrawRestoresDeckChoiceOnReconnect() => Wait(PalantirDraw());
        [UnityTest] public IEnumerator RemovingPalantirPreviewsTheChosenDieAndRing() => Wait(PalantirRemove());
        [UnityTest] public IEnumerator CaptainRecruitmentCanFinishWithoutMovingAnArmy() => Wait(Captain());
        [UnityTest] public IEnumerator RingwraithsPlanTwoArmiesWithRequiredCommanders() => Wait(Ringwraiths());
        [UnityTest] public IEnumerator GandalfArrivalActivatesRohanAndDiscardsWormtongue() => Wait(Wormtongue());

        private static async Task Spells()
        {
            using (var room = await Connect("shadow-spells", ScenarioKind.CardPack))
            {
                room.Sh.Board.Map.Select("osgiliath");
                await room.Confirm(room.Sh);
                Assert.AreEqual(2, room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds.Count);
                room.Sh.Board.Map.Select("north-ithilien");
                await room.Confirm(room.Sh);
                Assert.IsTrue(room.Fp.Board.View.History.Any(h => h.Kind == "DreadfulSpells" && h.Text.Contains("north-ithilien")));
                Assert.IsNull(room.Sh.Board.View.Battle);
            }
        }

        private static async Task Losses()
        {
            using (var room = await Connect("shadow-spells-loss", ScenarioKind.CardPack))
            {
                Assert.IsNull(room.Sh.Board.View.Decision);
                var unit = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.TakeEventLosses).EligibleIds[0];
                Click(room.Fp, "loss-" + unit);
                // UI-486 (owner, 2026-11-04): the Shadow watches Dreadful Spells' casualty leave the stack.
                var watch = new ObservedDeltaWatch(room.Sh, "ui486-network-spells");
                await room.Confirm(room.Fp);
                await watch.Finish();
                CollectionAssert.AreEqual(new[] { "−1" }, watch.Counts, "The casualty has its scene.");
                Assert.IsFalse(room.Sh.Board.View.Units.Any(u => u.Id == unit && u.Location == "minas-tirith"));
                Assert.IsNull(room.Sh.Board.View.EventLoss);
            }
        }

        private static async Task Grond()
        {
            using (var room = await Connect("shadow-grond", ScenarioKind.CardPack))
            {
                room.Sh.Board.Map.Select("minas-tirith");
                await room.Settle();
                MapContourProbe.ClickRegion(room.Sh.Board.Map, "minas-tirith");
                await room.Confirm(room.Sh);
                Assert.AreEqual(3, room.Fp.Board.View.Battle.MandatorySiegeRounds);
                Assert.IsTrue(room.Sh.Board.Tray.Query<Label>().ToList().Any(l => l.text.Contains("Round 1/3")));
                Assert.That(room.Fp.Board.Tray.Q<Button>("battle-card-defender").text, Does.Contain("No Companion · No card this round"));
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Resolving));
                await room.Direct(room.Sh, "battle-no-card");
                Assert.IsFalse(room.Fp.Board.View.Decision?.Kind == DecisionKind.CombatCard && room.Fp.Board.View.Battle.Round == 1);
            }
        }

        private static async Task PalantirDraw()
        {
            using (var room = await Connect("shadow-palantir-draw", ScenarioKind.CardPack))
            {
                Assert.IsNull(room.Fp.Board.View.Battle);
                var count = room.Sh.Board.View.Hand.Count;
                await CardGestures.Draw(room, room.Sh, "strategy");
                var connected = room.Sh.Connected;
                room.Sh.Connection.Reconnect();
                await room.Until(() => room.Sh.Connected > connected);
                await room.Settle();
                Assert.AreEqual(count + 1, room.Sh.Board.View.Hand.Count);
                Assert.IsTrue(room.Sh.Board.View.Effects.Any(e => e.SourceId == "base.sh.character.21"));
                Assert.AreEqual(count + 1, room.Fp.Board.View.Players.Single(p => p.Side == Side.Shadow).HandCount);
                Assert.IsFalse(room.Fp.Board.View.History.Any(h => h.Text.Contains(room.Sh.Board.View.Hand.Last())));
            }
        }

        private static async Task PalantirRemove()
        {
            using (var room = await Connect("shadow-palantir-remove", ScenarioKind.CardPack))
            {
                var view = room.Fp.Board.View;
                var die = view.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Army && d.Location == DieLocation.Available);
                var ring = view.Options.Single(o => o.Kind == CommandKind.DiscardPalantir).EligibleIds[0];
                await ComponentGestures.Cost(room, room.Fp, die.Id);
                await ComponentGestures.Remove(room.Fp, "base.sh.character.21");
                await room.Settle();
                Assert.IsNull(room.Fp.Board.Tray.Q<Button>("confirm-decision"));
                Assert.AreEqual(DieLocation.Available, room.Fp.Board.View.Dice.Single(d => d.Id == die.Id).Location);
                var preview = await room.Fp.Api.Post<ActionPreview>($"api/matches/{view.MatchId}/preview", new GameCommand
                {
                    MatchId = view.MatchId,
                    DecisionId = view.Decision.Id,
                    ExpectedRevision = view.Revision,
                    CommandId = System.Guid.NewGuid().ToString("N"),
                    Kind = CommandKind.DiscardPalantir,
                    Payload = new CommandPayload { DieId = die.Id, RingId = ring }
                }, room.Token);
                Assert.IsTrue(preview.Legal, preview.Error);
                CollectionAssert.AreEquivalent(new[] { die.Id, ring }, preview.Costs);
                var revision = room.Fp.Board.View.Revision;
                CardNetworkTests.Click(room.Fp.Board.Console.Rings.For(ring));
                await room.At(revision + 1);
                Assert.IsFalse(room.Sh.Board.View.Effects.Any(e => e.SourceId == "base.sh.character.21"));
                Assert.AreEqual(Side.Shadow, room.Sh.Board.View.Rings.Single(r => r.Id == ring).Owner);
                Assert.AreEqual(DieLocation.Spent, room.Fp.Board.View.Dice.Single(d => d.Id == die.Id).Location);
            }
        }

        private static async Task Captain()
        {
            using (var room = await Connect("shadow-captain", ScenarioKind.CardPack))
            {
                var before = room.Sh.Board.View.Units.Count(u => u.Location == "osgiliath" && u.Kind == UnitKind.Nazgul);
                // UI-431 (CC05; owner's rule, UI-430): the Black Captain's orders are the Reserve's and the map's Nazgûl, and the
                // army step's Skip maneuver is the console's ✓; no console buttons.
                AssertConsoleOptions(room.Sh);
                await BoardDrawnOptionsTests.RecruitFromTheReserve(room);
                Assert.AreEqual(before + 2, room.Fp.Board.View.Units.Count(u => u.Location == "osgiliath" && u.Kind == UnitKind.Nazgul));
                AssertConsoleOptions(room.Sh);
                await BoardDrawnOptionsTests.FinishWithTheCheckMark(room);
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Spent));
                Assert.AreEqual("osgiliath", room.Fp.Board.View.Characters.Single(c => c.Id == "base.character.witch-king").Location);
            }
        }

        private static async Task Ringwraiths()
        {
            using (var room = await Connect("shadow-ringwraiths", ScenarioKind.CardPack))
            {
                room.Sh.Board.Map.Select("osgiliath");
                await room.Settle();
                // UI-216: the lit army's panel lights its figures, moving with the whole army.
                var king = room.Sh.Board.Hover.Panel.Root.Q<Button>("hover-figure-base.character.witch-king");
                Assert.IsTrue(king.ClassListContains("eligible-figure") && king.ClassListContains("selected"));
                room.Sh.Board.Map.Select("north-ithilien");
                room.Sh.Board.Map.Select("south-ithilien");
                await room.Settle();
                room.Sh.Board.Map.Select("north-ithilien");
                await room.Settle();
                Assert.That(room.Sh.Board.Console.Status.Hint.text, Does.StartWith("Armies moved 2"));
                // UI-431: the event's Attack lights on the map with its moves; no console buttons.
                AssertConsoleOptions(room.Sh);
                Assert.IsTrue(room.Sh.Board.View.Units.Any(u => u.Location == "south-ithilien"));
                await room.Confirm(room.Sh);
                Assert.AreEqual(4, room.Fp.Board.View.Units.Count(u => u.Location == "north-ithilien" && u.Kind == UnitKind.Regular));
                Assert.AreEqual(2, room.Fp.Board.View.Units.Count(u => u.Location == "north-ithilien" && u.Kind == UnitKind.Nazgul));
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Spent));
            }
        }

        private static async Task Wormtongue()
        {
            using (var room = await Connect("shadow-wormtongue", ScenarioKind.CardPack))
            {
                FellowshipGestures.Die(room.Fp);
                await room.Settle();
                var origin = room.Fp.Board.View.Characters.Single(c => c.Id == "base.character.gandalf-grey").Location;
                FellowshipGestures.Figure(room.Fp, origin, "base.character.gandalf-grey");
                Assert.IsFalse(room.Sh.Board.View.Nations.Single(n => n.Id == "rohan").Active);
                await room.MoveTo(room.Fp, "helms-deep");
                Assert.IsTrue(room.Sh.Board.View.Nations.Single(n => n.Id == "rohan").Active);
                Assert.IsFalse(room.Sh.Board.View.Effects.Any(e => e.SourceId == "base.sh.character.22"));
                Assert.AreEqual(1, room.Sh.Board.View.Nations.Single(n => n.Id == "rohan").StepsToWar);
            }
        }
    }
}
