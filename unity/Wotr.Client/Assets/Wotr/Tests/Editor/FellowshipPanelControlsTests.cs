using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-298 (owner's decision): the Fellowship panel keeps no control that does nothing or repeats another one.
    public sealed class FellowshipPanelControlsTests
    {
        private const string Strider = "base.character.strider";
        private const string Merry = "base.character.merry";
        private const string Pippin = "base.character.pippin";
        private const string Gollum = "base.character.gollum";

        [Test]
        public void ChoosingAFellowshipActionIsOneUndoStep()
        {
            var view = Action(CommandKind.MoveFellowship);
            var context = new ActionDieContext();
            context.Sync(view, "character");
            var draft = new TurnDraft();
            Assert.IsTrue(FellowshipActions.Choose(view, draft, context, CommandKind.MoveFellowship));
            draft.Back();
            Assert.IsNull(draft.Kind, "The first Back drops the chosen move.");
        }

        // The move, hide and entry are sent with their click; after a failed send the console's Back drops them.
        [TestCase(CommandKind.MoveFellowship)]
        [TestCase(CommandKind.HideFellowship)]
        [TestCase(CommandKind.EnterMordor)]
        public void AnActionSentWithItsClickHasNoArrowInTheFellowshipPanel(CommandKind kind)
        {
            var view = Action(kind);
            var draft = new TurnDraft();
            draft.Choose(kind, "character");
            Assert.IsTrue(FellowshipActions.SentOnClick(kind));
            var panel = new VisualElement();
            new DecisionTray(panel) { ContextualFellowship = true }.Render(view, draft, false, false);
            Assert.IsNull(panel.Q<Button>("decision-back"));
            var window = new VisualElement();
            new DecisionTray(window).Render(view, draft, false, false);
            Assert.IsNotNull(window.Q<Button>("decision-back"), "Outside the Fellowship panel the tray keeps its arrow.");
        }

        [Test]
        public void TheFellowshipPanelChecksAnInterruptedResponseWithTheConsole()
        {
            var view = Action(CommandKind.MoveFellowship);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveFellowship, "character");
            var panel = new VisualElement();
            var tray = new DecisionTray(panel) { ContextualFellowship = true };
            tray.Render(view, draft, false, true);
            Assert.IsTrue(tray.RecoversInConsole);
            Assert.IsNull(panel.Query<Button>().ToList().FirstOrDefault(b => b.text == "Check result"));
            StringAssert.EndsWith(DecisionTray.ConsoleCheckHint, panel.Q<Label>("pending-command-message").text);
            var window = new VisualElement();
            var docked = new DecisionTray(window);
            docked.Render(view, draft, false, true);
            Assert.IsFalse(docked.RecoversInConsole);
            Assert.IsNotNull(window.Query<Button>().ToList().FirstOrDefault(b => b.text == "Check result"));
        }

        [Test]
        public void TheCorruptionTrackSymbolIsAPictureNotAButton()
        {
            var status = new FellowshipStatus(null);
            status.Apply(new FellowshipView { Hidden = true, Progress = 2, Corruption = 1, RegionId = "rivendell" }, BoardPresentation.Load());
            Assert.IsNull(status.Root.Q<Button>("corruption-track-move"));
            Assert.IsNotNull(status.Root.Q("corruption-track-symbol"));
            Assert.AreEqual(2, status.Root.Query<Button>(className: "fellowship-track-move").ToList().Count, "The journey and Mordor tracks keep their move.");
        }

        [Test]
        public void TheTrayShowsOnlyTheGuidesTheRosterLacks()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "guide", Kind = DecisionKind.Guide },
                Fellowship = new FellowshipView { Guide = Merry },
                OwnCompanions = { new CharacterView { Id = Strider, Level = 3 }, new CharacterView { Id = Merry, Level = 1 } },
                Options = { new ActionOption { Kind = CommandKind.SelectGuide, Enabled = true, EligibleIds = { Strider, Gollum } } }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.SelectGuide);
            var panel = new VisualElement();
            new FellowshipChoices(panel, view, draft, change => change(), BoardPresentation.Load(), contextual: true);
            Assert.IsNull(panel.Q<Button>("companion-" + Strider), "Strider is chosen on the roster.");
            Assert.IsNotNull(panel.Q<Button>("companion-" + Gollum), "Gollum has no roster portrait.");
            var window = new VisualElement();
            new FellowshipChoices(window, view, draft, change => change(), BoardPresentation.Load());
            Assert.AreEqual(2, window.Query<Button>(className: "companion-portrait").ToList().Count);
        }

        [Test]
        public void CancelWaitsUntilAForcedSeparationHasSomethingToClear()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "separation", Kind = DecisionKind.CompanionSeparation },
                Fellowship = new FellowshipView { Guide = Merry, RegionId = "hollin" },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.SeparateCompanions, Enabled = true, EligibleIds = { Merry, Pippin, Strider },
                        Travel = { new TravelOption { Id = Merry, Origin = "hollin", Range = 1, Targets = { "hollin" }, RequiredGroup = { Merry, Pippin } } }
                    }
                }
            };
            var draft = new TurnDraft();
            Assert.IsTrue(DraftGesture.Choose(draft, view, CommandKind.SeparateCompanions));
            CollectionAssert.AreEquivalent(new[] { Merry, Pippin }, draft.Payload.Ids);
            Assert.IsFalse(FellowshipDraft.CancelChanges(draft, view), "The forced companions are what Cancel restores.");
            draft.Toggle(Strider);
            Assert.IsTrue(FellowshipDraft.CancelChanges(draft, view));
        }

        private static PlayerView Action(CommandKind kind) => new PlayerView
        {
            MatchId = "fixture",
            You = Side.FreePeoples,
            DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action },
            Fellowship = new FellowshipView { Hidden = kind != CommandKind.HideFellowship, RegionId = "rivendell", Guide = Strider },
            Options = { new ActionOption { Kind = kind, Enabled = true, EligibleDice = { "character" } } },
            Dice = { new DieView { Id = "character", Owner = Side.FreePeoples, Source = "standard", Location = DieLocation.Available, Face = DieFace.Character } }
        };
    }
}
