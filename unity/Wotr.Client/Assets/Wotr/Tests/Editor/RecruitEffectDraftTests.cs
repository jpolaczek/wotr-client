using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class RecruitEffectDraftTests
    {
        [Test]
        public void TwoRecruitsStayTogetherAndUndoRestoresTheSecondFigure()
        {
            var view = View();
            var option = view.Options.Single();
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            CardDraft.Choose(draft, option, "regular");
            Assert.IsFalse(draft.Ready(option));
            CardDraft.Choose(draft, option, "elite");
            Assert.IsTrue(draft.Ready(option));
            Assert.AreEqual(2, EffectChoiceDraft.Placements(view, draft).Count());
            Assert.IsFalse(EffectChoiceDraft.CanChoose(draft, option, "elite-2"));
            CardDraft.Choose(draft, option, "regular");
            Assert.IsFalse(draft.Ready(option));
            draft.Back();
            CollectionAssert.AreEquivalent(new[] { "regular", "elite" }, draft.Payload.Ids);
            Assert.IsTrue(draft.Ready(option));
        }

        [Test]
        public void ReconnectPreservesDraftButLostReserveRemovesItsGhost()
        {
            var view = View();
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            CardDraft.Choose(draft, view.Options[0], "regular");
            CardDraft.Choose(draft, view.Options[0], "elite");
            draft.Reconcile(view);
            Assert.IsTrue(draft.Ready(view.Options[0]));
            view.Options[0].EligibleIds.Remove("elite");
            view.Options[0].EffectChoices.RemoveAll(c => c.Id == "elite");
            draft.Reconcile(view);
            Assert.IsFalse(draft.Ready(view.Options[0]));
            Assert.AreEqual("regular", EffectChoiceDraft.Placements(view, draft).Single().UnitId);
        }

        private static PlayerView View()
        {
            var view = new PlayerView { Decision = new DecisionView { Id = "recruit", Kind = DecisionKind.EffectChoice, Presentation = "ReserveFigures" } };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 2, Maximum = 2 };
            foreach (var id in new[] { "regular", "elite", "elite-2" })
            {
                option.EligibleIds.Add(id);
                option.EffectChoices.Add(new EffectChoiceView { Id = id, TargetId = "grey-havens", SelectionGroup = "Recruits", GroupMinimum = 2, GroupMaximum = 2 });
            }
            view.Options.Add(option);
            return view;
        }
    }
}
