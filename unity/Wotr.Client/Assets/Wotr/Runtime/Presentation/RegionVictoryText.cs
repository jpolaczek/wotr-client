using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-265: where a region's victory points come from, for its panel: a settlement scores for the side that captures
    // it from its original owner (Fate of Erebor makes Erebor a Shadow Stronghold and the Iron Hills a Free Peoples
    // City; New Cities adds Ered Luin and South Rhûn). Empty for a region that scores nothing.
    public static class RegionVictoryText
    {
        public static string For(RegionDefinition region, Side? control)
        {
            if (region == null || region.VictoryPoints <= 0 || !region.OriginalOwner.HasValue)
                return "";
            var captor = region.OriginalOwner == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
            var points = region.VictoryPoints + " VP";
            return control == captor
                ? "Scores " + points + " for the " + BoardScreen.SideName(captor)
                : "Worth " + points + " to the " + BoardScreen.SideName(captor) + " if captured";
        }
    }
}
