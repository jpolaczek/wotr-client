using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // AUDIO-02 (AU02, AU17): one cue per event, priorities, limits, the background and mute filters before the
    // priorities, the two-track sequence and its cancellation, on a clock the test moves.
    public sealed class SoundPolicyTests
    {
        private sealed class Library : ISoundLibrary
        {
            public int Variants(SoundCue cue) => cue == SoundCue.UiClick ? 3 : 1;
            public float Length(SoundCue cue, int variant) => cue switch
            {
                SoundCue.BattleStarted => 0.88f,
                SoundCue.FellowshipAdvanced or SoundCue.CorruptionChanged => 0.38f,
                SoundCue.DecisionRequired => 0.7f,
                _ => 0.2f
            };
            public SoundChannel Channel(SoundCue cue) => cue is SoundCue.BattleStarted or SoundCue.DecisionRequired or
                SoundCue.ActionFailed or SoundCue.Victory or SoundCue.Defeat ? SoundChannel.Alerts : SoundChannel.Effects;
        }

        private sealed class Recorder : ISoundOutput
        {
            public readonly List<(SoundCue Cue, int Variant, float Volume)> Played = new List<(SoundCue, int, float)>();
            public int Stops;
            public void Play(SoundCue cue, int variant, float volume) => Played.Add((cue, variant, volume));
            public void Stop() => Stops++;
            public void Update(float seconds) { }
            public SoundCue[] Cues => Played.Select(p => p.Cue).ToArray();
        }

        private double now;
        private Recorder output;
        private SoundPolicy policy;

        [SetUp]
        public void Create()
        {
            now = 0;
            output = new Recorder();
            policy = new SoundPolicy(new Library(), output, () => now);
        }

        private void At(double seconds)
        {
            now = seconds;
            policy.Update(0);
        }

        [Test]
        public void OneResolutionPlaysItsMostImportantCueOnTheRightChannel()
        {
            policy.Resolve(new[] { new SoundRequest(SoundCue.UiClick), new SoundRequest(SoundCue.CardMove, "r1"),
                new SoundRequest(SoundCue.DiceRoll, "r1/dice") });
            CollectionAssert.AreEqual(new[] { SoundCue.DiceRoll }, output.Cues);
            Assert.AreEqual(0.4f, output.Played[0].Volume, "Effects at 40% by default.");
            At(1);
            policy.Resolve(new[] { new SoundRequest(SoundCue.DecisionRequired, "d1"), new SoundRequest(SoundCue.BattleStarted, "b1") });
            Assert.AreEqual(SoundCue.BattleStarted, output.Cues.Last(), "On screen the battle stands for the decision of the same event.");
            Assert.AreEqual(0.6f, output.Played.Last().Volume, "Alerts at 60% by default.");
        }

        [Test]
        public void AnEventSoundsOnceButANewIdenticalEventSoundsAgain()
        {
            policy.Request(new SoundRequest(SoundCue.DiceRoll, "game/roll/7"));
            At(1);
            policy.Request(new SoundRequest(SoundCue.DiceRoll, "game/roll/7"));
            Assert.AreEqual(1, output.Played.Count, "The same roll seen again by the receipt and the view.");
            At(2);
            policy.Request(new SoundRequest(SoundCue.DiceRoll, "game/roll/8"));
            Assert.AreEqual(2, output.Played.Count, "A new roll with the same numbers is a new event.");
            policy.BeginGame();
            At(3);
            policy.Request(new SoundRequest(SoundCue.DiceRoll, "game/roll/7"));
            Assert.AreEqual(3, output.Played.Count, "A new game starts with nothing heard.");
        }

        [Test]
        public void ClicksAndCardLooksKeepTheirShortestRepeatAndTakeTurnsOnVariants()
        {
            policy.Request(new SoundRequest(SoundCue.UiClick));
            At(0.05);
            policy.Request(new SoundRequest(SoundCue.UiClick));
            At(0.12);
            policy.Request(new SoundRequest(SoundCue.UiClick));
            At(0.3);
            policy.Request(new SoundRequest(SoundCue.UiClick));
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, output.Played.Select(p => p.Variant), "100 ms between clicks; variants in turn.");
            At(1);
            policy.Request(new SoundRequest(SoundCue.CardInspect));
            At(1.2);
            policy.Request(new SoundRequest(SoundCue.CardInspect));
            At(1.26);
            policy.Request(new SoundRequest(SoundCue.CardInspect));
            Assert.AreEqual(2, output.Cues.Count(c => c == SoundCue.CardInspect), "250 ms between card looks.");
        }

        [Test]
        public void OneClipAtATimeTheMoreImportantFadesTheLessImportantNothingQueues()
        {
            policy.Request(new SoundRequest(SoundCue.BattleStarted, "b1"));
            At(0.2);
            policy.Request(new SoundRequest(SoundCue.UiClick));
            policy.Request(new SoundRequest(SoundCue.CardMove, "c1"));
            Assert.AreEqual(1, output.Played.Count, "Less important cues are dropped while the battle sounds.");
            policy.Request(new SoundRequest(SoundCue.Victory, "end"));
            Assert.AreEqual(SoundCue.Victory, output.Cues.Last(), "The end of the game fades the battle.");
            At(5);
            Assert.AreEqual(2, output.Played.Count, "Nothing dropped comes back later.");
        }

        [Test]
        public void TheBackgroundFilterRunsBeforeThePrioritiesAndNothingIsCaughtUp()
        {
            policy.Focused = false;
            policy.Resolve(new[] { new SoundRequest(SoundCue.BattleStarted, "b1"), new SoundRequest(SoundCue.DecisionRequired, "d1") });
            CollectionAssert.AreEqual(new[] { SoundCue.DecisionRequired }, output.Cues, "In the background the battle is filtered first.");
            At(1);
            policy.Request(new SoundRequest(SoundCue.CorruptionChanged, "ring"));
            policy.Request(new SoundRequest(SoundCue.DiceRoll, "roll"));
            policy.Focused = true;
            At(2);
            policy.Request(new SoundRequest(SoundCue.CorruptionChanged, "ring"));
            Assert.AreEqual(1, output.Played.Count, "Coming back plays nothing missed.");
            policy.Levels = new SoundLevels(true, 0.4f, 0.6f, false);
            policy.Focused = false;
            policy.Request(new SoundRequest(SoundCue.Defeat, "end"));
            Assert.AreEqual(1, output.Played.Count, "Background alerts off: silence without focus.");
        }

        [Test]
        public void MutingStopsTheClipAndUnmutingPlaysNothingMissed()
        {
            policy.Request(new SoundRequest(SoundCue.BattleStarted, "b1"));
            policy.Levels = new SoundLevels(false, 0.4f, 0.6f, true);
            Assert.AreEqual(1, output.Stops, "Muting stops the clip at once.");
            policy.Request(new SoundRequest(SoundCue.DecisionRequired, "d1"));
            policy.Levels = SoundLevels.Default;
            policy.Request(new SoundRequest(SoundCue.DecisionRequired, "d1"));
            Assert.AreEqual(1, output.Played.Count);
        }

        [Test]
        public void TheTwoTracksPlayInOrderThenOneImportantCueAndANewerStateCancelsTheRest()
        {
            var journey = new SoundSequence("move/1", new[] { (SoundCue.FellowshipAdvanced, 0.0), (SoundCue.CorruptionChanged, 0.38) },
                new SoundRequest(SoundCue.DecisionRequired, "d2"), 0.76);
            policy.Sequence(journey);
            At(0.1);
            policy.Request(new SoundRequest(SoundCue.UiClick));
            policy.Request(new SoundRequest(SoundCue.ActionAccepted, "move/1/accepted"));
            At(0.38);
            At(0.5);
            At(0.76);
            CollectionAssert.AreEqual(new[] { SoundCue.FellowshipAdvanced, SoundCue.CorruptionChanged, SoundCue.DecisionRequired }, output.Cues,
                "Boots, then the Ring, then the decision; the click and the acceptance of the same step stay silent.");
            policy.Sequence(journey);
            At(2);
            Assert.AreEqual(3, output.Played.Count, "The same effect seen again does not replay.");
            var mordor = new SoundSequence("move/2", new[] { (SoundCue.CorruptionChanged, 0.0), (SoundCue.FellowshipAdvanced, 0.38) });
            policy.Sequence(mordor);
            At(2.1);
            policy.CancelSequence();
            At(3);
            Assert.AreEqual(SoundCue.CorruptionChanged, output.Cues.Last(), "Cancelled: the steps never come.");
            Assert.IsFalse(policy.Sequencing);
        }

        [Test]
        public void ASequenceInTheBackgroundKeepsOnlyItsImportantCue()
        {
            policy.Focused = false;
            policy.Sequence(new SoundSequence("move/3", new[] { (SoundCue.CorruptionChanged, 0.0), (SoundCue.FellowshipAdvanced, 0.38) },
                new SoundRequest(SoundCue.Victory, "end"), 0.76));
            At(0.4);
            At(0.8);
            CollectionAssert.AreEqual(new[] { SoundCue.Victory }, output.Cues);
        }
    }
}
