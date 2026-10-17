using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class BattleRollText
    {
        public static string Describe(BattleDieView die, string source, BattleView battle)
        {
            var text = source + "\nFirst roll " + die.Result + " · Modifier " + die.Modifier + " · Target " + die.HitTarget + "+" +
                " · " + (die.Hit ? "Hit" : "Miss");
            if (die.Reroll.HasValue)
                text += "\nLeader reroll " + die.Reroll + " · Modifier " + die.RerollModifier + " · " + (die.RerollHit ? "Hit" : "Miss");
            if (die.ConvertedHit) text += "\nMiss converted to a hit by a Combat card.";
            text += "\nNatural 1 misses; natural 6 hits.";
            if (die.CannotReroll) text += "\nThis die cannot be rerolled.";
            var cards = new[] { battle.Attacker, battle.Defender }.Where(f => !string.IsNullOrEmpty(f.CardId))
                .Select(f => (f == battle.Attacker ? "Attacker" : "Defender") + ": " + CardLibrary.Title(f.CardId));
            return text + "\n" + string.Join("\n", cards);
        }

        public static string Modifier(BattleDieView die)
        {
            if (die.Reroll.HasValue && die.Modifier != die.RerollModifier)
                return Signed(die.Modifier) + " › " + Signed(die.RerollModifier);
            var modifier = die.Reroll.HasValue ? die.RerollModifier : die.Modifier;
            return modifier == 0 ? "" : modifier > 0 ? "+" + modifier : modifier.ToString();
        }

        private static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();
    }
}
