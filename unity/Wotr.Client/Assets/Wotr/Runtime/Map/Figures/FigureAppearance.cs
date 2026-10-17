using Wotr.Contracts;

namespace Wotr.Client
{
    // Atlas slots are presentation identifiers, never rule or physical figure identities.
    public enum FigureAppearance
    {
        FreeRegular, FreeElite, FreeLeader, Companion,
        ShadowRegular, ShadowElite, ShadowLeader, Nazgul,
        Wizard, Sovereign, Minion, Fellowship
    }

    public static class FigureAppearances
    {
        public static FigureAppearance For(UnitView unit) => For(unit.Kind, unit.Side);

        public static FigureAppearance For(UnitKind kind, Side side)
        {
            if (kind == UnitKind.Nazgul)
                return FigureAppearance.Nazgul;
            var shadow = side == Side.Shadow;
            return kind switch
            {
                UnitKind.Elite => shadow ? FigureAppearance.ShadowElite : FigureAppearance.FreeElite,
                UnitKind.Leader => shadow ? FigureAppearance.ShadowLeader : FigureAppearance.FreeLeader,
                _ => shadow ? FigureAppearance.ShadowRegular : FigureAppearance.FreeRegular
            };
        }

        public static FigureAppearance For(CharacterView character) => ForCharacter(character.DefinitionId, character.Side);

        // ART-01: the figure of a unit's own nation and kind, e.g. "gondor-elite" or "sauron-nazgul".
        public static FigureArt Art(UnitView unit) => Unit(unit.Nation, unit.Kind, unit.Side);

        public static FigureArt Unit(string nation, UnitKind kind, Side side) => new FigureArt(UnitKey(nation, kind), For(kind, side));

        public static string UnitKey(string nation, UnitKind kind) =>
            string.IsNullOrEmpty(nation) ? "" : nation + "-" + kind.ToString().ToLowerInvariant();

        // ART-02: the figure of a named character, keyed like its portrait ("gandalf-grey").
        public static FigureArt Art(CharacterView character) => Character(character.DefinitionId, character.Side);

        public static FigureArt Character(string definitionId, Side side) =>
            new FigureArt(PortraitArtwork.Key(definitionId), ForCharacter(definitionId, side));

        // The Fellowship's marker shows the Ring-bearers.
        public static FigureArt Fellowship => new FigureArt("ring-bearers", FigureAppearance.Fellowship);

        public static FigureAppearance ForCharacter(string definitionId, Side side)
        {
            if (side == Side.Shadow)
                return FigureAppearance.Minion;
            if (definitionId.Contains("gandalf"))
                return FigureAppearance.Wizard;
            if (definitionId.StartsWith("kings.") || definitionId.Contains("aragorn"))
                return FigureAppearance.Sovereign;
            return FigureAppearance.Companion;
        }

        public static string Badge(FigureAppearance appearance) => appearance switch
        {
            FigureAppearance.FreeRegular or FigureAppearance.ShadowRegular => "●",
            FigureAppearance.FreeElite or FigureAppearance.ShadowElite => "◆",
            FigureAppearance.FreeLeader or FigureAppearance.ShadowLeader => "▲",
            FigureAppearance.Nazgul => "N",
            FigureAppearance.Fellowship => "◇",
            _ => "C"
        };
    }
}
