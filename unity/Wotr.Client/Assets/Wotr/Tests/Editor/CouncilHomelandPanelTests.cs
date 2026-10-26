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

        // UI-270: both private setup steps are held by the Fellowship panel; the Shadow's token choice is made in its pool.
        [Test]
        public void TheSetupStepsAreHeldByTheFellowshipPanelAndTheTokenByThePool()
        {
            var view = View();
            var draft = Draft(view);
            Assert.AreEqual(DecisionHost.Fellowship, DecisionRoutes.Of(view));
            Assert.IsTrue(FellowshipActions.UsesPanel(view, draft));
            Assert.IsTrue(CompanionSetupPresentation.Homelands(view));
            view.Decision.Kind = DecisionKind.CouncilVersions;
            Assert.AreEqual(DecisionHost.Fellowship, DecisionRoutes.Of(view));
            Assert.IsTrue(FellowshipActions.UsesPanel(view, draft));
            view.Decision.Kind = DecisionKind.CouncilToken;
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            Assert.AreEqual(DecisionHost.Dice, DecisionRoutes.Of(view));
            Assert.IsFalse(FellowshipActions.UsesPanel(view, new TurnDraft()));
            Assert.IsTrue(ResourcePresentation.PoolDecision(view));
        }

        [Test]
        public void NoCandidatesStillRequiresOneExplicitEmptyConfirmation()
        {
            var view = View();
            var option = view.Options.Single();
            option.CharacterActions.Clear();
            var draft = Draft(view);
            var root = new VisualElement();
            var choices = new CouncilHomelandChoices(root, view, draft);
            choices.Refresh();
            Assert.IsTrue(CouncilHomelandPresentation.Owns(view));
            Assert.IsTrue(draft.Ready(option));
            Assert.IsEmpty(draft.Payload.Placements);
            Assert.IsEmpty(draft.Payload.CharacterId);
            // UI-347 (D14): no Keep button; the hint says what the ✓ does.
            Assert.IsNull(root.Q<Button>("homeland-stay"));
            Assert.AreEqual("All companions stay with the Fellowship · ✓ confirms.", root.Q<Label>("homeland-hint").text);
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
            var choices = new CouncilHomelandChoices(root, view, draft);
            for (var i = 0; i < count; i++)
                draft.Payload.Placements.Add(new UnitPlacementChoice { UnitId = "companion-" + i, RegionId = "the-shire" });
            choices.Refresh();
            Assert.AreEqual(expected, root.Q<Label>("homeland-token-effect").text);
            Assert.IsTrue(root.Query<Label>(className: "homeland-token").ToList().All(token => token.ClassListContains(style)));
            Assert.IsEmpty(view.CouncilTokens, "Preview never grants a resource.");
        }

        // UI-270: the homelands on the Fellowship panel's portraits: each companion who may leave glows and its tooltip
        // names its homelands and its version's nation; a placed one shows where it starts; destinations stay on the map.
        [Test]
        public void ThePortraitsOfferTheHomelandsAndShowWhereACompanionStarts()
        {
            var view = View();
            var draft = Draft(view);
            var roster = new FellowshipRoster { RegionName = id => id == "iron-hills" ? "Iron Hills" : id };
            roster.Apply(view);
            roster.Context(view, draft, new ActionDieContext(), true);
            var gimli = roster.Root.Q<Button>("fellowship-portrait-" + Gimli);
            var legolas = roster.Root.Q<Button>("fellowship-portrait-" + Legolas);
            Assert.IsTrue(gimli.ClassListContains("legal-target"));
            Assert.IsTrue(gimli.ClassListContains(ChoiceGlow.ClassName));
            StringAssert.StartsWith("Click, then a lit homeland", gimli.tooltip);
            StringAssert.Contains("does not activate", gimli.tooltip);
            StringAssert.Contains("activates the Elves", legolas.tooltip);
            Assert.AreEqual(DraftObject.CouncilPortrait, ((DraftToken)gimli.userData).Object, "A portrait may be dragged to its homeland.");
            draft.Change(p => p.CharacterId = Gimli);
            Assert.IsTrue(CouncilDraft.Region(draft, view.Options.Single(), "iron-hills"));
            roster.Context(view, draft, new ActionDieContext(), true);
            Assert.IsTrue(gimli.ClassListContains("selected"));
            Assert.AreEqual("→ Iron Hills", gimli.Q<Label>("homeland-location").text);
            Assert.AreEqual(DisplayStyle.None, legolas.Q<Label>("homeland-location").style.display.value);
            Assert.IsNull(roster.Root.Q<Button>("homeland-" + Gimli + "-iron-hills"), "Destinations belong to the map.");
            view.Decision.Kind = DecisionKind.FellowshipPhase;
            roster.Context(view, draft, new ActionDieContext(), true);
            Assert.AreEqual(DisplayStyle.None, gimli.Q<Label>("homeland-location").style.display.value);
            Assert.AreEqual(DraftObject.FellowshipPortrait, ((DraftToken)gimli.userData).Object);
        }

        [Test]
        public void OpponentAndFinishedGameCannotRenderPrivateCandidates()
        {
            var view = View();
            view.You = Side.Shadow;
            var root = new VisualElement();
            var choices = new CouncilHomelandChoices(root, view, new TurnDraft());
            choices.Refresh();
            Assert.IsFalse(CouncilHomelandPresentation.Owns(view));
            Assert.AreEqual(0, root.childCount);
            view.You = Side.FreePeoples;
            view.Winner = Side.Shadow;
            Assert.IsFalse(CompanionSetupPresentation.Setup(view));
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
