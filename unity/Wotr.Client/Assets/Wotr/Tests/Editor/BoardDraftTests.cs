using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    public class BoardDraftTests
    {
        [Test]
        public void WholeArmyClickPreparesTheMoveAndLeavesServerPiecesUntouched()
        {
            var view = new BoardFixture().View;
            var click = new TurnDraft();
            Assert.IsTrue(BoardDraft.SelectRegion(click, view, "minas-tirith"));
            Assert.AreEqual("army", click.Payload.DieId);
            Assert.IsTrue(BoardDraft.SelectRegion(click, view, "osgiliath"));
            Assert.IsTrue(click.Ready(view.Options[0]));
            Assert.AreEqual(3, view.Units.Count(u => u.Location == "minas-tirith"));
        }

        [Test]
        public void TwoMovesExcludeArrivalsAndTheFirstRemainsEditable()
        {
            var view = new BoardFixture().View;
            var draft = new TurnDraft();
            BoardDraft.SelectRegion(draft, view, "minas-tirith");
            BoardDraft.SelectRegion(draft, view, "osgiliath");
            BoardDraft.SelectRegion(draft, view, "osgiliath");
            Assert.AreEqual(1, draft.Payload.Moves[1].UnitIds.Count);
            BoardDraft.SelectRegion(draft, view, "south-ithilien");
            Assert.IsEmpty(draft.Payload.Moves[0].UnitIds.Intersect(draft.Payload.Moves[1].UnitIds));
            ArmyDraft.Edit(draft, 0);
            BoardDraft.SelectRegion(draft, view, "lossarnach");
            Assert.AreEqual("lossarnach", draft.Payload.Moves[0].To);
            Assert.AreEqual("south-ithilien", draft.Payload.Moves[1].To);
            draft.Back();
            Assert.AreEqual("osgiliath", draft.Payload.Moves[0].To);
        }

        [Test]
        public void ALeaderCannotBeLeftAloneAndMixedPoliticalEligibilityDoesNotAutoDropFigures()
        {
            var view = new BoardFixture().View;
            var draft = new TurnDraft();
            BoardDraft.SelectRegion(draft, view, "minas-tirith");
            var leader = view.Units.First(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Leader);
            ArmyDraft.Toggle(draft, new[] { leader.Id });
            StringAssert.Contains("leader", ArmyDraft.Problem(draft, view));
            Assert.IsFalse(BoardDraft.SelectRegion(draft, view, "lossarnach"));
            ArmyDraft.Toggle(draft, new[] { leader.Id });
            var target = view.Options[0].Armies.First(a => a.RegionId == "minas-tirith").Targets.First(t => t.RegionId == "lossarnach");
            target.EligibleIds.Remove(leader.Id);
            var before = JsonConvert.SerializeObject(draft.Payload);
            Assert.IsFalse(ArmyDraft.Target(draft, view, "lossarnach"));
            Assert.AreEqual(before, JsonConvert.SerializeObject(draft.Payload));
        }

        // UI-128: armies move by clicks only; a dropped army never changes the draft, legal target or not.
        [Test]
        public void AnArmyDropNeverChangesTheDraft()
        {
            var view = new BoardFixture().View;
            var draft = new TurnDraft();
            BoardDraft.SelectRegion(draft, view, "dale");
            var before = JsonConvert.SerializeObject(draft.Payload);
            Assert.IsFalse(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.Army, "minas-tirith"), new DraftToken(DraftObject.Region, "erebor")));
            Assert.IsFalse(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.Army, "dale"), new DraftToken(DraftObject.Region, "erebor")));
            Assert.AreEqual(before, JsonConvert.SerializeObject(draft.Payload));
        }

        [Test]
        public void SpecialCostsRemainExplicitAndCharacterCostUsesOneLedArmy()
        {
            var fixture = new BoardFixture();
            fixture.View.Dice.Add(new DieView { Id = "wild", Face = DieFace.WillOfTheWest, Source = "standard", Location = DieLocation.Available });
            fixture.Refresh();
            var draft = new TurnDraft();
            BoardDraft.SelectRegion(draft, fixture.View, "minas-tirith");
            Assert.IsEmpty(draft.Payload.DieId);
            draft.Payload.DieId = "wild";
            draft.Payload.MovementCost = DieFace.Character;
            Assert.IsTrue(ArmyDraft.Target(draft, fixture.View, "osgiliath"));
            Assert.IsFalse(ArmyDraft.Start(draft, fixture.View, "dale"));
        }

        [Test]
        public void RecruitmentClicksAndDragMatchAndNeitherAllowTwoFiguresInOneSettlement()
        {
            var view = new BoardFixture().View;
            var first = view.Units.First(u => u.Nation == "gondor" && u.Kind == UnitKind.Regular && u.Location == "reserve").Id;
            var second = view.Units.First(u => u.Nation == "gondor" && u.Kind == UnitKind.Leader && u.Location == "reserve").Id;
            var click = new TurnDraft();
            RecruitDraft.Select(click, view, first);
            RecruitDraft.Place(click, view, "pelargir");
            var drag = new TurnDraft();
            Assert.IsTrue(DraftGesture.Drop(drag, view, new DraftToken(DraftObject.Reserve, first), new DraftToken(DraftObject.Region, "pelargir")));
            Assert.AreEqual(JsonConvert.SerializeObject(click.Payload), JsonConvert.SerializeObject(drag.Payload));
            RecruitDraft.Select(click, view, second);
            Assert.IsFalse(RecruitDraft.Place(click, view, "pelargir"));
            Assert.IsTrue(RecruitDraft.Place(click, view, "minas-tirith"));
            Assert.AreEqual(2, click.Payload.Placements.Count);
            click.Back();
            Assert.AreEqual(1, click.Payload.Placements.Count);
            Assert.AreEqual(second, click.ReserveId);
        }

        [Test]
        public void EliteUsesTheWholeMusterAndUndoDoesNotMutateACommandSnapshot()
        {
            var view = new BoardFixture().View;
            var elite = view.Units.First(u => u.Nation == "gondor" && u.Kind == UnitKind.Elite).Id;
            var draft = new TurnDraft();
            RecruitDraft.Select(draft, view, elite);
            RecruitDraft.Place(draft, view, "minas-tirith");
            var command = draft.Command(view);
            Assert.IsFalse(RecruitDraft.CanAdd(draft, view, UnitKind.Regular));
            RecruitDraft.Remove(draft, elite);
            Assert.AreEqual(1, command.Payload.Placements.Count);
            Assert.AreEqual(0, draft.Payload.Placements.Count);
            draft.Back();
            Assert.AreEqual(1, draft.Payload.Placements.Count);
        }

        [Test]
        public void ReconcileKeepsValidDestinationsButRemovesStaleFiguresAndCostsWithoutSubstitution()
        {
            var view = new BoardFixture().View;
            var draft = new TurnDraft();
            ArmyDraft.Start(draft, view, "minas-tirith");
            ArmyDraft.Target(draft, view, "osgiliath");
            var removed = draft.Payload.Moves[0].UnitIds[0];
            view.Options[0].Armies.First(a => a.RegionId == "minas-tirith").UnitIds.Remove(removed);
            view.Options[0].EligibleDice.Clear();
            draft.Reconcile(view);
            Assert.AreEqual("osgiliath", draft.Payload.Moves[0].To);
            Assert.IsFalse(draft.Payload.Moves[0].UnitIds.Contains(removed));
            Assert.IsEmpty(draft.Payload.DieId);
        }
    }
}
