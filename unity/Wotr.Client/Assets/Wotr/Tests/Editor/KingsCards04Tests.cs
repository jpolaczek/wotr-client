using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class KingsCards04Tests
    {
        private static ActionOption Armies() => new ActionOption
        {
            Kind = CommandKind.ChooseEffect, Minimum = 1, Maximum = 1,
            EligibleIds = new List<string> { "minas-tirith:field", "minas-tirith:garrison", "osgiliath:field" },
            EffectChoices = new List<EffectChoiceView>
            {
                new EffectChoiceView { Id = "minas-tirith:field", TargetId = "minas-tirith" },
                new EffectChoiceView { Id = "minas-tirith:garrison", TargetId = "minas-tirith" },
                new EffectChoiceView { Id = "osgiliath:field", TargetId = "osgiliath" }
            }
        };

        [Test]
        public void RegionWithTwoArmiesRequiresExplicitArmyAndChangingRegionClearsOldChoice()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            var option = Armies();
            EffectChoiceDraft.ChooseRegion(draft, option, "osgiliath");
            Assert.AreEqual("osgiliath:field", draft.Payload.Ids.Single());
            EffectChoiceDraft.ChooseRegion(draft, option, "minas-tirith");
            Assert.IsEmpty(draft.Payload.Ids);
            Assert.IsFalse(draft.Ready(option));
            Assert.AreEqual("minas-tirith", draft.Payload.RegionId);
            EffectChoiceDraft.Choose(draft, option, "minas-tirith:garrison");
            Assert.IsTrue(draft.Ready(option));
            Assert.AreEqual("minas-tirith:garrison", draft.Payload.Ids.Single());
            EffectChoiceDraft.ChooseRegion(draft, option, "osgiliath");
            Assert.AreEqual("osgiliath", draft.Payload.RegionId);
            Assert.AreEqual("osgiliath:field", draft.Payload.Ids.Single());
            draft.Back();
            Assert.AreEqual("minas-tirith", draft.Payload.RegionId);
            Assert.AreEqual("minas-tirith:garrison", draft.Payload.Ids.Single());
        }

        [Test]
        public void ReconnectPreservesSelectedArmyAndNeverSubstitutesAChangedTarget()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            var option = Armies();
            EffectChoiceDraft.ChooseRegion(draft, option, "minas-tirith");
            EffectChoiceDraft.Choose(draft, option, "minas-tirith:garrison");
            EffectChoiceDraft.Reconcile(draft, option);
            Assert.AreEqual("minas-tirith:garrison", draft.Payload.Ids.Single());
            option.EligibleIds.Remove("minas-tirith:garrison");
            option.EffectChoices.RemoveAll(c => c.Id == "minas-tirith:garrison");
            EffectChoiceDraft.Reconcile(draft, option);
            Assert.IsEmpty(draft.Payload.Ids);
            Assert.IsFalse(draft.Ready(option));
        }

        [Test]
        public void WorkOfTheEnemyShowsBothPrintedThresholdsAndCombatInitiative()
        {
            var card = CardLibrary.Cards["kings.sh.strategy.26"];
            Assert.IsTrue(card.Event.Contains("5+"));
            Assert.IsTrue(card.Event.Contains("4+"));
            Assert.AreEqual("His Power Waxes", card.CombatTitle);
            Assert.AreEqual(3, card.Initiative.Single());
            Assert.IsNotEmpty(card.Combat);
        }
    }
}
