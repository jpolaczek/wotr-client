namespace Wotr.Client
{
    // UI-430 (owner, 2026-10-30): a table card an effect lets the player discard is red on its tile in the Effects panel
    // (the hand's discard parchment, UI-222); a click on the tile or its × discards it. Its tooltip names the card, what the
    // click does and the effect that offers it (TOOLTIP_AUDIT rule 1).
    public static class TableDiscardText
    {
        // The console's hint while the decision's choices are red table cards.
        public const string Hint = "Click a red table card in Effects to discard it";

        // "Discard Wizard's Staff · Click to discard it because of The Nazgûl Strike!: a Nazgûl is in the Fellowship's
        // region".
        public static string Offered(string card, string effect, string reason = "")
        {
            var because = "Click to discard it because of " + (effect.Length > 0 ? effect : "this effect");
            return TooltipText.Parts("Discard " + CardLibrary.Title(card), reason.Length > 0 ? because + ": " + reason : because);
        }

        // UI-247: a table card's own reaction discards it ("Discard The Balrog of Moria · Click to discard it and use its
        // reaction · …").
        public static string Reaction(string card, string detail) =>
            TooltipText.Parts("Discard " + CardLibrary.Title(card), "Click to discard it and use its reaction", detail);
    }
}
