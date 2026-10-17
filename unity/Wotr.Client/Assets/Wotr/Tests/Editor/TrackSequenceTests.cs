using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // AUDIO-06 (AU10–AU13): a real march, Corruption both ways, the two stages and their order, and a new battle once.
    public sealed class TrackSequenceTests
    {
        private static FellowshipView Track(int progress, int corruption, int? mordor = null) =>
            new FellowshipView { Progress = progress, Corruption = corruption, MordorStep = mordor, RegionId = "rivendell" };

        [Test]
        public void OnlyARealStepIsAMarchAndCorruptionCountsBothWays()
        {
            Assert.IsTrue(TrackSequence.Marched(Track(1, 0), Track(2, 0)));
            Assert.IsFalse(TrackSequence.Marched(Track(3, 0), Track(0, 0)), "The reset of a declaration is no march.");
            Assert.IsFalse(TrackSequence.Marched(Track(3, 0), Track(0, 0, 0)), "Entering the Mordor track is no march.");
            Assert.IsTrue(TrackSequence.Marched(Track(0, 0, 1), Track(0, 0, 2)));
            Assert.IsFalse(TrackSequence.Marched(Track(0, 0, 2), Track(0, 0, 2)), "A stopped move is no march.");
            Assert.IsTrue(TrackSequence.Corrupted(Track(1, 3), Track(1, 5)));
            Assert.IsTrue(TrackSequence.Corrupted(Track(1, 5), Track(1, 4)), "Healing moves the Ring too.");
            Assert.IsFalse(TrackSequence.Both(Track(1, 3), Track(1, 5)));
            Assert.IsTrue(TrackSequence.Both(Track(1, 3), Track(2, 5)));
        }

        [Test]
        public void TheJourneyShowsTheFellowshipFirstAndMordorTheCorruptionFirst()
        {
            var journey = TrackSequence.FirstStage(Track(1, 3), Track(2, 5));
            Assert.AreEqual(2, journey.Progress);
            Assert.AreEqual(3, journey.Corruption, "The Ring waits for the second stage.");
            CollectionAssert.AreEqual(new[] { SoundCue.FellowshipAdvanced, SoundCue.CorruptionChanged },
                TrackSequence.Sounds("k", Track(2, 5)).Stages.Select(s => s.Cue));
            var mordor = TrackSequence.FirstStage(Track(0, 6, 2), Track(0, 8, 3));
            Assert.AreEqual(8, mordor.Corruption);
            Assert.AreEqual(2, mordor.MordorStep, "The step waits for the second stage.");
            var sounds = TrackSequence.Sounds("k", Track(0, 8, 3), new SoundRequest(SoundCue.Victory, "end"));
            CollectionAssert.AreEqual(new[] { SoundCue.CorruptionChanged, SoundCue.FellowshipAdvanced }, sounds.Stages.Select(s => s.Cue));
            Assert.AreEqual(TrackSequence.SecondStageSeconds, sounds.Stages[1].At);
            Assert.AreEqual(SoundCue.Victory, sounds.Trailing.Value.Cue);
            Assert.LessOrEqual(sounds.TrailingAt, 0.9, "The pair takes under 900 ms.");
        }

        [Test]
        public void ANewBattleSoundsOnceAndASingleTrackHasItsOwnCue()
        {
            var before = new PlayerView { Revision = 1, Fellowship = Track(1, 3) };
            var battle = new PlayerView { Revision = 2, Fellowship = Track(1, 3), Battle = new BattleView { Id = "b1", Round = 1 } };
            Assert.AreEqual("battle/b1", SoundFacts.Between(before, battle).Single(r => r.Cue == SoundCue.BattleStarted).Key);
            var round = new PlayerView { Revision = 3, Fellowship = Track(1, 3), Battle = new BattleView { Id = "b1", Round = 2 } };
            Assert.IsFalse(SoundFacts.Between(battle, round).Any(r => r.Cue == SoundCue.BattleStarted), "A new round is the same battle.");
            var march = new PlayerView { Revision = 4, Fellowship = Track(2, 3) };
            Assert.AreEqual(SoundCue.FellowshipAdvanced, SoundFacts.Between(round, march).Single().Cue);
            var both = new PlayerView { Revision = 5, Fellowship = Track(3, 4) };
            Assert.IsEmpty(SoundFacts.Between(march, both), "Both tracks are the pair's, played as a sequence.");
        }
    }
}
