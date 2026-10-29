namespace Wotr.Client
{
    // UI-400 (TABLETOP_FIGURES_UX, "Stature"): how tall a figure stands, as a share of an elite's height. The painted
    // atlases (units-v3, characters-v1) draw every cell on one height with the feet on its bottom edge and the stature
    // already painted in — a regular about .78 of an elite, a leader about .88, a hobbit about .62, Gimli and Gollum about
    // .70 — so a painted figure is shown at the common height; only the large minions stand taller, and the UI-76
    // fallback silhouettes take their kind's share.
    public static class FigureStature
    {
        public const float Elite = 1f;
        public const float LargeMinion = 1.1f;

        public static float Of(FigureArt art)
        {
            if (art.Key is "balrog")
                return LargeMinion;
            return FigureArtwork.Painted(art) ? Elite : Of(art.Fallback);
        }

        public static float Of(FigureAppearance appearance) => appearance switch
        {
            FigureAppearance.FreeRegular or FigureAppearance.ShadowRegular => .78f,
            FigureAppearance.FreeLeader or FigureAppearance.ShadowLeader => .88f,
            FigureAppearance.Fellowship => .62f,
            _ => Elite
        };
    }
}
