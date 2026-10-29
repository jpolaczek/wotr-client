using Wotr.Contracts;

namespace Wotr.Client
{
    public static class BattleRollText
    {
        // UI-420 (owner): the die's lines without blank ones; a modifier is signed and shown only when it is not zero.
        public static string Describe(BattleDieView die, string source, BattleView battle) => TooltipText.Lines(
            source,
            FirstRoll(die),
            die.Reroll.HasValue ? LeaderReroll(die) : "",
            die.ConvertedHit ? "Miss converted to a hit by a Combat card." : "",
            "Natural 1 misses; natural 6 hits.",
            die.CannotReroll ? "This die cannot be rerolled." : "",
            Card("Attacker's card", battle.Attacker),
            Card("Defender's card", battle.Defender));

        public static string Modifier(BattleDieView die)
        {
            if (die.Reroll.HasValue && die.Modifier != die.RerollModifier)
                return Signed(die.Modifier) + " › " + Signed(die.RerollModifier);
            var modifier = die.Reroll.HasValue ? die.RerollModifier : die.Modifier;
            return modifier == 0 ? "" : Signed(modifier);
        }

        private static string FirstRoll(BattleDieView die) => TooltipText.Parts(
            "First roll " + die.Result, ModifierPart(die.Modifier), "Target " + die.HitTarget + "+", die.Hit ? "Hit" : "Miss");

        private static string LeaderReroll(BattleDieView die) => TooltipText.Parts(
            "Leader reroll " + die.Reroll, ModifierPart(die.RerollModifier), die.RerollHit ? "Hit" : "Miss");

        private static string ModifierPart(int modifier) => modifier == 0 ? "" : "Modifier " + Signed(modifier);

        private static string Card(string owner, BattleForceView force) =>
            string.IsNullOrEmpty(force?.CardId) ? "" : owner + TooltipText.Separator + CardLibrary.Title(force.CardId);

        private static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();
    }
}
