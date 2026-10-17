using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class RecruitTogetherDraftTests
    {
        [Test]
        public void EachNationKeepsOneFigureAndBothGhostsUndoTogether()
        {
            var view = View();
            var option = view.Options[0];
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            CardDraft.Choose(draft, option, "north-regular");
            CardDraft.Choose(draft, option, "north-elite");
            Assert.IsFalse(draft.Ready(option));
            Assert.AreEqual("north-elite", draft.Payload.Ids.Single());
            CardDraft.Choose(draft, option, "dwarves-regular");
            Assert.IsTrue(draft.Ready(option));
            CollectionAssert.AreEquivalent(new[] { "the-shire", "ered-luin" }, EffectChoiceDraft.Placements(view, draft).Select(p => p.RegionId));
            CardDraft.Choose(draft, option, "north-regular");
            draft.Back();
            Assert.IsTrue(draft.Ready(option));
            CollectionAssert.AreEquivalent(new[] { "north-elite", "dwarves-regular" }, draft.Payload.Ids);
            draft.Reconcile(view);
            Assert.IsTrue(draft.Ready(option));
            option.EligibleIds.Remove("dwarves-regular");
            option.EffectChoices.RemoveAll(c => c.Id == "dwarves-regular");
            draft.Reconcile(view);
            Assert.IsFalse(draft.Ready(option));
            Assert.AreEqual("the-shire", EffectChoiceDraft.Placements(view, draft).Single().RegionId);
        }

        private static PlayerView View()
        {
            var view = new PlayerView { Decision = new DecisionView { Id = "swords", Kind = DecisionKind.EffectChoice, Presentation = "ReserveFigures" } };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 2, Maximum = 2 };
            foreach (var nation in new[] { "north", "dwarves" })
                foreach (var kind in new[] { "regular", "elite" })
                {
                    var id = nation + "-" + kind;
                    option.EligibleIds.Add(id);
                    option.EffectChoices.Add(new EffectChoiceView
                    {
                        Id = id,
                        TargetId = nation == "north" ? "the-shire" : "ered-luin",
                        SelectionGroup = nation,
                        GroupMinimum = 1,
                        GroupMaximum = 1
                    });
                }
            view.Options.Add(option);
            return view;
        }
    }
}
