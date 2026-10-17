using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class ShadowCardDraftTests
    {
        [Test]
        public void LidlessChoosesZeroToThreeDiceAndUndoDoesNotMutateResources()
        {
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 0, Maximum = 3 };
            foreach (var id in new[] { "a", "b", "c", "d" })
            {
                option.EligibleIds.Add(id);
                option.EffectChoices.Add(new EffectChoiceView { Id = id, SelectionGroup = "Dice", GroupMinimum = 0, GroupMaximum = 3 });
            }
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            Assert.IsTrue(draft.Ready(option));
            foreach (var id in option.EligibleIds)
                CardDraft.Choose(draft, option, id);
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, draft.Payload.Ids);
            Assert.IsTrue(draft.Ready(option));
            draft.Back();
            CollectionAssert.AreEqual(new[] { "a", "b" }, draft.Payload.Ids);
            CardDraft.Choose(draft, option, "a");
            CollectionAssert.AreEqual(new[] { "b" }, draft.Payload.Ids);
        }

        [Test]
        public void BreakingRequiresWholeGroupAndPreviewsItsFixedDestination()
        {
            var view = new PlayerView { Decision = new DecisionView { Id = "break", Presentation = "FellowshipCompanions" } };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 2, Maximum = 2 };
            foreach (var id in new[] { "merry", "pippin", "gimli" })
            {
                option.EligibleIds.Add(id);
                option.EffectChoices.Add(new EffectChoiceView { Id = id, TargetId = "hollin", SelectionGroup = "Companions", GroupMinimum = 2, GroupMaximum = 2 });
            }
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            CardDraft.Choose(draft, option, "merry");
            Assert.IsFalse(draft.Ready(option));
            CardDraft.Choose(draft, option, "gimli");
            Assert.IsTrue(draft.Ready(option));
            Assert.AreEqual(2, EffectChoiceDraft.Placements(view, draft).Count());
            Assert.IsTrue(EffectChoiceDraft.Placements(view, draft).All(p => p.RegionId == "hollin"));
            option.EligibleIds.Remove("gimli");
            draft.Reconcile(view);
            Assert.IsFalse(draft.Ready(option));
        }
    }
}
