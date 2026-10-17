using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class LordsShadowDraftTests
    {
        [Test]
        public void SixShadowCardsHaveReadableHalvesAndMumakilHasBothInitiatives()
        {
            var cards = CardLibrary.Cards.Values.Where(c => c.Id.StartsWith("lords.sh.character.") || c.Id == "lords.sh.strategy.25").ToArray();
            Assert.AreEqual(6, cards.Length);
            foreach (var card in cards)
            {
                Assert.IsNotEmpty(card.Title);
                Assert.IsNotEmpty(card.Event);
                Assert.IsNotEmpty(card.Combat);
            }
            CollectionAssert.AreEqual(new[] { 3, 5 }, CardLibrary.Cards["lords.sh.strategy.25"].Initiative);
        }

        [Test]
        public void LoreChoosesDieAndFaceInOneDraftWithDependenciesBackAndDecline()
        {
            var option = new ActionOption
            {
                Kind = CommandKind.ChooseEffect, Minimum = 1, Maximum = 2,
                EligibleIds = { "standard", "gothmog", "character", "army", "keep" },
                EffectChoices = {
                    new EffectChoiceView { Id = "standard", SelectionGroup = "Die" },
                    new EffectChoiceView { Id = "gothmog", SelectionGroup = "Die" },
                    new EffectChoiceView { Id = "character", SelectionGroup = "Result", RequiresAny = { "standard" } },
                    new EffectChoiceView { Id = "army", SelectionGroup = "Result", RequiresAny = { "standard", "gothmog" } },
                    new EffectChoiceView { Id = "keep", Exclusive = true }
                }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            Assert.IsFalse(EffectChoiceDraft.CanChoose(draft, option, "character"));
            CardDraft.Choose(draft, option, "standard");
            Assert.IsFalse(draft.Ready(option));
            CardDraft.Choose(draft, option, "character");
            Assert.IsTrue(draft.Ready(option));
            CardDraft.Choose(draft, option, "gothmog");
            CollectionAssert.AreEqual(new[] { "gothmog" }, draft.Payload.Ids);
            Assert.IsFalse(draft.Ready(option));
            draft.Back();
            CollectionAssert.AreEquivalent(new[] { "standard", "character" }, draft.Payload.Ids);
            CardDraft.Choose(draft, option, "keep");
            CollectionAssert.AreEqual(new[] { "keep" }, draft.Payload.Ids);
            Assert.IsTrue(draft.Ready(option));
        }

        [Test]
        public void LoreRemovalRequiresAnExplicitRingExceptForWillAndBackRestoresItsIdentity()
        {
            var option = new ActionOption
            {
                Kind = CommandKind.DiscardLore,
                EligibleDice = { "will", "army" }, DiceWithoutRing = { "will" }, EligibleIds = { "nenya", "vilya" }
            };
            var draft = new TurnDraft();
            draft.Choose(option.Kind);
            draft.Change(p => p.DieId = "army");
            Assert.IsFalse(draft.Ready(option));
            draft.Change(p => p.RingId = "nenya");
            Assert.IsTrue(draft.Ready(option));
            draft.Change(p => p.RingId = "vilya");
            draft.Back();
            Assert.AreEqual("nenya", draft.Payload.RingId);
            draft.Change(p => { p.DieId = "will"; p.RingId = ""; });
            Assert.IsTrue(draft.Ready(option));
        }

        [Test]
        public void GatesRemovalNeedsOneOfTheServerEligibleDice()
        {
            var option = new ActionOption { Kind = CommandKind.DiscardGates, EligibleDice = { "character" } };
            var draft = new TurnDraft();
            draft.Choose(option.Kind);
            Assert.IsFalse(draft.Ready(option));
            draft.Change(p => p.DieId = "army");
            Assert.IsFalse(draft.Ready(option));
            draft.Change(p => p.DieId = "character");
            Assert.IsTrue(draft.Ready(option));
        }
    }
}
