using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class CompanionSetupPanelTests
    {
        private const string Grey = "base.character.gandalf-grey";
        private const string Gimli = "base.character.gimli";

        [Test]
        public void OpeningAndComparingDoNotChooseRequiredVersionOrGuide()
        {
            var view = Setup();
            var option = view.Options.Single();
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            var versions = new VersionComparison();
            versions.Select(Gimli);
            var root = new VisualElement();
            var edits = 0;
            var choices = new CompanionSetupChoices(root, option, draft, action => { edits++; action(); }, versions);
            choices.Refresh();
            Assert.AreEqual(0, edits);
            Assert.IsEmpty(draft.Payload.Ids);
            Assert.IsEmpty(draft.Payload.GuideId);
            Assert.AreEqual("Choose a version for Gandalf the Grey", CompanionSetupPresentation.Missing(draft, option));
            Assert.AreEqual("base.character.gimli", CouncilDraft.Version(draft.Payload, option.CompanionVersions[1]));
            Assert.IsNotNull(root.Q<Button>("version-base.character.gimli"));
            Assert.IsNull(root.Q<Button>("version-" + Grey));
            Assert.AreEqual(2, root.Query<Button>(className: "setup-version-card").ToList().Count);
            // UI-345: the version not chosen glows as a choice; the chosen one is marked selected instead.
            var chosen = root.Q<Button>("version-base.character.gimli");
            var other = root.Query<Button>(className: "setup-version-card").ToList().Single(card => card != chosen);
            Assert.IsTrue(other.ClassListContains(ChoiceGlow.ClassName));
            Assert.IsFalse(chosen.ClassListContains(ChoiceGlow.ClassName));
            Assert.IsTrue(chosen.ClassListContains("selected"));
        }

        [Test]
        public void RequiredChoiceAndExplicitGuideBecomeReadyTogether()
        {
            var view = Setup();
            var option = view.Options.Single();
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            CouncilDraft.Version(draft, option.CompanionVersions[0], Grey);
            Assert.IsFalse(draft.Ready(option));
            Assert.AreEqual("Choose the starting Guide", CompanionSetupPresentation.Missing(draft, option));
            draft.Change(p => p.GuideId = Grey);
            Assert.IsTrue(draft.Ready(option));
            Assert.IsEmpty(CompanionSetupPresentation.Missing(draft, option));
            Assert.AreEqual(Grey, CompanionSetupPresentation.Definition(draft, option, Grey));
            Assert.AreEqual(Gimli, CompanionSetupPresentation.Definition(draft, option, Gimli));
        }

        // UI-270: only the Free Peoples' own, unfinished setup decision is private setup.
        [Test]
        public void OtherSideBaseAndFinishedGameNeverOwnThePrivateSetup()
        {
            var view = Setup();
            Assert.IsTrue(CompanionSetupPresentation.Setup(view));
            view.You = Side.Shadow;
            Assert.IsFalse(CompanionSetupPresentation.Setup(view));
            view.You = Side.FreePeoples;
            view.Decision = null;
            Assert.IsFalse(CompanionSetupPresentation.Setup(view));
            view.Decision = Setup().Decision;
            view.Winner = Side.Shadow;
            Assert.IsFalse(CompanionSetupPresentation.Setup(view));
        }

        [Test]
        public void FateComparisonKeepsMovementDistinctAndSharesReviewedAbilityText()
        {
            var view = Setup();
            var option = view.Options.Single();
            option.CompanionVersions[1].BaseId = "fate.character.gimli";
            option.CompanionVersions[1].AlternateId = "fate.character.gimli-dwarven-lord";
            var root = new VisualElement();
            var versions = new VersionComparison();
            versions.Select(Gimli);
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            _ = new CompanionSetupChoices(root, option, draft, action => action(), versions);
            var card = root.Q<Button>("version-fate.character.gimli-dwarven-lord");
            var stats = card.Q<Label>(className: "setup-version-statistics").text;
            StringAssert.Contains("Level 1", stats);
            StringAssert.Contains("Move 2", stats);
            foreach (var ability in CharacterAbilityLibrary.For("fate.character.gimli-dwarven-lord"))
            {
                var label = card.Q<Label>("setup-ability-" + ability.Id);
                Assert.IsTrue(label.focusable);
                StringAssert.Contains(ability.Text, label.tooltip);
                StringAssert.Contains(ability.Source, label.tooltip);
            }
        }

        // UI-269: the versions on the Fellowship panel's portraits — a badge on each companion with two versions, the
        // required one glowing until chosen, the compared one selected, a crown on each starting Guide candidate.
        [Test]
        public void ThePortraitsCarryTheVersionBadgesAndTheCandidatesTheirCrowns()
        {
            var view = Setup();
            view.OwnCompanions.Add(new CharacterView { Id = Grey, DefinitionId = Grey, Side = Side.FreePeoples });
            view.OwnCompanions.Add(new CharacterView { Id = Gimli, DefinitionId = Gimli, Side = Side.FreePeoples });
            view.OwnCompanions.Add(new CharacterView { Id = "base.character.merry", DefinitionId = "base.character.merry", Side = Side.FreePeoples });
            var option = view.Options.Single();
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            var roster = new FellowshipRoster();
            roster.Apply(view);
            roster.Context(view, draft, new ActionDieContext(), true, Gimli);
            var grey = roster.Root.Q<Button>("fellowship-portrait-" + Grey);
            var gimli = roster.Root.Q<Button>("fellowship-portrait-" + Gimli);
            var merry = roster.Root.Q<Button>("fellowship-portrait-base.character.merry");
            Assert.AreEqual(DisplayStyle.Flex, grey.Q("versions-badge").style.display.value);
            Assert.AreEqual(DisplayStyle.None, merry.Q("versions-badge").style.display.value);
            Assert.IsTrue(grey.ClassListContains(ChoiceGlow.ClassName), "The required version glows until chosen.");
            Assert.IsFalse(gimli.ClassListContains(ChoiceGlow.ClassName));
            Assert.IsTrue(gimli.ClassListContains("selected"), "The compared companion is selected.");
            StringAssert.StartsWith("Click to compare", grey.tooltip);
            var crown = roster.Root.Q<Button>("starting-guide-" + Grey);
            Assert.AreEqual(DisplayStyle.Flex, crown.style.display.value);
            Assert.AreEqual(DisplayStyle.None, roster.Root.Q<Button>("starting-guide-base.character.merry").style.display.value);
            StringAssert.StartsWith("Click to make", crown.tooltip);
            CouncilDraft.Version(draft, option.CompanionVersions[0], "lords.character.gandalf-grey");
            draft.Change(p => p.GuideId = Grey);
            roster.Context(view, draft, new ActionDieContext(), true, Gimli);
            Assert.IsFalse(grey.ClassListContains(ChoiceGlow.ClassName));
            Assert.IsTrue(crown.ClassListContains("selected"));
            StringAssert.StartsWith("Starting Guide", crown.tooltip);
            StringAssert.Contains("Alternate", grey.tooltip);
            view.DecisionOwner = Side.Shadow;
            roster.Context(view, draft, new ActionDieContext(), true);
            Assert.AreEqual(DisplayStyle.None, grey.Q("versions-badge").style.display.value, "Nothing private shows without the decision.");
            Assert.AreEqual(DisplayStyle.None, crown.style.display.value);
        }

        private static PlayerView Setup() => new PlayerView
        {
            MatchId = "companion-setup", You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "versions", Kind = DecisionKind.CouncilVersions },
            Options =
            {
                new ActionOption
                {
                    Kind = CommandKind.ChooseCompanionVersions,
                    CompanionVersions =
                    {
                        new CompanionVersionOption { FigureId = Grey, BaseId = Grey, AlternateId = "lords.character.gandalf-grey", Required = true },
                        new CompanionVersionOption { FigureId = Gimli, BaseId = Gimli, AlternateId = "lords.character.gimli" }
                    },
                    EligibleIds = { Grey, "lords.character.gandalf-grey", Gimli, "lords.character.gimli" },
                    Targets = { Grey, Gimli }
                }
            }
        };
    }
}
