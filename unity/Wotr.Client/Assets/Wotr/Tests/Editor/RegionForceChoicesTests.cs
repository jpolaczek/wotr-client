using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-251: Dreadful Spells' field army and garrison in one region are chosen on their own force sections; a region
    // with one eligible force is the region click itself.
    public sealed class RegionForceChoicesTests
    {
        [Test]
        public void TheFieldArmyAndTheGarrisonOfOneRegionAreSeparateTargets()
        {
            var view = new PlayerView
            {
                You = Side.Shadow,
                DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "dreadful", Kind = DecisionKind.EffectChoice, Presentation = RegionForceChoices.Presentation },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1,
                        EligibleIds = { "minas-tirith:field", "minas-tirith:garrison", "osgiliath:field" },
                        EffectChoices =
                        {
                            new EffectChoiceView { Id = "minas-tirith:field", TargetId = "minas-tirith", Title = "2 dice · 5+ · Field" },
                            new EffectChoiceView { Id = "minas-tirith:garrison", TargetId = "minas-tirith", Title = "2 dice · 5+ · Garrison" },
                            new EffectChoiceView { Id = "osgiliath:field", TargetId = "osgiliath", Title = "1 die · 5+ · Field" }
                        }
                    }
                }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            Assert.AreEqual("minas-tirith:garrison", RegionForceChoices.For(view, draft, "minas-tirith", true));
            Assert.AreEqual("minas-tirith:field", RegionForceChoices.For(view, draft, "minas-tirith", false));
            Assert.AreEqual("", RegionForceChoices.For(view, draft, "osgiliath", false), "One force is the region click.");
            Assert.AreEqual("", RegionForceChoices.For(view, draft, "osgiliath", true));
            view.DecisionOwner = Side.FreePeoples;
            Assert.AreEqual("", RegionForceChoices.For(view, draft, "minas-tirith", true), "Only the deciding player's panel chooses.");
        }
    }
}
