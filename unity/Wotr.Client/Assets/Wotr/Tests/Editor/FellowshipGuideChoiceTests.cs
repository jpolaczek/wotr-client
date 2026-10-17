using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-229: the next Guide is chosen from the glowing portraits of the Fellowship panel, not from a decision window.
    public sealed class FellowshipGuideChoiceTests
    {
        [Test]
        public void AGuideDecisionLightsItsCandidatesAndOpensNoWindow()
        {
            var view = View();
            var roster = new FellowshipRoster();
            roster.Apply(view);
            roster.Context(view, new TurnDraft(), new ActionDieContext(), true);
            var strider = roster.Root.Q<Button>("fellowship-portrait-base.character.strider");
            var merry = roster.Root.Q<Button>("fellowship-portrait-base.character.merry");
            Assert.IsTrue(strider.ClassListContains(ChoiceGlow.ClassName));
            StringAssert.StartsWith("Click to make Strider the Guide", strider.tooltip);
            Assert.IsFalse(merry.ClassListContains(ChoiceGlow.ClassName), "Only the server's candidates glow.");
            StringAssert.DoesNotContain("the Guide\n", merry.tooltip);
            var tray = new DecisionTray(new VisualElement()) { ContextualFellowship = true };
            tray.Render(view, new TurnDraft(), false, false);
            Assert.IsFalse(tray.HasChoices, "The Guide decision opens no window.");
            Assert.AreEqual(FellowshipGuideText.ChooseHint, TurnConsoleState.For(view, new TurnDraft(), new ActionDieContext(), id => id).Guidance);
            roster.Context(view, new TurnDraft(), new ActionDieContext(), false);
            Assert.IsFalse(strider.ClassListContains(ChoiceGlow.ClassName));
        }

        private static PlayerView View() => new PlayerView
        {
            You = Side.FreePeoples,
            DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "guide", Kind = DecisionKind.Guide },
            Fellowship = new FellowshipView { Guide = "base.character.gandalf-grey" },
            OwnCompanions =
            {
                new CharacterView { Id = "base.character.strider", Level = 3 },
                new CharacterView { Id = "base.character.merry", Level = 1 }
            },
            Options = { new ActionOption { Kind = CommandKind.SelectGuide, Enabled = true, EligibleIds = { "base.character.strider" } } }
        };
    }
}
