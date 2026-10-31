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

        // UI-431 (owner's rule, UI-430): the force sections are the only click for the two forces. The region with two of them
        // opens its panel on its click and waits, marked, for the force; each section's tooltip says what it chooses and why.
        [Test]
        public void TheRegionWaitsForItsForceAndEachSectionSaysWhy()
        {
            var view = View();
            view.Decision.SourceId = "kings.sh.strategy.26";
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            Assert.IsTrue(RegionForceChoices.HasAlternatives(view, draft, "minas-tirith"));
            Assert.IsFalse(RegionForceChoices.HasAlternatives(view, draft, "osgiliath"), "One force is the region click.");
            Assert.AreEqual("", RegionForceChoices.Waiting(view, draft), "Two regions: the map's click comes first.");
            Assert.AreEqual("", RegionForceChoices.Hint(view, draft));
            EffectChoiceDraft.ChooseRegion(draft, view.Options[0], "minas-tirith");
            Assert.AreEqual("minas-tirith", RegionForceChoices.Waiting(view, draft));
            Assert.AreEqual("Click the glowing field army or garrison in the region's panel", RegionForceChoices.Hint(view, draft));
            Assert.AreEqual("2 dice · 5+ · Garrison · Click to choose the garrison because of " + CardLibrary.Title("kings.sh.strategy.26"),
                RegionForceChoices.Tooltip(view, draft, "minas-tirith:garrison"));
            StringAssert.Contains("Click to choose the field army because of", RegionForceChoices.Tooltip(view, draft, "minas-tirith:field"));
            EffectChoiceDraft.Choose(draft, view.Options[0], "minas-tirith:garrison", "minas-tirith");
            Assert.AreEqual("", RegionForceChoices.Waiting(view, draft), "A chosen force ends the wait.");
            Assert.AreEqual("minas-tirith", draft.Payload.RegionId, "The force's click keeps the region, as the region's click and the old button did.");
            // A choice with one region opens with that region waiting.
            view.Options[0].EligibleIds.Remove("osgiliath:field");
            view.Options[0].EffectChoices.RemoveAll(c => c.Id == "osgiliath:field");
            var single = new TurnDraft();
            single.Choose(CommandKind.ChooseEffect);
            Assert.AreEqual("minas-tirith", RegionForceChoices.Waiting(view, single));
            Assert.AreEqual("Click the lit region, then its glowing field army or garrison", RegionForceChoices.Hint(view, single));
        }

        private static PlayerView View() => new PlayerView
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
    }
}
