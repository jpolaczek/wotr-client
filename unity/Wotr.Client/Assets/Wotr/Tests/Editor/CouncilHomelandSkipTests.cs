using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-482 (owner, 2026-11-04): the Council's homelands with no companion to place answer themselves, once per decision,
    // after a short pause, only while the client is free; the decision itself stays on the server.
    public sealed class CouncilHomelandSkipTests
    {
        private readonly List<(Action Run, long Pause)> scheduled = new List<(Action, long)>();
        private int sends;

        [SetUp]
        public void Reset()
        {
            scheduled.Clear();
            sends = 0;
        }

        private CouncilHomelandSkip Skip() =>
            new CouncilHomelandSkip((action, pause) => scheduled.Add((action, pause)), () => sends++, () => 1500);

        [Test]
        public void AppliesOnlyToTheOwnEmptyStepWithItsMandatoryDraft()
        {
            var view = View();
            var draft = Draft(view);
            Assert.IsTrue(CouncilHomelandSkip.Applies(view, draft));
            Assert.IsFalse(CouncilHomelandSkip.Applies(view, new TurnDraft()), "No draft to send.");
            Assert.IsFalse(CouncilHomelandSkip.Applies(null, draft));
            draft.Payload.Placements.Add(new UnitPlacementChoice { UnitId = "base.character.gimli", RegionId = "ered-luin" });
            Assert.IsFalse(CouncilHomelandSkip.Applies(view, draft), "A placed companion is the player's own choice.");
        }

        [Test]
        public void ACandidateMakesTheStepARealDecision()
        {
            var view = View();
            var draft = Draft(view);
            view.Options[0].CharacterActions.Add(new CharacterActionOption { CharacterId = "base.character.gimli", Targets = { "ered-luin" } });
            Assert.IsFalse(CouncilHomelandSkip.Applies(view, draft));
            var skip = Skip();
            skip.Refresh(view, draft, true);
            Assert.IsEmpty(scheduled);
        }

        [Test]
        public void NothingIsSentForAViewThatDoesNotOwnTheStep()
        {
            var draft = Draft(View());
            // An observer's or an opponent's board sees no decision of its own.
            var shadow = View();
            shadow.You = Side.Shadow;
            Assert.IsFalse(CouncilHomelandSkip.Applies(shadow, draft));
            var waiting = View();
            waiting.DecisionOwner = Side.Shadow;
            Assert.IsFalse(CouncilHomelandSkip.Applies(waiting, draft));
            var none = View();
            none.Decision = null;
            Assert.IsFalse(CouncilHomelandSkip.Applies(none, draft));
            var other = View();
            other.Decision.Kind = DecisionKind.FellowshipPhase;
            Assert.IsFalse(CouncilHomelandSkip.Applies(other, draft));
            var finished = View();
            finished.Winner = Side.Shadow;
            Assert.IsFalse(CouncilHomelandSkip.Applies(finished, draft));
        }

        [Test]
        public void ThePauseSendsOnceAndARefusedSendIsNotRepeated()
        {
            var view = View();
            var draft = Draft(view);
            var skip = Skip();
            skip.Refresh(view, draft, true);
            skip.Refresh(view, draft, true);
            Assert.AreEqual(1, scheduled.Count, "Every render keeps the same pause.");
            Assert.AreEqual(1500, scheduled[0].Pause);
            Assert.AreEqual(0, sends, "Nothing leaves before the pause ends.");
            scheduled[0].Run();
            Assert.AreEqual(1, sends);
            skip.Refresh(view, draft, false);
            skip.Refresh(view, draft, true);
            Assert.AreEqual(1, scheduled.Count, "A refused send leaves the ✓ to the player; no retry loop.");
        }

        [Test]
        public void TheRandomPauseLooksLikeAQuickClick()
        {
            long shortest = long.MaxValue, longest = 0;
            var skip = new CouncilHomelandSkip((_, pause) =>
            {
                shortest = Math.Min(shortest, pause);
                longest = Math.Max(longest, pause);
            }, () => { });
            var draft = Draft(View());
            for (var index = 0; index < 200; index++)
            {
                var view = View();
                view.Decision.Id = "homelands-" + index;
                skip.Refresh(view, draft, true);
            }
            Assert.GreaterOrEqual(shortest, CouncilHomelandSkip.ShortestPause);
            Assert.LessOrEqual(longest, CouncilHomelandSkip.LongestPause);
            Assert.Greater(longest, shortest, "The pause is not one fixed length.");
        }

        [Test]
        public void ABusyOrOfflineClientWaitsAndThenStartsAFreshPause()
        {
            var view = View();
            var draft = Draft(view);
            var skip = Skip();
            skip.Refresh(view, draft, true);
            skip.Refresh(view, draft, false);
            scheduled[0].Run();
            Assert.AreEqual(0, sends, "The pause ended while the client was not free.");
            skip.Refresh(view, draft, true);
            Assert.AreEqual(2, scheduled.Count);
            scheduled[1].Run();
            Assert.AreEqual(1, sends);
        }

        [Test]
        public void ADecisionThatClosedDuringThePauseSendsNothing()
        {
            var view = View();
            var draft = Draft(view);
            var skip = Skip();
            skip.Refresh(view, draft, true);
            var next = View();
            next.Decision = new DecisionView { Id = "fellowship", Kind = DecisionKind.FellowshipPhase };
            next.Options.Clear();
            skip.Refresh(next, draft, true);
            scheduled[0].Run();
            Assert.AreEqual(0, sends);
        }

        [Test]
        public void AReconnectionArmsTheSameOpenDecisionOnceMore()
        {
            var view = View();
            var draft = Draft(view);
            var skip = Skip();
            skip.Refresh(view, draft, true);
            scheduled[0].Run();
            skip.Rearm();
            skip.Refresh(view, draft, true);
            Assert.AreEqual(2, scheduled.Count);
            scheduled[1].Run();
            Assert.AreEqual(2, sends);
        }

        [Test]
        public void ANewDecisionHasItsOwnAnswerAndDisposingEndsThePause()
        {
            var view = View();
            var draft = Draft(view);
            var skip = Skip();
            skip.Refresh(view, draft, true);
            scheduled[0].Run();
            var later = View();
            later.Decision.Id = "homelands-2";
            skip.Refresh(later, draft, true);
            Assert.AreEqual(2, scheduled.Count);
            skip.Dispose();
            scheduled[1].Run();
            Assert.AreEqual(1, sends);
        }

        private static TurnDraft Draft(PlayerView view)
        {
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, CommandKind.ChooseCouncilHomelands);
            return draft;
        }

        private static PlayerView View() => new PlayerView
        {
            MatchId = "homelands", You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "homelands", Kind = DecisionKind.CouncilHomelands },
            Options = { new ActionOption { Kind = CommandKind.ChooseCouncilHomelands, AllowEmptySelection = true } }
        };
    }
}
