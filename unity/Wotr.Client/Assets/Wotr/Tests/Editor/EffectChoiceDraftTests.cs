using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class EffectChoiceDraftTests
    {
        [Test]
        public void TwoMissesToggleIndependentlyAndAnExclusiveAlternativeClearsBoth()
        {
            var option = new ActionOption
            {
                Kind = CommandKind.ChooseEffect,
                Minimum = 1,
                Maximum = 2,
                EligibleIds = { "a", "b", "c", "keep" },
                EffectChoices = {
                    new EffectChoiceView { Id = "a", SelectionGroup = "Miss", GroupMaximum = 2 },
                    new EffectChoiceView { Id = "b", SelectionGroup = "Miss", GroupMaximum = 2 },
                    new EffectChoiceView { Id = "c", SelectionGroup = "Miss", GroupMaximum = 2 },
                    new EffectChoiceView { Id = "keep", SelectionGroup = "Miss", Exclusive = true }
                }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            CardDraft.Choose(draft, option, "a");
            CardDraft.Choose(draft, option, "b");
            Assert.IsTrue(CardDraft.Ready(draft, option));
            Assert.IsFalse(EffectChoiceDraft.CanChoose(draft, option, "c"));
            CardDraft.Choose(draft, option, "a");
            CollectionAssert.AreEqual(new[] { "b" }, draft.Payload.Ids);
            CardDraft.Choose(draft, option, "keep");
            Assert.IsTrue(CardDraft.Ready(draft, option));
            CollectionAssert.AreEqual(new[] { "keep" }, draft.Payload.Ids);
            CardDraft.Choose(draft, option, "c");
            CollectionAssert.AreEqual(new[] { "c" }, draft.Payload.Ids);
        }

        [Test]
        public void ChangingSacrificeClearsIneligibleHitCountWithoutChoosingAnotherForThePlayer()
        {
            var option = new ActionOption
            {
                Kind = CommandKind.ChooseEffect,
                Minimum = 1,
                Maximum = 2,
                EligibleIds = { "aragorn", "leader", "one", "three", "skip" },
                EffectChoices = {
                    new EffectChoiceView { Id = "aragorn", SelectionGroup = "Sacrifice" },
                    new EffectChoiceView { Id = "leader", SelectionGroup = "Sacrifice" },
                    new EffectChoiceView { Id = "skip", SelectionGroup = "Sacrifice", Exclusive = true },
                    new EffectChoiceView { Id = "one", SelectionGroup = "Hits", RequiresAny = { "aragorn", "leader" } },
                    new EffectChoiceView { Id = "three", SelectionGroup = "Hits", RequiresAny = { "aragorn" } }
                }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            Assert.IsFalse(EffectChoiceDraft.CanChoose(draft, option, "one"));
            CardDraft.Choose(draft, option, "aragorn");
            CardDraft.Choose(draft, option, "three");
            Assert.IsTrue(CardDraft.Ready(draft, option));
            CardDraft.Choose(draft, option, "leader");
            CollectionAssert.AreEqual(new[] { "leader" }, draft.Payload.Ids);
            Assert.IsFalse(CardDraft.Ready(draft, option));
            Assert.IsFalse(EffectChoiceDraft.CanChoose(draft, option, "three"));
            draft.Back();
            CollectionAssert.AreEquivalent(new[] { "aragorn", "three" }, draft.Payload.Ids);
            CardDraft.Choose(draft, option, "skip");
            Assert.IsTrue(CardDraft.Ready(draft, option));
        }
    }
}
