using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class CardJourneyDraftTests
    {
        [Test]
        public void OneGroupJourneyCannotAddAnotherGroupAndStopsAtBesiegedDestination()
        {
            var view = new PlayerView { Decision = new DecisionView { Kind = DecisionKind.CompanionSeparation } };
            var option = new ActionOption { Kind = CommandKind.MoveCompanions, MaximumGroups = 1, EligibleIds = { "gimli", "legolas" } };
            foreach (var id in option.EligibleIds)
            {
                option.Travel.Add(new TravelOption { Id = id, Origin = "dimrill-dale", Range = 4, Targets = { "dimrill-dale", "lorien" }, StopRegions = { "lorien" } });
                view.Characters.Add(new CharacterView { Id = id, Location = "dimrill-dale" });
            }
            view.Options.Add(option);
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            TravelDraft.ToggleCompanion(draft, view, option, "gimli");
            Assert.IsTrue(TravelDraft.Step(draft, view, BoardPresentation.Load(), "lorien"));
            Assert.IsTrue(draft.Ready(option));
            Assert.IsEmpty(TravelDraft.Next(draft, view, BoardPresentation.Load()));
            TravelDraft.NewGroup(draft);
            Assert.IsFalse(TravelDraft.CanToggle(draft, view, option, "legolas"));
            draft.Payload.CompanionMoves.Add(new CompanionMoveChoice { Ids = { "legolas" }, Route = { "dimrill-dale" } });
            Assert.IsFalse(draft.Ready(option));
        }

        [Test]
        public void MirrorDieDragUsesChoiceIdsWithoutRepayingTheEvent()
        {
            var view = new PlayerView { Decision = new DecisionView { Kind = DecisionKind.EffectChoice, Presentation = "ActionDie" } };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 1, Maximum = 1, EligibleIds = { "unused" } };
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            Assert.IsFalse(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.Die, "paid"), new DraftToken(DraftObject.Tray)));
            Assert.IsTrue(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.Die, "unused"), new DraftToken(DraftObject.Tray)));
            Assert.AreEqual("unused", draft.Payload.Ids.Single());
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.IsTrue(draft.Ready(option));
        }
    }
}
