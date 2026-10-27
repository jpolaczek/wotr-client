using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // AUDIO-05 (AU07, AU08): the sounds a new projection shows, keyed by their event: a roll is new by its turn,
    // battle round or Hunt, not by its numbers; cards sound for own changes and the opponent's public table and
    // discards, never for the opponent's hand count (a hidden combat card).
    public sealed class SoundFactsTests
    {
        private static PlayerView View(long revision)
        {
            var view = new PlayerView { MatchId = "m", Revision = revision, Turn = 1, You = Side.FreePeoples };
            view.Players.Add(new PlayerSummary { Side = Side.FreePeoples, HandCount = 2 });
            view.Players.Add(new PlayerSummary { Side = Side.Shadow, HandCount = 3 });
            return view;
        }

        private static PlayerView Next(PlayerView view) =>
            JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view)).With(v => v.Revision = view.Revision + 1);

        private static List<SoundRequest> Between(PlayerView before, PlayerView after) => SoundFacts.Between(before, after);

        // AUDIO-12: a step or an activation of a nation already shown sounds the Politics bell once; the first view of the
        // nations and an unchanged track add nothing.
        [Test]
        public void APoliticalStepSoundsThePoliticsBell()
        {
            var before = View(1);
            before.Nations.Add(new NationView { Id = "dwarves", Active = false, StepsToWar = 2 });
            before.Nations.Add(new NationView { Id = "elves", Active = true, StepsToWar = 1 });
            var same = Next(before);
            Assert.IsFalse(Between(before, same).Any(r => r.Cue == SoundCue.PoliticsMoved));
            var stepped = Next(same);
            stepped.Nations.Single(n => n.Id == "dwarves").StepsToWar = 1;
            var requests = Between(same, stepped);
            Assert.AreEqual(1, requests.Count(r => r.Cue == SoundCue.PoliticsMoved));
            Assert.AreEqual("track/politics/" + stepped.Revision, requests.Single(r => r.Cue == SoundCue.PoliticsMoved).Key);
            var activated = Next(stepped);
            activated.Nations.Single(n => n.Id == "dwarves").Active = true;
            Assert.AreEqual(1, Between(stepped, activated).Count(r => r.Cue == SoundCue.PoliticsMoved));
            var first = View(4);
            var shown = Next(first);
            shown.Nations.Add(new NationView { Id = "north", StepsToWar = 2 });
            Assert.IsFalse(Between(first, shown).Any(r => r.Cue == SoundCue.PoliticsMoved));
        }

        [Test]
        public void TheActionDiceSoundOnceForTheTurnsRoll()
        {
            var before = View(1);
            before.Dice.AddRange(Enumerable.Range(0, 4).Select(i => new DieView { Id = "d" + i, Location = DieLocation.Unrolled }));
            var rolled = Next(before);
            foreach (var die in rolled.Dice)
            {
                die.Face = DieFace.Muster;
                die.Location = DieLocation.Available;
            }
            var requests = Between(before, rolled);
            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual(SoundCue.DiceRoll, requests[0].Cue);
            Assert.AreEqual("dice/action/1", requests[0].Key);
            var spent = Next(rolled);
            spent.Dice[0].Location = DieLocation.Spent;
            Assert.IsEmpty(Between(rolled, spent), "Spending a die is not a roll.");
        }

        [Test]
        public void ABattleRoundsRollAndRerollAreNewEventsEvenWithTheSameNumbers()
        {
            var before = View(1);
            before.Battle = new BattleView { Id = "b1", Round = 1 };
            var rolled = Next(before);
            rolled.Battle.Attacker.Dice.Add(new BattleDieView { Id = "a0", Result = 4 });
            rolled.Battle.Defender.Dice.Add(new BattleDieView { Id = "d0", Result = 2 });
            CollectionAssert.AreEqual(new[] { "dice/battle/b1/1" }, Between(before, rolled).Select(r => r.Key), "Both sides' roll is one sound.");
            Assert.IsEmpty(Between(rolled, Next(rolled)), "The same roll seen again is silent.");
            var rerolled = Next(rolled);
            rerolled.Battle.Attacker.Dice[0].Reroll = 4;
            CollectionAssert.AreEqual(new[] { "dice/battle/b1/1/reroll/1" }, Between(rolled, rerolled).Select(r => r.Key));
            var nextRound = Next(rerolled);
            nextRound.Battle.Round = 2;
            nextRound.Battle.Attacker.Dice[0].Reroll = null;
            CollectionAssert.AreEqual(new[] { "dice/battle/b1/2" }, Between(rerolled, nextRound).Select(r => r.Key),
                "A new round with the same numbers is a new roll.");
        }

        [Test]
        public void TheHuntRollAndRerollSoundOnce()
        {
            var before = View(1);
            before.Hunt = new HuntView { SourceId = "fellowship-move" };
            var rolled = Next(before);
            rolled.Hunt.Dice.Add(new HuntDieView { Id = "h0", First = 6, Result = 6, Success = true });
            Assert.AreEqual(SoundCue.DiceRoll, Between(before, rolled).Single().Cue);
            Assert.IsEmpty(Between(rolled, Next(rolled)));
            var rerolled = Next(rolled);
            rerolled.Hunt.Dice[0].Rerolled = true;
            Assert.AreEqual(SoundCue.DiceRoll, Between(rolled, rerolled).Single().Cue);
        }

        [Test]
        public void CardsSoundForOwnChangesAndPublicPlaysButNotTheOpponentsHand()
        {
            var before = View(1);
            before.Hand.AddRange(new[] { "base.fp.character.01", "base.fp.strategy.01" });
            var played = Next(before);
            played.Hand.RemoveAt(0);
            var requests = Between(before, played);
            Assert.AreEqual(SoundCue.CardMove, requests.Single().Cue, "Playing one's own card.");
            var drawn = Next(played);
            drawn.Hand.AddRange(new[] { "base.fp.character.02", "base.fp.strategy.02" });
            Assert.AreEqual(1, Between(played, drawn).Count, "Drawing two cards is one sound.");
            var hidden = Next(drawn);
            hidden.Players[1].HandCount -= 1;
            hidden.Players[1].CombatCardCommitted = true;
            Assert.IsEmpty(Between(drawn, hidden), "A hidden combat card declaration is silent.");
            var discarded = Next(hidden);
            discarded.Players[1].DiscardCount += 1;
            Assert.AreEqual(SoundCue.CardMove, Between(hidden, discarded).Single().Cue, "The opponent's public discard.");
            Assert.IsEmpty(Between(discarded, Next(discarded)), "Nothing changed, nothing sounds.");
        }
    }

    internal static class ViewChanges
    {
        public static PlayerView With(this PlayerView view, System.Action<PlayerView> change)
        {
            change(view);
            return view;
        }
    }
}
