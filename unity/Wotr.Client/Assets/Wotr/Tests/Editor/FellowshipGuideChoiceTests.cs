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

        // UI-249: in the Fellowship phase the same portraits choose the Guide directly; the tooltip says first that the
        // click ends the phase, and the panel has no Change guide button.
        [Test]
        public void TheFellowshipPhaseChoosesTheGuideFromItsPortraitWithoutAMode()
        {
            var view = View();
            view.Decision.Kind = DecisionKind.FellowshipPhase;
            view.Options.Add(new ActionOption { Kind = CommandKind.ContinueFellowship, Enabled = true });
            Assert.AreEqual(CommandKind.SelectGuide, FellowshipActions.PortraitKind(view, new ActionDieContext(), "base.character.strider"));
            Assert.IsNull(FellowshipActions.PortraitKind(view, new ActionDieContext(), "base.character.merry"));
            var roster = new FellowshipRoster();
            roster.Apply(view);
            roster.Context(view, new TurnDraft(), new ActionDieContext(), true);
            var strider = roster.Root.Q<Button>("fellowship-portrait-base.character.strider");
            Assert.IsTrue(strider.ClassListContains(ChoiceGlow.ClassName));
            StringAssert.StartsWith("Click to make Strider the Guide · This ends the Fellowship phase", strider.tooltip);
            var draft = new TurnDraft();
            Assert.IsTrue(FellowshipActions.Portrait(view, draft, new ActionDieContext(), "base.character.strider"));
            Assert.AreEqual(CommandKind.SelectGuide, draft.Kind);
            Assert.AreEqual("base.character.strider", draft.Payload.GuideId);
            Assert.IsTrue(FellowshipActions.Immediate(draft.Kind), "One click sends the choice.");
            var controls = new FellowshipControls();
            controls.Refresh(view, new TurnDraft(), new ActionDieContext(), true, false);
            Assert.IsNull(controls.Root.Q<Button>("fellowship-change-guide"));
            Assert.AreEqual(DisplayStyle.Flex, controls.Root.Q<Button>("fellowship-continue").style.display.value, "Finish phase keeps the Guide.");
        }

        // UI-249: The Breaking of the Fellowship chooses its Companions on the Hunt panel's roster; the group's limit holds,
        // and the tooltip says placement outside Mordor or removal on the Mordor track.
        [Test]
        public void TheBreakingOfTheFellowshipChoosesItsCompanionsOnTheRoster()
        {
            var view = View();
            view.Hunt = new HuntView();
            view.Decision = new DecisionView { Id = "breaking", Kind = DecisionKind.EffectChoice, Presentation = FellowshipActions.CompanionsPresentation };
            view.Options.Clear();
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1 };
            foreach (var id in new[] { "base.character.strider", "base.character.merry" })
            {
                option.EligibleIds.Add(id);
                option.EffectChoices.Add(new EffectChoiceView { Id = id, SourceId = id, SelectionGroup = "Companions", TargetId = "rivendell" });
            }
            view.Options.Add(option);
            var context = new ActionDieContext();
            Assert.AreEqual(CommandKind.ChooseEffect, FellowshipActions.PortraitKind(view, context, "base.character.merry"));
            var roster = new FellowshipRoster();
            roster.Apply(view);
            roster.Context(view, new TurnDraft(), context, true);
            var merry = roster.Root.Q<Button>("fellowship-portrait-base.character.merry");
            Assert.IsTrue(merry.ClassListContains(ChoiceGlow.ClassName));
            StringAssert.StartsWith("Click to separate Merry into the Fellowship's region", merry.tooltip);

            var draft = new TurnDraft();
            Assert.IsTrue(FellowshipActions.Portrait(view, draft, context, "base.character.merry"));
            Assert.AreEqual(CommandKind.ChooseEffect, draft.Kind);
            CollectionAssert.AreEqual(new[] { "base.character.merry" }, draft.Payload.Ids);
            Assert.IsTrue(FellowshipActions.Portrait(view, draft, context, "base.character.strider"));
            CollectionAssert.AreEqual(new[] { "base.character.strider" }, draft.Payload.Ids, "A limit of one replaces the choice.");
            Assert.IsTrue(CardDraft.Ready(draft, option));
            Assert.IsFalse(FellowshipActions.Immediate(draft.Kind), "The Companions keep the one confirmation.");
            roster.Context(view, draft, context, true);
            Assert.IsTrue(roster.Root.Q<Button>("fellowship-portrait-base.character.strider").ClassListContains("selected"));

            view.Fellowship.MordorStep = 2;
            roster.Context(view, new TurnDraft(), context, true);
            StringAssert.StartsWith("Click to remove Merry from play", merry.tooltip);
            view.Hunt = null;
            Assert.IsFalse(FellowshipActions.RosterChoice(view, "base.character.merry"), "Only the Hunt panel's roster takes them.");
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
