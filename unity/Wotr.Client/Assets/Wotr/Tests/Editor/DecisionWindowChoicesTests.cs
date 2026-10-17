using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-164: the decision window opens only for choices without an object of their own; a step whose choice is a
    // region, a die or a deck keeps its hint and Back in the turn console.
    public sealed class DecisionWindowChoicesTests
    {
        [Test]
        public void ARegionChoiceOnTheMapOffersNothingInTheWindow()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "region", Kind = DecisionKind.EffectChoice, Presentation = "Region" },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1,
                        EligibleIds = { "to-edoras", "to-rohan" },
                        EffectChoices =
                        {
                            new EffectChoiceView { Id = "to-edoras", TargetId = "edoras", Title = "Edoras" },
                            new EffectChoiceView { Id = "to-rohan", TargetId = "fords-of-isen", Title = "Fords of Isen" }
                        }
                    }
                }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            var tray = new DecisionTray(new VisualElement());
            tray.Render(view, draft, false, false);
            Assert.IsFalse(tray.HasChoices, "The highlighted regions are the choices; Back alone opens no window.");
        }

        // UI-211: dice clicked in the pool with a Confirm (The Lidless Eye) keep the window closed; ✓ is in the console.
        [Test]
        public void ACardChoiceOnTheBoardWithAConfirmOffersNothingInTheWindow()
        {
            var view = new PlayerView
            {
                You = Side.Shadow,
                DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "lidless", Kind = DecisionKind.EffectChoice, Presentation = "ActionDie", SourceId = "base.sh.character.18" },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 0, Maximum = 3, EligibleIds = { "sh-1", "sh-2" },
                        EffectChoices =
                        {
                            new EffectChoiceView { Id = "sh-1", TargetId = "sh-1", Title = "Character die" },
                            new EffectChoiceView { Id = "sh-2", TargetId = "sh-2", Title = "Army die" }
                        }
                    }
                }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            var root = new VisualElement();
            var tray = new DecisionTray(root);
            tray.Render(view, draft, false, false);
            Assert.IsNotNull(root.Q<Button>("confirm-decision"), "The tray keeps its Confirm for its own host.");
            Assert.IsFalse(tray.HasChoices, "A card's Confirm alone opens no window: the console's ✓ confirms it.");
        }

        [Test]
        public void AReactionGoesToTheConsoleAndARecoveryIsAChoiceOfTheWindow()
        {
            var choice = new EffectChoiceView { Id = "card.reaction", SourceId = "base.fp.character.05", Title = "Use" };
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "reaction", Kind = DecisionKind.Reaction },
                Options = { new ActionOption { Kind = CommandKind.RespondEffect, Enabled = true, EligibleIds = { choice.Id }, EffectChoices = { choice } } }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.RespondEffect);
            var tray = new DecisionTray(new VisualElement());
            tray.Render(view, draft, false, false);
            Assert.IsFalse(tray.HasChoices, "UI-214: a reaction outside the Hunt and battles answers in the console.");
            tray.Render(view, draft, false, true);
            Assert.IsTrue(tray.HasChoices, "Check result is a choice of the window.");
        }
    }
}
