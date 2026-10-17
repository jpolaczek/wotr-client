using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class NazgulLeadershipDraftTests
    {
        [Test]
        public void VariableCostKeepsAllSelectedPointsAndExplainsItsOwnEffect()
        {
            var view = new PlayerView
            {
                Decision = new DecisionView
                {
                    Id = "dread",
                    Kind = DecisionKind.EffectChoice,
                    Presentation = "NazgulLeadership",
                    SourceId = "base.sh.character.08"
                }
            };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 1, Maximum = 10 };
            foreach (var index in Enumerable.Range(0, 10))
            {
                var id = "point-" + index;
                option.EligibleIds.Add(id);
                option.EffectChoices.Add(new EffectChoiceView { Id = id, SelectionGroup = "Leadership", GroupMinimum = 1, GroupMaximum = 10 });
            }
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            Assert.IsFalse(draft.Ready(option));
            foreach (var id in option.EligibleIds)
                CardDraft.Choose(draft, option, id);
            Assert.IsTrue(draft.Ready(option));
            Assert.AreEqual(10, draft.Payload.Ids.Count);
            Assert.That(NazgulLeadershipSummary.Describe(draft, view), Does.Contain("10 Leadership").And.Contain("Fewer opposing Combat dice"));
            draft.Back();
            Assert.AreEqual(9, draft.Payload.Ids.Count);
            Assert.IsTrue(draft.Ready(option));
            view.Decision.SourceId = "base.sh.character.07";
            Assert.That(NazgulLeadershipSummary.Describe(draft, view), Does.Contain("+1 Leader re-roll"));
        }

        [Test]
        public void TwoPointsCanComeFromOneFigureAndRemainOneUndoableDraft()
        {
            var view = new PlayerView { Decision = new DecisionView { Id = "cruel", Kind = DecisionKind.EffectChoice, Presentation = "NazgulLeadership" } };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 2, Maximum = 2 };
            foreach (var id in new[] { "wk:1", "wk:2", "nazgul:1" })
            {
                option.EligibleIds.Add(id);
                option.EffectChoices.Add(new EffectChoiceView
                {
                    Id = id,
                    SourceId = id.StartsWith("wk") ? "witch-king" : "nazgul",
                    SelectionGroup = "Leadership",
                    GroupMinimum = 2,
                    GroupMaximum = 2
                });
            }
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            CardDraft.Choose(draft, option, "wk:1");
            Assert.IsFalse(draft.Ready(option));
            CardDraft.Choose(draft, option, "wk:2");
            Assert.IsTrue(draft.Ready(option));
            Assert.IsFalse(EffectChoiceDraft.CanChoose(draft, option, "nazgul:1"));
            Assert.IsEmpty(EffectChoiceDraft.Placements(view, draft));
            draft.Back();
            Assert.AreEqual("wk:1", draft.Payload.Ids.Single());
            CardDraft.Choose(draft, option, "nazgul:1");
            draft.Reconcile(view);
            Assert.IsTrue(draft.Ready(option));
            option.EligibleIds.Remove("wk:1");
            option.EffectChoices.RemoveAll(c => c.Id == "wk:1");
            draft.Reconcile(view);
            Assert.IsFalse(draft.Ready(option));
            Assert.AreEqual("nazgul:1", draft.Payload.Ids.Single());
        }
    }
}
