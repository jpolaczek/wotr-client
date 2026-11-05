namespace Wotr.Client
{
    // UI-420 (owner): Keep roll and Reroll selected in one wording for the battle, the Hunt and the tray, without the
    // buttons' own labels: "Accept the Combat roll (2 hits); use none of your 2 rerolls", "Roll the selected failed die
    // again · 1 of 2 rerolls".
    public static class RerollChoiceText
    {
        public static string Keep(string roll, int hits, int allowed) =>
            "Accept the " + roll + " roll (" + TooltipText.Count(hits, "hit") + ")" + Unused(allowed);

        // Without a known allowance (0) the count of rerolls is left out.
        public static string Reroll(int selected, int allowed) =>
            "Roll " + Dice(selected) + " again" + (allowed > 0 ? " · " + selected + " of " + TooltipText.Count(allowed, "reroll") : "");

        private static string Dice(int selected) =>
            selected == 1 ? "the selected failed die" : "the " + selected + " selected failed dice";

        private static string Unused(int allowed) => allowed switch
        {
            0 => "",
            1 => "; do not use your reroll",
            _ => "; use none of your " + allowed + " rerolls"
        };
    }
}
