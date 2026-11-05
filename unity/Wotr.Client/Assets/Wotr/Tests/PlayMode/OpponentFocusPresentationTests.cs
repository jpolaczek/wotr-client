using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-490 (owner, 2026-11-04): looking away pauses the opponent's presentation, on a mounted board with the application's
    // own focus event, so everything else that listens to it (the card, the Fellowship panel, the map's flights) answers as
    // in the game: the card stays where it was read, a flight stands still in the air, the gate stays closed, and the
    // presentation resumes on the return. Skip and the release work whichever way the focus went.
    public sealed class OpponentFocusPresentationTests
    {
        private const string Orcs = "base.sh.strategy.20";

        // The event Unity raises when the window gains or loses focus; a test window never does.
        private static void Focus(bool focused)
        {
            var raise = typeof(Application).GetMethod("InvokeFocusChanged", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(raise, "Application.InvokeFocusChanged");
            raise.Invoke(null, new object[] { focused });
        }

        [TearDown]
        public void Return() => Focus(true);

        [UnityTest]
        public IEnumerator AFlightStandsStillInTheAirAndResumesOnTheReturn([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var army = stage.Put(stage.Main, UnitKind.Regular, "dol-guldur", 3);
            yield return stage.Ready();
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, army, "eastern-brown-lands");
            stage.Play(latest, "between:dol-guldur:eastern-brown-lands", March(stage, army));
            var key = "unit:" + army[0];
            yield return stage.Until(() => stage.Board.Map.Figures.Flight(key) != null);
            yield return new WaitForSecondsRealtime(.15f);
            Focus(false);
            var held = stage.Board.Map.Figures.Flight(key).Value.Ground;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsNotNull(stage.Board.Map.Figures.Flight(key), "The flight waits in the air: it is neither ended nor landed.");
            Assert.AreEqual(held, stage.Board.Map.Figures.Flight(key).Value.Ground, "It does not move.");
            Assert.IsTrue(stage.Playback.Holding, "The gate stays closed.");
            Assert.AreEqual(latest.Revision - 1, stage.Board.View.Revision, "The board's view stays the presented one.");
            LayoutTests.Save(stage.Texture, "ui490-flight-held-" + viewer + ".png");
            Focus(true);
            yield return stage.Until(() => stage.Board.Map.Figures.Flight(key) == null ||
                stage.Board.Map.Figures.Flight(key).Value.Ground != held);
            Assert.IsTrue(stage.Playback.Holding, "It flies on where it stopped.");
            yield return stage.Released();
            stage.AssertFinal(latest);
            yield return stage.NothingFlies();
        }

        // The card a lost focus would close as a transient view stays for the player; its 3 s go on after the return.
        [UnityTest]
        public IEnumerator TheRevealedCardWaitsAndItsReadingGoesOnAfterTheReturn()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var latest = OpponentCardPresentationTests.Play(stage, "grey-havens");
            var cards = stage.Board.Cards;
            yield return stage.Until(() => cards.CardId == Orcs && OpponentCardPresentationTests.Standing(cards));
            yield return new WaitForSecondsRealtime(.5f);
            Focus(false);
            yield return new WaitForSecondsRealtime((float)CardRevealPresenter.HoldSeconds + .5f);
            Assert.AreEqual(DisplayStyle.Flex, cards.Root.style.display.value, "The card is still shown.");
            Assert.AreEqual(Orcs, cards.CardId);
            Assert.IsNull(stage.Board.Root.Q(CardRevealPresenter.StandInName), "It has not left.");
            Assert.AreEqual(0, stage.Army("dol-guldur"), "Its effects wait.");
            LayoutTests.Save(stage.Texture, "ui490-card-held.png");
            Focus(true);
            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(DisplayStyle.Flex, cards.Root.style.display.value, "The time away did not count: about 2.5 s of reading were left.");
            yield return stage.Until(() => stage.Army("dol-guldur") == 3);
            yield return stage.Released();
            stage.AssertFinal(latest);
        }

        // A transient card the player looked at without a presentation still closes, as ever.
        [UnityTest]
        public IEnumerator ACardOutsideAPresentationStillClosesOnALostFocus()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            stage.Board.Cards.Show(Orcs, keep: false, allowPlay: false);
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, stage.Board.Cards.Root.style.display.value);
            Focus(false);
            yield return null;
            Assert.AreEqual(DisplayStyle.None, stage.Board.Cards.Root.style.display.value);
        }

        [UnityTest]
        public IEnumerator TheFellowshipPanelAPresentationOpenedStaysOpen()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, Side.Shadow);
            yield return stage.Ready();
            var latest = stage.Latest();
            var step = stage.Step(1);
            step.Facts.FellowshipBefore = new HuntOutcomeView { RegionId = "rivendell", Hidden = true, Corruption = 0 };
            step.Facts.FellowshipAfter = new HuntOutcomeView { RegionId = "rivendell", Hidden = true, Corruption = 2 };
            latest.Fellowship.Corruption = 2;
            stage.Play(latest, null, step);
            yield return stage.Until(() => stage.Board.Fellowship.IsOpen);
            Focus(false);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsTrue(stage.Board.Fellowship.IsOpen, "The panel waits with the beat.");
            Focus(true);
            yield return stage.Released();
            Assert.IsFalse(stage.Board.Fellowship.IsOpen, "The release closes what the presentation opened.");
        }

        [UnityTest]
        public IEnumerator SkipWorksAfterTheReturnAndLeavesNothingBehind()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var army = stage.Put(stage.Main, UnitKind.Regular, "dol-guldur", 3);
            yield return stage.Ready();
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, army, "eastern-brown-lands");
            stage.Play(latest, "between:dol-guldur:eastern-brown-lands", March(stage, army));
            yield return stage.Until(() => stage.Board.Map.Figures.ActiveCount > 0);
            Focus(false);
            yield return new WaitForSecondsRealtime(.3f);
            Focus(true);
            yield return null;
            Assert.IsNotNull(stage.Presenters.Skip.Button.parent, "The Skip target is still there.");
            PointerGestureEvents.ClickAt(stage.Presenters.Skip.Button);
            yield return stage.Released();
            stage.AssertFinal(latest);
            yield return stage.NothingFlies();
        }

        // A presentation released while away does not leave the board's flights frozen for the views after it.
        [UnityTest]
        public IEnumerator SkipWhileAwayReleasesAndTheBoardsFlightsRunAgain()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var army = stage.Put(stage.Main, UnitKind.Regular, "dol-guldur", 3);
            yield return stage.Ready();
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, army, "eastern-brown-lands");
            stage.Play(latest, "between:dol-guldur:eastern-brown-lands", March(stage, army));
            yield return stage.Until(() => stage.Board.Map.Figures.ActiveCount > 0);
            Focus(false);
            yield return null;
            PointerGestureEvents.ClickAt(stage.Presenters.Skip.Button);
            yield return stage.Released();
            stage.AssertFinal(latest);
            yield return stage.NothingFlies();
        }

        // Over the cap: the latest state at once, as a lost focus always gave, and nothing replays on the return.
        [UnityTest]
        public IEnumerator ABacklogOverTheCapShowsTheLatestStateAndNothingReplays()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var armies = stage.Put(stage.Main, UnitKind.Regular, "dol-guldur", 3);
            yield return stage.Ready();
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, armies, "eastern-brown-lands");
            stage.Play(latest, "between:dol-guldur:eastern-brown-lands", March(stage, armies));
            yield return stage.Until(() => stage.Board.Map.Figures.ActiveCount > 0);
            Focus(false);
            var next = stage.Next(latest);
            stage.Play(next, null, Enumerable.Range(1, PresentationPlayback.MostCuesAway + 1).Select(order => Capture(stage, order)).ToArray());
            yield return stage.Until(() => !stage.Playback.Holding);
            Assert.AreEqual(next.Revision, stage.Board.View.Revision, "The latest state.");
            Assert.AreEqual(0, stage.Board.Map.Figures.ActiveCount);
            stage.AssertFinal(next);
            Focus(true);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsFalse(stage.Playback.Holding, "Nothing replays on the return.");
        }

        private static ActionLogStep March(PresentedBoardStage stage, System.Collections.Generic.IEnumerable<string> ids)
        {
            var step = stage.Step(1);
            step.Movements.Add(new ActionMovement
            {
                Kind = ActionMovementKind.March, From = "dol-guldur", To = "eastern-brown-lands",
                Participants = ids.Select(id => new ActionParticipant { Id = id, Side = stage.Opponent, UnitKind = UnitKind.Regular }).ToList()
            });
            return step;
        }

        // One more cue of the opponent's: a capture is the simplest, and takes its beat like any other.
        private static ActionLogStep Capture(PresentedBoardStage stage, int order)
        {
            var step = stage.Step(order, 2);
            var region = stage.Board.Map.Data.Regions.Keys.ElementAt(order);
            step.Facts.Control.Add(new ActionControlChange { RegionId = region, Before = null, After = stage.Opponent });
            return step;
        }
    }
}
