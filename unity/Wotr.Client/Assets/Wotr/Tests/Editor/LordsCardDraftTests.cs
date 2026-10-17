using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class LordsCardDraftTests
    {
        [Test]
        public void SixLordsCardsHaveBothReadableHalvesAndStableInitiatives()
        {
            var cards = CardLibrary.Cards.Values.Where(c => c.Id.StartsWith("lords.fp.")).ToArray();
            Assert.AreEqual(6, cards.Length);
            foreach (var card in cards)
            {
                Assert.IsNotEmpty(card.Title);
                Assert.IsNotEmpty(card.Event);
                Assert.IsNotEmpty(card.Combat);
                Assert.AreEqual(1, card.Initiative.Length);
            }
            Assert.AreEqual("Orderly Withdrawal", CardLibrary.Cards["lords.fp.character.27"].CombatTitle);
        }

        [Test]
        public void RingChoiceRequiresRemovedRingAndPreservesTheChosenShadowIdentity()
        {
            var option = new ActionOption
            {
                Kind = CommandKind.ChooseEffect,
                Minimum = 2,
                Maximum = 2,
                EligibleIds = { "vilya", "nenya", "narya" },
                EffectChoices = {
                    new EffectChoiceView { Id = "vilya", SelectionGroup = "Out of play" },
                    new EffectChoiceView { Id = "nenya", SelectionGroup = "Held by Shadow" },
                    new EffectChoiceView { Id = "narya", SelectionGroup = "Held by Shadow" }
                }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            CardDraft.Choose(draft, option, "nenya");
            Assert.IsFalse(CardDraft.Ready(draft, option));
            CardDraft.Choose(draft, option, "vilya");
            Assert.IsTrue(CardDraft.Ready(draft, option));
            CardDraft.Choose(draft, option, "narya");
            CollectionAssert.AreEquivalent(new[] { "vilya", "narya" }, draft.Payload.Ids);
            draft.Back();
            CollectionAssert.AreEquivalent(new[] { "vilya", "nenya" }, draft.Payload.Ids);
        }

        [Test]
        public void NoShadowRingCanBeSelectedWhenOnlyRemovedRingsAreRequired()
        {
            var option = new ActionOption
            {
                Kind = CommandKind.ChooseEffect,
                Minimum = 1,
                Maximum = 1,
                EligibleIds = { "vilya", "nenya", "narya" },
                EffectChoices = {
                    new EffectChoiceView { Id = "vilya", SelectionGroup = "Out of play" },
                    new EffectChoiceView { Id = "nenya", SelectionGroup = "Out of play" },
                    new EffectChoiceView { Id = "narya", SelectionGroup = "Held by Shadow", GroupMinimum = 0, GroupMaximum = 0 }
                }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            Assert.IsFalse(EffectChoiceDraft.CanChoose(draft, option, "narya"));
            CardDraft.Choose(draft, option, "nenya");
            Assert.IsTrue(CardDraft.Ready(draft, option));
        }
    }
}
