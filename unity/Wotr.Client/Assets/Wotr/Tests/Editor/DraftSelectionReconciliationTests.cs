using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.EditorTests.DraftReconciliationTests;

namespace Wotr.EditorTests
{
    public class DraftSelectionReconciliationTests
    {
        [Test]
        public void ADieSelectedBeforeTheActionIsRemovedOnlyWhenItStopsBeingAvailable()
        {
            var view = View(CommandKind.DrawCard);
            var die = new DieView { Id = "wild", Owner = view.You, Location = DieLocation.Available };
            view.Dice.Add(die);
            var draft = new TurnDraft();
            draft.Change(payload => payload.DieId = die.Id);
            Assert.IsFalse(draft.Reconcile(view));
            die.Location = DieLocation.Spent;
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.IsNull(draft.Kind);
        }

        [Test]
        public void LosingARingPreservesTheExactDieAndChosenFaceWithoutChoosingAnotherRing()
        {
            var view = View(CommandKind.UseRing);
            view.Options[0].EligibleDice.Add("die");
            view.Options[0].EligibleIds.Add("ring-two");
            view.Options[0].Faces.Add(DieFace.Character);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.UseRing, "die");
            draft.Payload.RingId = "ring-one";
            draft.Payload.Face = DieFace.Character;
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsEmpty(draft.Payload.RingId);
            Assert.AreEqual("die", draft.Payload.DieId);
            Assert.AreEqual(DieFace.Character, draft.Payload.Face);
            Assert.IsFalse(draft.Ready(view.Options[0]));
        }

        [Test]
        public void PoliticsLosesOnlyItsUnavailableNationAndKeepsTheDie()
        {
            var view = View(CommandKind.AdvancePolitics);
            view.Options[0].EligibleDice.Add("muster");
            view.Options[0].Targets.Add("elves");
            var draft = new TurnDraft();
            draft.Choose(CommandKind.AdvancePolitics, "muster");
            draft.Payload.NationId = "gondor";
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsEmpty(draft.Payload.NationId);
            Assert.AreEqual("muster", draft.Payload.DieId);
            Assert.IsFalse(draft.Ready(view.Options[0]));
        }

        [Test]
        public void OrdinaryRecruitmentKeepsLegalPlacementsAndAnIndependentSelectedReserve()
        {
            var view = View(CommandKind.Recruit);
            var option = view.Options[0];
            option.EligibleDice.Add("muster");
            option.Recruitment.Add(new RecruitmentOption
            {
                ReserveIds = { "removed", "kept", "pending" }, Targets = { "town", "city" }
            });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.Recruit, "muster");
            draft.Payload.Placements.Add(new UnitPlacementChoice { UnitId = "removed", RegionId = "town" });
            draft.Payload.Placements.Add(new UnitPlacementChoice { UnitId = "kept", RegionId = "city" });
            option.Recruitment[0].ReserveIds.Remove("removed");
            Assert.IsTrue(draft.Reconcile(view));
            Assert.AreEqual("kept", draft.Payload.Placements[0].UnitId);
            Assert.IsTrue(RecruitDraft.Select(draft, view, "pending"));
            Assert.IsFalse(draft.Reconcile(view));
            Assert.AreEqual("pending", draft.ReserveId);
            Assert.AreEqual("muster", draft.Payload.DieId);
        }

        [Test]
        public void SmallerEventRecruitmentLimitDoesNotChooseWhichLegalPlacementToDiscard()
        {
            var view = View(CommandKind.ChooseEffect);
            var option = view.Options[0];
            option.Minimum = option.Maximum = 2;
            option.Targets.Add("town");
            option.EventRecruitment = new EventRecruitmentOption
            {
                Limits = { new RecruitmentLimit { Nation = "gondor", RegionId = "town", Kinds = { UnitKind.Regular }, Maximum = 2 } }
            };
            option.Recruitment.Add(new RecruitmentOption
            {
                Nation = "gondor", Kind = UnitKind.Regular, ReserveIds = { "one", "two" }, Targets = { "town" }
            });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            Assert.IsTrue(EventRecruitDraft.Drop(draft, option, "one", "town"));
            Assert.IsTrue(EventRecruitDraft.Drop(draft, option, "two", "town"));
            Assert.IsFalse(draft.Reconcile(view));

            option.Minimum = option.Maximum = 1;
            option.EventRecruitment.Limits[0].Maximum = 1;
            Assert.IsFalse(draft.Reconcile(view));
            Assert.AreEqual(2, draft.Payload.Placements.Count);
            Assert.IsFalse(draft.Ready(option));
            EventRecruitDraft.Remove(draft, "one");
            Assert.IsTrue(draft.Ready(option));
            Assert.AreEqual("two", draft.Payload.Placements[0].UnitId);
        }

        [Test]
        public void SmallerEffectGroupLimitPreservesAllChoicesUntilThePlayerRemovesOne()
        {
            var view = View(CommandKind.ChooseEffect);
            var option = view.Options[0];
            option.EligibleIds.AddRange(new[] { "one", "two" });
            foreach (var id in option.EligibleIds)
                option.EffectChoices.Add(new EffectChoiceView { Id = id, SelectionGroup = "recruits", GroupMinimum = 1, GroupMaximum = 1 });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            draft.Payload.Ids.AddRange(new[] { "one", "two" });
            Assert.IsFalse(draft.Reconcile(view));
            CollectionAssert.AreEqual(new[] { "one", "two" }, draft.Payload.Ids);
            Assert.IsFalse(draft.Ready(option));
        }

        [Test]
        public void AnUnavailableCombatCardDoesNotBecomeASelectedNoCardAction()
        {
            var view = View(CommandKind.CommitCombatCard);
            view.Options[0].EligibleIds.Add("replacement-card");
            var draft = new TurnDraft();
            draft.Choose(CommandKind.CommitCombatCard);
            draft.Payload.CardId = "old-card";
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsEmpty(draft.Payload.CardId);
            Assert.IsFalse(draft.Ready(view.Options[0]));
        }
    }
}
