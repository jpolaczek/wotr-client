using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // AUDIO-05: what a new projection shows that has a sound, read only from what the player may see. Each event has
    // its own key, so a roll is new by its battle round or Hunt or turn, never by its numbers:
    // - dice: the action dice rolled for the turn; a battle round's roll and its reroll; the Hunt's roll and reroll;
    // - cards: own cards drawn, played or discarded; the opponent's public table and discard pile (never their hand
    //   count, so a hidden combat card declaration has no sound).
    // AUDIO-06: a new battle (by its id) and a single track change; both tracks at once are TrackSequence's pair,
    // played by GameSounds. AUDIO-07: the decision passing to the player (TurnSignal.Passes) and the game ending, once,
    // in the player's own perspective.
    public static class SoundFacts
    {
        public static List<SoundRequest> Between(PlayerView before, PlayerView after)
        {
            var requests = new List<SoundRequest>();
            End(before, after, requests);
            Decision(before, after, requests);
            Battle(before, after, requests);
            Tracks(before, after, requests);
            ActionDice(before, after, requests);
            BattleDice(before, after, requests);
            HuntDice(before, after, requests);
            Cards(before, after, requests);
            return requests;
        }

        private static void End(PlayerView before, PlayerView after, List<SoundRequest> requests)
        {
            if (after.Winner.HasValue && !before.Winner.HasValue)
                requests.Add(new SoundRequest(after.Winner == after.You ? SoundCue.Victory : SoundCue.Defeat, "end"));
        }

        private static void Decision(PlayerView before, PlayerView after, List<SoundRequest> requests)
        {
            if (TurnSignal.Passes(before, after))
                requests.Add(new SoundRequest(SoundCue.DecisionRequired, "decision/" + after.Decision.Id));
        }

        // A new real battle, once for both players; its rounds, a new mode and reopening the window add nothing.
        private static void Battle(PlayerView before, PlayerView after, List<SoundRequest> requests)
        {
            if (after.Battle != null && after.Battle.Id != before.Battle?.Id)
                requests.Add(new SoundRequest(SoundCue.BattleStarted, "battle/" + after.Battle.Id));
        }

        private static void Tracks(PlayerView before, PlayerView after, List<SoundRequest> requests)
        {
            var march = TrackSequence.Marched(before.Fellowship, after.Fellowship);
            var ring = TrackSequence.Corrupted(before.Fellowship, after.Fellowship);
            if (march && ring)
                return;
            if (march)
                requests.Add(new SoundRequest(SoundCue.FellowshipAdvanced, "track/fellowship/" + after.Revision));
            if (ring)
                requests.Add(new SoundRequest(SoundCue.CorruptionChanged, "track/corruption/" + after.Revision));
        }

        private static void ActionDice(PlayerView before, PlayerView after, List<SoundRequest> requests)
        {
            if (Rolled(after) > 0 && Rolled(before) == 0)
                requests.Add(new SoundRequest(SoundCue.DiceRoll, "dice/action/" + after.Turn));
        }

        // Action dice with a face, in the pool or being resolved: a new turn's roll turns them from none to all.
        private static int Rolled(PlayerView view) =>
            view.Dice.Count(d => d.Face.HasValue && d.Location is DieLocation.Available or DieLocation.Resolving);

        private static void BattleDice(PlayerView before, PlayerView after, List<SoundRequest> requests)
        {
            var battle = after.Battle;
            if (battle == null)
                return;
            var same = before.Battle?.Id == battle.Id && before.Battle.Round == battle.Round;
            var key = "dice/battle/" + battle.Id + "/" + battle.Round;
            if (BattleRolled(battle) > 0 && (!same || BattleRolled(before.Battle) == 0))
                requests.Add(new SoundRequest(SoundCue.DiceRoll, key));
            if (BattleRerolled(battle) > (same ? BattleRerolled(before.Battle) : 0))
                requests.Add(new SoundRequest(SoundCue.DiceRoll, key + "/reroll/" + BattleRerolled(battle)));
        }

        private static IEnumerable<BattleDieView> BattleDiceOf(BattleView battle) => battle.Attacker.Dice.Concat(battle.Defender.Dice);

        private static int BattleRolled(BattleView battle) => BattleDiceOf(battle).Count(d => d.Result > 0);

        private static int BattleRerolled(BattleView battle) => BattleDiceOf(battle).Count(d => d.Reroll.HasValue);

        private static void HuntDice(PlayerView before, PlayerView after, List<SoundRequest> requests)
        {
            var hunt = after.Hunt;
            if (hunt == null || hunt.Dice.Count == 0)
                return;
            var earlier = before.Hunt?.SourceId == hunt.SourceId ? before.Hunt : null;
            if (earlier == null || earlier.Dice.Count == 0)
                requests.Add(new SoundRequest(SoundCue.DiceRoll, "dice/hunt/" + after.Revision));
            else if (hunt.Dice.Count(d => d.Rerolled) > earlier.Dice.Count(d => d.Rerolled))
                requests.Add(new SoundRequest(SoundCue.DiceRoll, "dice/hunt-reroll/" + after.Revision));
        }

        private static void Cards(PlayerView before, PlayerView after, List<SoundRequest> requests)
        {
            var own = !before.Hand.OrderBy(c => c).SequenceEqual(after.Hand.OrderBy(c => c));
            var opponent = after.Players.FirstOrDefault(p => p.Side != after.You);
            var earlier = before.Players.FirstOrDefault(p => p.Side != after.You);
            var table = opponent != null && earlier != null &&
                (opponent.DiscardCount != earlier.DiscardCount || !opponent.TableCards.SequenceEqual(earlier.TableCards));
            if (own || table)
                requests.Add(new SoundRequest(SoundCue.CardMove, "cards/" + after.Revision));
        }
    }
}
