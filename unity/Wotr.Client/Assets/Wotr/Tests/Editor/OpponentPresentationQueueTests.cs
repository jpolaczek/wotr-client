using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-304: the playback coordinator on a manual clock — what the board presents, when the gate holds commands, and
    // which cues play, for history that is late, missing, repeated or private, for Skip, a rebase and disposal.
    public sealed class OpponentPresentationQueueTests
    {
        private static readonly BoardPresentation Data = BoardPresentation.Load();
        private double now;
        private List<long> presented;
        private List<bool> holds;
        private List<string> diagnostics;
        private int failing;
        private PresentationPlayback playback;

        [SetUp]
        public void Mount()
        {
            now = 0;
            presented = new List<long>();
            holds = new List<bool>();
            diagnostics = new List<string>();
            failing = 0;
            playback = new PresentationPlayback(Present, holds.Add, () => now)
            {
                Diagnostics = new ClientDiagnostics(diagnostics.Add)
            };
            // As MatchConnection does: the first view arrives while connecting, then the session is live.
            playback.Receive(View(0));
            playback.SetConnection("Connected");
        }

        [Test]
        public void WithoutAPresenterEveryViewIsAppliedAtOnce()
        {
            playback.Receive(View(1));
            playback.Enqueue(new[] { March(1, "a") });
            CollectionAssert.AreEqual(new long[] { 0, 1 }, presented);
            CollectionAssert.IsEmpty(holds, "Nothing is presented, so nothing is held.");
            Assert.AreEqual(0, playback.Queued);
        }

        [Test]
        public void ANewRevisionWaitsForItsHistoryAndAppliesTheLatestViewAfterItsCues()
        {
            var presenter = Timed(PresentationCueKind.March, .5);
            playback.Receive(View(1));
            Tick(.1);
            CollectionAssert.AreEqual(new long[] { 0 }, presented, "The board keeps the earlier view while the history is read.");
            CollectionAssert.AreEqual(new[] { true }, holds, "Commands are held from the moment the view is queued.");
            playback.Enqueue(new[] { March(1, "first"), March(1, "second") });
            playback.HistoryRead(1);
            Tick(0);
            Assert.AreEqual("first", Key(playback.Current));
            Tick(.5);
            Assert.AreEqual("second", Key(playback.Current));
            CollectionAssert.AreEqual(new long[] { 0 }, presented, "The source still shows while its move plays.");
            Tick(.5);
            CollectionAssert.AreEqual(new long[] { 0, 1 }, presented);
            CollectionAssert.AreEqual(new[] { true, false }, holds);
            CollectionAssert.AreEqual(new[] { "first", "second" }, presenter.Begun);
        }

        [Test]
        public void ViewsArrivingDuringABeatQueueTheirCuesAndOnlyTheNewestIsApplied()
        {
            var presenter = Timed(PresentationCueKind.March, .5);
            playback.Receive(View(1));
            playback.Enqueue(new[] { March(1, "first") });
            playback.HistoryRead(1);
            Tick(0);
            Tick(.4);
            playback.Receive(View(2));
            playback.Enqueue(new[] { March(2, "second") });
            Assert.AreEqual("first", Key(playback.Current), "The later move waits for the one being shown.");
            Tick(.1);
            Assert.AreEqual("second", Key(playback.Current));
            Tick(.5);
            CollectionAssert.AreEqual(new long[] { 0 }, presented, "Revision 2's history has not been read yet (0.6 s).");
            playback.HistoryRead(2);
            Tick(0);
            CollectionAssert.AreEqual(new long[] { 0, 2 }, presented, "A coalesced revision is never applied on its own.");
            CollectionAssert.AreEqual(new[] { true, false }, holds);
            CollectionAssert.AreEqual(new[] { "first", "second" }, presenter.Begun);
        }

        [Test]
        public void AHistoryStillMissingAfterOneSecondReleasesTheViewAndItsLateCuesNeverPlay()
        {
            var presenter = Timed(PresentationCueKind.March, .5);
            playback.Receive(View(1));
            Tick(.9);
            CollectionAssert.AreEqual(new long[] { 0 }, presented);
            Tick(.1);
            CollectionAssert.AreEqual(new long[] { 0, 1 }, presented);
            playback.Enqueue(new[] { March(1, "late") });
            playback.HistoryRead(1);
            Tick(0);
            CollectionAssert.IsEmpty(presenter.Begun, "A late page never restarts an old move.");
            Assert.AreEqual(0, playback.Queued);
            CollectionAssert.AreEqual(new[] { true, false }, holds);
        }

        [Test]
        public void APrivateUpdateWaitsForTheEndWhilePlayingAndIsAppliedAtOnceOtherwise()
        {
            Timed(PresentationCueKind.March, .5);
            playback.Receive(View(1));
            playback.Enqueue(new[] { March(1, "move") });
            playback.HistoryRead(1);
            Tick(0);
            var preference = View(1, version: 4);
            playback.Receive(preference);
            CollectionAssert.AreEqual(new long[] { 0 }, presented);
            Tick(.5);
            Assert.AreSame(preference, playback.Latest, "The release uses the newest private version.");
            CollectionAssert.AreEqual(new long[] { 0, 1 }, presented);
            playback.Receive(View(1, version: 5));
            CollectionAssert.AreEqual(new long[] { 0, 1, 1 }, presented, "A private-only update plays no public cue.");
            CollectionAssert.AreEqual(new[] { true, false }, holds);
        }

        [Test]
        public void SkipEndsTheBeatAndTheBacklogAndAppliesTheLatestViewOnTheNextTick()
        {
            var presenter = Timed(PresentationCueKind.March, 5);
            playback.Receive(View(1));
            playback.Enqueue(new[] { March(1, "first"), March(1, "second") });
            playback.HistoryRead(1);
            Tick(0);
            playback.Skip();
            Assert.AreEqual(1, presenter.Finished, "The beat ends at once.");
            CollectionAssert.AreEqual(new long[] { 0 }, presented, "The skip click cannot reach a control the release opens.");
            Assert.IsTrue(playback.Holding);
            Tick(0);
            CollectionAssert.AreEqual(new long[] { 0, 1 }, presented);
            CollectionAssert.AreEqual(new[] { "first" }, presenter.Begun, "The backlog is drained, not played.");
            playback.Receive(View(2));
            playback.Enqueue(new[] { March(2, "next") });
            playback.HistoryRead(2);
            Tick(0);
            Assert.AreEqual("next", Key(playback.Current), "A later event presents again.");
        }

        [TestCase("Reconnecting")]
        [TestCase("focus")]
        public void ADisconnectOrALostFocusShowsTheLatestStateAtOnce(string loss)
        {
            var presenter = Timed(PresentationCueKind.March, 5);
            playback.Receive(View(1));
            playback.Enqueue(new[] { March(1, "move") });
            playback.HistoryRead(1);
            Tick(0);
            if (loss == "focus")
                playback.SetFocus(false);
            else
                playback.SetConnection(loss);
            Assert.AreEqual(1, presenter.Finished);
            CollectionAssert.AreEqual(new long[] { 0, 1 }, presented);
            CollectionAssert.AreEqual(new[] { true, false }, holds);
            playback.Receive(View(2));
            playback.Enqueue(new[] { March(2, "missed") });
            CollectionAssert.AreEqual(new long[] { 0, 1, 2 }, presented, "The catch-up view is shown without a catch-up.");
            if (loss == "focus")
                playback.SetFocus(true);
            else
                playback.SetConnection("Connected");
            playback.Enqueue(new[] { March(2, "missed") });
            Tick(0);
            CollectionAssert.AreEqual(new[] { "move" }, presenter.Begun);
        }

        [Test]
        public void DisposalFinishesTheBeatAndAppliesNothingMore()
        {
            var presenter = Timed(PresentationCueKind.March, 5);
            playback.Receive(View(1));
            playback.Enqueue(new[] { March(1, "move") });
            playback.HistoryRead(1);
            Tick(0);
            playback.Dispose();
            Assert.AreEqual(1, presenter.Finished);
            playback.Receive(View(2));
            Tick(5);
            CollectionAssert.AreEqual(new long[] { 0 }, presented);
        }

        [Test]
        public void AFailedReleaseIsAppliedAgainThenTheGateOpens()
        {
            Timed(PresentationCueKind.March, .5);
            playback.Receive(View(1));
            playback.HistoryRead(1);
            failing = 5;
            Tick(0);
            Tick(0);
            Assert.IsTrue(playback.Holding, "Two failures: the same view is applied again.");
            Tick(0);
            Assert.IsFalse(playback.Holding, "After three failures the player is never trapped behind the presentation.");
            Assert.AreEqual(1, playback.PresentedRevision);
            Assert.AreEqual(3, diagnostics.Count);
            StringAssert.Contains("presentation match=m revision=1", diagnostics[0]);
        }

        [Test]
        public void APresenterDecliningACueLeavesItToTheNextAndAnUnshownCueTakesNoTime()
        {
            var attacks = Timed(PresentationCueKind.Attack, .5);
            var marches = Timed(PresentationCueKind.March, .5);
            playback.Receive(View(1));
            playback.Enqueue(new[] { Cost(1), March(1, "move") });
            playback.HistoryRead(1);
            Tick(0);
            Assert.AreEqual("move", Key(playback.Current), "No presenter shows the cost, so the move starts at once.");
            CollectionAssert.IsEmpty(attacks.Begun);
            CollectionAssert.AreEqual(new[] { "move" }, marches.Begun);
        }

        private void Present(PlayerView view)
        {
            if (failing > 0)
            {
                failing--;
                throw new InvalidOperationException("drawing failed");
            }
            presented.Add(view.Revision);
        }

        private void Tick(double seconds)
        {
            now += seconds;
            playback.Tick();
        }

        private TimedPresenter Timed(PresentationCueKind kind, double seconds)
        {
            var presenter = new TimedPresenter(kind, seconds);
            playback.Register(presenter);
            return presenter;
        }

        private static string Key(PresentationCue cue) => cue?.Movement?.Participants.Single().Id;

        private static PlayerView View(long revision, int version = 0)
        {
            var view = new PlayerView { MatchId = "m", Revision = revision, You = Side.FreePeoples };
            view.AutoDecision.Version = version;
            return view;
        }

        // A real cue of the Shadow's recorded march, made by the UI-303 model; its moving figure names it.
        private static PresentationCue March(long revision, string figure)
        {
            var step = Step(revision, figure);
            step.Movements.Add(new ActionMovement
            {
                Kind = ActionMovementKind.March, From = "gorgoroth", To = "nurn",
                Participants = { new ActionParticipant { Id = figure, Side = Side.Shadow } }
            });
            return Cues(revision, step).Single();
        }

        private static PresentationCue Cost(long revision)
        {
            var step = Step(revision, "cost");
            step.Costs.Add(new ActionCost { Kind = ActionCostKind.Die, Id = "die-1", Face = DieFace.Army });
            return Cues(revision, step).Single();
        }

        private static ActionLogStep Step(long revision, string id) => new ActionLogStep
        {
            Id = revision + "-" + id, ActionId = "action-" + revision + "-" + id, Revision = revision, Order = 1,
            Actor = Side.Shadow, Kind = "ArmiesMoved"
        };

        private static IReadOnlyList<PresentationCue> Cues(long revision, ActionLogStep step)
        {
            var model = new OpponentPresentationModel(Side.FreePeoples, Data);
            model.Baseline(new ActionLogPage[0], revision - 1);
            model.Advance(revision);
            return model.Merge(new[] { new ActionLogPage { Steps = { step } } });
        }

        private sealed class TimedPresenter : ICuePresenter
        {
            private readonly PresentationCueKind kind;
            private readonly double seconds;
            public List<string> Begun { get; } = new List<string>();
            public int Finished { get; private set; }

            public TimedPresenter(PresentationCueKind kind, double seconds)
            {
                this.kind = kind;
                this.seconds = seconds;
            }

            public IPresentationBeat Begin(PresentationCue cue, double now)
            {
                if (cue.Kind != kind)
                    return null;
                Begun.Add(Key(cue));
                return new Beat(this, now + seconds);
            }

            private sealed class Beat : IPresentationBeat
            {
                private readonly TimedPresenter owner;
                private readonly double end;

                public Beat(TimedPresenter owner, double end)
                {
                    this.owner = owner;
                    this.end = end;
                }

                public bool Advance(double now) => now >= end;

                public void Finish() => owner.Finished++;
            }
        }
    }
}
