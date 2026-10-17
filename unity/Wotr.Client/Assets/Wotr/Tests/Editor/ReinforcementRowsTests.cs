using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    // UI-130: rows of the Reinforcements panel, the server's reasons and choosing a reserve without a settlement.
    public sealed class ReinforcementRowsTests
    {
        [Test]
        public void RowsShowEveryOwnNationWithTheServerReasonAndTheEliteBudget()
        {
            var view = Fixture(out var recruit);
            var rows = ReinforcementRows.For(view, recruit);
            CollectionAssert.AreEqual(new[] { "dwarves", "elves", "gondor", "north", "rohan" }, rows.Select(r => r.Nation));
            var rohan = rows.Single(r => r.Nation == "rohan");
            Assert.IsFalse(rohan.CanRecruit);
            Assert.AreEqual("Rohan is 1 step from war.", rohan.Reason);
            Assert.IsEmpty(rohan.Status, "UI-179: a row carries no political status.");
            Assert.IsTrue(rohan.Figures.All(f => f.ReserveId == null));
            var gondor = rows.Single(r => r.Nation == "gondor");
            Assert.IsTrue(gondor.CanRecruit);
            Assert.IsEmpty(gondor.Status);
            Assert.NotNull(gondor.Figures.Single(f => f.Kind == UnitKind.Regular).ReserveId);
            var elite = gondor.Figures.Single(f => f.Kind == UnitKind.Elite);
            Assert.IsNull(elite.ReserveId);
            Assert.AreEqual("An Elite needs both recruitment points.", elite.Reason);
            var elves = rows.Single(r => r.Nation == "elves");
            Assert.IsEmpty(elves.Status);
            Assert.AreEqual("", elves.Reason, "Without a server entry the panel does not guess a reason.");
        }

        // UI-179: outside a recruitment the tab lists the reserves, every row inert.
        [Test]
        public void WithoutARecruitmentTheRowsOnlyListTheReserves()
        {
            var view = Fixture(out _);
            var state = ReinforcementsState.Browse(view);
            Assert.IsFalse(state.Open);
            Assert.IsFalse(state.Interactive);
            CollectionAssert.AreEqual(new[] { "dwarves", "elves", "gondor", "north", "rohan" }, state.Rows.Select(r => r.Nation));
            Assert.IsTrue(state.Rows.All(r => !r.CanRecruit && r.Status.Length == 0));
            Assert.IsTrue(state.Rows.SelectMany(r => r.Figures).All(f => f.ReserveId == null));
            var regular = state.Rows.Single(r => r.Nation == "gondor").Figures.Single(f => f.Kind == UnitKind.Regular);
            Assert.AreEqual(ReinforcementRows.InReserve(view, "gondor", UnitKind.Regular), regular.Reserve, "The reserve count still shows.");
        }

        [Test]
        public void TheRefusalNamesTheClosedSettlementInsteadOfTheBudget()
        {
            var view = Fixture(out _);
            Assert.AreEqual("Pelargir is held by the enemy.", ReinforcementRows.Refusal(view, "gondor", "pelargir", UnitKind.Regular));
            Assert.AreEqual("An Elite needs both recruitment points.", ReinforcementRows.Refusal(view, "gondor", "minas-tirith", UnitKind.Elite));
            Assert.AreEqual("Rohan is 1 step from war.", ReinforcementRows.Refusal(view, "rohan", "edoras", UnitKind.Regular));
            Assert.AreEqual("Not available here now", ReinforcementRows.Refusal(view, "elves", "rivendell", UnitKind.Regular));
        }

        // UI-135: the palette's lone-leader warning now rides on the Leader token.
        [Test]
        public void ALeaderThatCouldStandAloneCarriesTheWarning()
        {
            var view = Fixture(out var recruit);
            var gondor = ReinforcementRows.For(view, recruit).Single(r => r.Nation == "gondor");
            Assert.AreEqual(ReinforcementRows.OrphanWarning, gondor.Figures.Single(f => f.Kind == UnitKind.Leader).Warning);
            Assert.AreEqual("", gondor.Figures.Single(f => f.Kind == UnitKind.Regular).Warning);
        }

        [Test]
        public void BudgetFollowsTheRecruitmentSequence()
        {
            var view = Fixture(out _);
            Assert.AreEqual((0, 2), ReinforcementRows.Budget(view));
            view.ActionSequence = new ActionSequenceView { Kind = CommandKind.Recruit, Remaining = 1 };
            Assert.AreEqual((1, 2), ReinforcementRows.Budget(view));
        }

        [Test]
        public void FinishAppearsOnlyInTheViewersOwnRecruitmentSequence()
        {
            var view = Fixture(out var recruit);
            Assert.IsFalse(ReinforcementsState.For(view, recruit, "", true).Finishable, "No sequence before the first step.");
            view.Decision.Kind = DecisionKind.ActionSequence;
            view.ActionSequence = new ActionSequenceView { Kind = CommandKind.Recruit, Owner = view.You, Remaining = 1 };
            var state = ReinforcementsState.For(view, recruit, "", true);
            Assert.IsTrue(state.Finishable);
            Assert.AreEqual((1, 2), state.Points);
            view.ActionSequence.Kind = CommandKind.MoveArmies;
            Assert.IsFalse(ReinforcementsState.For(view, recruit, "", true).Finishable, "A march sequence has its own ✓.");
            Assert.IsFalse(ReinforcementsState.Closed.Open);
        }

        [Test]
        public void ChoosingAReserveLightsOnlyItsSettlementsInOneUndoStep()
        {
            var view = Fixture(out var recruit);
            var context = new ActionDieContext();
            context.Sync(view, "muster");
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "muster");
            var regular = recruit.Recruitment.First(g => g.Nation == "gondor" && g.Kind == UnitKind.Regular).ReserveIds[0];
            Assert.IsTrue(MusterActions.Select(draft, view, context, regular));
            Assert.AreEqual(CommandKind.Recruit, draft.Kind);
            Assert.AreEqual(regular, draft.ReserveId);
            Assert.IsEmpty(draft.Payload.Placements, "Choosing a reserve sends nothing and places nothing yet.");
            CollectionAssert.AreEquivalent(MusterActions.FigureTargets(view, context, regular),
                MusterActions.Targets(view, draft, context).ToList());
            Assert.IsFalse(MusterActions.Select(draft, view, context, "no-such-figure"));
            draft.Back();
            Assert.IsNull(draft.Kind);
            Assert.AreEqual("muster", draft.Payload.DieId);
        }

        private static PlayerView Fixture(out ActionOption recruit)
        {
            var fixture = new BoardFixture();
            fixture.View.Nations.First(n => n.Id == "rohan").StepsToWar = 1;
            fixture.Refresh();
            var view = fixture.View;
            view.Availability.Add(new ActionAvailabilityView
            {
                SubjectId = "rohan", ActionId = "recruit", Conditions =
                {
                    new AvailabilityConditionView { Code = "at-war", Description = "Rohan is 1 step from war.", Met = false },
                    new AvailabilityConditionView { Code = "settlement:edoras:at-war", Description = "Edoras: the nation is not at war.", Met = false }
                }
            });
            view.Availability.Add(new ActionAvailabilityView
            {
                SubjectId = "gondor", ActionId = "recruit", Conditions =
                {
                    new AvailabilityConditionView { Code = "at-war", Description = "Gondor is at war.", Met = true },
                    new AvailabilityConditionView { Code = "settlement:pelargir:captured", Description = "Pelargir is held by the enemy.", Met = false },
                    new AvailabilityConditionView { Code = "settlement:minas-tirith", Description = "Minas Tirith can take a reinforcement.", Met = true },
                    new AvailabilityConditionView { Code = "elite-budget", Description = "An Elite needs both recruitment points.", Met = false }
                }
            });
            recruit = view.Options.First(o => o.Kind == CommandKind.Recruit);
            recruit.Recruitment.RemoveAll(g => g.Nation == "gondor" && g.Kind == UnitKind.Elite);
            return view;
        }
    }
}
