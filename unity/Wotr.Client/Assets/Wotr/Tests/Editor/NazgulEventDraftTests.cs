using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class NazgulEventDraftTests
    {
        [Test]
        public void FellowshipGhostUsesProjectedTargetAndClearsWithUndo()
        {
            var view = new PlayerView();
            var option = new ActionOption
            {
                Kind = CommandKind.ChooseEffect,
                Minimum = 1,
                Maximum = 1,
                EligibleIds = { "north-dunland" },
                EffectChoices = { new EffectChoiceView { Id = "north-dunland", SourceId = "fellowship", TargetId = "north-dunland" } }
            };
            view.Options.Add(option);
            view.Fellowship.RegionId = "hollin";
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            CardDraft.Choose(draft, option, "north-dunland");
            Assert.AreEqual("north-dunland", EffectChoiceDraft.FellowshipDestination(view, draft));
            Assert.AreEqual("hollin", view.Fellowship.RegionId);
            draft.Back();
            Assert.IsEmpty(EffectChoiceDraft.FellowshipDestination(view, draft));
        }

        [Test]
        public void EventAllowsEmptyFlightWithoutDieAndInvalidDestinationCannotConfirm()
        {
            var view = new PlayerView { Decision = new DecisionView { Id = "flight", Kind = DecisionKind.CharacterEvent } };
            var option = new ActionOption
            {
                Kind = CommandKind.MoveShadowCharacters,
                AllowEmptySelection = true,
                EligibleIds = { "nazgul" },
                Travel = { new TravelOption { Id = "nazgul", Origin = "moria", Flight = true, Targets = { "moria", "hollin" } } }
            };
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveShadowCharacters);
            Assert.IsTrue(draft.Ready(option));
            Assert.That(FellowshipDraft.Hint(draft, view), Does.Contain("Nazgûl"));
            TravelDraft.ToggleCompanion(draft, view, option, "nazgul");
            Assert.IsFalse(draft.Ready(option));
            draft.Change(p => p.CompanionMoves[0].Route.AddRange(new[] { "moria", "hollin" }));
            Assert.IsTrue(draft.Ready(option));
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.That(FellowshipSummary.Describe(draft, view, new ActionPreview(), null), Does.Contain("remaining effects"));
            option.Travel[0].Targets.Remove("hollin");
            draft.Reconcile(view);
            Assert.IsFalse(draft.Ready(option));
        }
    }
}
