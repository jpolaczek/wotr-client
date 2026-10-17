using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class CouncilHomelandPanelTests
    {
        private const string Gimli = "base.character.gimli";
        private const string Legolas = "base.character.legolas";

        [Test]
        public void HomelandIsANewPrivateContextAndTokenReleasesItsOnlyTray()
        {
            var view = View();
            var panel = new CompanionSetupPanel();
            var tray = new VisualElement();
            var dock = new VisualElement();
            dock.Add(tray);
            panel.Attach(tray, dock);
            view.Decision.Id = "versions";
            view.Decision.Kind = DecisionKind.CouncilVersions;
            panel.Context(view);
            panel.Close();
            view.Decision.Id = "homelands";
            view.Decision.Kind = DecisionKind.CouncilHomelands;
            panel.Context(view);
            Assert.IsTrue(panel.IsOpen);
            Assert.AreSame(panel.DecisionHost, tray.parent);
            Assert.AreEqual("Homelands", panel.Tab.text);
            panel.Close();
            view.Revision++;
            panel.Context(view);
            Assert.IsFalse(panel.IsOpen);
            view.Decision.Kind = DecisionKind.CouncilToken;
            view.DecisionOwner = Side.Shadow;
            panel.Context(view);
            Assert.IsFalse(panel.Active);
            Assert.AreSame(dock, tray.parent);
        }

        [Test]
        public void NoCandidatesStillRequiresOneExplicitEmptyConfirmation()
        {
            var view = View();
            var option = view.Options.Single();
            option.CharacterActions.Clear();
            var draft = Draft(view);
            var root = new VisualElement();
            var edits = 0;
            var choices = new CouncilHomelandChoices(root, view, option, draft, action => { edits++; action(); }, BoardPresentation.Load());
            choices.Refresh();
            Assert.IsTrue(CouncilHomelandPresentation.Owns(view));
            Assert.IsTrue(draft.Ready(option));
            Assert.IsEmpty(draft.Payload.Placements);
            Assert.IsEmpty(draft.Payload.CharacterId);
            Assert.AreEqual(0, edits);
            Assert.IsFalse(root.Q<Button>("homeland-stay").enabledSelf);
            Assert.AreEqual("No tokens", root.Q<Label>("homeland-token-effect").text);
            Assert.IsNotNull(root.Q<VisualElement>("council-homeland-choices"));
        }

        [Test]
        public void ClickAndDropCanReplaceOnlyTheSelectedPlacementAndStayUndoesIt()
        {
            var view = View();
            var option = view.Options.Single();
            var draft = Draft(view);
            draft.Change(payload => payload.CharacterId = Gimli);
            Assert.IsTrue(CouncilDraft.Region(draft, option, "iron-hills"));
            Assert.IsTrue(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.CouncilPortrait, Legolas),
                new DraftToken(DraftObject.Region, "woodland-realm")));
            Assert.IsTrue(CouncilDraft.Region(draft, option, "ered-luin"));
            Assert.IsFalse(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.CouncilPortrait, Gimli),
                new DraftToken(DraftObject.Region, "erebor")));
            Assert.AreEqual("ered-luin", draft.Payload.Placements.Single(p => p.UnitId == Gimli).RegionId);
            Assert.AreEqual(2, draft.Payload.Placements.Count);
            CouncilDraft.Homeland(draft, Gimli, "");
            Assert.AreEqual(Legolas, draft.Payload.Placements.Single().UnitId);
            Assert.IsTrue(draft.Ready(option));
        }

        [TestCase(0, "No tokens", "homeland-token-none")]
        [TestCase(1, "Shadow chooses one", "homeland-token-choice")]
        [TestCase(2, "Shadow receives both", "homeland-token-granted")]
        [TestCase(5, "Shadow receives both", "homeland-token-granted")]
        public void TokenPreviewDistinguishesNoneChoiceAndAutomaticPair(int count, string expected, string style)
        {
            var view = View();
            var draft = Draft(view);
            var root = new VisualElement();
            var choices = new CouncilHomelandChoices(root, view, view.Options.Single(), draft, action => action(), BoardPresentation.Load());
            for (var i = 0; i < count; i++)
                draft.Payload.Placements.Add(new UnitPlacementChoice { UnitId = "companion-" + i, RegionId = "the-shire" });
            choices.Refresh();
            Assert.AreEqual(expected, root.Q<Label>("homeland-token-effect").text);
            Assert.IsTrue(root.Query<Label>(className: "homeland-token").ToList().All(token => token.ClassListContains(style)));
            Assert.IsEmpty(view.CouncilTokens, "Preview never grants a resource.");
        }

        [Test]
        public void PortraitUsesOwnVersionAndDoesNotInventANationIcon()
        {
            var view = View();
            var root = new VisualElement();
            var choices = new CouncilHomelandChoices(root, view, view.Options.Single(), Draft(view), action => action(), BoardPresentation.Load());
            choices.Refresh();
            var gimli = root.Q<Button>("homeland-character-" + Gimli);
            Assert.AreEqual("—", gimli.Q<Label>("homeland-nation").text);
            StringAssert.Contains("does not activate", gimli.tooltip);
            Assert.AreEqual("◇ Elves", root.Q<Button>("homeland-character-" + Legolas).Q<Label>("homeland-nation").text);
            Assert.IsNull(root.Q<Button>("homeland-" + Gimli + "-iron-hills"), "Destinations belong to the map.");
        }

        [Test]
        public void OpponentAndFinishedGameCannotRenderPrivateCandidates()
        {
            var view = View();
            view.You = Side.Shadow;
            var root = new VisualElement();
            var choices = new CouncilHomelandChoices(root, view, view.Options.Single(), new TurnDraft(), action => action(), BoardPresentation.Load());
            choices.Refresh();
            Assert.IsFalse(CouncilHomelandPresentation.Owns(view));
            Assert.AreEqual(0, root.childCount);
            view.You = Side.FreePeoples;
            view.Winner = Side.Shadow;
            Assert.IsFalse(CompanionSetupPresentation.Owns(view));
        }

        private static TurnDraft Draft(PlayerView view)
        {
            var result = new TurnDraft();
            DraftGesture.Choose(result, view, CommandKind.ChooseCouncilHomelands);
            return result;
        }

        private static PlayerView View() => new PlayerView
        {
            MatchId = "homelands", You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "homelands", Kind = DecisionKind.CouncilHomelands },
            OwnCompanions =
            {
                new CharacterView { Id = Gimli, DefinitionId = "fate.character.gimli-dwarven-lord", NationIcon = "" },
                new CharacterView { Id = Legolas, DefinitionId = "lords.character.legolas", NationIcon = "elves" }
            },
            Options =
            {
                new ActionOption { Kind = CommandKind.ChooseCouncilHomelands, AllowEmptySelection = true,
                    CharacterActions =
                    {
                        new CharacterActionOption { CharacterId = Gimli, Title = "Gimli", Targets = { "iron-hills", "ered-luin" } },
                        new CharacterActionOption { CharacterId = Legolas, Title = "Legolas", Targets = { "woodland-realm" } }
                    }
                }
            }
        };
    }
}
