using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class FellowshipGuideTests
    {
        [Test]
        public void VoluntaryGuideNeedsAnExplicitModeButMandatoryGuideDoesNot()
        {
            var view = new PlayerView { You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Kind = DecisionKind.FellowshipPhase },
                Options = { new ActionOption { Kind = CommandKind.SelectGuide, EligibleIds = { "gimli", "legolas" } } } };
            var draft = new TurnDraft();
            var context = new ActionDieContext();
            Assert.IsNull(FellowshipActions.PortraitKind(view, context, "gimli", draft));
            Assert.IsFalse(FellowshipActions.Portrait(view, draft, context, "gimli"));
            Assert.IsTrue(FellowshipActions.Choose(view, draft, context, CommandKind.SelectGuide));
            Assert.IsFalse(draft.Ready(view.Options[0]));
            Assert.IsNull(FellowshipActions.PortraitKind(view, context, "merry", draft));
            Assert.IsTrue(FellowshipActions.Portrait(view, draft, context, "legolas"));
            Assert.AreEqual("legolas", draft.Payload.GuideId);
            Assert.IsEmpty(draft.Payload.DieId);
            draft.Clear();
            view.Decision.Kind = DecisionKind.Guide;
            Assert.IsTrue(FellowshipActions.Portrait(view, draft, context, "gimli"));
            Assert.AreEqual("gimli", draft.Payload.GuideId);
        }

        [Test]
        public void AShorterGroupInvalidatesItsRouteWithoutPickingAnotherGroupOrCost()
        {
            var view = new PlayerView { Decision = new DecisionView { Kind = DecisionKind.Action },
                Options = { new ActionOption { Kind = CommandKind.SeparateCompanions, Minimum = 1, Maximum = 2,
                    EligibleIds = { "gimli", "merry" }, EligibleDice = { "chosen" }, Travel =
                    {
                        new TravelOption { Id = "gimli", Origin = "hollin", Range = 2, Targets = { "hollin", "rivendell" } },
                        new TravelOption { Id = "merry", Origin = "hollin", Range = 1, Targets = { "hollin", "fords-of-bruinen" } }
                    } } } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.SeparateCompanions, "chosen");
            draft.Change(p => { p.Ids.AddRange(new[] { "gimli", "merry" }); p.Route.AddRange(new[] { "hollin", "fords-of-bruinen", "rivendell" }); });
            var option = view.Options[0];
            TravelDraft.ToggleCompanion(draft, view, option, "gimli");
            FellowshipDraft.Reconcile(draft, option);
            CollectionAssert.AreEqual(new[] { "merry" }, draft.Payload.Ids);
            Assert.IsEmpty(draft.Payload.Route);
            Assert.AreEqual("chosen", draft.Payload.DieId);
            Assert.IsTrue(TravelDraft.Step(draft, view, BoardPresentation.Load(), "hollin"));
            Assert.IsTrue(draft.Ready(option));
            view.Decision.Kind = DecisionKind.CompanionSeparation;
            FellowshipDraft.CancelRoute(draft, view);
            Assert.AreEqual(CommandKind.SeparateCompanions, draft.Kind);
            Assert.IsEmpty(draft.Payload.Ids);
            Assert.IsEmpty(draft.Payload.Route);
            Assert.IsEmpty(draft.Payload.DieId);
        }

        [Test]
        public void CancellingAForcedHobbitPlacementClearsTheRouteButKeepsTheRequiredGroup()
        {
            var view = new PlayerView { Decision = new DecisionView { Kind = DecisionKind.CompanionSeparation }, Options =
            {
                new ActionOption { Kind = CommandKind.SeparateCompanions, EligibleIds = { "merry", "pippin" }, Minimum = 2, Maximum = 2,
                    Travel = { new TravelOption { Id = "merry", Origin = "hollin", Range = 2, Targets = { "hollin" }, RequiredGroup = { "merry", "pippin" } } } }
            } };
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, CommandKind.SeparateCompanions);
            TravelDraft.Stay(draft, view.Options[0]);
            FellowshipDraft.CancelRoute(draft, view);
            Assert.AreEqual(CommandKind.SeparateCompanions, draft.Kind);
            CollectionAssert.AreEqual(new[] { "merry", "pippin" }, draft.Payload.Ids);
            Assert.IsEmpty(draft.Payload.Route);
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.IsTrue(TravelDraft.Step(draft, view, BoardPresentation.Load(), "hollin"));
            Assert.IsTrue(draft.Ready(view.Options[0]));
        }

        [Test]
        public void MixedSeparationDoesNotDescribePlacedCompanionsAsRemoved()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.SeparateCompanions);
            var preview = new ActionPreview { Placements = { new UnitPlacementChoice { UnitId = "base.character.gimli", RegionId = "hollin" } },
                RemovedIds = { "lords.character.smeagol" } };
            var text = FellowshipSummary.Describe(draft, new PlayerView(), preview, BoardPresentation.Load());
            StringAssert.Contains("Separate to Hollin", text);
            StringAssert.Contains("Leave play: Sméagol", text);
            StringAssert.DoesNotContain("Leave play: Gimli", text);
        }

        [Test]
        public void PublicGuideTextUsesOnlyRevealedVersionsAndDoesNotInferCouncil()
        {
            var fellowship = new FellowshipView { Guide = "base.character.strider" };
            Assert.AreEqual("", FellowshipGuideText.PublicVersion(fellowship, "gandalf"));
            fellowship.RevealedCompanionVersions.Add("base.character.strider", "lords.character.strider");
            Assert.AreEqual("lords.character.strider", FellowshipGuideText.PublicVersion(fellowship, fellowship.Guide));
            var publicIdentity = CharacterLibrary.Identity(fellowship.Guide);
            StringAssert.Contains("Fellowship Box", FellowshipGuideText.Ability(FellowshipGuideText.PublicVersion(fellowship, publicIdentity)));
            Assert.IsTrue(FellowshipGuideText.IsGuide(fellowship, publicIdentity));
            StringAssert.Contains("no figure", FellowshipGuideText.Ability("lords.character.smeagol"));
            StringAssert.Contains("standard numbered", FellowshipGuideText.Ability("base.character.gollum"));
        }
    }
}
