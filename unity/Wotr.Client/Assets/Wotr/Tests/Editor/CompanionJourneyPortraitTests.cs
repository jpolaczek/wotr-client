using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-284: a card's companion journey (Book of Mazarbul and the like) shows the companions to move as their painted
    // portraits with their names, not as name buttons; a portrait glows while its click can still choose it.
    public sealed class CompanionJourneyPortraitTests
    {
        [Test]
        public void TheJourneysCompanionsArePortraitsThatGlowUntilChosen()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                Decision = new DecisionView { Kind = DecisionKind.CompanionSeparation },
                Fellowship = new FellowshipView { RegionId = "rivendell", Guide = "base.character.gandalf-grey" }
            };
            var option = new ActionOption
            {
                Kind = CommandKind.MoveCompanions, Enabled = true, MaximumGroups = 0, AllowEmptySelection = true,
                EligibleIds = { "base.character.merry", "base.character.strider" }
            };
            foreach (var id in option.EligibleIds)
            {
                option.Travel.Add(new TravelOption { Id = id, Origin = "moria", Range = 3, Targets = { "moria", "lorien" } });
                view.Characters.Add(new CharacterView { Id = id, DefinitionId = id, Side = Side.FreePeoples, Location = "moria" });
            }
            view.Options.Add(option);
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            var root = new VisualElement();
            var choices = new FellowshipChoices(root, view, draft, change => change(), BoardPresentation.Load(), contextual: true);
            choices.Refresh();

            foreach (var id in option.EligibleIds)
            {
                var portrait = root.Q<Button>("companion-" + id);
                Assert.IsNotNull(portrait.Q<Image>("figure-art"), id + " shows its portrait.");
                Assert.AreEqual(CharacterLibrary.ShortName(view, id), portrait.Q<Label>("portrait-name").text);
                Assert.AreEqual("", portrait.text, "No name button.");
                Assert.IsTrue(portrait.ClassListContains(ChoiceGlow.ClassName), id + " glows while it can be chosen.");
            }
            TravelDraft.ToggleCompanion(draft, view, option, "base.character.merry");
            choices.Refresh();
            Assert.IsFalse(root.Q<Button>("companion-base.character.merry").ClassListContains(ChoiceGlow.ClassName),
                "A chosen companion shows its selection, not the invitation.");
        }
    }
}
