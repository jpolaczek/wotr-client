using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // AUDIO-07 (AU15, AU16): the decision passing to the player sounds once — after waiting on the opponent, of any
    // kind — while their own next steps do not; the end of the game sounds once in the player's own perspective.
    public sealed class DecisionSoundsTests
    {
        private static PlayerView View(long revision, Side owner, string decision, DecisionKind kind = DecisionKind.Action, Side? winner = null) =>
            new PlayerView
            {
                MatchId = "m", Revision = revision, You = Side.FreePeoples, DecisionOwner = owner, Winner = winner,
                Decision = decision.Length == 0 ? null : new DecisionView { Id = decision, Kind = kind }
            };

        [Test]
        public void TheDecisionSoundsWhenItPassesToThePlayerNotForTheirNextSteps()
        {
            var waiting = View(1, Side.Shadow, "s1");
            var mine = View(2, Side.FreePeoples, "f1");
            Assert.AreEqual("decision/f1", SoundFacts.Between(waiting, mine).Single(r => r.Cue == SoundCue.DecisionRequired).Key);
            var next = View(3, Side.FreePeoples, "f2");
            Assert.IsFalse(SoundFacts.Between(mine, next).Any(r => r.Cue == SoundCue.DecisionRequired), "An own next step is not announced.");
            var reaction = View(4, Side.FreePeoples, "f3", DecisionKind.HuntDamage);
            Assert.IsTrue(SoundFacts.Between(View(3, Side.Shadow, "s2"), reaction).Any(r => r.Cue == SoundCue.DecisionRequired),
                "A reaction after waiting on the opponent is announced too.");
            var skipped = View(5, Side.Shadow, "s3");
            Assert.IsEmpty(SoundFacts.Between(View(4, Side.Shadow, "s2"), skipped), "A choice the server skipped never reached the player.");
        }

        [Test]
        public void TheEndSoundsOnceInThePlayersPerspective()
        {
            var playing = View(1, Side.Shadow, "s1");
            var won = View(2, Side.FreePeoples, "", winner: Side.FreePeoples);
            CollectionAssert.AreEqual(new[] { SoundCue.Victory }, SoundFacts.Between(playing, won).Select(r => r.Cue));
            var lost = View(2, Side.FreePeoples, "", winner: Side.Shadow);
            CollectionAssert.AreEqual(new[] { SoundCue.Defeat }, SoundFacts.Between(playing, lost).Select(r => r.Cue));
            Assert.IsEmpty(SoundFacts.Between(won, View(3, Side.FreePeoples, "", winner: Side.FreePeoples)), "Still finished: silent.");
        }
    }
}
