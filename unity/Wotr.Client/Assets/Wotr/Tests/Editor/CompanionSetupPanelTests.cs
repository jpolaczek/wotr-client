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
            var panel = new CompanionSetupPanel { ComparedFigure = Gimli };
            var root = new VisualElement();
            var edits = 0;
            var choices = new CompanionSetupChoices(root, option, draft, action => { edits++; action(); }, panel);
            choices.Refresh();
            Assert.AreEqual(0, edits);
            Assert.IsEmpty(draft.Payload.Ids);
            Assert.IsEmpty(draft.Payload.GuideId);
            Assert.AreEqual("Choose a version for Gandalf the Grey", CompanionSetupPresentation.Missing(draft, option));
            Assert.AreEqual("base.character.gimli", CouncilDraft.Version(draft.Payload, option.CompanionVersions[1]));
            Assert.IsNotNull(root.Q<Button>("version-base.character.gimli"));
            Assert.IsNull(root.Q<Button>("version-" + Grey));
            Assert.AreEqual(2, root.Query<Button>(className: "setup-version-card").ToList().Count);
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

        [Test]
        public void CloseSurvivesSameDecisionRefreshAndNextDecisionReleasesTheOnlyTray()
        {
            var view = Setup();
            var panel = new CompanionSetupPanel();
            var tray = new VisualElement();
            var dock = new VisualElement();
            dock.Add(tray);
            panel.Attach(tray, dock);
            panel.Context(view);
            Assert.IsTrue(panel.IsOpen);
            Assert.AreSame(panel.DecisionHost, tray.parent);
            panel.ComparedFigure = Gimli;
            panel.Close();
            view.Revision++;
            panel.Context(view);
            Assert.IsFalse(panel.IsOpen);
            Assert.AreEqual(Gimli, panel.ComparedFigure);
            panel.Open();
            Assert.IsTrue(panel.IsOpen);
            view.Decision.Kind = DecisionKind.FellowshipPhase;
            panel.Context(view);
            Assert.IsFalse(panel.IsOpen);
            Assert.IsFalse(panel.Active);
            Assert.AreSame(dock, tray.parent);
            Assert.AreEqual(DisplayStyle.None, panel.Tab.style.display.value);
        }

        [Test]
        public void OtherSideBaseAndFinishedGameNeverOwnThePrivatePanel()
        {
            var view = Setup();
            var panel = new CompanionSetupPanel();
            view.You = Side.Shadow;
            panel.Context(view);
            Assert.IsFalse(panel.Active);
            Assert.IsEmpty(panel.ComparedFigure);
            view.You = Side.FreePeoples;
            view.Decision = null;
            panel.Context(view);
            Assert.IsFalse(panel.Active);
            view.Decision = Setup().Decision;
            view.Winner = Side.Shadow;
            panel.Context(view);
            Assert.IsFalse(panel.Active);
        }

        [Test]
        public void FateComparisonKeepsMovementDistinctAndSharesReviewedAbilityText()
        {
            var view = Setup();
            var option = view.Options.Single();
            option.CompanionVersions[1].BaseId = "fate.character.gimli";
            option.CompanionVersions[1].AlternateId = "fate.character.gimli-dwarven-lord";
            var root = new VisualElement();
            var panel = new CompanionSetupPanel { ComparedFigure = Gimli };
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            _ = new CompanionSetupChoices(root, option, draft, action => action(), panel);
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
